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

using Microsoft.Extensions.Configuration;
using Windows.Storage;
using Magneto.Desktop.WinUI.Core;
using System.Reflection;
using Magneto.Desktop.WinUI.Core.Contracts.Services.Database.Seeders;
using Magneto.Desktop.WinUI.Core.Contracts.Services.Database;
using Magneto.Desktop.WinUI.Core.Services.Database.Seeders;
using MongoDB.Driver;
using Magneto.Desktop.WinUI.Core.Services.Database;
using Magneto.Desktop.WinUI.Core.Models.Print;
using Magneto.Desktop.WinUI.Core.Models.Controllers;
using Magneto.Desktop.WinUI.Core.Contracts.Services.Controllers;
using Magneto.Desktop.WinUI.Core.Factories;
using Magneto.Desktop.WinUI.Core.Models;
using Magneto.Desktop.WinUI.Core.Models.Motors;

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

    public static WindowEx MainWindow { get; private set; }

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
            services.AddSingleton<ISamplePrintService, SamplePrintService>();
            services.AddSingleton<IFileService, FileService>();

            // Motors
            services.AddSingleton(MotorFactory.CreateMotor("powder"));
            services.AddSingleton(MotorFactory.CreateMotor("build"));
            services.AddSingleton(MotorFactory.CreateMotor("sweep"));

            // Controllers
            services.AddSingleton<BuildMotorController>(provider =>
                new BuildMotorController(
                    provider.GetServices<StepperMotor>().First(m => m.GetMotorName() == "powder"),
                    provider.GetServices<StepperMotor>().First(m => m.GetMotorName() == "build")
                )
            );

            services.AddSingleton<SweepMotorController>(provider =>
                new SweepMotorController(
                    provider.GetServices<StepperMotor>().First(m => m.GetMotorName() == "sweep")
                )
            );

            // Laser
            services.AddSingleton<LaserController>();

            // ActuationManager
            services.AddSingleton<ProgramsManager>(provider =>
                new ProgramsManager(
                    provider.GetRequiredService<BuildMotorController>(),
                    provider.GetRequiredService<SweepMotorController>(),
                    provider.GetRequiredService<LaserController>()
                )
            );

            // MissionControl
            services.AddSingleton<MissionControl>(provider =>
                new MissionControl(provider.GetRequiredService<ProgramsManager>())
            );


            // Register MissionControl
            services.AddSingleton<MissionControl>(provider =>
            {
                var am = provider.GetRequiredService<ProgramsManager>();
                return new MissionControl(am);
            });

            // Register MotorService (needs ActuationManager)
            services.AddSingleton<IMotorService, MotorService>(provider =>
            {
                var am = provider.GetRequiredService<ProgramsManager>();
                var ms = new MotorService(am);
                ms.HandleStartUp(); // Now that motors/controllers are fully built
                return ms;
            });

            // Peripheral Services
            services.AddSingleton<IWaverunnerService, WaverunerService>();
            services.AddSingleton<IMotorService, MotorService>();

            // MongoDb Services
            services.AddSingleton<IMongoClient>(_ => new MongoClient("mongodb://localhost:27017"));
            services.AddSingleton<IMongoDbService, MongoDbService>();
            services.AddSingleton<IPrintService, PrintService>();
            services.AddSingleton<ISliceService, SliceService>();
            services.AddSingleton<IMongoDbSeeder, MongoDbSeeder>();
            services.AddSingleton<IPrintSeeder, PrintSeeder>();

            // Create a scope and call the seeding method to add prints to the db
            var serviceProvider = services.BuildServiceProvider();
            var mongoDbSeeder = serviceProvider.GetRequiredService<IMongoDbSeeder>();

            // Seed or clear magnetoDb
            Task.Run(async () =>
            {
                // WARNING: only run one of these
                await mongoDbSeeder.ClearDatabaseAsync(true);
                //await mongoDbSeeder.SeedDatabaseAsync();
            });

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
            services.AddTransient<TestMotorsViewModel>();
            services.AddTransient<TestMotorsPage>();
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

        MainWindow = new MainWindow();
        MainWindow.Activate();

        await App.GetService<IActivationService>().ActivateAsync(args);
    }
}
