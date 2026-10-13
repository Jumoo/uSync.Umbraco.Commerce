using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        ISyncCommerceEventHandler<LocationSavedNotification>,
        ISyncCommerceEventHandler<LocationDeletedNotification>
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

        // location names can contain characters like '/' that would otherwise
        // end up as extra folders (or invalid paths) on disk.
        var name = GetItemName(item).ReplaceMany(InvalidFileNameChars, '-');

        var store = _CommerceApi.GetStoreAsync(item.StoreId).GetAwaiter().GetResult();
        if (store is null)
            return name;

        return $"{store.Alias.ToSafeFileName(shortStringHelper)}_{name}";
    }

    // Path.GetInvalidFileNameChars() only contains '/' on Linux, so add the
    // Windows set explicitly to get the same file names on every platform.
    private static readonly char[] InvalidFileNameChars =
        [.. Path.GetInvalidFileNameChars().Union(['\\', '/', ':', '*', '?', '"', '<', '>', '|'])];

    protected override string GetItemName(LocationReadOnly item) => item.Name;

    protected override Task<LocationReadOnly> GetFromServiceAsync(Guid key) =>
        _CommerceApi.GetLocationAsync(key);

    protected override Task DeleteViaServiceAsync(LocationReadOnly item) =>
        _CommerceApi.DeleteLocationAsync(item.Id);

    public Task HandleNotificationAsync(LocationSavedNotification notification) =>
        CommerceItemSavedAsync(notification.Location);

    public Task HandleNotificationAsync(LocationDeletedNotification notification) =>
        CommerceItemDeletedAsync(notification.Location);
}
