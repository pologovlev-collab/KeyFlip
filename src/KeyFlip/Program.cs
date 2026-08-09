namespace KeyFlip;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        using var mutex = new Mutex(initiallyOwned: true, "Local\\KeyFlip.SingleInstance", out var createdNew);
        ApplicationConfiguration.Initialize();
        if (!createdNew)
        {
            MessageBox.Show(
                "KeyFlip уже запущен. Закройте текущий экземпляр через tray перед запуском другой версии.",
                "KeyFlip",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        Application.Run(new KeyFlipContext());
    }
}
