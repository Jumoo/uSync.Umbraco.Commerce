using System;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Umbraco.Commerce.Common;
using Umbraco.Commerce.Core.Api;
using Umbraco.Commerce.Core.Models;
using Umbraco.Commerce.Extensions;
using Umbraco.Extensions;
using uSync.Core;
using uSync.Core.Models;
using uSync.Core.Serialization;
using uSync.Umbraco.Commerce.Configuration;
using uSync.Umbraco.Commerce.Extensions;

namespace uSync.Umbraco.Commerce.Serializers
{
    [SyncSerializer(
        "79ED6CC2-B1B6-42DC-9B38-7C6ACCBAF895",
        "Currency Serializer",
        CommerceConstants.Serialization.Currency
    )]
    public class CurrencySerializer
        : CommerceSerializerBase<CurrencyReadOnly>,
            ISyncSerializer<CurrencyReadOnly>
    {
        public CurrencySerializer(
            ICommerceApi CommerceApi,
            CommerceSyncSettingsAccessor settingsAccessor,
            IUnitOfWorkProvider uowProvider,
            ILogger<CurrencySerializer> logger
        )
            : base(CommerceApi, settingsAccessor, uowProvider, logger)
        {
            _CommerceApi = CommerceApi;
            _uowProvider = uowProvider;
        }

        protected override Task<SyncAttempt<XElement>> SerializeCoreAsync(
            CurrencyReadOnly item,
            SyncSerializerOptions options
        )
        {
            var node = InitializeBaseNode(item, ItemAlias(item));

            node.Add(new XElement("Name", item.Name));
            node.Add(new XElement(nameof(item.SortOrder), item.SortOrder));
            node.AddStoreId(item.StoreId);

            node.Add(new XElement(nameof(item.Code), item.Code));
            node.Add(new XElement(nameof(item.CultureName), item.CultureName));
            node.Add(
                new XElement(
                    nameof(item.AllowedCountries),
                    string.Join(",", item.AllowedCountries.Select(x => x.CountryId))
                )
            );
            node.Add(new XElement(nameof(item.FormatTemplate), item.FormatTemplate));

            return Task.FromResult(
                SyncAttemptSucceedIf(node != null, item.Name, node, ChangeType.Export)
            );
        }

        public override bool IsValid(XElement node) =>
            base.IsValid(node) && node.GetStoreId() != Guid.Empty;

        protected override async Task<SyncAttempt<CurrencyReadOnly>> DeserializeCoreAsync(
            XElement node,
            SyncSerializerOptions options
        )
        {
            var readOnlyCurrency = await FindItemAsync(node);

            var alias = node.GetAlias();
            var id = node.GetKey();
            var name = node.Element("Name").ValueOrDefault(alias);
            var storeId = node.GetStoreId();
            var code = node.Element(nameof(readOnlyCurrency.Code)).ValueOrDefault(string.Empty);
            var culture = node.Element(nameof(readOnlyCurrency.CultureName))
                .ValueOrDefault(string.Empty);

            return await _uowProvider.ExecuteAsync(async uow =>
            {
                Currency currency;
                if (readOnlyCurrency == null)
                {
                    currency = await Currency.CreateAsync(uow, id, storeId, code, name, culture);
                }
                else
                {
                    currency = await readOnlyCurrency.AsWritableAsync(uow);

                    await currency.SetCodeAsync(code).SetCultureAsync(culture).SetNameAsync(name);
                }

                var sortOrder = node.Element(nameof(currency.SortOrder))
                    .ValueOrDefault(currency.SortOrder);
                await currency.SetSortOrderAsync(sortOrder);

                var formatTemplate = node.Element(nameof(currency.FormatTemplate))
                    .ValueOrDefault(currency.FormatTemplate);
                await currency.SetCustomFormatTemplateAsync(formatTemplate);

                await DeserializeCountriesAsync(node, currency);

                await _CommerceApi.SaveCurrencyAsync(currency);

                uow.Complete();

                return SyncAttemptSucceed(name, currency.AsReadOnly(), ChangeType.Import, true);
            });
        }

        private async Task DeserializeCountriesAsync(XElement node, Currency currency)
        {
            var allowedCountries = node.Element(nameof(currency.AllowedCountries))
                .ValueOrDefault(string.Empty)
                .ToDelimitedList()
                .Select(x => Guid.Parse(x));

            var countriesToRemove = currency
                .AllowedCountries.Where(x => !allowedCountries.Contains(x.CountryId))
                .Select(x => x.CountryId);

            foreach (var countryGuid in allowedCountries)
            {
                if (await _CommerceApi.GetCountryAsync(countryGuid) != null)
                {
                    await currency.AllowInCountryAsync(countryGuid);
                }
            }

            foreach (var countryGuid in countriesToRemove)
            {
                await currency.DisallowInCountryAsync(countryGuid);
            }
        }

        // overloads to let base functions do the bulk of the work.

        public override string GetItemAlias(CurrencyReadOnly item) => item.Code;

        public override Task<CurrencyReadOnly> DoFindItemAsync(Guid key) =>
            _CommerceApi.GetCurrencyAsync(key);

        public override Task DoSaveItemAsync(CurrencyReadOnly item) =>
            _uowProvider.ExecuteAsync(async uow =>
            {
                var entity = await item.AsWritableAsync(uow);
                await _CommerceApi.SaveCurrencyAsync(entity);
                uow.Complete();
            });

        public override Task DoDeleteItemAsync(CurrencyReadOnly item) =>
            _CommerceApi.DeleteCurrencyAsync(item.Id);
    }
}
