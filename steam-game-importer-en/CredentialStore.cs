using Microsoft.Win32;
using System.Security.Cryptography;
using System.Text;

namespace SteamGameImporter;

internal static class CredentialStore
{
    private const string RegistryPath = @"Software\SteamGameImporter";

    public static void SaveApiKey(string apiKey)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RegistryPath);
        if (string.IsNullOrWhiteSpace(apiKey)) { key.DeleteValue("SteamGridDbApiKey", false); return; }
        byte[] encrypted = ProtectedData.Protect(Encoding.UTF8.GetBytes(apiKey.Trim()), null, DataProtectionScope.CurrentUser);
        key.SetValue("SteamGridDbApiKey", Convert.ToBase64String(encrypted), RegistryValueKind.String);
    }

    public static string LoadApiKey()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryPath);
            string? value = key?.GetValue("SteamGridDbApiKey")?.ToString();
            if (string.IsNullOrWhiteSpace(value)) return "";
            return Encoding.UTF8.GetString(ProtectedData.Unprotect(Convert.FromBase64String(value), null, DataProtectionScope.CurrentUser));
        }
        catch { return ""; }
    }
}

