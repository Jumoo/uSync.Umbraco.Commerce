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
        "CommerceCurrencyHandler",
        "Currencies",
        "Commerce\\Currency",
        CommerceConstants.Priorites.Currency,
        Icon = "icon-coins-dollar-alt",
        EntityType = CommerceConstants.UdiEntityType.Currency
    )]
    public class CurrencyHandler : CommerceSyncHandlerBase<CurrencyReadOnly>, ISyncHandler,
        ISyncCommerceEventHandler<CurrencySavedNotification>,
        ISyncCommerceEventHandler<CurrencyDeletedNotification>
    {
        public CurrencyHandler(
            ILogger<SyncHandlerRoot<CurrencyReadOnly, CurrencyReadOnly>> logger,
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

        protected override Guid GetStoreId(CurrencyReadOnly item) => item.StoreId;

        protected override Task DeleteViaServiceAsync(CurrencyReadOnly item) =>
            _CommerceApi.DeleteCurrencyAsync(item.Id);

        protected override Task<IEnumerable<CurrencyReadOnly>> GetByStoreAsync(Guid storeId) =>
            _CommerceApi.GetCurrenciesAsync(storeId);

        protected override Task<CurrencyReadOnly> GetFromServiceAsync(Guid key) =>
            _CommerceApi.GetCurrencyAsync(key);

        protected override string GetItemName(CurrencyReadOnly item) => item.Name;

        public Task HandleNotificationAsync(CurrencySavedNotification notification) =>
            CommerceItemSavedAsync(notification.Currency);

        public Task HandleNotificationAsync(CurrencyDeletedNotification notification) =>
            CommerceItemDeletedAsync(notification.Currency);
    }
}
