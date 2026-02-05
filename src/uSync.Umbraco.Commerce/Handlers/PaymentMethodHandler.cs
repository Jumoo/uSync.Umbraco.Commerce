using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Strings;
using Umbraco.Commerce.Common.Events;
using Umbraco.Commerce.Core.Api;
using Umbraco.Commerce.Core.Events.Notification;
using Umbraco.Commerce.Core.Models;
using uSync.BackOffice.Configuration;
using uSync.BackOffice.Services;
using uSync.BackOffice.SyncHandlers;
using uSync.BackOffice.SyncHandlers.Interfaces;
using uSync.BackOffice.SyncHandlers.Models;
using uSync.Core;

namespace uSync.Umbraco.Commerce.Handlers
{
    [SyncHandler(
        "CommercePaymentMethodHandler",
        "Payment Methods",
        "Commerce\\PaymentMethod",
        CommerceConstants.Priorites.PaymentMethod,
        Icon = "icon-multiple-credit-cards",
        EntityType = CommerceConstants.UdiEntityType.PaymentMethod
    )]
    public class PaymentMethodHandler
        : CommerceSyncHandlerBase<PaymentMethodReadOnly>,
            ISyncHandler,
            ISyncCommerceEventHandler<PaymentMethodSavedNotification>,
            ISyncCommerceEventHandler<PaymentMethodDeletedNotification>
    {
        public PaymentMethodHandler(
            ILogger<SyncHandlerRoot<PaymentMethodReadOnly, PaymentMethodReadOnly>> logger,
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
                itemFactory,
                commerceApi
            ) { }

        protected override Guid GetStoreId(PaymentMethodReadOnly item) => item.StoreId;

        protected override Task<IEnumerable<PaymentMethodReadOnly>> GetByStoreAsync(Guid storeId) =>
            _CommerceApi.GetPaymentMethodsAsync(storeId);

        protected override Task DeleteViaServiceAsync(PaymentMethodReadOnly item) =>
            _CommerceApi.DeletePaymentMethodAsync(item.Id);

        protected override Task<PaymentMethodReadOnly> GetFromServiceAsync(Guid key) =>
            _CommerceApi.GetPaymentMethodAsync(key);

        protected override string GetItemName(PaymentMethodReadOnly item) => item.Name;

        public Task HandleNotificationAsync(PaymentMethodSavedNotification notification) =>
            CommerceItemSavedAsync(notification.PaymentMethod);

        public Task HandleNotificationAsync(PaymentMethodDeletedNotification notification) =>
            CommerceItemDeletedAsync(notification.PaymentMethod);
    }
}
