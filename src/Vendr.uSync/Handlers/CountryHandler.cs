﻿using System;
using System.Collections.Generic;
using System.Linq;

using Umbraco.Core.Cache;
using Umbraco.Core.Logging;

using uSync8.BackOffice.Configuration;
using uSync8.BackOffice.Services;
using uSync8.BackOffice.SyncHandlers;
using uSync8.Core;
using uSync8.Core.Serialization;

using Vendr.Core.Api;
using Vendr.Core.Events;
using Vendr.Core.Models;

namespace Vendr.uSync.Handlers
{
    [SyncHandler("vendrCountryHandler", "Country", "vendr\\Country",
        VendrConstants.Priorites.Country, Icon = "icon-globe")]
    public class CountryHandler : VendrSyncHandlerBase<CountryReadOnly>,
        ISyncPostImportHandler, ISyncExtendedHandler
    {
        public override string Group => VendrConstants.Group;

        public CountryHandler(
            IVendrApi vendrApi, 
            IProfilingLogger logger, 
            AppCaches appCaches, 
            ISyncSerializer<CountryReadOnly> serializer, 
            ISyncItemFactory itemFactory,
            SyncFileService syncFileService) 
            : base(vendrApi, logger, appCaches, serializer, itemFactory, syncFileService)
        { }

        protected override void DeleteViaService(CountryReadOnly item)
            => _vendrApi.DeleteCountry(item.Id);

        protected override IEnumerable<CountryReadOnly> GetChildItems(CountryReadOnly parent)
        {
            if (parent == null)
            {
                var countries = new List<CountryReadOnly>();
                var stores = _vendrApi.GetStores();
                foreach(var store in stores)
                {
                    countries.AddRange(_vendrApi.GetCountries(store.Id));
                }

                return countries;
            }

            return Enumerable.Empty<CountryReadOnly>();
        }

        protected override CountryReadOnly GetFromService(Guid key)
            => _vendrApi.GetCountry(key);

        protected override string GetItemName(CountryReadOnly item)
            => item.Name;

        protected override void InitializeEvents(HandlerSettings settings)
        {
            EventHub.NotificationEvents.OnCountrySaved((e) => VendrItemSaved(e.Country));
            EventHub.NotificationEvents.OnCountryDeleted((e) => VendrItemDeleted(e.Country));
        }
    }
}