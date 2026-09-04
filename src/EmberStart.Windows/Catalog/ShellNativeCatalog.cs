using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security;
using EmberStart.Core.Catalog;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Storage.FileSystem;
using Windows.Win32.UI.Shell;

namespace EmberStart.Windows.Catalog;

internal static class ShellNativeCatalog
{
    private const uint SeeMaskIdList = 0x00000004;
    private const uint SeeMaskNoAsync = 0x00000100;
    private const uint SeeMaskFlagLogUsage = 0x04000000;
    private const int SwShowNormal = 1;
    private const int MaximumStartMenuFiles = 4096;

    public static IReadOnlyList<CatalogEntry> Enumerate()
    {
        var entries = new List<CatalogEntry>();
        AddAppsFolderEntries(entries);
        AddStartMenuEntries(entries, Environment.SpecialFolder.Programs);
        AddStartMenuEntries(entries, Environment.SpecialFolder.CommonPrograms);
        return CatalogIdentityPolicy.Normalize(entries);
    }

    public static unsafe ShellIconHandle? GetIcon(CatalogEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        var result = PInvoke.SHParseDisplayName(
            entry.Id,
            pbc: null!,
            out var itemIdList,
            sfgaoIn: 0);
        ThrowIfFailed(result, "Shell icon identity could not be resolved.");

        try
        {
            var fileInfo = new SHFILEINFOW();
            var flags = SHGFI_FLAGS.SHGFI_PIDL |
                SHGFI_FLAGS.SHGFI_ICON |
                SHGFI_FLAGS.SHGFI_LARGEICON;
            var found = PInvoke.SHGetFileInfo(
                new PCWSTR((char*)itemIdList),
                default(FILE_FLAGS_AND_ATTRIBUTES),
                &fileInfo,
                (uint)sizeof(SHFILEINFOW),
                flags);

            return found == 0 || fileInfo.hIcon.IsNull
                ? null
                : new ShellIconHandle(fileInfo.hIcon);
        }
        finally
        {
            Marshal.FreeCoTaskMem((nint)itemIdList);
        }
    }

    public static ShellLaunchResult Launch(CatalogEntry entry, nint ownerWindow)
    {
        ArgumentNullException.ThrowIfNull(entry);

        return entry.Kind == CatalogEntryKind.Packaged &&
            !string.IsNullOrWhiteSpace(entry.AppUserModelId)
                ? LaunchPackaged(entry.AppUserModelId)
                : LaunchShellItem(entry.Id, ownerWindow);
    }

    private static void AddAppsFolderEntries(List<CatalogEntry> entries)
    {
        var result = PInvoke.SHGetKnownFolderItem<IShellItem>(
            in PInvoke.FOLDERID_AppsFolder,
            KNOWN_FOLDER_FLAG.KF_FLAG_DEFAULT,
            hToken: null!,
            out var appsFolder);
        ThrowIfFailed(result, "AppsFolder could not be opened.");

        IEnumShellItems? enumerator = null;
        try
        {
            appsFolder.BindToHandler(
                pbc: null!,
                in PInvoke.BHID_EnumItems,
                out enumerator);

            var buffer = new IShellItem[1];
            while (true)
            {
                enumerator.Next(buffer, out var fetched);
                if (fetched == 0)
                {
                    break;
                }

                var item = buffer[0];
                try
                {
                    if (TryCreateEntry(item, out var entry))
                    {
                        entries.Add(entry);
                    }
                }
                finally
                {
                    ReleaseComObject(item);
                    buffer[0] = null!;
                }
            }
        }
        finally
        {
            ReleaseComObject(enumerator);
            ReleaseComObject(appsFolder);
        }
    }

    private static void AddStartMenuEntries(
        List<CatalogEntry> entries,
        Environment.SpecialFolder specialFolder)
    {
        var root = Environment.GetFolderPath(specialFolder, Environment.SpecialFolderOption.DoNotVerify);
        if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
        {
            return;
        }

        var options = new EnumerationOptions
        {
            AttributesToSkip = FileAttributes.ReparsePoint,
            IgnoreInaccessible = true,
            RecurseSubdirectories = true,
            ReturnSpecialDirectories = false,
        };

        IEnumerable<string> candidates;
        try
        {
            candidates = Directory.EnumerateFiles(root, "*", options)
                .Where(IsLaunchableStartMenuFile)
                .Take(MaximumStartMenuFiles)
                .ToArray();
        }
        catch (IOException)
        {
            return;
        }
        catch (UnauthorizedAccessException)
        {
            return;
        }
        catch (SecurityException)
        {
            return;
        }

        foreach (var path in candidates)
        {
            IShellItem? item = null;
            try
            {
                var result = PInvoke.SHCreateItemFromParsingName<IShellItem>(
                    path,
                    pbc: null!,
                    out item);
                if (result.Failed)
                {
                    continue;
                }

                if (TryCreateEntry(item, out var entry))
                {
                    entries.Add(entry);
                }
            }
            catch (Exception exception) when (exception is COMException or ArgumentException)
            {
            }
            finally
            {
                ReleaseComObject(item);
            }
        }
    }

