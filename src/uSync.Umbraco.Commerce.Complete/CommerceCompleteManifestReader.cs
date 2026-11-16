using System.Text.Json.Nodes;

using Umbraco.Cms.Core.Manifest;
using Umbraco.Cms.Infrastructure.Manifest;

namespace uSync.Umbraco.Commerce.Complete;

// the manifest is built here rather than in a static umbraco-package.json so the
// version is the assembly's, and not a fixed value that has to be kept in step.
internal class CommerceCompleteManifestReader : IPackageManifestReader
{
    public Task<IEnumerable<PackageManifest>> ReadPackageManifestsAsync()
    {
        PackageManifest manifest = new()
        {
            Id = "uSync.Umbraco.Commerce.Complete",
            Name = "uSync.Umbraco.Commerce.Complete",
            AllowTelemetry = true,
            Version = typeof(CommerceCompleteManifestReader).Assembly.GetName().Version?.ToString(3) ?? "17.0.0",
            Extensions = [
                new JsonObject {
                    ["name"] = "uSync Commerce Complete Bundle",
                    ["alias"] = "uSync.Umbraco.Commerce.Complete.Bundle",
                    ["type"] = "bundle",
                    ["js"] = "/App_Plugins/uSyncCommerceComplete/usync-commerce-complete.js",
                }
            ]
        };

        return Task.FromResult<IEnumerable<PackageManifest>>([manifest]);
    }
}
