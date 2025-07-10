using uSync.BackOffice.Models;

namespace uSync.Umbraco.Commerce
{
    /// <summary>
    ///  Info class, so the version etc, appear on the dashboard
    /// </summary>
    /// <remarks>
    ///  Not strictly required, just lets people see its installed.
    /// </remarks>
    public class CommerceSync : ISyncAddOn
    {
        public string Name => "uSync.Umbraco.Commerce";

        public string Version =>
            typeof(CommerceSync).Assembly.GetName().Version.ToString() ?? "16.0.0";

        public string Icon => "icon-store";

        public string View => string.Empty;

        public string Alias => "CommerceSync";

        public string DisplayName => "uSync for Commerce";

        public int SortOrder => 11;
    }
}
