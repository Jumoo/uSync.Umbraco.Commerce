using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Strings;
using Umbraco.Commerce.Common.Events;
using Umbraco.Commerce.Core.Api;
using Umbraco.Commerce.Core.Models;
using Umbraco.Extensions;
using uSync.BackOffice;
using uSync.BackOffice.Configuration;
using uSync.BackOffice.Models;
using uSync.BackOffice.Services;
using uSync.BackOffice.SyncHandlers;
using uSync.Core;

namespace uSync.Umbraco.Commerce.Handlers
{
    public abstract class CommerceSyncHandlerBase<TObject>
        : SyncHandlerRoot<TObject, TObject>,
            IAsyncEventHandler
        where TObject : EntityBase
    {
        protected ICommerceApi _CommerceApi;

        protected CommerceSyncHandlerBase(
            ILogger<SyncHandlerRoot<TObject, TObject>> logger,
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
                itemFactory
            )
        {
            _CommerceApi = commerceApi;
        }

        public override string Group => CommerceConstants.Group;

        protected virtual Guid GetStoreId(TObject item) => Guid.Empty;

        protected async Task<string> GetStoreNameAsync(TObject item)
        {
            var storeId = GetStoreId(item);
            if (storeId == Guid.Empty)
                return string.Empty;

            return (await _CommerceApi.GetStoreAsync(storeId))?.Name ?? string.Empty;
        }

        protected override string GetItemFileName(TObject item)
        {
            var storeName = GetStoreNameAsync(item).GetAwaiter().GetResult();
            if (string.IsNullOrWhiteSpace(storeName))
                return base.GetItemFileName(item);

            return $"{storeName.ToSafeFileName(shortStringHelper)}_{base.GetItemFileName(item)}";
        }

        /// <summary>
        ///  get the item by store
        /// </summary>
        protected virtual Task<IEnumerable<TObject>> GetByStoreAsync(Guid storeId) =>
            Task.FromResult(Enumerable.Empty<TObject>());

        protected override async Task<IEnumerable<TObject>> GetChildItemsAsync(TObject parent)
        {
            if (parent != null)
                return Enumerable.Empty<TObject>();

            var items = new List<TObject>();

            var stores = await _CommerceApi.GetStoresAsync();
            foreach (var store in stores)
            {
                items.AddRange(await GetByStoreAsync(store.Id));
            }
            return items;
        }

        public virtual async Task<IEnumerable<uSyncAction>> ProcessPostImportAsync(
            IEnumerable<uSyncAction> actions,
            HandlerSettings config
        )
        {
            if (actions == null || !actions.Any())
                return null;

            var postActions = new List<uSyncAction>();

            foreach (var action in actions)
            {
                var results = await ImportAsync(action.FileName, config, new uSyncImportOptions());
                foreach (var result in results)
                {
                    if (result.Success)
                    {
                        var attempt = await ImportSecondPassAsync(
                            result,
                            config,
                            new uSyncImportOptions()
                        );
                        // postActions.Add();
                    }
                }
            }

            return postActions;
        }

        /// <summary>
        ///  if there is a 'OneWay' (or CreateOnly) setting in the config, then we will only import something
        ///  if it doesn't already exist.
        /// </summary>
        /// <remarks>
        ///  On the Commerce base class means, it can be applied to any of the handler configs.
        /// </remarks>
        protected override async Task<bool> ShouldImportAsync(XElement node, HandlerSettings config)
        {
            if (config.GetSetting("OneWay", false) || config.GetSetting("CreateOnly", false))
            {
                // only import if it doesn't already exist.
                var item = await GetFromServiceAsync(node.GetKey());
                return item == null;
            }

            return await base.ShouldImportAsync(node, config);
        }

        /// <summary>
        ///  Handles the deleting of items in Umbraco but not the sync.
        /// </summary>
        /// <remarks>
        ///  this isn't always used, its only when a user explicity asks for
        ///  the folder to be cleaned - are things deleted this way.
        ///  TODO: this ideally should be implimented
        /// </remarks>
        protected override Task<IEnumerable<uSyncAction>> DeleteMissingItemsAsync(
            TObject parent,
            IEnumerable<Guid> keysToKeep,
            bool reportOnly
        ) => Task.FromResult(Enumerable.Empty<uSyncAction>());

        protected override Task<TObject> GetFromServiceAsync(TObject item) => Task.FromResult(item);

        protected override Task<IEnumerable<TObject>> GetFoldersAsync(TObject parent) =>
            Task.FromResult(Enumerable.Empty<TObject>());

        protected virtual async Task CommerceItemSavedAsync(TObject item)
        {
            if (!ShouldProcessEvent())
                return;

            var handlerFolders = GetDefaultHandlerFolders();
            await ExportAsync(item, handlerFolders, DefaultConfig);
        }

        protected virtual async Task CommerceItemDeletedAsync(TObject item)
        {
            if (!ShouldProcessEvent())
                return;

            var handlerFolders = GetDefaultHandlerFolders();
            await ExportDeletedItemAsync(item, handlerFolders, DefaultConfig);
        }

        /// <summary>
        ///  checks to see if we should process the Commerce events/notifications
        /// </summary>
        private new bool ShouldProcessEvent()
        {
            if (_mutexService.IsPaused)
                return false;
            if (!DefaultConfig.Enabled)
                return false;
            return true;
        }

        /// <summary>
        ///  make the handling of Commerce events a bit more generic, so we can clean up the handler code a bit.
        /// </summary>
        async Task IAsyncEventHandler.HandleAsync(IEvent evt, CancellationToken cancellationToken)
        {
            var type = evt.GetType();
            if (typeof(INotificationEvent).IsAssignableFrom(type) is false) return;

            var handlerType = this.GetType();
            var handleMethod = handlerType.GetMethod("HandleNotificationAsync", new[] { type });
            if (handleMethod != null)
            {
                var result = (Task)handleMethod.Invoke(this, new[] { evt });
                await result;
            }
        }
    }
}
