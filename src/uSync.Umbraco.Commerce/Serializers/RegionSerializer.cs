using System;
using System.Threading.Tasks;
using System.Xml.Linq;
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
        "62503EA1-6B7E-4567-92E2-9B67E2408434",
        "Region Serializer",
        CommerceConstants.Serialization.Region
    )]
    public class RegionSerializer
        : CommerceSerializerBase<RegionReadOnly>,
            ISyncSerializer<RegionReadOnly>
    {
        public RegionSerializer(
            ICommerceApi CommerceApi,
            CommerceSyncSettingsAccessor settingsAccessor,
            IUnitOfWorkProvider uowProvider,
            ILogger<RegionSerializer> logger
        )
            : base(CommerceApi, settingsAccessor, uowProvider, logger) { }

        protected override Task<SyncAttempt<XElement>> SerializeCoreAsync(
            RegionReadOnly item,
            SyncSerializerOptions options
        )
        {
            var node = InitializeBaseNode(item, ItemAlias(item));

            node.Add(new XElement(nameof(item.Name), item.Name));
            node.AddStoreId(item.StoreId);

            node.Add(new XElement(nameof(item.SortOrder), item.SortOrder));

            node.Add(new XElement(nameof(item.Code), item.Code));
            node.Add(new XElement(nameof(item.CountryId), item.CountryId));
            node.Add(
                new XElement(nameof(item.DefaultPaymentMethodId), item.DefaultPaymentMethodId)
            );
            node.Add(
                new XElement(nameof(item.DefaultShippingMethodId), item.DefaultShippingMethodId)
            );

            return Task.FromResult(
                SyncAttemptSucceedIf(node != null, item.Name, node, ChangeType.Export)
            );
        }

        public override bool IsValid(XElement node) =>
            base.IsValid(node)
            && node.GetStoreId() != Guid.Empty
            && node.Element("CountryId").ValueOrDefault(Guid.Empty) != Guid.Empty;

        protected override async Task<SyncAttempt<RegionReadOnly>> DeserializeCoreAsync(
            XElement node,
            SyncSerializerOptions options
        )
        {
            var readonlyItem = await FindItemAsync(node);

            var alias = node.GetAlias();
            var id = node.GetKey();
            var name = node.Element(nameof(readonlyItem.Name)).ValueOrDefault(alias);
            var storeId = node.GetStoreId();
            var countryId = node.Element(nameof(readonlyItem.CountryId)).ValueOrDefault(Guid.Empty);

            var code = node.Element(nameof(readonlyItem.Code)).ValueOrDefault(string.Empty);

            if (storeId == Guid.Empty || countryId == Guid.Empty)
            {
                // fail
            }

            return await _uowProvider.ExecuteAsync(async uow =>
            {
                Region item;
                if (readonlyItem == null)
                {
                    item = await Region.CreateAsync(uow, id, storeId, countryId, code, name);
                }
                else
                {
                    item = await readonlyItem.AsWritableAsync(uow);
                    await item.SetCodeAsync(code).SetNameAsync(name);
                }

                await item.SetSortOrderAsync(
                    node.Element(nameof(item.SortOrder)).ValueOrDefault(item.SortOrder)
                );

                var paymentMethodId = node.GetGuidValue(nameof(item.DefaultPaymentMethodId));
                if (
                    paymentMethodId != null
                    && await _CommerceApi.GetPaymentMethodAsync(paymentMethodId.Value) != null
                )
                {
                    await item.SetDefaultPaymentMethodAsync(paymentMethodId);
                }

                var shippingMethodId = node.GetGuidValue(nameof(item.DefaultShippingMethodId));
                if (
                    shippingMethodId != null
                    && await _CommerceApi.GetShippingMethodAsync(shippingMethodId.Value) != null
                )
                {
                    await item.SetDefaultShippingMethodAsync(shippingMethodId);
                }

                await _CommerceApi.SaveRegionAsync(item);

                uow.Complete();

                return SyncAttemptSucceed(name, item.AsReadOnly(), ChangeType.Import);
            });
        }

        public override string GetItemAlias(RegionReadOnly item) => item.Code;

        public override Task DoDeleteItemAsync(RegionReadOnly item) =>
            _CommerceApi.DeleteRegionAsync(item.Id);

        public override Task<RegionReadOnly> DoFindItemAsync(Guid key) =>
            _CommerceApi.GetRegionAsync(key);

        /// <summary>
        ///  regions are store / and country specifc so when finding them by alias, we have to 
        ///  lookup these other bits. 
        /// </summary>
        public override async Task<RegionReadOnly> DoFinalFindAttempt(XElement node)
        {
            var code = node.GetAlias();
            var storeId = node.GetStoreId();
            var countryId = node.Element(nameof(RegionReadOnly.CountryId)).ValueOrDefault(Guid.Empty);

            if (string.IsNullOrEmpty(code) || storeId == Guid.Empty || countryId == Guid.Empty)
                return null;

            return await _CommerceApi.GetRegionAsync(storeId, countryId, code);
        }

        public override Task DoSaveItemAsync(RegionReadOnly item) =>
            _uowProvider.ExecuteAsync(async uow =>
            {
                var entity = await item.AsWritableAsync(uow);
                await _CommerceApi.SaveRegionAsync(entity);
                uow.Complete();
            });
    }
}
