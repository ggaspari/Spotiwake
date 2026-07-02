using System.Diagnostics;

namespace Spotiwake;

internal enum SpotifyStatus
{
    NotRunning,
    Paused,
    Playing,
}

/// <summary>
/// Detecta se o Spotify (aplicativo desktop) está em execução e tocando.
/// Estratégia em duas camadas:
///   1. Título da janela: quando está tocando, o título vira "Artista - Música";
///      pausado, volta a ser "Spotify", "Spotify Free" ou "Spotify Premium".
///   2. Medidor de áudio (WASAPI): cobre casos em que o título não ajuda,
///      como anúncios ou janela minimizada para a bandeja.
/// </summary>
internal sealed class SpotifyDetector
{
    private const string ProcessName = "Spotify";

    public SpotifyStatus GetStatus()
    {
        bool running = false;
        bool playing = false;

        foreach (var process in Process.GetProcessesByName(ProcessName))
        {
            using (process)
            {
                running = true;

                string title;
                try
                {
                    title = process.MainWindowTitle;
                }
                catch
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(title) && IsPlayingTitle(title))
                {
                    playing = true;
                }
            }
        }

        if (!running)
        {
            return SpotifyStatus.NotRunning;
        }

        if (!playing)
        {
            playing = AudioSessions.IsProcessPlayingAudio(ProcessName);
        }

        return playing ? SpotifyStatus.Playing : SpotifyStatus.Paused;
    }

    private static bool IsPlayingTitle(string title)
    {
        if (title is "Spotify" or "Spotify Free" or "Spotify Premium")
        {
            return false;
        }

        return title.Contains(" - ", StringComparison.Ordinal);
    }
}
