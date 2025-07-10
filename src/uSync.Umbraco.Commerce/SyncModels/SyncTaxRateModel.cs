using System;

namespace uSync.Umbraco.Commerce.SyncModels
{
    public class SyncTaxRateModel
    {
        public Guid? CountryId { get; set; }
        public Guid? RegionId { get; set; }
        public decimal Rate { get; set; }
        public string TaxCode { get; set; }
    }
}
