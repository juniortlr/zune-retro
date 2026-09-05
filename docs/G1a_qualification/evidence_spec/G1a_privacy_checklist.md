# G1a Evidence Privacy Checklist — Reviewed 2026-09-05

Status: checklist, not a signed or completed review.

Keep raw captures, TRX files, catalog ledgers, and environment reports in ignored `artifacts/g1a/`. Before publishing an aggregate packet, verify:

- No usernames, personal machine names, profile paths, queries, document contents, window titles, or application inventory appear in text, filenames, screenshots, or embedded metadata.
- Application names are limited to controlled, documented fixture identities.
- Result tables use structural measurements and fixed failure codes. Exception text and free-form notes are reviewed before export.
- Screenshots are checked after redaction; covering only the title bar is insufficient if search results or the desktop expose personal information.
- Build and fixture hashes identify the tested binaries. Source commit/patch identity and aggregate test counts are retained.
- Missing, failed, and skipped cases stay visible in the summary.
- Raw evidence remains owner-controlled; only the reviewed sanitized summary is committed.

Record reviewer, timestamp, artifact list and hashes, findings, and redaction outcome. This checklist does not create a mandatory manual approval for routine code edits or tests, and its existence does not imply any gate has passed.
