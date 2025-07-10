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
        "CommercePrintTemplateHandler",
        "Print Templates",
        "Commerce\\PrintTemplate",
        CommerceConstants.Priorites.PrintTemplate,
        Icon = "icon-print",
        EntityType = CommerceConstants.UdiEntityType.PrintTemplate
    )]
    public class PrintTemplateHandler
        : CommerceSyncHandlerBase<PrintTemplateReadOnly>,
            ISyncHandler,
            IAsyncEventHandlerFor<PrintTemplateSavedNotification>,
            IAsyncEventHandlerFor<PrintTemplateDeletedNotification>
    {
        public PrintTemplateHandler(
            ILogger<SyncHandlerRoot<PrintTemplateReadOnly, PrintTemplateReadOnly>> logger,
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

        protected override Guid GetStoreId(PrintTemplateReadOnly item) => item.StoreId;

        protected override Task DeleteViaServiceAsync(PrintTemplateReadOnly item) =>
            _CommerceApi.DeletePrintTemplateAsync(item.Id);

        protected override Task<IEnumerable<PrintTemplateReadOnly>> GetByStoreAsync(Guid storeId) =>
            _CommerceApi.GetPrintTemplatesAsync(storeId);

        protected override Task<PrintTemplateReadOnly> GetFromServiceAsync(Guid key) =>
            _CommerceApi.GetPrintTemplateAsync(key);

        protected override string GetItemName(PrintTemplateReadOnly item) => item.Name;

        public Task HandleAsync(PrintTemplateSavedNotification notification) =>
            CommerceItemSavedAsync(notification.PrintTemplate);

        public Task Handle(PrintTemplateDeletedNotification notification) =>
            CommerceItemDeletedAsync(notification.PrintTemplate);
    }
}
