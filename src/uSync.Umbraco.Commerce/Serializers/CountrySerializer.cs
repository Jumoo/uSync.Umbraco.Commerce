using System;
using System.Threading.Tasks;
using System.Xml.Linq;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Umbraco.Commerce.Common;
using Umbraco.Commerce.Core.Api;
using Umbraco.Commerce.Core.Models;
using Umbraco.Commerce.Extensions;
using uSync.Core;
using uSync.Core.Models;
using uSync.Core.Serialization;
using uSync.Umbraco.Commerce.Configuration;
using uSync.Umbraco.Commerce.Extensions;

namespace uSync.Umbraco.Commerce.Serializers
{
    [SyncSerializer(
        "A5C0B948-BA5F-45FF-B6E6-EBA0BB3C6139",
        "Country Serializer",
        CommerceConstants.Serialization.Country
    )]
    public class CountrySerializer
        : CommerceSerializerBase<CountryReadOnly>,
            ISyncSerializer<CountryReadOnly>
    {
        public CountrySerializer(
            ICommerceApi CommerceApi,
            CommerceSyncSettingsAccessor settingsAccessor,
            IUnitOfWorkProvider uowProvider,
            ILogger<CountrySerializer> logger
        )
            : base(CommerceApi, settingsAccessor, uowProvider, logger) { }

        /// <summary>
        ///  Confirm that the xml contains the minimum set of things we need to perform the sync.
        /// </summary>
        public override bool IsValid(XElement node) =>
            base.IsValid(node) && node.GetStoreId() != Guid.Empty;

        protected override async Task<SyncAttempt<CountryReadOnly>> DeserializeCoreAsync(
            XElement node,
            SyncSerializerOptions options
        )
        {
            var readOnlyCountry = await FindItemAsync(node);

            var alias = node.GetAlias();
            var id = node.GetKey();
            var name = node.Element("Name").ValueOrDefault(alias);
            var storeId = node.GetStoreId();
            var code = node.Element(nameof(readOnlyCountry.Code)).ValueOrDefault(string.Empty);

            return await _uowProvider.ExecuteAsync(async uow =>
            {
                Country country;
                if (readOnlyCountry == null)
                {
                    var store = await LookupStoreAsync(node);
                    if (store is null)
                        return SyncAttempt<CountryReadOnly>.Fail(alias, ChangeType.Import, $"Store with id {storeId} not found.");

                    country = await Country.CreateAsync(uow, id, store.Id, code, name);
                }
                else
                {
                    country = await readOnlyCountry.AsWritableAsync(uow);

                    await country.SetNameAsync(name).SetCodeAsync(code);
                }

                var sortOrder = node.Element(nameof(country.SortOrder))
                    .ValueOrDefault(country.SortOrder);
                await country.SetSortOrderAsync(sortOrder);

                var defaultCurrencyId = node.Element(nameof(country.DefaultCurrencyId))
                    .ValueOrDefault(country.DefaultCurrencyId);
                if (
                    defaultCurrencyId.HasValue
                    && await _CommerceApi.GetCurrencyAsync(defaultCurrencyId.Value) != null
                )
                {
                    await country.SetDefaultCurrencyAsync(defaultCurrencyId);
                }

                var defaultPaymentId = node.Element(nameof(country.DefaultPaymentMethodId))
                    .ValueOrDefault(country.DefaultPaymentMethodId);
                if (
                    defaultPaymentId.HasValue
                    && await _CommerceApi.GetPaymentMethodAsync(defaultPaymentId.Value) != null
                )
                {
                    await country.SetDefaultPaymentMethodAsync(defaultPaymentId);
                }

                var defaultShippingId = node.Element(nameof(country.DefaultShippingMethodId))
                    .ValueOrDefault(country.DefaultShippingMethodId);
                if (
                    defaultShippingId.HasValue
                    && await _CommerceApi.GetShippingMethodAsync(defaultShippingId.Value) != null
                )
                {
                    await country.SetDefaultShippingMethodAsync(defaultShippingId);
                }

                var taxCalculationMethodId = node.Element(nameof(country.TaxCalculationMethodId))
                    .ValueOrDefault(country.TaxCalculationMethodId);
                if (
                    taxCalculationMethodId.HasValue
                    && await _CommerceApi.GetTaxCalculationMethodAsync(taxCalculationMethodId.Value)
                        != null
                )
                {
                    await country.SetTaxCalculationMethodAsync(taxCalculationMethodId);
                }

                await _CommerceApi.SaveCountryAsync(country);

                uow.Complete();

                return SyncAttemptSucceed(name, country.AsReadOnly(), ChangeType.Import, true);
            });
        }

        protected override async Task<SyncAttempt<XElement>> SerializeCoreAsync(
            CountryReadOnly item,
            SyncSerializerOptions options
        )
        {
            var node = InitializeBaseNode(item, ItemAlias(item));

            node.Add(new XElement("Name", item.Name));
            node.Add(new XElement(nameof(item.Code), item.Code));
            node.Add(new XElement(nameof(item.DefaultCurrencyId), item.DefaultCurrencyId));
            node.Add(
                new XElement(nameof(item.DefaultPaymentMethodId), item.DefaultPaymentMethodId)
            );
            node.Add(
                new XElement(nameof(item.DefaultShippingMethodId), item.DefaultShippingMethodId)
            );
            node.Add(new XElement(nameof(item.SortOrder), item.SortOrder));
            node.Add(
                new XElement(nameof(item.TaxCalculationMethodId), item.TaxCalculationMethodId)
            );

            var store = await LookupStoreAsync(item.StoreId);
            node.AddStoreId(item.StoreId, store?.Alias);

            return SyncAttemptSucceedIf(node != null, item.Name, node, ChangeType.Export);
        }

        // overloads to let base functions do the bulk of the work.

        public override string GetItemAlias(CountryReadOnly item) => item.Code;

        public override Task<CountryReadOnly> DoFindItemAsync(Guid key) =>
            _CommerceApi.GetCountryAsync(key);

        public override async Task<CountryReadOnly> DoFindItemAsync(string alias, Guid storeId)
            => await _CommerceApi.GetCountryAsync(storeId, alias);

        public override Task DoSaveItemAsync(CountryReadOnly item) =>
            _uowProvider.ExecuteAsync(async uow =>
            {
                var entity = await item.AsWritableAsync(uow);
                await _CommerceApi.SaveCountryAsync(entity);
                uow.Complete();
            });

        public override Task DoDeleteItemAsync(CountryReadOnly item) =>
            _CommerceApi.DeleteCountryAsync(item.Id);
    }
}
