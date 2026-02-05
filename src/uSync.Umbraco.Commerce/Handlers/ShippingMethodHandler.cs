using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Strings;
using Umbraco.Commerce.Common.Events;
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
        "CommerceShippingMethodHandler",
        "Shipping",
        "Commerce\\ShippingMethod",
        CommerceConstants.Priorites.ShippingMethod,
        Icon = "icon-truck",
        EntityType = CommerceConstants.UdiEntityType.ShippingMethod
    )]
    public class ShippingMethodHandler
        : CommerceSyncHandlerBase<ShippingMethodReadOnly>,
            ISyncHandler,
            ISyncCommerceEventHandler<ShippingMethodSavedNotification>,
            ISyncCommerceEventHandler<ShippingMethodDeletedNotification>
    {
        public ShippingMethodHandler(
            ILogger<SyncHandlerRoot<ShippingMethodReadOnly, ShippingMethodReadOnly>> logger,
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

        protected override Guid GetStoreId(ShippingMethodReadOnly item) => item.StoreId;

        protected override Task<IEnumerable<ShippingMethodReadOnly>> GetByStoreAsync(
            Guid storeId
        ) => _CommerceApi.GetShippingMethodsAsync(storeId);

        protected override Task DeleteViaServiceAsync(ShippingMethodReadOnly item) =>
            _CommerceApi.DeleteShippingMethodAsync(item.Id);

        protected override Task<ShippingMethodReadOnly> GetFromServiceAsync(Guid key) =>
            _CommerceApi.GetShippingMethodAsync(key);

        protected override string GetItemName(ShippingMethodReadOnly item) => item.Name;

        public Task HandleNotificationAsync(ShippingMethodSavedNotification notification) =>
            CommerceItemSavedAsync(notification.ShippingMethod);

        public Task HandleNotificationAsync(ShippingMethodDeletedNotification notification) =>
            CommerceItemDeletedAsync(notification.ShippingMethod);
    }
}
