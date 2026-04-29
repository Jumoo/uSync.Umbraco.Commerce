using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;

using Umbraco.Cms.Core.Services.ImportExport;
using Umbraco.Commerce.Common;
using Umbraco.Commerce.Core.Api;
using Umbraco.Commerce.Core.Models;
using Umbraco.Commerce.Extensions;
using uSync.Core;
using uSync.Core.Models;
using uSync.Core.Serialization;
using uSync.Umbraco.Commerce.Configuration;
using uSync.Umbraco.Commerce.Extensions;
using StringExtensions = Umbraco.Commerce.Extensions.StringExtensions;

namespace uSync.Umbraco.Commerce.Serializers
{
    [SyncSerializer(
        "707A16D7-AAA8-4399-8CF4-BEC82B8F6C8E",
        "PaymentMethod Serializer",
        CommerceConstants.Serialization.PaymentMethod
    )]
    public class PaymentMethodSeralizer
        : MethodSerializerBase<PaymentMethodReadOnly>,
            ISyncSerializer<PaymentMethodReadOnly>
    {
        public PaymentMethodSeralizer(
            ICommerceApi CommerceApi,
            CommerceSyncSettingsAccessor settingsAccessor,
            IUnitOfWorkProvider uowProvider,
            ILogger<PaymentMethodSeralizer> logger
        )
            : base(CommerceApi, settingsAccessor, uowProvider, logger) { }

        protected override async Task<SyncAttempt<XElement>> SerializeCoreAsync(
            PaymentMethodReadOnly item,
            SyncSerializerOptions options
        )
        {
            var node = InitializeBaseNode(item, ItemAlias(item));

            node.Add(new XElement(nameof(item.Name), item.Name));
            node.Add(new XElement(nameof(item.SortOrder), item.SortOrder));

            var store = await LookupStoreAsync(item.StoreId);
            node.AddStoreId(item.StoreId, store?.Alias);

            node.Add(SerializeCountryRegions(item.AllowedCountryRegions));

            node.Add(new XElement(nameof(item.CanCancelPayments), item.CanCancelPayments));
            node.Add(new XElement(nameof(item.CanCapturePayments), item.CanCapturePayments));
            node.Add(
                new XElement(nameof(item.CanFetchPaymentStatuses), item.CanFetchPaymentStatuses)
            );
            node.Add(new XElement(nameof(item.CanRefundPayments), item.CanRefundPayments));

            node.Add(SerializeProviderSettings(item.PaymentProviderSettings));
            node.Add(SerializePrices(item.Prices));

            node.Add(new XElement(nameof(item.ImageId), item.ImageId));
            node.Add(new XElement(nameof(item.PaymentProviderAlias), item.PaymentProviderAlias));

            node.Add(new XElement(nameof(item.Sku), item.Sku));
            node.Add(new XElement(nameof(item.TaxClassId), item.TaxClassId));

            return SyncAttemptSucceedIf(node != null, item.Name, node, ChangeType.Export);
        }

        private XElement SerializeProviderSettings(IReadOnlyDictionary<string, string> values)
        {
            var root = new XElement("ProviderSettings");

            if (values != null && values.Any())
            {
                foreach (
                    var setting in values.Where(x =>
                        !StringExtensions.InvariantContains(
                            _settingsAccessor.Settings.PaymentMethods.IgnoreSettings,
                            x.Key
                        )
                    )
                )
                {
                    root.Add(
                        new XElement(
                            "Setting",
                            new XElement("Key", setting.Key),
                            new XElement("Value", setting.Value)
                        )
                    );
                }
            }

            return root;
        }

        public override bool IsValid(XElement node) =>
            base.IsValid(node) && node.GetStoreId() != Guid.Empty;

        protected override async Task<SyncAttempt<PaymentMethodReadOnly>> DeserializeCoreAsync(
            XElement node,
            SyncSerializerOptions options
        )
        {
            var readonlyItem = await FindItemAsync(node);

            var alias = node.GetAlias();
            var id = node.GetKey();
            var name = node.Element(nameof(readonlyItem.Name)).ValueOrDefault(alias);
            var storeId = node.GetStoreId();
            var providerAlias = node.Element(nameof(readonlyItem.PaymentProviderAlias))
                .ValueOrDefault(string.Empty);

            return await _uowProvider.ExecuteAsync(async uow =>
            {
                PaymentMethod item;
                if (readonlyItem == null)
                {
                    var store = await LookupStoreAsync(node);
                    if (store is null)
                        return SyncAttempt<PaymentMethodReadOnly>.Fail(alias, ChangeType.Import, $"Store with id {storeId} not found.");

                    item = await PaymentMethod.CreateAsync(
                        uow,
                        id,
                        store.Id,
                        alias,
                        name,
                        providerAlias
                    );
                }
                else
                {
                    item = await readonlyItem.AsWritableAsync(uow);
                    await item.SetAliasAsync(alias).SetNameAsync(name);
                }

                await item.SetSortOrderAsync(
                        node.Element(nameof(item.SortOrder)).ValueOrDefault(item.SortOrder)
                    )
                    .SetImageAsync(node.Element(nameof(item.ImageId)).ValueOrDefault(item.ImageId))
                    .SetSkuAsync(node.Element(nameof(item.Sku)).ValueOrDefault(item.Sku))
                    .SetTaxClassAsync(
                        node.Element(nameof(item.TaxClassId)).ValueOrDefault(item.TaxClassId)
                    )
                    .ToggleFeaturesAsync(new PaymentMethodToggleFeatures
                    {
                        CanCancelPayments = node.Element(nameof(item.CanCancelPayments)).ValueOrDefault(item.CanCancelPayments),
                        CanCapturePayments = node.Element(nameof(item.CanCapturePayments)).ValueOrDefault(item.CanCapturePayments),
                        CanFetchPaymentStatuses = node.Element(nameof(item.CanFetchPaymentStatuses)).ValueOrDefault(item.CanFetchPaymentStatuses),
                        CanRefundPayments = node.Element(nameof(item.CanRefundPayments)).ValueOrDefault(item.CanRefundPayments)
                    });

                // do the payment method stuff
                await DeserializeProviderSettingsAsync(node, item);

                // Country regions
                await DeserializeCountryRegionsAsync(node, item);

                // currency
                await DeserializePricesAsync(node, item);

                await _CommerceApi.SavePaymentMethodAsync(item);

                uow.Complete();

                return SyncAttemptSucceed(name, item.AsReadOnly(), ChangeType.Import);
            });
        }

        private async Task DeserializeProviderSettingsAsync(XElement node, PaymentMethod item)
        {
            var settings = new Dictionary<string, string>();

            var root = node.Element("ProviderSettings");
            if (root != null && root.HasElements)
            {
                foreach (var setting in root.Elements("Setting"))
                {
                    var key = setting.Element("Key").ValueOrDefault(string.Empty);
                    if (!string.IsNullOrWhiteSpace(key))
                    {
                        var value = setting.Element("Value").ValueOrDefault(string.Empty);
                        settings.Add(key, value);
                    }
                }
            }

            await item.SetSettingsAsync(settings, SetBehavior.Merge);
        }

        private async Task DeserializeCountryRegionsAsync(XElement node, PaymentMethod item)
        {
            var countryRegions = GetCountryRegionsList(node);

            var valuesToRemove = item
                .AllowedCountryRegions.Where(x =>
                    countryRegions == null
                    || !item.AllowedCountryRegions.Any(y =>
                        y.CountryId == x.CountryId && y.RegionId == y.RegionId
                    )
                )
                .ToList();

            if (countryRegions.Count > 0)
            {
                foreach (var acr in countryRegions)
                {
                    if (acr.RegionId != null)
                    {
                        await item.AllowInRegionAsync(acr.CountryId, acr.RegionId.Value);
                    }
                    else
                    {
                        await item.AllowInCountryAsync(acr.CountryId);
                    }
                }
            }

            foreach (var acr in valuesToRemove)
            {
                if (acr.RegionId != null)
                {
                    await item.DisallowInRegionAsync(acr.CountryId, acr.RegionId.Value);
                }
                else
                {
                    await item.DisallowInCountryAsync(acr.CountryId);
                }
            }
        }

        private async Task DeserializePricesAsync(XElement node, PaymentMethod item)
        {
            var prices = GetServicePrices(node);

            var pricesToRemove = item
                .Prices.Where(x =>
                    item.Prices == null
                    || !prices.Any(y =>
                        y.CountryId == x.CountryId
                        && y.RegionId == x.RegionId
                        && y.CurrencyId == y.CurrencyId
                    )
                )
                .ToList();

            foreach (var price in prices)
            {
                if (price.CountryId == null && price.RegionId == null)
                {
                    await item.SetDefaultPriceForCurrencyAsync(price.CurrencyId.Value, price.Value);
                }
                else
                {
                    if (price.RegionId != null)
                    {
                        await item.SetRegionPriceForCurrencyAsync(
                            price.CountryId.Value,
                            price.RegionId.Value,
                            price.CurrencyId.Value,
                            price.Value
                        );
                    }
                    else
                    {
                        await item.SetCountryPriceForCurrencyAsync(
                            price.CountryId.Value,
                            price.CurrencyId.Value,
                            price.Value
                        );
                    }
                }
            }

            foreach (var price in pricesToRemove)
            {
                if (price.CountryId == null && price.RegionId == null)
                {
                    await item.ClearDefaultPriceForCurrencyAsync(price.CurrencyId);
                }
                else if (price.CountryId != null && price.RegionId == null)
                {
                    await item.ClearCountryPriceForCurrencyAsync(
                        price.CountryId.Value,
                        price.CurrencyId
                    );
                }
                else
                {
                    await item.ClearRegionPriceForCurrencyAsync(
                        price.CountryId.Value,
                        price.RegionId.Value,
                        price.CurrencyId
                    );
                }
            }
        }

        public override string GetItemAlias(PaymentMethodReadOnly item) => item.Alias;

        public override Task DoDeleteItemAsync(PaymentMethodReadOnly item) =>
            _CommerceApi.DeletePaymentMethodAsync(item.Id);

        public override Task<PaymentMethodReadOnly> DoFindItemAsync(Guid key) =>
            _CommerceApi.GetPaymentMethodAsync(key);

        public override Task<PaymentMethodReadOnly> DoFindItemAsync(string alias, Guid storeId)
            => _CommerceApi.GetPaymentMethodAsync(storeId, alias);

        public override Task DoSaveItemAsync(PaymentMethodReadOnly item) =>
            _uowProvider.ExecuteAsync(async uow =>
            {
                var entity = await item.AsWritableAsync(uow);
                await _CommerceApi.SavePaymentMethodAsync(entity);
                uow.Complete();
            });
    }
}
