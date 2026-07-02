using Microsoft.Win32;

namespace Spotiwake;

/// <summary>
/// Registra ou remove o aplicativo da inicialização do Windows
/// (chave Run do usuário atual — não requer privilégios de administrador).
/// </summary>
internal static class StartupManager
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "Spotiwake";

    public static bool IsEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath);
            return key?.GetValue(ValueName) is not null;
        }
        catch
        {
            return false;
        }
    }

    public static void SetEnabled(bool enabled)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath);
            if (enabled)
            {
                var exePath = Environment.ProcessPath;
                if (!string.IsNullOrEmpty(exePath))
                {
                    key.SetValue(ValueName, $"\"{exePath}\"");
                }
            }
            else
            {
                key.DeleteValue(ValueName, throwOnMissingValue: false);
            }
        }
        catch
        {
            // Sem acesso ao registro: ignora silenciosamente.
        }
    }
}
