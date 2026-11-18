using Microsoft.Extensions.DependencyInjection;

using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Infrastructure.Manifest;

namespace uSync.Umbraco.Commerce.Complete;

public class CommerceCompleteComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddSingleton<IPackageManifestReader, CommerceCompleteManifestReader>();
    }
}
