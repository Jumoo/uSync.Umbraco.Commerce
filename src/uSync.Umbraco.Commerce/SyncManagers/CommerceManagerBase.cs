using System;
using System.Collections.Generic;
using System.Linq;

using Umbraco.Cms.Core;
using Umbraco.Commerce.Core.Api;
using Umbraco.Commerce.Core.Models;
using Umbraco.Extensions;

using uSync.Core.Dependency;
using uSync.Core.Sync;

using static Umbraco.Commerce.Cms.Constants.Trees.Stores;

namespace uSync.Umbraco.Commerce.SyncManagers;

public abstract class CommerceManagerBase
{
    protected readonly ICommerceApi _CommerceApi;

    public CommerceManagerBase(ICommerceApi commerceApi)
    {
        _CommerceApi = commerceApi;
    }

    /// <summary>
    ///  return the local entity, based on what the user picked from the tree.
    /// </summary>
    /// <remarks>
    ///  the localitem is enough for uSync to start a sync process it tells us
    ///  the Id, Udi & Entity type of an item (and the name for nice UI)
    /// </remarks>
    public SyncLocalItem GetEntity(SyncTreeItem treeItem)
    {
        var entityType = GetEntityTypeFromTree(treeItem);
        if (string.IsNullOrEmpty(entityType)) return null;

        switch (entityType)
        {
            case CommerceConstants.UdiEntityType.Store:
                return GetStoreItem(treeItem.Id);
            default:
                return GetStoreSubItem(treeItem.Id, treeItem.QueryStrings["storeId"], entityType);
        }

    }

    private SyncLocalItem GetStoreItem(string id)
    {
        // only showing the menu for the store 
        var storeGuid = GetStoreGuid(id);
        if (storeGuid == null) return null;

        // the isCommerceStore proved this was a guid.

        var store = _CommerceApi.GetStore(storeGuid.Value);
        if (store == null) return null;

        return new SyncLocalItem
        {
            EntityType = CommerceConstants.UdiEntityType.Store,
            Id = store.Id.ToString(),
            Name = store.Name,
            Udi = Udi.Create(CommerceConstants.UdiEntityType.Store, store.Id)
        };
    }

    private StoreReadOnly GetStoreById(string id)
    {
        var storeId = GetStoreGuid(id);
        if (storeId == null) return null;
        return _CommerceApi.GetStore(storeId.Value);
    }

    /// <summary>
    ///  a sub item of the store - we return the 'root' item this type - as we are going to sync it all.
    /// </summary>
    private SyncLocalItem GetStoreSubItem(string id, string storeId, string entityType)
    {
        var store = GetStoreById(storeId);

        return new SyncLocalItem
        {
            EntityType = entityType,
            Id = id,
            Name = $"{store.Name} {entityType}",
            Udi = Udi.Create(entityType, store.Id),
        };
    }

    public IEnumerable<SyncItem> GetItems(SyncItem item)
    {
        // for the store just return ths store item,
        // the depdency checker will do the rest.
        if (item.Udi.EntityType == CommerceConstants.UdiEntityType.Store)
            return item.AsEnumerableOfOne();

        // for other items the ID might be the store ID 
        // which acts as a root Udi for that type in the store. 
        if (item.Udi is GuidUdi guidUdi)
        {
            var store = _CommerceApi.GetStore(guidUdi.Guid);
            if (store == null) return item.AsEnumerableOfOne();

            // if it was the store, get all the items of that type 

            return GetStoreItems(store.Id, item.Udi.EntityType, item.Flags);
        }
        return item.AsEnumerableOfOne();
    }

    protected abstract Dictionary<string, Func<Guid,DependencyFlags,  IEnumerable<SyncItem>>> ItemFetchers { get; }

    protected IEnumerable<SyncItem> GetStoreItems(Guid storeId, string entityType, DependencyFlags flags) 
    {
        if (ItemFetchers.TryGetValue(entityType, out Func<Guid, DependencyFlags, IEnumerable<SyncItem>> fetcher))
        {
            return fetcher(storeId, flags);
        }
        
        return Enumerable.Empty<SyncItem>();
    }

    /// <summary>
    ///  uSync Exporter - supply the info for it to open the picker. 
    /// </summary>
    public SyncEntityInfo GetSyncInfo(string entityType)
    {
        if (entityType != CommerceConstants.UdiEntityType.Store) return null;

        return new SyncEntityInfo
        {
            DoNotPickContainers = true,
            PickerView = "/App_Plugins/UmbracoCommerce/backoffice/views/dialogs/storepicker.html",
            SectionAlias = "Settings",
            TreeAlias = Alias,
        };
    }

    /// <summary>
    ///  tells usync what type of sync this is (settings, content, files)
    /// </summary>
    public SyncTreeType GetTreeType(SyncTreeItem treeItem)
    {
        var entityType = GetEntityTypeFromTree(treeItem);
        if (entityType != null) return SyncTreeType.Settings;

        return SyncTreeType.None;
    }

    protected Guid? GetStoreGuid(string id)
    {
        if (Guid.TryParse(id, out Guid storeGuid))
            return storeGuid;

        return null;
    }

    protected abstract string GetEntityTypeFromTree(SyncTreeItem item);
}
