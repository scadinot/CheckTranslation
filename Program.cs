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
        services.AddSingleton<ITranslationSourceFactory, TranslationSourceFactory>();
        services.AddSingleton<ILayoutCheckService, LayoutCheckService>();
        services.AddSingleton<ITranslationService, TranslationService>();
        services.AddSingleton<IGlossaryService, GlossaryService>();

        services.AddTransient<ConfigForm>();
        services.AddTransient<Func<ConfigForm>>(sp => () => sp.GetRequiredService<ConfigForm>());

        services.AddTransient<GlossaryForm>();
        services.AddTransient<Func<GlossaryForm>>(sp => () => sp.GetRequiredService<GlossaryForm>());

        services.AddTransient<GlossaryExtractionDialog>();
        services.AddTransient<Func<GlossaryExtractionDialog>>(sp => () => sp.GetRequiredService<GlossaryExtractionDialog>());

        services.AddTransient<MainForm>(sp => new MainForm(
            sp.GetRequiredService<IExcelService>(),
            sp.GetRequiredService<ITranslationSourceFactory>(),
            sp.GetRequiredService<ILayoutCheckService>(),
            sp.GetRequiredService<ITranslationService>(),
            sp.GetRequiredService<IGlossaryService>(),
            sp.GetRequiredService<Func<ConfigForm>>(),
            sp.GetRequiredService<Func<GlossaryForm>>(),
            sp.GetRequiredService<Func<GlossaryExtractionDialog>>()));

        using var serviceProvider = services.BuildServiceProvider();
        Application.Run(serviceProvider.GetRequiredService<MainForm>());
    }
}
