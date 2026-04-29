using Org.BouncyCastle.Crypto;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Umbraco.Cms.Core;
using Umbraco.Commerce.Core.Api;
using Umbraco.Extensions;

using uSync.Core.Dependency;
using uSync.Core.Sync;

namespace uSync.Umbraco.Commerce.SyncManagers;

public abstract class CommerceManagerBase
{
    protected readonly ICommerceApi _CommerceApi;

    protected CommerceManagerBase(ICommerceApi commerceApi)
    {
        _CommerceApi = commerceApi;
    }

    public async Task<IEnumerable<SyncItem>> GetItemsAsync(SyncItem item)
    {
        // for the store just return ths store item,
        // the depdency checker will do the rest.
        if (item.Udi.EntityType == CommerceConstants.UdiEntityType.Store)
            return [item];

        // for other items the ID might be the store ID
        // which acts as a root Udi for that type in the store.
        if (item.Udi is GuidUdi guidUdi)
        {
            var store = await _CommerceApi.GetStoreAsync(guidUdi.Guid);
            if (store == null)
                return [item];

            // if it was the store, get all the items of that type

            return await GetStoreItemsAsync(guidUdi.Guid, item.Udi.EntityType, item.Flags);
        }
        return [item];
    }

    protected abstract Dictionary<string, Func<Guid, DependencyFlags, Task<IEnumerable<SyncItem>>>> SyncItemFetchers { get; }
    protected async Task<IEnumerable<SyncItem>> GetStoreItemsAsync(Guid storeId, string entityType, DependencyFlags flags)
    {
        if (SyncItemFetchers.TryGetValue(entityType, out Func<Guid, DependencyFlags, Task<IEnumerable<SyncItem>>> fetcherAsync))
        {
            return await fetcherAsync(storeId, flags);
        }

        return [];
    }

    protected abstract List<Func<Guid, Task<SyncEntity>>> SyncEntityFetchers { get; }
    
    public async Task<SyncEntity> GetSyncEntityAsync(string key)
    {
        if (!Guid.TryParse(key, out var guidKey))
            return null;

        foreach (var fetcher in SyncEntityFetchers)
        {
            var entity = await fetcher(guidKey);
            if (entity != null)
                return entity;
        }
        return null;
    }


    /// <summary>
    ///  uSync Exporter - supply the info for it to open the picker.
    /// </summary>
    public SyncEntityInfo GetSyncInfo(string entityType)
    {
        var x = entityType;
        return null;
    }

    protected string GetEntityTypeFromTree(SyncTreeItem item)
    { 
        if (GetStoreGuid(item.Id) != null)
            return CommerceConstants.UdiEntityType.Store;

        var storeId = item.QueryStrings?["storeId"];
        if (string.IsNullOrWhiteSpace(storeId))
            return string.Empty;

        var attempt = item.Id.TryConvertTo<int>();
        if (!attempt.Success)
            return string.Empty;

        var entityType = LookupNodeEntityType(attempt.Result);
        return entityType == null ? string.Empty : entityType;
    }

    protected abstract string LookupNodeEntityType(int id);

    /// <summary>
    ///  tells usync what type of sync this is (settings, content, files)
    /// </summary>
    public SyncTreeType GetTreeType(SyncTreeItem treeItem)
    {
        var entityType = GetEntityTypeFromTree(treeItem);
        if (entityType != null)
            return SyncTreeType.Settings;

        return SyncTreeType.None;
    }

    protected static Guid? GetStoreGuid(string id)
    {
        if (Guid.TryParse(id, out Guid storeGuid))
            return storeGuid;

        return null;
    }

}
