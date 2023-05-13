using Magneto.Desktop.WinUI.Activation;
using Magneto.Desktop.WinUI.Contracts.Services;
using Magneto.Desktop.WinUI.Core.Contracts.Services;
using Magneto.Desktop.WinUI.Core.Services;
using Magneto.Desktop.WinUI.Helpers;
using Magneto.Desktop.WinUI.Models;
using Magneto.Desktop.WinUI.Notifications;
using Magneto.Desktop.WinUI.Services;
using Magneto.Desktop.WinUI.ViewModels;
using Magneto.Desktop.WinUI.Views;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;

namespace Magneto.Desktop.WinUI;

// To learn more about WinUI 3, see https://docs.microsoft.com/windows/apps/winui/winui3/.
public partial class App : Application
{
    // The .NET Generic Host provides dependency injection, configuration, logging, and other services.
    // https://docs.microsoft.com/dotnet/core/extensions/generic-host
    // https://docs.microsoft.com/dotnet/core/extensions/dependency-injection
    // https://docs.microsoft.com/dotnet/core/extensions/configuration
    // https://docs.microsoft.com/dotnet/core/extensions/logging
    public IHost Host
    {
        get;
    }

    public static T GetService<T>()
        where T : class
    {
        if ((App.Current as App)!.Host.Services.GetService(typeof(T)) is not T service)
        {
            throw new ArgumentException($"{typeof(T)} needs to be registered in ConfigureServices within App.xaml.cs.");
        }

        return service;
    }

    public static WindowEx MainWindow { get; } = new MainWindow();

    public App()
    {
        InitializeComponent();

        Host = Microsoft.Extensions.Hosting.Host.
        CreateDefaultBuilder().
        UseContentRoot(AppContext.BaseDirectory).
        ConfigureServices((context, services) =>
        {
            // Default Activation Handler
            services.AddTransient<ActivationHandler<LaunchActivatedEventArgs>, DefaultActivationHandler>();

            // Other Activation Handlers
            services.AddTransient<IActivationHandler, AppNotificationActivationHandler>();

            // Services
            services.AddSingleton<IAppNotificationService, AppNotificationService>();
            services.AddSingleton<ILocalSettingsService, LocalSettingsService>();
            services.AddSingleton<IThemeSelectorService, ThemeSelectorService>();
            services.AddTransient<INavigationViewService, NavigationViewService>();

            services.AddSingleton<IActivationService, ActivationService>();
            services.AddSingleton<IPageService, PageService>();
            services.AddSingleton<INavigationService, NavigationService>();

            // Core Services
            services.AddSingleton<ISampleDataService, SampleDataService>();
            services.AddSingleton<IFileService, FileService>();

            // Views and ViewModels
            services.AddTransient<SettingsViewModel>();
            services.AddTransient<SettingsPage>();
            services.AddTransient<CompletedPrintsViewModel>();
            services.AddTransient<CompletedPrintsPage>();
            services.AddTransient<MaterialsMonitorViewModel>();
            services.AddTransient<MaterialsMonitorPage>();
            services.AddTransient<ArgonMonitorViewModel>();
            services.AddTransient<ArgonMonitorPage>();
            services.AddTransient<LaserMonitorViewModel>();
            services.AddTransient<LaserMonitorPage>();
            services.AddTransient<MonitorDetailViewModel>();
            services.AddTransient<MonitorDetailPage>();
            services.AddTransient<MonitorViewModel>();
            services.AddTransient<MonitorPage>();
            services.AddTransient<TestPrintViewModel>();
            services.AddTransient<TestPrintPage>();
            services.AddTransient<CleaningViewModel>();
            services.AddTransient<CleaningPage>();
            services.AddTransient<PrintQueueViewModel>();
            services.AddTransient<PrintQueuePage>();
            services.AddTransient<PrintSettingsViewModel>();
            services.AddTransient<PrintSettingsPage>();
            services.AddTransient<UtilitiesDetailViewModel>();
            services.AddTransient<UtilitiesDetailPage>();
            services.AddTransient<UtilitiesViewModel>();
            services.AddTransient<UtilitiesPage>();
            services.AddTransient<PrintingViewModel>();
            services.AddTransient<PrintingPage>();
            services.AddTransient<PrintViewModel>();
            services.AddTransient<PrintPage>();
            services.AddTransient<MainDetailViewModel>();
            services.AddTransient<MainDetailPage>();
            services.AddTransient<MainViewModel>();
            services.AddTransient<MainPage>();
            services.AddTransient<ShellPage>();
            services.AddTransient<ShellViewModel>();

            // Configuration
            services.Configure<LocalSettingsOptions>(context.Configuration.GetSection(nameof(LocalSettingsOptions)));
        }).
        Build();

        App.GetService<IAppNotificationService>().Initialize();

        UnhandledException += App_UnhandledException;
    }

    private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        // TODO: Log and handle exceptions as appropriate.
        // https://docs.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.application.unhandledexception.
    }

    protected async override void OnLaunched(LaunchActivatedEventArgs args)
    {
        base.OnLaunched(args);

        App.GetService<IAppNotificationService>().Show(string.Format("AppNotificationSamplePayload".GetLocalized(), AppContext.BaseDirectory));

        await App.GetService<IActivationService>().ActivateAsync(args);
    }
}
