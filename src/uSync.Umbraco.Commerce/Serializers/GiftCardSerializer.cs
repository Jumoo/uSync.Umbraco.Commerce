using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

using Umbraco.Commerce.Common;
using Umbraco.Commerce.Core.Api;
using Umbraco.Commerce.Core.Models;
using Umbraco.Commerce.Core.Services;

using uSync.Core;
using uSync.Core.Models;
using uSync.Core.Serialization;
using uSync.Umbraco.Commerce.Configuration;
using uSync.Umbraco.Commerce.Extensions;

namespace uSync.Umbraco.Commerce.Serializers;

[SyncSerializer("A1B2C3D4-E5F6-7890-ABCD-EF1234567890", "GiftCard Serializer", CommerceConstants.Serialization.GiftCard)]
public class GiftCardSerializer : CommerceSerializerBase<GiftCardReadOnly>,
    ISyncSerializer<GiftCardReadOnly>
{
    private readonly IGiftCardService _giftCardService;

    public GiftCardSerializer(
        ICommerceApi commerceApi,
        IGiftCardService giftCardService,
        CommerceSyncSettingsAccessor settingsAccessor,
        IUnitOfWorkProvider uowProvider,
        ILogger<CommerceSerializerBase<GiftCardReadOnly>> logger)
        : base(commerceApi, settingsAccessor, uowProvider, logger)
    {
        _giftCardService = giftCardService;
    }

    public override string GetItemAlias(GiftCardReadOnly item)
        => item.Code;

    public override void DoDeleteItem(GiftCardReadOnly item)
        => _giftCardService.DeleteGiftCard(item.Id);

    public override GiftCardReadOnly DoFindItem(Guid key)
        => _giftCardService.GetGiftCard(key);

    public override void DoSaveItem(GiftCardReadOnly item)
    {
        using var uow = _uowProvider.Create();
        var entity = item.AsWritable(uow);
        _giftCardService.SaveGiftCard(entity);
        uow.Complete();
    }

    public override bool IsValid(XElement node)
        => base.IsValid(node)
        && node.GetStoreId() != Guid.Empty;

    protected override SyncAttempt<XElement> SerializeCore(GiftCardReadOnly item, SyncSerializerOptions options)
    {
        var node = InitializeBaseNode(item, ItemAlias(item));

        node.Add(new XElement(nameof(item.Code), item.Code));
        node.AddStoreId(item.StoreId);

        node.Add(new XElement(nameof(item.CurrencyId), item.CurrencyId));
        node.Add(new XElement(nameof(item.OriginalAmount), item.OriginalAmount.Value));
        node.Add(new XElement(nameof(item.RemainingAmount), item.RemainingAmount.Value));
        node.Add(new XElement(nameof(item.ExpiryDate), item.ExpiryDate));
        node.Add(new XElement(nameof(item.IsActive), item.IsActive));
        node.Add(new XElement(nameof(item.OrderId), item.OrderId));
        node.Add(new XElement(nameof(item.CreateDate), item.CreateDate));

        node.Add(SerializeProperties(item.Properties));

        return SyncAttemptSucceedIf(node != null, item.Code, node, ChangeType.Export);
    }

    protected override SyncAttempt<GiftCardReadOnly> DeserializeCore(XElement node, SyncSerializerOptions options)
    {
        var readonlyItem = FindItem(node);

        var code = node.Element(nameof(GiftCardReadOnly.Code)).ValueOrDefault(string.Empty);
        var id = node.GetKey();
        var storeId = node.GetStoreId();
        var currencyId = node.Element(nameof(GiftCardReadOnly.CurrencyId)).ValueOrDefault(Guid.Empty);
        var originalAmount = node.Element(nameof(GiftCardReadOnly.OriginalAmount)).ValueOrDefault(0m);
        var orderId = node.Element(nameof(GiftCardReadOnly.OrderId)).ValueOrDefault<Guid?>(null);

        using var uow = _uowProvider.Create();

        GiftCard item;
        if (readonlyItem == null)
        {
            item = GiftCard.Create(uow, id, storeId, code, currencyId, originalAmount, orderId);
        }
        else
        {
            item = readonlyItem.AsWritable(uow);
            item.SetCode(code)
                .SetCurrency(currencyId)
                .SetOriginalAmount(originalAmount, resetRemainingAmount: false);
        }

        item.SetRemainingAmount(node.Element(nameof(item.RemainingAmount)).ValueOrDefault(item.RemainingAmount.Value));
        item.SetExpiryDate(node.Element(nameof(item.ExpiryDate)).ValueOrDefault<DateTime?>(null));
        item.SetActive(node.Element(nameof(item.IsActive)).ValueOrDefault(item.IsActive));
        item.SetOrder(node.Element(nameof(item.OrderId)).ValueOrDefault<Guid?>(null));

        item.SetProperties(DeserializeProperties(node), SetBehavior.Replace);

        _giftCardService.SaveGiftCard(item);
        uow.Complete();

        return SyncAttemptSucceed(code, item.AsReadOnly(), ChangeType.Import);
    }

    private XElement SerializeProperties(IReadOnlyDictionary<string, PropertyValue> properties)
    {
        var root = new XElement(nameof(GiftCardReadOnly.Properties));

        if (properties != null)
        {
            foreach (var kvp in properties)
            {
                root.Add(new XElement("Property",
                    new XElement("Alias", kvp.Key),
                    new XElement("Value", kvp.Value.Value),
                    new XElement("IsServerSideOnly", kvp.Value.IsServerSideOnly),
                    new XElement("IsReadOnly", kvp.Value.IsReadOnly)));
            }
        }

        return root;
    }

    private IDictionary<string, PropertyValue> DeserializeProperties(XElement node)
    {
        var result = new Dictionary<string, PropertyValue>();

        var root = node.Element(nameof(GiftCardReadOnly.Properties));
        if (root == null || !root.HasElements)
            return result;

        foreach (var el in root.Elements("Property"))
        {
            var alias = el.Element("Alias").ValueOrDefault(string.Empty);
            if (string.IsNullOrWhiteSpace(alias))
                continue;

            var value = el.Element("Value").ValueOrDefault(string.Empty);
            var isServerSideOnly = el.Element("IsServerSideOnly").ValueOrDefault(false);
            var isReadOnly = el.Element("IsReadOnly").ValueOrDefault(false);

            result[alias] = new PropertyValue(value, isServerSideOnly, isReadOnly);
        }

        return result;
    }
}
