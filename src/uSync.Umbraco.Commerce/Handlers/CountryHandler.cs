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
    /// <summary>
    ///  Handler for Country entries in Commerce
    /// </summary>
    /// <remarks>
    ///  PostImportHandler means the import is ran again at the end, because it depends on payment & shipping
    ///  which have to run after country as they depend on them.
    /// </remarks>
    [SyncHandler(
        "CommerceCountryHandler",
        "Countries",
        "Commerce\\Country",
        CommerceConstants.Priorites.Country,
        Icon = "icon-globe",
        IsTwoPass = true,
        EntityType = CommerceConstants.UdiEntityType.Country
    )]
    public class CountryHandler
        : CommerceSyncHandlerBase<CountryReadOnly>,
            ISyncPostImportHandler,
            ISyncHandler
    {
        public CountryHandler(
            ILogger<SyncHandlerRoot<CountryReadOnly, CountryReadOnly>> logger,
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

        protected override Guid GetStoreId(CountryReadOnly item) => item.StoreId;

        protected override Task DeleteViaServiceAsync(CountryReadOnly item) =>
            _CommerceApi.DeleteCountryAsync(item.Id);

        protected override Task<IEnumerable<CountryReadOnly>> GetByStoreAsync(Guid storeId) =>
            _CommerceApi.GetCountriesAsync(storeId);

        protected override Task<CountryReadOnly> GetFromServiceAsync(Guid key) =>
            _CommerceApi.GetCountryAsync(key);

        protected override string GetItemName(CountryReadOnly item) => item.Name;

        public Task HandleAsync(CountrySavedNotification notification) =>
            CommerceItemSavedAsync(notification.Country);

        public Task HandleAsync(CountryDeletedNotification notification) =>
            CommerceItemDeletedAsync(notification.Country);
    }
}
