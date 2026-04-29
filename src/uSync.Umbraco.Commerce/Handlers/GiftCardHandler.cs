using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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
using uSync.BackOffice.SyncHandlers.Interfaces;
using uSync.BackOffice.SyncHandlers.Models;
using uSync.Core;

namespace uSync.Umbraco.Commerce.Handlers;

[SyncHandler("CommerceGiftCardHandler", "Gift Cards", "Commerce\\GiftCard", CommerceConstants.Priorites.GiftCard,
    Icon = "icon-gift", EntityType = CommerceConstants.UdiEntityType.GiftCard)]
public class GiftCardHandler : CommerceSyncHandlerBase<GiftCardReadOnly>, ISyncHandler,
    ISyncCommerceEventHandler<GiftCardSavedNotification>,
    ISyncCommerceEventHandler<GiftCardDeletedNotification>
{
   private readonly IGiftCardService _giftCardService;

    public GiftCardHandler(
        ILogger<SyncHandlerRoot<GiftCardReadOnly, GiftCardReadOnly>> logger,
        AppCaches appCaches,
        IShortStringHelper shortStringHelper,
        ISyncFileService syncFileService,
        ISyncEventService mutexService,
        ISyncConfigService uSyncConfig,
        ISyncItemFactory itemFactory,
        ICommerceApi commerceApi,
        IGiftCardService giftCardService) : base(logger, appCaches, shortStringHelper, syncFileService, mutexService, uSyncConfig, itemFactory, commerceApi)
    {
        _giftCardService = giftCardService;
    }

    protected override Guid GetStoreId(GiftCardReadOnly item)
        => item.StoreId;

    protected override Task DeleteViaServiceAsync(GiftCardReadOnly item)
        => _giftCardService.DeleteGiftCardAsync(item.Id);

    protected override Task<IEnumerable<GiftCardReadOnly>> GetByStoreAsync(Guid storeId)
        => _giftCardService.GetGiftCardsAsync(storeId);

    protected override Task<GiftCardReadOnly> GetFromServiceAsync(Guid key)
        => _giftCardService.GetGiftCardAsync(key);
    protected override string GetItemName(GiftCardReadOnly item)
        => item.Code;

    public Task HandleNotificationAsync(GiftCardSavedNotification notification)
        => CommerceItemSavedAsync(notification.GiftCard);

    public Task HandleNotificationAsync(GiftCardDeletedNotification notification)
        => CommerceItemDeletedAsync(notification.GiftCard);
}
