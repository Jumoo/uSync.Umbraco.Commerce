# uSync.Umbraco.Commerce

uSync serializers and handlers for [Umbraco Commerce](https://umbraco.com/products/add-ons/commerce/),
the eCommerce solution for Umbraco.

Extends [uSync](https://github.com/KevinJump/uSync) so store settings — stores, currencies,
countries, regions, tax classes, shipping/payment methods, order statuses, and email/print/export
templates — export and import as config files, the same way uSync handles content types and the
rest of the backoffice. Discounts and gift cards are supported too, opt-in.

Published as `uSync.Umbraco.Commerce`, targeting `net10.0` / Umbraco 17.

This is the `v17/main` branch, the Umbraco 17 release line. The Umbraco 18 line lives on
[`v18/main`](https://github.com/Jumoo/uSync.Umbraco.Commerce/tree/v18/main).

> Requires both uSync and Umbraco Commerce installed — this is a bridge between the two, not a
> standalone package.

## Settings

| Config key | Default | Effect |
| --- | --- | --- |
| `uSync:Commerce:SyncDiscounts` | `false` | Enables the discount handler |
| `uSync:Commerce:SyncGiftCards` | `false` | Enables the gift card handler |

Both default to off — enabling them changes what a `uSync export` produces, so it's opt-in
rather than a silent behaviour change on upgrade.

## Repository layout

| Path | What it is |
| --- | --- |
| `src/uSync.Umbraco.Commerce` | The package — this is what ships |
| `dist/build-package.ps1` | Local packaging script, for a package you don't want to release |

`uSync.Umbraco.Commerce.slnx` references `test/Commerce.Site`, a local test setup that is **not**
in the repository — `.gitignore` excludes `test/`. Build the package project directly, which is
what CI does.

## Building

Requires the .NET SDK pinned in [`global.json`](global.json).

```bash
dotnet build src/uSync.Umbraco.Commerce/uSync.Umbraco.Commerce.csproj -c Release
```

Shared build and package metadata lives in [`Directory.Build.props`](Directory.Build.props).
Restores are locked, so if you change a dependency you have to commit the regenerated lock file
alongside it:

```bash
dotnet restore src/uSync.Umbraco.Commerce/uSync.Umbraco.Commerce.csproj --force-evaluate --source https://api.nuget.org/v3/index.json
```

`--source` is not optional. The lock file records a content hash per package, and nuget.org
repository-signs what it serves — so a hash taken from a locally built `.nupkg` (a folder feed,
or one already in the global packages cache) never validates against nuget.org, and CI fails the
`--locked-mode` restore with NU1403.

## Releasing

Pushing a `v{version}` tag on `v17/main` (or `v18/main`, for the 18.x line) publishes to NuGet:

```bash
git tag v17.0.0 && git push origin v17.0.0
```

The tag is the version — `v17.0.0` publishes `17.0.0`. The workflow refuses to run if the tag
isn't a valid version, or if the tagged commit isn't on a release branch.

**Pushing the tag publishes.** Nothing pauses for approval — environment protection rules aren't
available on private repositories at this plan level — so the deliberate step is creating the tag
itself. Publishing a draft GitHub release is the usual way to do that, and gives you somewhere to
write the release notes first. A published version can be delisted but never replaced.

Authentication is [trusted publishing](https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing) —
the job exchanges a GitHub OIDC token for a short-lived NuGet key, so there is no API key stored
in the repository.

Every push to `v17/main` also builds a package and uploads it as a build artifact, so a release
candidate can be tested without publishing anything.

## Contributing

Please read [SECURITY.md](SECURITY.md) before reporting anything security related, and
[CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md) before taking part.

## Licence

[MIT](LICENSE.md).
