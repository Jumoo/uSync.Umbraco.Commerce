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
        "FA15B3E1-8100-431E-BC95-4B74134A42DD",
        "OrderStatus Serializer",
        CommerceConstants.Serialization.OrderStatus
    )]
    public class OrderStatusSerializer
        : CommerceSerializerBase<OrderStatusReadOnly>,
            ISyncSerializer<OrderStatusReadOnly>
    {
        public OrderStatusSerializer(
            ICommerceApi CommerceApi,
            CommerceSyncSettingsAccessor settingsAccessor,
            IUnitOfWorkProvider uowProvider,
            ILogger<OrderStatusSerializer> logger
        )
            : base(CommerceApi, settingsAccessor, uowProvider, logger) { }

        protected override Task<SyncAttempt<XElement>> SerializeCoreAsync(
            OrderStatusReadOnly item,
            SyncSerializerOptions options
        )
        {
            var node = InitializeBaseNode(item, ItemAlias(item));

            node.Add(new XElement(nameof(item.Name), item.Name));
            node.Add(new XElement(nameof(item.SortOrder), item.SortOrder));
            node.AddStoreId(item.StoreId);

            node.Add(new XElement(nameof(item.Color), item.Color));

            return Task.FromResult(
                SyncAttemptSucceedIf(node != null, item.Name, node, ChangeType.Export)
            );
        }

        public override bool IsValid(XElement node) =>
            base.IsValid(node) && node.GetStoreId() != Guid.Empty;

        protected override async Task<SyncAttempt<OrderStatusReadOnly>> DeserializeCoreAsync(
            XElement node,
            SyncSerializerOptions options
        )
        {
            var readonlyItem = await FindItemAsync(node);

            var alias = node.GetAlias();
            var id = node.GetKey();
            var name = node.Element(nameof(readonlyItem.Name)).ValueOrDefault(alias);
            var storeId = node.GetStoreId();

            return await _uowProvider.ExecuteAsync(async uow =>
            {
                OrderStatus item;
                if (readonlyItem == null)
                {
                    item = await OrderStatus.CreateAsync(uow, id, storeId, alias, name);
                }
                else
                {
                    item = await readonlyItem.AsWritableAsync(uow);
                    await item.SetAliasAsync(alias).SetNameAsync(name);
                }

                await item.SetColorAsync(
                        node.Element(nameof(item.Color)).ValueOrDefault(item.Color)
                    )
                    .SetSortOrderAsync(
                        node.Element(nameof(item.SortOrder)).ValueOrDefault(item.SortOrder)
                    );

                await _CommerceApi.SaveOrderStatusAsync(item);
                uow.Complete();

                return SyncAttemptSucceed(name, item.AsReadOnly(), ChangeType.Import);
            });
        }

        public override string GetItemAlias(OrderStatusReadOnly item) => item.Alias;

        public override Task DoDeleteItemAsync(OrderStatusReadOnly item) =>
            _CommerceApi.DeleteOrderStatusAsync(item.Id);

        public override Task<OrderStatusReadOnly> DoFindItemAsync(Guid key) =>
            _CommerceApi.GetOrderStatusAsync(key);

        public override Task<OrderStatusReadOnly> DoFindItemAsync(string alias, Guid storeId)
            => _CommerceApi.GetOrderStatusAsync(storeId, alias);

        public override Task DoSaveItemAsync(OrderStatusReadOnly item) =>
            _uowProvider.ExecuteAsync(async uow =>
            {
                var entity = await item.AsWritableAsync(uow);
                await _CommerceApi.SaveOrderStatusAsync(entity);
                uow.Complete();
            });
    }
}
