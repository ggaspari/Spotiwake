namespace Spotiwake;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        // Garante uma única instância do aplicativo.
        using var mutex = new Mutex(initiallyOwned: true, "Spotiwake_SingleInstance", out bool createdNew);
        if (!createdNew)
        {
            return;
        }

        ApplicationConfiguration.Initialize();
        Application.Run(new TrayAppContext());
    }
}