    private static bool TryCreateEntry(IShellItem item, out CatalogEntry entry)
    {
        try
        {
            var displayName = ReadName(item, SIGDN.SIGDN_NORMALDISPLAY);
            var parsingName = ReadName(item, SIGDN.SIGDN_DESKTOPABSOLUTEPARSING);
            var appUserModelId = TryReadAppUserModelId(item);
            var kind = appUserModelId?.Contains('!', StringComparison.Ordinal) == true
                ? CatalogEntryKind.Packaged
                : CatalogEntryKind.ShellItem;

            entry = new CatalogEntry(parsingName, displayName, kind, appUserModelId);
            return !string.IsNullOrWhiteSpace(displayName) && !string.IsNullOrWhiteSpace(parsingName);
        }
        catch (Exception exception) when (exception is COMException or ArgumentException)
        {
            entry = null!;
            return false;
        }
    }

    private static unsafe string ReadName(IShellItem item, SIGDN format)
    {
        item.GetDisplayName(format, out var value);
        try
        {
            return value.ToString();
        }
        finally
        {
            Marshal.FreeCoTaskMem((nint)value.Value);
        }
    }

    private static unsafe string? TryReadAppUserModelId(IShellItem item)
    {
        if (item is not IShellItem2 itemWithProperties)
        {
            return null;
        }

        try
        {
            itemWithProperties.GetString(in PInvoke.PKEY_AppUserModel_ID, out var value);
            try
            {
                var result = value.ToString();
                return string.IsNullOrWhiteSpace(result) ? null : result;
            }
            finally
            {
                Marshal.FreeCoTaskMem((nint)value.Value);
            }
        }
        catch (Exception exception) when (exception is COMException or ArgumentException)
        {
            return null;
        }
    }

    private static bool IsLaunchableStartMenuFile(string path)
    {
        var extension = Path.GetExtension(path);
        return extension.Equals(".lnk", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".appref-ms", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".url", StringComparison.OrdinalIgnoreCase);
    }

    private static ShellLaunchResult LaunchPackaged(string appUserModelId)
    {
        IApplicationActivationManager? manager = null;
        try
        {
            manager = (IApplicationActivationManager)(object)new ApplicationActivationManager();
            var result = manager.ActivateApplication(appUserModelId, null, 0, out _);
            if (result < 0)
            {
                throw new InvalidOperationException(
                    $"Packaged application activation failed with HRESULT 0x{result:X8}.");
            }

            return ShellLaunchResult.Success();
        }
        finally
        {
            ReleaseComObject(manager);
        }
    }

    private static unsafe ShellLaunchResult LaunchShellItem(string parsingName, nint ownerWindow)
    {
        var result = PInvoke.SHParseDisplayName(
            parsingName,
            pbc: null!,
            out var itemIdList,
            sfgaoIn: 0);
        ThrowIfFailed(result, "Shell launch identity could not be resolved.");

        try
        {
            var execute = new SHELLEXECUTEINFOW
            {
                cbSize = (uint)sizeof(SHELLEXECUTEINFOW),
                fMask = SeeMaskIdList | SeeMaskNoAsync | SeeMaskFlagLogUsage,
                hwnd = new HWND(ownerWindow),
                lpIDList = itemIdList,
                nShow = SwShowNormal,
            };

            if (!PInvoke.ShellExecuteEx(ref execute))
            {
                throw new Win32Exception(Marshal.GetLastPInvokeError(), "Shell activation failed.");
            }

            return ShellLaunchResult.Success();
        }
        finally
        {
            Marshal.FreeCoTaskMem((nint)itemIdList);
        }
    }

    private static void ReleaseComObject(object? value)
    {
        if (value is not null && Marshal.IsComObject(value))
        {
            _ = Marshal.FinalReleaseComObject(value);
        }
    }

    private static void ThrowIfFailed(HRESULT result, string message)
    {
        if (result.Failed)
        {
            throw new InvalidOperationException($"{message} HRESULT 0x{result.Value:X8}.");
        }
    }

    [ComImport]
    [Guid("45BA127D-10A8-46EA-8AB7-56EA9078943C")]
    [ClassInterface(ClassInterfaceType.None)]
    private sealed class ApplicationActivationManager;

    [ComImport]
    [Guid("2E941141-7F97-4756-BA1D-9DECDE894A3D")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IApplicationActivationManager
    {
        [PreserveSig]
        int ActivateApplication(
            [MarshalAs(UnmanagedType.LPWStr)] string appUserModelId,
            [MarshalAs(UnmanagedType.LPWStr)] string? arguments,
            uint options,
            out uint processId);
    }
}
