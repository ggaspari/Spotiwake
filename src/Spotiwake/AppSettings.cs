using System.Text.Json;

namespace Spotiwake;

/// <summary>
/// Preferências do usuário, persistidas em %AppData%\Spotiwake\settings.json.
/// </summary>
internal sealed class AppSettings
{
    public bool Enabled { get; set; } = true;

    public bool KeepDisplayOn { get; set; }

    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };

    private static string FilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Spotiwake",
        "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath));
                if (settings is not null)
                {
                    return settings;
                }
            }
        }
        catch
        {
            // Arquivo corrompido ou ilegível: volta ao padrão.
        }

        return new AppSettings();
    }

    public void Save()
    {
        try
        {
            var directory = Path.GetDirectoryName(FilePath)!;
            Directory.CreateDirectory(directory);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(this, SerializerOptions));
        }
        catch
        {
            // Falha ao salvar não deve derrubar o aplicativo.
        }
    }
}
