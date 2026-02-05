using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Strings;
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
        "CommerceEmailTemplateHandler",
        "Email",
        "Commerce\\EmailTemplate",
        CommerceConstants.Priorites.EmailTemplate,
        Icon = "icon-mailbox",
        EntityType = CommerceConstants.UdiEntityType.EmailTemplate
    )]
    public class EmailTemplateHandler : CommerceSyncHandlerBase<EmailTemplateReadOnly>, ISyncHandler,
        ISyncCommerceEventHandler<EmailTemplateSavedNotification>,
        ISyncCommerceEventHandler<EmailTemplateDeletedNotification>
    {
        public EmailTemplateHandler(
            ILogger<SyncHandlerRoot<EmailTemplateReadOnly, EmailTemplateReadOnly>> logger,
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

        protected override Guid GetStoreId(EmailTemplateReadOnly item) => item.StoreId;

        protected override Task DeleteViaServiceAsync(EmailTemplateReadOnly item) =>
            _CommerceApi.DeleteEmailTemplateAsync(item.Id);

        protected override Task<IEnumerable<EmailTemplateReadOnly>> GetByStoreAsync(Guid storeId) =>
            _CommerceApi.GetEmailTemplatesAsync(storeId);

        protected override Task<EmailTemplateReadOnly> GetFromServiceAsync(Guid key) =>
            _CommerceApi.GetEmailTemplateAsync(key);

        protected override string GetItemName(EmailTemplateReadOnly item) => item.Name;

        public Task HandleNotificationAsync(EmailTemplateSavedNotification notification) =>
            CommerceItemSavedAsync(notification.EmailTemplate);

        public Task HandleNotificationAsync(EmailTemplateDeletedNotification notification) =>
            CommerceItemDeletedAsync(notification.EmailTemplate);
    }
}
