using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;

using Umbraco.Cms.Api.Management.ViewModels.RelationType.Item;
using Umbraco.Commerce.Common;
using Umbraco.Commerce.Core.Api;
using Umbraco.Commerce.Core.Models;
using Umbraco.Extensions;
using uSync.Core;
using uSync.Core.Models;
using uSync.Core.Serialization;
using uSync.Umbraco.Commerce.Configuration;
using uSync.Umbraco.Commerce.Extensions;

namespace uSync.Umbraco.Commerce.Serializers
{
    /// <summary>
    ///  base for Commerce Serializers for uSync.
    /// </summary>
    public abstract class CommerceSerializerBase<TObject> : SyncSerializerRoot<TObject>
        where TObject : EntityBase
    {
        protected ICommerceApi _CommerceApi;
        protected CommerceSyncSettingsAccessor _settingsAccessor;
        protected IUnitOfWorkProvider _uowProvider;

        protected readonly Type _itemType = typeof(TObject);

        private readonly IList<uSyncChange> _noSyncChanges = new List<uSyncChange>();

        protected CommerceSerializerBase(
            ICommerceApi CommerceApi,
            CommerceSyncSettingsAccessor settingsAccessor,
            IUnitOfWorkProvider uowProvider,
            ILogger<CommerceSerializerBase<TObject>> logger
        )
            : base(logger)
        {
            _CommerceApi = CommerceApi;
            _settingsAccessor = settingsAccessor;
            _uowProvider = uowProvider;
        }

        protected async Task<StoreReadOnly> LookupStoreAsync(Guid key)
            => await _CommerceApi.GetStoreAsync(key) as StoreReadOnly;

        protected async Task<StoreReadOnly> LookupStoreAsync(XElement node)
        {
            var storeId = node.GetStoreId();
            if (storeId == Guid.Empty) return null;

            var store = await _CommerceApi.GetStoreAsync(storeId);
            if (store is StoreReadOnly storeReadOnly && !storeReadOnly.IsDeleted)
                return storeReadOnly;

            var storeAlias = node.GetStoreAlias();
            if (string.IsNullOrEmpty(storeAlias)) return null;

            return await _CommerceApi.GetStoreAsync(storeAlias);
        }

        public override Guid ItemKey(TObject item) => item.Id;

        public override string ItemAlias(TObject item) => GetItemAlias(item);

        public override Task<TObject> FindItemAsync(string alias) => Task.FromResult<TObject>(null);

        public override async Task<TObject> FindItemAsync(Guid key)
            => NullIfDeleted(await DoFindItemAsync(key));

        /// <summary>
        ///  Commerce soft deletes items: the row stays in the database with a deleted timestamp,
        ///  and lookups by key still return it. A deleted item is treated as not there, so an
        ///  import can create it again rather than comparing against something that is hidden.
        /// </summary>
        /// <remarks>
        ///  every Commerce read-only model has a public IsDeleted, but not through a shared
        ///  base class or interface, so it is read once by reflection per item type.
        /// </remarks>
        protected static bool IsDeleted(TObject item)
            => item is not null && _isDeletedProperty?.GetValue(item) is true;

        private static readonly System.Reflection.PropertyInfo _isDeletedProperty
            = typeof(TObject).GetProperty("IsDeleted", typeof(bool));

        protected static TObject NullIfDeleted(TObject item)
            => IsDeleted(item) ? null : item;

        /// <summary>
        ///  When an item was found by its alias rather than its key (for example one that was
        ///  recreated under a new key after a delete) only the key differs from the file. That
        ///  shouldn't count as a change, or every import would save the item again.
        /// </summary>
        public override Task<ChangeType> IsCurrentAsync(XElement node, XElement current, SyncSerializerOptions options)
        {
            var key = node.GetKey();
            if (IgnoreKeyDifferences && current is not null && key != Guid.Empty && current.GetKey() != key)
            {
                current = new XElement(current);
                current.SetAttributeValue("Key", key);
            }

            return base.IsCurrentAsync(node, current, options);
        }

        /// <summary>
        ///  Whether an item matched by alias with a different key counts as current.
        /// </summary>
        protected virtual bool IgnoreKeyDifferences => true;

        /// <summary>
        ///  The key to create an item with on import.
        /// </summary>
        /// <remarks>
        ///  Normally this is the key from the file. If Commerce still holds a deleted item with
        ///  that key, the key can't be reused, so the item is created with a new one. Later
        ///  imports still find it, because lookups fall back to the alias.
        /// </remarks>
        protected async Task<Guid> GetCreateKeyAsync(XElement node, TObject existing)
        {
            var key = node.GetKey();
            if (existing is not null || key == Guid.Empty || !IsDeleted(await DoFindItemAsync(key)))
                return key;

            var newKey = Guid.NewGuid();
            logger.LogInformation(
                "{alias} ({key}) was deleted in Umbraco Commerce, and the deleted item still holds its key. Creating it with the new key {newKey}",
                node.GetAlias(), key, newKey);
            return newKey;
        }

        public override Task DeleteItemAsync(TObject item) => DoDeleteItemAsync(item);

        public override Task SaveItemAsync(TObject item) => DoSaveItemAsync(item);

        public virtual string GetItemAlias(TObject item) => null;

        public abstract Task DoDeleteItemAsync(TObject item);

        public abstract Task<TObject> DoFindItemAsync(Guid key);

        public virtual Task<TObject> DoFindItemAsync(string alias) => Task.FromResult<TObject>(null);

        public virtual Task<TObject> DoFindItemAsync(string alias, Guid storeId) => Task.FromResult<TObject>(null);

        public abstract Task DoSaveItemAsync(TObject item);

        public virtual Task<TObject> DoFinalFindAttempt(XElement node) => Task.FromResult<TObject>(null);

        /// <summary>
        ///  for most lookups we also need the store id. so we override this and always pass the store
        ///  id to the FindItemAsync that takes the alias.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public override async Task<TObject> FindItemAsync(XElement node)
        {
            var item = await base.FindItemAsync(node);
            if (item != null) return item;

            var alias = node.GetAlias();
            var store = node.GetStoreId();

            if (string.IsNullOrEmpty(alias) || store == Guid.Empty)
                return null;

            item = NullIfDeleted(await DoFindItemAsync(alias, store));
            if (item != null) return item;

            return NullIfDeleted(await DoFinalFindAttempt(node));
        }

        protected XElement SerailizeList<TResult>(
            string collectionName,
            string elementName,
            IEnumerable<TResult> items
        )
        {
            var root = new XElement(collectionName);

            if (items.Any())
            {
                foreach (var item in items)
                {
                    root.Add(new XElement(elementName, item));
                }
            }

            return root;
        }

        protected IEnumerable<TResult> DeserializeList<TResult>(
            XElement node,
            string collectionName,
            string elementName
        )
        {
            var root = node.Element(collectionName);
            if (root == null || !root.HasElements)
                return Enumerable.Empty<TResult>();

            var items = new List<TResult>();
            foreach (var item in root.Elements(elementName))
            {
                var attempt = item.Value.TryConvertTo<TResult>();
                if (attempt.Success)
                    items.Add(attempt.Result);
            }

            return items;
        }

        protected SyncAttempt<T> SyncAttemptSucceed<T>(
            string name,
            T item,
            ChangeType change,
            bool saved = false,
            IList<uSyncChange> changes = null
        )
        {
            return SyncAttempt<T>.Succeed(
                name,
                item,
                change,
                null,
                saved,
                changes ?? _noSyncChanges
            );
        }

        protected SyncAttempt<T> SyncAttemptSucceedIf<T>(
            bool condition,
            string name,
            T item,
            ChangeType change
        )
        {
            return SyncAttempt<T>.SucceedIf(condition, name, item, _itemType, change);
        }
    }
}
