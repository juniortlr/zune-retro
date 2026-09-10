# Shell Catalog Vertical Slice

**Recorded:** 2026-09-04

**Branch:** `codex/phase-1-foundation`

**Decision:** **ITERATE — ES-006 is implemented for the Phase 1 slice; ES-007 has a working classic-launch path and packaged-launch adapter, with controlled packaged-success evidence still required.**

## Implemented

- Enumerates the virtual AppsFolder with `SHGetKnownFolderItem`, `IShellItem::BindToHandler`, and `IEnumShellItems` on a dedicated STA worker.
- Supplements AppsFolder with `.lnk`, `.appref-ms`, and `.url` Shell items from the current-user and common Programs folders.
- Preserves localized Shell display names, desktop-absolute parsing identities, and `PKEY_AppUserModel_ID` where available.
- Deduplicates first by case-insensitive AUMID and otherwise by canonical Shell identity. Display names are never used as identity.
- Resolves a fresh PIDL from the stored Shell identity for icon extraction and classic launch.
- Extracts large Shell icons with `SHGetFileInfo(SHGFI_PIDL | SHGFI_ICON)` and releases every returned `HICON` with `DestroyIcon`.
- Launches classic entries with `ShellExecuteEx(SEE_MASK_IDLIST)` and no reconstructed command line or user-controlled arguments.
- Launches packaged entries that have a package-style AUMID through `IApplicationActivationManager::ActivateApplication`, also with no arguments.
- Runs catalog, icon, and launch calls through a 64-item bounded STA queue. Initial catalog work has a two-second UI wait, incremental icons have a 250 ms wait, and launch has a two-second wait. A timed-out Shell call opens the worker circuit instead of blocking the WPF UI.
- Replaces the fake list in the Ember Fusion window with real entries and incremental icons. Enter or double-click launches the selected entry; successful launch dismisses the menu.

## Verification snapshot

| Check | Result |
|---|---|
| Owner-PC Shell catalog with required mode | PASS — nonempty, both classic and packaged identities present |
| Shell icon extraction | PASS — a catalog icon was acquired and released |
| Classic launch fixture | PASS — the fixture recorded exactly one nonce without command-line arguments |
| Invalid packaged identity | PASS — rejected as `LaunchFailed` without starting a process |
| Core tests | PASS — 23/23 |
| Windows integration tests | PASS — 9/9 |
| Release solution build | PASS — 0 warnings, 0 errors |
| Real WPF catalog startup and IPC dismissal | PASS — catalog initialized, secondary hide exited 0, resident remained healthy |

The catalog test does not print or persist application names, paths, or the owner's inventory. Only aggregate pass/fail properties are recorded.

## Remaining evidence and hardening

- Build and install a benign packaged/AUMID fixture that writes a nonce, then prove one successful packaged activation. The adapter exists, but an invalid-identity rejection is not a success-path substitute.
- Compare the normalized catalog with the owner's AppsFolder inventory and record aggregate completeness; investigate any unsupported Shell item without logging private names or paths.
- Add icon cache size/eviction behavior and prove the 250 ms incremental deadline under slow and malformed icon handlers.
- Add a recoverable worker-process experiment only if profiling demonstrates that an in-process STA can remain wedged after its circuit opens.
- Complete keyboard/UI Automation invocation behavior, Narrator review, text/contrast/localization checks, and native WPF visual capture.
- Complete mixed-DPI placement, IPC hardening, packaging, recovery, performance, and activation campaigns before Gate G1a.
- Prove the installed RetroBar Start-button route separately before Gate G1b.

## Primary API references

- [SHGetKnownFolderItem](https://learn.microsoft.com/windows/win32/api/shlobj_core/nf-shlobj_core-shgetknownfolderitem)
- [IShellItem::BindToHandler](https://learn.microsoft.com/windows/win32/api/shobjidl_core/nf-shobjidl_core-ishellitem-bindtohandler)
- [SHGetFileInfo](https://learn.microsoft.com/windows/win32/api/shellapi/nf-shellapi-shgetfileinfow)
- [IApplicationActivationManager::ActivateApplication](https://learn.microsoft.com/windows/win32/api/shobjidl_core/nf-shobjidl_core-iapplicationactivationmanager-activateapplication)
