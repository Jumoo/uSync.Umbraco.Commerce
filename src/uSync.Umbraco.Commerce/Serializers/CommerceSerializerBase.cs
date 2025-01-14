﻿using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Umbraco.Commerce.Common;
using Umbraco.Commerce.Core.Api;
using Umbraco.Commerce.Core.Models;
using Umbraco.Extensions;
using uSync.Core;
using uSync.Core.Models;
using uSync.Core.Serialization;
using uSync.Umbraco.Commerce.Configuration;

namespace uSync.Umbraco.Commerce.Serializers
{
    /// <summary>
    ///  base for Commerce Serializers for uSync. 
    /// </summary>
    public abstract class CommerceSerializerBase<TObject>
        : SyncSerializerRoot<TObject>
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
            ILogger<CommerceSerializerBase<TObject>> logger) : base(logger)
        {
            _CommerceApi = CommerceApi;
            _settingsAccessor = settingsAccessor;
            _uowProvider = uowProvider;
        }

        public override Guid ItemKey(TObject item)
            => item.Id;

        public override string ItemAlias(TObject item)
            => GetItemAlias(item);

        public override TObject FindItem(string alias)
            => null;

        public override TObject FindItem(int id)
            => null;

        public override TObject FindItem(Guid key)
            => DoFindItem(key);

        public override void DeleteItem(TObject item)
            => DoDeleteItem(item);

        public override void SaveItem(TObject item)
            => DoSaveItem(item);

        public virtual string GetItemAlias(TObject item)
            => null;

        public abstract void DoDeleteItem(TObject item);

        public abstract TObject DoFindItem(Guid key);

        public virtual TObject DoFindItem(string alias)
            => null;

        public abstract void DoSaveItem(TObject item);

        protected XElement SerailizeList<TResult>(string collectionName, string elementName, IEnumerable<TResult> items)
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

        protected IEnumerable<TResult> DeserializeList<TResult>(XElement node, string collectionName, string elementName)
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

        protected SyncAttempt<T> SyncAttemptSucceed<T>(string name, T item, ChangeType change, bool saved = false, IList<uSyncChange> changes = null)
        {
            return SyncAttempt<T>.Succeed(name, item, change, null, saved, changes ?? _noSyncChanges);
        }

        protected SyncAttempt<T> SyncAttemptSucceedIf<T>(bool condition, string name, T item, ChangeType change)
        {
            return SyncAttempt<T>.SucceedIf(condition, name, item, _itemType, change);
        }
    }
}
