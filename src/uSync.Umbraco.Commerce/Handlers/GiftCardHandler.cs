using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;

using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Strings;
using Umbraco.Commerce.Common.Events;
using Umbraco.Commerce.Core.Api;
using Umbraco.Commerce.Core.Events.Notification;
using Umbraco.Commerce.Core.Models;
using Umbraco.Commerce.Core.Services;

using uSync.BackOffice.Configuration;
using uSync.BackOffice.Services;
using uSync.BackOffice.SyncHandlers;
using uSync.Core;

namespace uSync.Umbraco.Commerce.Handlers;

[SyncHandler("CommerceGiftCardHandler", "Gift Cards", "Commerce\\GiftCard", CommerceConstants.Priorites.GiftCard,
    Icon = "icon-gift", EntityType = CommerceConstants.UdiEntityType.GiftCard)]
public class GiftCardHandler : CommerceSyncHandlerBase<GiftCardReadOnly>, ISyncHandler,
    IEventHandlerFor<GiftCardSavedNotification>,
    IEventHandlerFor<GiftCardDeletedNotification>
{
    private readonly IGiftCardService _giftCardService;

    public GiftCardHandler(
        ICommerceApi commerceApi,
        IGiftCardService giftCardService,
        ILogger<CommerceSyncHandlerBase<GiftCardReadOnly>> logger,
        AppCaches appCaches,
        IShortStringHelper shortStringHelper,
        SyncFileService syncFileService,
        uSyncEventService mutexService,
        uSyncConfigService uSyncConfig,
        ISyncItemFactory itemFactory)
        : base(commerceApi, logger, appCaches, shortStringHelper, syncFileService, mutexService, uSyncConfig, itemFactory)
    {
        _giftCardService = giftCardService;
    }

    protected override Guid GetStoreId(GiftCardReadOnly item)
        => item.StoreId;

    protected override void DeleteViaService(GiftCardReadOnly item)
        => _giftCardService.DeleteGiftCard(item.Id);

    protected override IEnumerable<GiftCardReadOnly> GetByStore(Guid storeId)
        => _giftCardService.GetGiftCards(storeId);

    protected override GiftCardReadOnly GetFromService(Guid key)
        => _giftCardService.GetGiftCard(key);

    protected override string GetItemName(GiftCardReadOnly item)
        => item.Code;

    public void Handle(GiftCardSavedNotification notification)
        => CommerceItemSaved(notification.GiftCard);

    public void Handle(GiftCardDeletedNotification notification)
        => CommerceItemDeleted(notification.GiftCard);
}
