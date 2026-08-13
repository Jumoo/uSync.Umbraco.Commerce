# Security Policy

## Supported versions

`uSync.Umbraco.Commerce` ships one release line per Umbraco major, matching the uSync and
Umbraco Commerce releases it plugs into. Fixes go to the current line, and to the previous one
where the issue is serious and the fix is practical.

| Version | Branch | Supported |
| --- | --- | --- |
| 18.x | `v18/main` | Yes |
| 17.x | `v17/main` | Yes |
| 16.x and earlier | — | No |

## Reporting a vulnerability

Please **do not** open a public issue for a security problem.

Email **info@jumoo.co.uk** with a description of the issue, the version affected, and steps to
reproduce it. We'll acknowledge within a few working days and keep you updated as we work on it.

This package reads and writes uSync config files (XML) under the site's `uSync/Commerce` folder,
and its serializers resolve store/currency/tax/payment data straight from those files during
import. If the issue involves file paths, XML parsing, or values from a synced file being trusted
without validation, say so — those get sequenced first.
