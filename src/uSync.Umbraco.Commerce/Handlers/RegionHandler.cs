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
        "CommerceRegionHandler",
        "Regions",
        "Commerce\\Region",
        CommerceConstants.Priorites.Region,
        Icon = "icon-flag-alt",
        IsTwoPass = true,
        EntityType = CommerceConstants.UdiEntityType.Region
    )]
    public class RegionHandler
        : CommerceSyncHandlerBase<RegionReadOnly>,
            ISyncHandler,
            ISyncPostImportHandler,
            IAsyncEventHandlerFor<RegionSavedNotification>,
            IAsyncEventHandlerFor<RegionDeletedNotification>
    {
        public RegionHandler(
            ILogger<SyncHandlerRoot<RegionReadOnly, RegionReadOnly>> logger,
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

        protected override Guid GetStoreId(RegionReadOnly item) => item.StoreId;

        protected override Task DeleteViaServiceAsync(RegionReadOnly item) =>
            _CommerceApi.DeleteRegionAsync(item.Id);

        protected override Task<IEnumerable<RegionReadOnly>> GetByStoreAsync(Guid storeId) =>
            _CommerceApi.GetRegionsAsync(storeId);

        protected override Task<RegionReadOnly> GetFromServiceAsync(Guid key) =>
            _CommerceApi.GetRegionAsync(key);

        protected override string GetItemName(RegionReadOnly item) => item.Name;

        public Task HandleAsync(RegionSavedNotification notification) =>
            CommerceItemSavedAsync(notification.Region);

        public Task HandleAsync(RegionDeletedNotification notification) =>
            CommerceItemDeletedAsync(notification.Region);
    }
}
