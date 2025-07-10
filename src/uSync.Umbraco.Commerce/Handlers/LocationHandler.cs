using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Strings;
using Umbraco.Commerce.Common.Events;
using Umbraco.Commerce.Core.Api;
using Umbraco.Commerce.Core.Events.Notification;
using Umbraco.Commerce.Core.Models;
using Umbraco.Extensions;
using uSync.BackOffice.Configuration;
using uSync.BackOffice.Services;
using uSync.BackOffice.SyncHandlers;
using uSync.BackOffice.SyncHandlers.Interfaces;
using uSync.BackOffice.SyncHandlers.Models;
using uSync.Core;

namespace uSync.Umbraco.Commerce.Handlers;

[SyncHandler(
    "CommerceLocationHandler",
    "Locations",
    "Commerce\\Location",
    CommerceConstants.Priorites.Location,
    Icon = "icon-map-location",
    EntityType = CommerceConstants.UdiEntityType.Location
)]
public class CommerceLocationHandler
    : CommerceSyncHandlerBase<LocationReadOnly>,
        ISyncHandler,
        IAsyncEventHandlerFor<LocationSavedNotification>,
        IAsyncEventHandlerFor<LocationDeletedNotification>
{
    public CommerceLocationHandler(
        ILogger<SyncHandlerRoot<LocationReadOnly, LocationReadOnly>> logger,
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

    protected override Guid GetStoreId(LocationReadOnly item) => item.StoreId;

    protected override Task<IEnumerable<LocationReadOnly>> GetByStoreAsync(Guid storeId) =>
        _CommerceApi.GetLocationsAsync(storeId);

    protected override string GetItemPath(LocationReadOnly item, bool useGuid, bool isFlat)
    {
        if (useGuid)
            return GetItemKey(item).ToString();

        var store = _CommerceApi.GetStoreAsync(item.StoreId).GetAwaiter().GetResult();
        if (store is null)
            return GetItemName(item);

        return $"{store.Alias.ToSafeFileName(shortStringHelper)}_{GetItemName(item)}";
    }

    protected override string GetItemName(LocationReadOnly item) => item.Name;

    protected override Task<LocationReadOnly> GetFromServiceAsync(Guid key) =>
        _CommerceApi.GetLocationAsync(key);

    protected override Task DeleteViaServiceAsync(LocationReadOnly item) =>
        _CommerceApi.DeleteLocationAsync(item.Id);

    public override async Task HandleAsync(
        SavedNotification<LocationReadOnly> notification,
        CancellationToken cancellationToken
    )
    {
        foreach (var location in notification.SavedEntities)
        {
            await CommerceItemSavedAsync(location);
        }
    }

    public override async Task HandleAsync(
        DeletedNotification<LocationReadOnly> notification,
        CancellationToken cancellationToken
    )
    {
        foreach (var location in notification.DeletedEntities)
        {
            await CommerceItemDeletedAsync(location);
        }
    }
}
