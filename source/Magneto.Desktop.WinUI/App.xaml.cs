﻿using Magneto.Desktop.WinUI.Activation;
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
using Magneto.Desktop.WinUI.Core.Models.States.PrintStates;
using Microsoft.UI.Xaml.Documents;

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
    private bool CLEAR_DB_ON_STARTUP = false;

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

            // ✅ Register RoutineStateMachine first
            services.AddSingleton<RoutineStateMachine>(provider =>
            {
                var build = provider.GetRequiredService<BuildMotorController>();
                var sweep = provider.GetRequiredService<SweepMotorController>();
                var laser = provider.GetRequiredService<LaserController>();
                return new RoutineStateMachine(build, sweep, laser);
            });

            // ✅ Register MotorService BEFORE PrintStateMachine
            services.AddSingleton<MotorService>();
            services.AddSingleton<IMotorService>(provider => provider.GetRequiredService<MotorService>());

            // ✅ Now register PrintStateMachine — safe to inject MotorService now
            services.AddSingleton<PrintStateMachine>(provider =>
            {
                var seeder = provider.GetRequiredService<IPrintSeeder>();
                var printService = provider.GetRequiredService<IPrintService>();
                var sliceService = provider.GetRequiredService<ISliceService>();
                var rsm = provider.GetRequiredService<RoutineStateMachine>();
                var motorService = provider.GetRequiredService<MotorService>();

                return new PrintStateMachine(seeder, printService, sliceService, rsm, motorService);
            });


            // MissionControl (which now contains PrintStateMachine)
            services.AddSingleton<MissionControl>(provider =>
            {
                var psm = provider.GetRequiredService<PrintStateMachine>();
                return new MissionControl(psm);
            });

            // Peripheral Services
            services.AddSingleton<IWaverunnerService, WaverunnerService>();
            services.AddSingleton<MotorService>(provider =>
            {
                var rsm = provider.GetRequiredService<RoutineStateMachine>();
                return new MotorService(rsm);
            });

            // MongoDb Services
            services.AddSingleton<IMongoClient>(_ => new MongoClient("mongodb://localhost:27017"));
            services.AddSingleton<IMongoDbService, MongoDbService>();
            services.AddSingleton<IPrintService, PrintService>();
            services.AddSingleton<ISliceService, SliceService>();
            services.AddSingleton<IMongoDbSeeder, MongoDbSeeder>();
            services.AddSingleton<IPrintSeeder, PrintSeeder>();

            
            if (CLEAR_DB_ON_STARTUP)
            {
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
            }

            // Views and ViewModels
            services.AddTransient<SettingsViewModel>();
            services.AddTransient<SettingsPage>();
            services.AddTransient<PrintingHistoryViewModel>();
            services.AddTransient<PrintingHistoryPage>();
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
            services.AddTransient<MaintenanceViewModel>();
            services.AddTransient<MaintenancePage>();
            services.AddTransient<PrintSettingsViewModel>();
            services.AddTransient<PrintSettingsPage>();
            services.AddTransient<TestingDetailViewModel>();
            services.AddTransient<TestingDetailPage>();
            services.AddTransient<TestingViewModel>();
            services.AddTransient<TestingPage>();
            services.AddTransient<PrintingHistoryViewModel>();
            services.AddTransient<PrintingHistoryPage>();
            services.AddTransient<PrintingViewModel>();
            services.AddTransient<PrintingPage>();
            services.AddTransient<NewPrintViewModel>();
            services.AddTransient<NewPrintPage>();
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
