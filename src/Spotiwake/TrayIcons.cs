using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace Spotiwake;

/// <summary>
/// Gera os ícones da bandeja em tempo de execução (nota musical sobre um
/// círculo colorido), dispensando arquivos .ico no repositório.
/// </summary>
internal static class TrayIcons
{
    /// <summary>Verde Spotify: tocando, suspensão bloqueada.</summary>
    public static Icon Blocking { get; } = Create(Color.FromArgb(29, 185, 84), disabled: false);

    /// <summary>Cinza: monitorando, mas o Spotify está parado ou pausado.</summary>
    public static Icon Idle { get; } = Create(Color.FromArgb(128, 128, 128), disabled: false);

    /// <summary>Cinza escuro com traço: proteção desativada pelo usuário.</summary>
    public static Icon Disabled { get; } = Create(Color.FromArgb(96, 96, 96), disabled: true);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr hIcon);

    private static Icon Create(Color background, bool disabled)
    {
        using var bitmap = new Bitmap(32, 32);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);

            using (var brush = new SolidBrush(background))
            {
                g.FillEllipse(brush, 1, 1, 30, 30);
            }

            // Nota musical branca: cabeça, haste e bandeirola.
            using (var white = new SolidBrush(Color.White))
            using (var pen = new Pen(Color.White, 3f))
            {
                g.FillEllipse(white, 8, 18, 9, 7);
                g.DrawLine(pen, 16, 21, 16, 8);
                g.DrawLine(pen, 16, 9, 23, 13);
            }

            if (disabled)
            {
                using var slash = new Pen(Color.FromArgb(220, 60, 60), 3.5f);
                g.DrawLine(slash, 6, 26, 26, 6);
            }
        }

        IntPtr handle = bitmap.GetHicon();
        try
        {
            using var temp = Icon.FromHandle(handle);
            return (Icon)temp.Clone();
        }
        finally
        {
            DestroyIcon(handle);
        }
    }
}
