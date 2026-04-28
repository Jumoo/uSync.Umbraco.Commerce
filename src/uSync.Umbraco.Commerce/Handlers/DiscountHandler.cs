using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Strings;
using Umbraco.Commerce.Common.Events;
using Umbraco.Commerce.Core.Api;
using Umbraco.Commerce.Core.Events.Notification;
using Umbraco.Commerce.Core.Models;
using uSync.BackOffice.Configuration;
using uSync.BackOffice.Services;
using uSync.BackOffice.SyncHandlers;
using uSync.Core;

namespace uSync.Umbraco.Commerce.Handlers
{
    [SyncHandler("CommerceDiscountHandler", "Discounts", "Commerce\\Discount", CommerceConstants.Priorites.Discount,
        Icon = "icon-tag", EntityType = CommerceConstants.UdiEntityType.Discount)]
    public class DiscountHandler : CommerceSyncHandlerBase<DiscountReadOnly>, ISyncHandler,
        IEventHandlerFor<DiscountSavedNotification>,
        IEventHandlerFor<DiscountDeletedNotification>
    {
        public DiscountHandler(
            ICommerceApi CommerceApi,
            ILogger<CommerceSyncHandlerBase<DiscountReadOnly>> logger,
            AppCaches appCaches,
            IShortStringHelper shortStringHelper,
            SyncFileService syncFileService,
            uSyncEventService mutexService,
            uSyncConfigService uSyncConfig,
            ISyncItemFactory itemFactory)
            : base(CommerceApi, logger, appCaches, shortStringHelper, syncFileService, mutexService, uSyncConfig, itemFactory)
        { }

        protected override Guid GetStoreId(DiscountReadOnly item)
            => item.StoreId;

        protected override void DeleteViaService(DiscountReadOnly item)
            => _CommerceApi.DeleteDiscount(item.Id);

        protected override IEnumerable<DiscountReadOnly> GetByStore(Guid storeId)
            => _CommerceApi.GetDiscounts(storeId);

        protected override DiscountReadOnly GetFromService(Guid key)
            => _CommerceApi.GetDiscount(key);

        protected override string GetItemName(DiscountReadOnly item)
            => item.Name;

        public void Handle(DiscountSavedNotification notification)
            => CommerceItemSaved(notification.Discount);

        public void Handle(DiscountDeletedNotification notification)
            => CommerceItemDeleted(notification.Discount);
    }
}
