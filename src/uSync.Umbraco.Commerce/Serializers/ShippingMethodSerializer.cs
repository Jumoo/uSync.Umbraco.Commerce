using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;
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
        "1C91B874-6028-4E50-AE1A-4481E9A267BD",
        "Shipping Method Serializer",
        CommerceConstants.Serialization.ShippingMethod
    )]
    public class ShippingMethodSerializer
        : MethodSerializerBase<ShippingMethodReadOnly>,
            ISyncSerializer<ShippingMethodReadOnly>
    {
        public ShippingMethodSerializer(
            ICommerceApi CommerceApi,
            CommerceSyncSettingsAccessor settingsAccessor,
            IUnitOfWorkProvider uowProvider,
            ILogger<ShippingMethodSerializer> logger
        )
            : base(CommerceApi, settingsAccessor, uowProvider, logger) { }

        protected override async Task<SyncAttempt<XElement>> SerializeCoreAsync(
            ShippingMethodReadOnly item,
            SyncSerializerOptions options
        )
        {
            var node = InitializeBaseNode(item, ItemAlias(item));

            node.Add(new XElement(nameof(item.Name), item.Name));
            node.Add(new XElement(nameof(item.SortOrder), item.SortOrder));

            var store = await LookupStoreAsync(item.StoreId);
            node.AddStoreId(item.StoreId, store?.Alias);

            node.Add(SerializeCountryRegions(item.AllowedCountryRegions));

            node.Add(new XElement(nameof(item.ImageId), item.ImageId));
            node.Add(new XElement(nameof(item.Sku), item.Sku));
            node.Add(new XElement(nameof(item.TaxClassId), item.TaxClassId));
            node.Add(new XElement(nameof(item.CalculationMode), (int)item.CalculationMode));
            node.Add(new XElement(nameof(item.ShippingProviderAlias), item.ShippingProviderAlias));
            node.Add(SerializeShippingProviderSettings(item.ShippingProviderSettings));
            node.Add(new XElement(nameof(item.ShippingProviderSettings),
                new SortedDictionary<string, string>(item.ShippingProviderSettings.ToDictionary(x => x.Key, x => x.Value))
            ));

            return SyncAttemptSucceedIf(node != null, item.Name, node, ChangeType.Export);
            
        }

        public override bool IsValid(XElement node) =>
            base.IsValid(node) && node.GetStoreId() != Guid.Empty;

        protected override async Task<SyncAttempt<ShippingMethodReadOnly>> DeserializeCoreAsync(
            XElement node,
            SyncSerializerOptions options
        )
        {
            var readonlyItem = await FindItemAsync(node);

            var alias = node.GetAlias();
            var id = node.GetKey();
            var name = node.Element(nameof(ShippingMethodReadOnly.Name)).ValueOrDefault(alias);
            var calculationMode = node.Element(nameof(ShippingMethodReadOnly.CalculationMode))
                .ValueOrDefault(readonlyItem?.CalculationMode ?? ShippingCalculationMode.Fixed);
            var shippingProviderAlias = node.Element(nameof(ShippingMethodReadOnly.ShippingProviderAlias))
                .ValueOrDefault(string.Empty);
            var storeId = node.GetStoreId();

            return await _uowProvider.ExecuteAsync(async uow =>
            {
                ShippingMethod item;
                if (readonlyItem == null)
                {
                    var store = await LookupStoreAsync(node);
                    if (store is null)
                        return SyncAttempt<ShippingMethodReadOnly>.Fail(alias, ChangeType.Import, $"Store with id {storeId} not found.");


                    item = await ShippingMethod.CreateAsync(
                        uow,
                        id,
                        store.Id,
                        alias,
                        name,
                        shippingProviderAlias,
                        calculationMode
                    );
                }
                else
                {
                    item = await readonlyItem.AsWritableAsync(uow);
                    await item.SetNameAsync(name).SetAliasAsync(alias);
                }

                await item.SetSortOrderAsync(
                        node.Element(nameof(item.SortOrder)).ValueOrDefault(item.SortOrder)
                    )
                    .SetImageAsync(node.Element(nameof(item.ImageId)).ValueOrDefault(item.ImageId))
                    .SetSkuAsync(node.Element(nameof(item.Sku)).ValueOrDefault(item.Sku))
                    .SetTaxClassAsync(
                        node.Element(nameof(item.TaxClassId)).ValueOrDefault(item.TaxClassId)
                    );

                await DeserializeCountryRegionsAsync(node, item);
                await DeserializeShippingProviderSettings(node, item);

                var settings = node.Element(nameof(item.ShippingProviderSettings)).ValueOrDefault(null);

                await _CommerceApi.SaveShippingMethodAsync(item);

                uow.Complete();

                return SyncAttemptSucceed(name, item.AsReadOnly(), ChangeType.Import);
            });
        }

        private async Task DeserializeCountryRegionsAsync(XElement node, ShippingMethod item)
        {
            var countryRegions = GetCountryRegionsList(node);

            var valuesToRemove = item
                .AllowedCountryRegions.Where(x =>
                    countryRegions == null
                    || !countryRegions.Any(y =>
                        y.CountryId == x.CountryId && y.RegionId == x.RegionId
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

        private async Task DeserializeShippingProviderSettings(XElement node, ShippingMethod item)
        {
            var root = node.Element("ShippingProviderSettings");

            var settings = new Dictionary<string, string>();
            if (root != null && root.HasElements)
            {

                foreach (var value in root.Elements("ShippingProviderSetting"))
                {
                    var key = value.Attribute("key").ValueOrDefault(string.Empty);
                    if (string.IsNullOrWhiteSpace(key)) continue;

                    var valueStr = value.ValueOrDefault(string.Empty);
                    settings.Add(key, valueStr);
                }
            }

            await item.SetSettingsAsync(settings, SetBehavior.Replace);
        }

        private XElement SerializeShippingProviderSettings(IReadOnlyDictionary<string, string> values)
        {
            var root = new XElement("ShippingProviderSettings");

            if (values != null && values.Any())
            {
                foreach (var value in values)
                {
                    var element = new XElement("ShippingProviderSetting", value.Value);
                    element.SetAttributeValue("key", value.Key);
                    root.Add(element);
                }
            }

            return root;
        }

        private async Task DeserializeCalculationConfig(XElement node, ShippingMethod item)
        {
            var root = node.Element(nameof(item.CalculationConfig));

            if (root == null || !root.HasElements)
            {
                return;
            }

            var calculationMode = node.Element(nameof(item.CalculationMode)).ValueOrDefault((int)item.CalculationMode);

            if (calculationMode == (int)ShippingCalculationMode.Fixed)
            {
                var prices = new List<ServicePrice>();
                foreach (var price in root.Elements(nameof(FixedRateShippingCalculationConfig)))
                {
                    var currencyId = price.Element("CurrencyId").ValueOrDefault(Guid.Empty);
                    var countryId = price.GetGuidValue("CountryId");
                    var regionId = price.GetGuidValue("RegionId");
                    var value = price.ValueOrDefault((decimal)0);

                    prices.Add(new ServicePrice(value, currencyId, countryId, regionId));
                }

                var config = new FixedRateShippingCalculationConfig(prices);
                await item.SetCalculationConfigAsync(config);
            }
            else if (calculationMode == (int)ShippingCalculationMode.Dynamic)
            {
                await item.SetCalculationConfigAsync(JsonSerializer.Deserialize<DynamicRateShippingCalculationConfig>(root.Value));
            }
            else if (calculationMode == (int)ShippingCalculationMode.Realtime)
            {
                await item.SetCalculationConfigAsync(JsonSerializer.Deserialize<RealtimeRateShippingCalculationConfig>(root.Value));
            }
            else
            {
                throw new ApplicationException($"Unknown calculation mode: {calculationMode}");
            }
        }

        private XElement SerializeCalculationConfig(ShippingMethodReadOnly item)
        {
            if (item.CalculationConfig is null)
            {
                return null;
            }

            var root = new XElement(nameof(item.CalculationConfig));

            if (item is { CalculationMode: ShippingCalculationMode.Fixed, CalculationConfig: FixedRateShippingCalculationConfig calcConfig })
            {
                foreach (var price in calcConfig.Prices.OrderBy(x => x.CurrencyId).ThenBy(x => x.CountryId).ThenBy(x => x.RegionId))
                {
                    var priceElement = new XElement(nameof(FixedRateShippingCalculationConfig));
                    priceElement.Add(new XElement(nameof(price.Value), price.Value));
                    priceElement.Add(AddNullableGuid(nameof(price.CurrencyId), price.CurrencyId));
                    priceElement.Add(AddNullableGuid(nameof(price.CountryId), price.CountryId));
                    priceElement.Add(AddNullableGuid(nameof(price.RegionId), price.RegionId));
                    root.Add(priceElement);
                }
            }
            else
            {
                root.Value = JsonSerializer.Serialize(item.CalculationConfig);
            }

            return root;
        }

        public override string GetItemAlias(ShippingMethodReadOnly item) => item.Alias;

        public override Task DoDeleteItemAsync(ShippingMethodReadOnly item) =>
            _CommerceApi.DeleteShippingMethodAsync(item.Id);

        public override Task<ShippingMethodReadOnly> DoFindItemAsync(Guid key) =>
            _CommerceApi.GetShippingMethodAsync(key);

        public override Task<ShippingMethodReadOnly> DoFindItemAsync(string alias, Guid storeId)
            => _CommerceApi.GetShippingMethodAsync(storeId, alias);

        public override Task DoSaveItemAsync(ShippingMethodReadOnly item) =>
            _uowProvider.ExecuteAsync(async uow =>
            {
                var entity = await item.AsWritableAsync(uow);
                await _CommerceApi.SaveShippingMethodAsync(entity);
                uow.Complete();
            });

        private XElement AddNullableGuid(string alias, Guid? value) =>
    new XElement(alias, value.HasValue ? value : Guid.Empty);

        private Guid? NullIfEmpty(Guid value) => value == Guid.Empty ? null : value;
    }
}
