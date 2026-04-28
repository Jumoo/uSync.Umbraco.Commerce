namespace uSync.Umbraco.Commerce
{
    internal static class CommerceConstants
    {
        public const string Group = "Commerce";

        internal static class Serialization
        {
            public const string Store = "Store";
            public const string Currency = "Currency";
            public const string Country = "Country";
            public const string Region = "Region";

            public const string TaxClass = "TaxClass";

            public const string EmailTemplate = "EmailTemplate";
            public const string PrintTemplate = "PrintTemplate";
            public const string ExportTemplate = "ExportTemplate";
            public const string OrderStatus = "OrderStatus";

            public const string PaymentMethod = "PaymentMethod";
            public const string ShippingMethod = "ShippingMethod";

            public const string Discount = "Discount";
            public const string GiftCard = "GiftCard";

            public const string Location = "Location";
        }

        internal static class Priorites
        {
            // content runs at 1200, so by being just below, we will build a store before we sync content
            public const int COMMERCE_RESERVED_LOWER = 1150;
            public const int COMMERCE_RESERVED_UPPER = 1199;

            //
            // Handlers are marked with these priorities - setting the order things are imported
            //
            // Some things need to be imported before things they require have been imported (e.g stores)
            // these handlers impliment ISyncPostImportHandler and uSync will run any imports 
            // again at the end of the process (in the post import step). to capture things that might
            // have been missing first time.
            // 
            // Stores, Country & Region are ISyncPostImportHandlers.
            //

            public const int Stores = COMMERCE_RESERVED_LOWER + 1; // has to be first **

            public const int EmailTemplate = COMMERCE_RESERVED_LOWER + 2; // requires only store
            public const int OrderStatus = COMMERCE_RESERVED_LOWER + 3; // requires only store
            public const int PrintTemplate = COMMERCE_RESERVED_LOWER + 4; // requires only store
            public const int ExportTemplate = COMMERCE_RESERVED_LOWER + 5; // requires only store

            public const int Country = COMMERCE_RESERVED_LOWER + 10; // requires store, currency, payment and shipping **
            public const int Region = COMMERCE_RESERVED_LOWER + 11; // requires store, country, paymeent and shipping **

            public const int Currency = COMMERCE_RESERVED_LOWER + 12;  // requires countries

            public const int TaxClass = COMMERCE_RESERVED_LOWER + 13; // requires countries
            
            public const int Location = COMMERCE_RESERVED_LOWER + 14; // requires store, country, region, payment and shipping **

            public const int PaymentMethod = COMMERCE_RESERVED_LOWER + 21; // requires store, countries, currencies
            public const int ShippingMethod = COMMERCE_RESERVED_LOWER + 22;// requires store, countries, currencies

            public const int Discount = COMMERCE_RESERVED_LOWER + 30; // requires store
            public const int GiftCard = COMMERCE_RESERVED_LOWER + 31; // requires store, currency
        }

        public static class UdiEntityType
        {
            public const string Store = "commerce-store";

            public const string Country = "commerce-country";

            public const string Region = "commerce-region";

            public const string Currency = "commerce-currency";

            public const string OrderStatus = "commerce-order-status";

            public const string ShippingMethod = "commerce-shipping-method";

            public const string PaymentMethod = "commerce-payment-method";

            public const string TaxClass = "commerce-tax-class";

            public const string EmailTemplate = "commerce-email-template";

            public const string PrintTemplate = "commerce-print-template";

            public const string ExportTemplate = "commerce-export-template";

            public const string Discount = "commerce-discount";

            public const string GiftCard = "commerce-gift-card";

            public const string ProductAttribute = "commerce-product-attribute";

            public const string Location = "commerce-location";
        }
    }
}

