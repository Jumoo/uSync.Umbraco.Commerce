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
        "CommerceTaxClassHandler",
        "Taxes",
        "Commerce\\TaxClass",
        CommerceConstants.Priorites.TaxClass,
        Icon = "icon-library",
        EntityType = CommerceConstants.UdiEntityType.TaxClass
    )]
    public class TaxClassHandler
        : CommerceSyncHandlerBase<TaxClassReadOnly>,
            ISyncHandler,
            ISyncCommerceEventHandler<TaxClassSavedNotification>,
            ISyncCommerceEventHandler<TaxClassDeletedNotification>
    {
        public TaxClassHandler(
            ILogger<SyncHandlerRoot<TaxClassReadOnly, TaxClassReadOnly>> logger,
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

        protected override Guid GetStoreId(TaxClassReadOnly item) => item.StoreId;

        protected override Task<TaxClassReadOnly> GetFromServiceAsync(Guid key) =>
            _CommerceApi.GetTaxClassAsync(key);

        protected override Task DeleteViaServiceAsync(TaxClassReadOnly item) =>
            _CommerceApi.DeleteTaxClassAsync(item.Id);

        protected override string GetItemName(TaxClassReadOnly item) => item.Name;

        protected override Task<IEnumerable<TaxClassReadOnly>> GetByStoreAsync(Guid storeId) =>
            _CommerceApi.GetTaxClassesAsync(storeId);

        public Task HandleNotificationAsync(TaxClassSavedNotification notification) =>
            CommerceItemSavedAsync(notification.TaxClass);

        public Task HandleNotificationAsync(TaxClassDeletedNotification notification) =>
            CommerceItemDeletedAsync(notification.TaxClass);
    }
}
