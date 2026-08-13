# Changelog

Notable changes to `uSync.Umbraco.Commerce`. Changes before this file existed are only in the
commit history. Like uSync itself, this package ships one release line per Umbraco major, so this
file covers the 17.x line; the 18.x line lives on
[`v18/main`](https://github.com/Jumoo/uSync.Umbraco.Commerce/blob/v18/main/CHANGELOG.md).

## Unreleased

## 17.1.0 - 2026-08-13

### Added

- Repository standards: `LICENSE.md`, `README.md`, `CHANGELOG.md`, `SECURITY.md`,
  `CODE_OF_CONDUCT.md`, `.editorconfig`, `.gitattributes`, `global.json`, `Directory.Build.props`,
  `GitVersion.yml`, dependabot, and issue and PR templates.
- CI workflows — PR build, package build, and release. CodeQL is present but disabled by default
  (see the workflow for why).
- Releases publish to NuGet from a `v{version}` tag pushed on a release branch, gated behind the
  `nuget` GitHub environment and authenticated with trusted publishing (OIDC) rather than a
  stored API key.
- `packages.lock.json`, so a transitive dependency update can't change what a build restores
  without a commit.

### Fixed

- Tax class country/region tax rates were never restored on import: the serializer wrote them
  under a `<TaxClasses>` element but read them back looking for `<TaxRates>`, so import always saw
  an empty list and cleared every existing override instead of restoring it ([#10](https://github.com/Jumoo/uSync.Umbraco.Commerce/issues/10)).
- The exported country/region tax rate value itself round-tripped as `0`, because it was serialized
  via `TaxRate.ToString()` (`"20.00%"`) instead of the underlying decimal value, which then failed
  to parse back on import. Tax codes are now round-tripped too.
- `ShippingMethodSerializer` never removed a country/region shipping allowance that had been
  removed from the XML, due to a collection-diff comparing the live list against itself instead of
  against the incoming XML values.
