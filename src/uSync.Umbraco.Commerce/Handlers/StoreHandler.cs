using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
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

namespace uSync.Umbraco.Commerce.Handlers;

[SyncHandler(
    "CommerceStoreHandler",
    "Stores",
    "Commerce\\Stores",
    CommerceConstants.Priorites.Stores,
    Icon = "icon-store",
    IsTwoPass = true,
    EntityType = CommerceConstants.UdiEntityType.Store
)]
public class StoreHandler
    : CommerceSyncHandlerBase<StoreReadOnly>,
        ISyncHandler,
        ISyncPostImportHandler,
        ISyncCommerceEventHandler<StoreSavedNotification>,
        ISyncCommerceEventHandler<StoreDeletedNotification>
{
    private readonly IServiceProvider _serviceProvider;

    public StoreHandler(
        ILogger<SyncHandlerRoot<StoreReadOnly, StoreReadOnly>> logger,
        AppCaches appCaches,
        IShortStringHelper shortStringHelper,
        ISyncFileService syncFileService,
        ISyncEventService mutexService,
        ISyncConfigService uSyncConfig,
        ISyncItemFactory itemFactory,
        ICommerceApi commerceApi,
        IServiceProvider serviceProvider
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
        )
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    ///  Delete a store
    /// </summary>
    /// <remarks>
    ///  This is called when an 'empty' file is found with a delete instruction in it.
    ///  These files are created when a user deletes a store
    /// </remarks>
    protected override Task DeleteViaServiceAsync(StoreReadOnly item) =>
        _CommerceApi.DeleteStoreAsync(item.Id);

    /// <summary>
    ///  get the child items, for a given store.
    /// </summary>
    /// <remarks>
    ///  if we are at the root, then the store will be null, and we should
    ///  return all items at the top level.
    /// </remarks>
    /// <param name="parent"></param>
    /// <returns></returns>
    protected override Task<IEnumerable<StoreReadOnly>> GetChildItemsAsync(StoreReadOnly parent)
    {
        if (parent == null)
        {
            return _CommerceApi.GetStoresAsync();
        }

        return Task.FromResult(Enumerable.Empty<StoreReadOnly>());
    }

    /// <summary>
    ///  Get store by key
    /// </summary>
    protected override Task<StoreReadOnly> GetFromServiceAsync(Guid key) =>
        _CommerceApi.GetStoreAsync(key);

    /// <summary>
    ///  get store by store alias
    /// </summary>
    protected override Task<StoreReadOnly> GetFromServiceAsync(string alias) =>
        _CommerceApi.GetStoreAsync(alias);

    protected override string GetItemName(StoreReadOnly item) => item.Name;

    public async Task HandleNotificationAsync(StoreSavedNotification notification)
    {
        await CommerceItemSavedAsync(notification.Store);
        await ExportStoreSettingsAsync(notification.Store.Id);
    }

    /// <summary>
    ///  export the settings that belong to a store, through their own handlers.
    /// </summary>
    /// <remarks>
    ///  Commerce raises one saved notification per type when it creates a store's default settings,
    ///  so without this a new store only gets a file for the last item of each type.
    ///  The handler collection is resolved here, not injected, because it contains this handler.
    /// </remarks>
    private async Task ExportStoreSettingsAsync(Guid storeId)
    {
        var handlers = _serviceProvider.GetRequiredService<SyncHandlerCollection>()
            .Handlers
            .OfType<ICommerceStoreItemExporter>()
            .Where(x => x is not StoreHandler && x.ExportWithStore);

        foreach (var handler in handlers)
        {
            await handler.ExportStoreItemsAsync(storeId);
        }
    }

    public Task HandleNotificationAsync(StoreDeletedNotification notification) =>
        CommerceItemDeletedAsync(notification.Store);
}
