# Changelog

Notable changes to `uSync.Umbraco.Commerce`. Changes before this file existed are only in the
commit history. Like uSync itself, this package ships one release line per Umbraco major, so this
file covers the 18.x line; the 17.x line lives on
[`v17/main`](https://github.com/Jumoo/uSync.Umbraco.Commerce/blob/v17/main/CHANGELOG.md).

## Unreleased

### Added

- Umbraco / Umbraco Commerce 18 support — `Umbraco.Commerce`, `uSync.Core` and `uSync.BackOffice`
  bumped to 18.1.0. No code changes were needed; the APIs this package uses were unchanged
  between the 17.x and 18.x lines.
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
