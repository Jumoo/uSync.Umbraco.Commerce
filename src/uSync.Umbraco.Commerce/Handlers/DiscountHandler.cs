using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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
    [SyncHandler("CommerceDiscountHandler", "Discounts", "Commerce\\Discount", CommerceConstants.Priorites.Discount,
        Icon = "icon-tag", EntityType = CommerceConstants.UdiEntityType.Discount)]
    public class DiscountHandler : CommerceSyncHandlerBase<DiscountReadOnly>, ISyncHandler,
        ISyncCommerceEventHandler<DiscountSavedNotification>,
        ISyncCommerceEventHandler<DiscountDeletedNotification>
    {
        public DiscountHandler(
            ILogger<SyncHandlerRoot<DiscountReadOnly, DiscountReadOnly>> logger,
            AppCaches appCaches,
            IShortStringHelper shortStringHelper,
            ISyncFileService syncFileService,
            ISyncEventService mutexService,
            ISyncConfigService uSyncConfig,
            ISyncItemFactory itemFactory,
            ICommerceApi commerceApi)
            : base(logger, appCaches, shortStringHelper, syncFileService, mutexService, uSyncConfig, itemFactory, commerceApi)
        {
        }

        protected override Guid GetStoreId(DiscountReadOnly item)
            => item.StoreId;

        protected override Task DeleteViaServiceAsync(DiscountReadOnly item)
            => _CommerceApi.DeleteDiscountAsync(item.Id);

        protected override Task<IEnumerable<DiscountReadOnly>> GetByStoreAsync(Guid storeId)
            => _CommerceApi.GetDiscountsAsync(storeId);

        protected override Task<DiscountReadOnly> GetFromServiceAsync(Guid key)
            => _CommerceApi.GetDiscountAsync(key);
        protected override string GetItemName(DiscountReadOnly item)
            => item.Name;

        public Task HandleNotificationAsync(DiscountSavedNotification notification)
            => CommerceItemSavedAsync(notification.Discount);

        public Task HandleNotificationAsync(DiscountDeletedNotification notification)
            => CommerceItemDeletedAsync(notification.Discount);
    }
}
