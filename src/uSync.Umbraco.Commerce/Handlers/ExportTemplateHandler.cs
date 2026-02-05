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
        "CommerceExportTemplateHandler",
        "Export",
        "Commerce\\ExportTemplate",
        CommerceConstants.Priorites.ExportTemplate,
        Icon = "icon-sharing-iphone",
        EntityType = CommerceConstants.UdiEntityType.ExportTemplate
    )]
    public class ExportTemplateHandler
        : CommerceSyncHandlerBase<ExportTemplateReadOnly>,
            ISyncHandler
    {
        public ExportTemplateHandler(
            ILogger<SyncHandlerRoot<ExportTemplateReadOnly, ExportTemplateReadOnly>> logger,
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

        protected override Guid GetStoreId(ExportTemplateReadOnly item) => item.StoreId;

        protected override Task DeleteViaServiceAsync(ExportTemplateReadOnly item) =>
            _CommerceApi.DeleteExportTemplateAsync(item.Id);

        protected override Task<IEnumerable<ExportTemplateReadOnly>> GetByStoreAsync(
            Guid storeId
        ) => _CommerceApi.GetExportTemplatesAsync(storeId);

        protected override Task<ExportTemplateReadOnly> GetFromServiceAsync(Guid key) =>
            _CommerceApi.GetExportTemplateAsync(key);

        protected override string GetItemName(ExportTemplateReadOnly item) => item.Name;

        public Task Handle(ExportTemplateSavedNotification notification) =>
            CommerceItemSavedAsync(notification.ExportTemplate);

        public Task Handle(ExportTemplateDeletedNotification notification) =>
            CommerceItemDeletedAsync(notification.ExportTemplate);
    }
}
