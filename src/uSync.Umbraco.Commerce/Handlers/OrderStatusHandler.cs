using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Strings;
using Umbraco.Commerce.Core.Api;
using Umbraco.Commerce.Core.Events.Notification;
using Umbraco.Commerce.Core.Models;
using uSync.BackOffice.Configuration;
using uSync.BackOffice.Services;
using uSync.BackOffice.SyncHandlers;
using uSync.BackOffice.SyncHandlers.Interfaces;
using uSync.BackOffice.SyncHandlers.Models;
using uSync.Core;

namespace uSync.Umbraco.Commerce.Handlers
{
    [SyncHandler(
        "CommerceOrderStatusHandler",
        "Order Statuses",
        "Commerce\\OrderStatus",
        CommerceConstants.Priorites.OrderStatus,
        Icon = "icon-file-cabinet",
        EntityType = CommerceConstants.UdiEntityType.OrderStatus
    )]
    public class OrderStatusHandler : CommerceSyncHandlerBase<OrderStatusReadOnly>, ISyncHandler,
        ISyncCommerceEventHandler<OrderStatusSavedNotification>,
        ISyncCommerceEventHandler<OrderStatusDeletedNotification>
    {
        public OrderStatusHandler(
            ILogger<SyncHandlerRoot<OrderStatusReadOnly, OrderStatusReadOnly>> logger,
            AppCaches appCaches,
            IShortStringHelper shortStringHelper,
            ISyncFileService syncFileService,
            ISyncEventService mutexService,
            ISyncConfigService uSyncConfig,
            ISyncItemFactory itemFactory,
            ICommerceApi commerceApi
        )
            : base(
                logger,
                appCaches,
                shortStringHelper,
                syncFileService,
                mutexService,
                uSyncConfig,
                itemFactory,
                commerceApi
            ) { }

        protected override Guid GetStoreId(OrderStatusReadOnly item) => item.StoreId;

        protected override Task DeleteViaServiceAsync(OrderStatusReadOnly item) =>
            _CommerceApi.DeleteOrderStatusAsync(item.Id);

        protected override Task<IEnumerable<OrderStatusReadOnly>> GetByStoreAsync(Guid storeId) =>
            _CommerceApi.GetOrderStatusesAsync(storeId);

        protected override Task<OrderStatusReadOnly> GetFromServiceAsync(Guid key) =>
            _CommerceApi.GetOrderStatusAsync(key);

        protected override string GetItemName(OrderStatusReadOnly item) => item.Name;

        public Task HandleNotificationAsync(OrderStatusSavedNotification notification) =>
            CommerceItemSavedAsync(notification.OrderStatus);

        public Task HandleNotificationAsync(OrderStatusDeletedNotification notification) =>
            CommerceItemDeletedAsync(notification.OrderStatus);
    }
}
