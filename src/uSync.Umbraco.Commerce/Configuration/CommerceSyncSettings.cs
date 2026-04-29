using System;
using System.ComponentModel;

namespace uSync.Umbraco.Commerce.Configuration
{
    public class CommerceSyncSettings
    {
        public CommerceSyncPaymentMethodSettings PaymentMethods { get; set; }

        public CommerceSyncSettings()
        {
            PaymentMethods = new CommerceSyncPaymentMethodSettings();
        }

        public bool SyncDiscounts { get; set; } = false;
        public bool SyncGiftCards { get; set; } = false; 
    }

    public class CommerceSyncPaymentMethodSettings
    {
        public string[] IgnoreSettings { get; set; }

        public CommerceSyncPaymentMethodSettings()
        {
            IgnoreSettings = Array.Empty<string>();
        }
    }
}
