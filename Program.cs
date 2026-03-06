namespace CheckTranslation;

using Microsoft.Extensions.DependencyInjection;

internal static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        // To customize application configuration such as set high DPI settings or default font,
        // see https://aka.ms/applicationconfiguration.
        AppConfig.Load();
        ApplicationConfiguration.Initialize();

        var services = new ServiceCollection();
        services.AddSingleton<IExcelService, ExcelService>();
        services.AddSingleton<ITranslationService, TranslationService>();

        services.AddTransient<ConfigForm>();
        services.AddTransient<Func<ConfigForm>>(sp => () => sp.GetRequiredService<ConfigForm>());

        services.AddTransient<MainForm>(sp => new MainForm(
            sp.GetRequiredService<IExcelService>(),
            sp.GetRequiredService<ITranslationService>(),
            sp.GetRequiredService<Func<ConfigForm>>()));

        using var serviceProvider = services.BuildServiceProvider();
        Application.Run(serviceProvider.GetRequiredService<MainForm>());
    }
}
