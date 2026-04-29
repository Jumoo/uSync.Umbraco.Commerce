using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml.Linq;

using Umbraco.Commerce.Common;
using Umbraco.Commerce.Core.Api;
using Umbraco.Commerce.Core.Events.Validation.Handlers.Order;
using Umbraco.Commerce.Core.Models;
using Umbraco.Commerce.Core.Services;
using Umbraco.Commerce.Extensions;

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
        ICommerceApi CommerceApi,
        CommerceSyncSettingsAccessor settingsAccessor,
        IUnitOfWorkProvider uowProvider,
        ILogger<CommerceSerializerBase<GiftCardReadOnly>> logger) : base(CommerceApi, settingsAccessor, uowProvider, logger)
    {
    }

    public override string GetItemAlias(GiftCardReadOnly item)
        => item.Code;

    public override Task DoDeleteItemAsync(GiftCardReadOnly item)
        => _giftCardService.DeleteGiftCardAsync(item.Id);

    public override Task<GiftCardReadOnly> DoFindItemAsync(Guid key)
        => _giftCardService.GetGiftCardAsync(key);
    public override Task DoSaveItemAsync(GiftCardReadOnly item)
    {
        return _uowProvider.ExecuteAsync(async uow =>
        {
            var entity = await item.AsWritableAsync(uow);
            await _giftCardService.SaveGiftCardAsync(entity);
            uow.Complete();
        }); 
    }

    public override bool IsValid(XElement node)
        => base.IsValid(node)
        && node.GetStoreId() != Guid.Empty;

    protected override async Task<SyncAttempt<XElement>> SerializeCoreAsync(GiftCardReadOnly item, SyncSerializerOptions options)
    {
        var node = InitializeBaseNode(item, ItemAlias(item));

        node.Add(new XElement(nameof(item.Code), item.Code));

        var store = await LookupStoreAsync(item.StoreId);
        node.AddStoreId(item.StoreId, store?.Alias);

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

    protected override async Task<SyncAttempt<GiftCardReadOnly>> DeserializeCoreAsync(XElement node, SyncSerializerOptions options)
    {
        var readonlyItem = await FindItemAsync(node);

        var code = node.Element(nameof(GiftCardReadOnly.Code)).ValueOrDefault(string.Empty);
        var id = node.GetKey();
        var storeId = node.GetStoreId();
        var currencyId = node.Element(nameof(GiftCardReadOnly.CurrencyId)).ValueOrDefault(Guid.Empty);
        var originalAmount = node.Element(nameof(GiftCardReadOnly.OriginalAmount)).ValueOrDefault(0m);
        var orderId = node.Element(nameof(GiftCardReadOnly.OrderId)).ValueOrDefault<Guid?>(null);

        return await _uowProvider.ExecuteAsync(async uow =>
        {

            GiftCard item;
            if (readonlyItem == null)
            {
                var store = await LookupStoreAsync(node);
                if (store is null)
                    return SyncAttempt<GiftCardReadOnly>.Fail(node.GetAlias(), ChangeType.Import, $"Store with id {storeId} not found.");

                item = await GiftCard.CreateAsync(uow, id, store.Id, code, currencyId, originalAmount, orderId);
            }
            else
            {
                item = await readonlyItem.AsWritableAsync(uow);
                await item.SetCodeAsync(code)
                        .SetCurrencyAsync(currencyId)
                        .SetOriginalAmountAsync(originalAmount, resetRemainingAmount: false);
            }

            var defaultRemaining = item.RemainingAmount.Value.Result.Value;
            await item.SetRemainingAmountAsync(node.Element(nameof(item.RemainingAmount)).ValueOrDefault(defaultRemaining));
            
            await item.SetExpiryDateAsync(node.Element(nameof(item.ExpiryDate)).ValueOrDefault<DateTime?>(null));
            await item.SetActiveAsync(node.Element(nameof(item.IsActive)).ValueOrDefault(item.IsActive));
            await item.SetOrderAsync(node.Element(nameof(item.OrderId)).ValueOrDefault<Guid?>(null));

            await item.SetPropertiesAsync(DeserializeProperties(node), SetBehavior.Replace);

            await _giftCardService.SaveGiftCardAsync(item);
            uow.Complete();

            return SyncAttemptSucceed(code, item.AsReadOnly(), ChangeType.Import);
        });
    }

    private static XElement SerializeProperties(IReadOnlyDictionary<string, PropertyValue> properties)
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

    private static IDictionary<string, PropertyValue> DeserializeProperties(XElement node)
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
