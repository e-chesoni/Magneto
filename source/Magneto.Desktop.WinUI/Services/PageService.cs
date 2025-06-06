using CommunityToolkit.Mvvm.ComponentModel;

using Magneto.Desktop.WinUI.Contracts.Services;
using Magneto.Desktop.WinUI.ViewModels;
using Magneto.Desktop.WinUI.Views;

using Microsoft.UI.Xaml.Controls;

namespace Magneto.Desktop.WinUI.Services;

public class PageService : IPageService
{
    private readonly Dictionary<string, Type> _pages = new();

    public PageService()
    {
        Configure<MainViewModel, MainPage>();
        Configure<MainDetailViewModel, MainDetailPage>();
        Configure<NewPrintViewModel, NewPrintPage>();
        Configure<PrintingViewModel, PrintingPage>();
        Configure<TestingViewModel, TestingPage>();
        Configure<TestingDetailViewModel, TestingDetailPage>();
        Configure<PrintSettingsViewModel, PrintSettingsPage>();
        Configure<MaintenanceViewModel, MaintenancePage>();
        Configure<CleaningViewModel, CleaningPage>();
        Configure<TestPrintViewModel, TestPrintPage>();
        Configure<TestMotorsViewModel, TestMotorsPage>();
        Configure<MonitorViewModel, MonitorPage>();
        Configure<MonitorDetailViewModel, MonitorDetailPage>();
        Configure<LaserMonitorViewModel, LaserMonitorPage>();
        Configure<ArgonMonitorViewModel, ArgonMonitorPage>();
        Configure<MaterialsMonitorViewModel, MaterialsMonitorPage>();
        Configure<PrintingHistoryViewModel, PrintingHistoryPage>();
        Configure<SettingsViewModel, SettingsPage>();
    }

    public Type GetPageType(string key)
    {
        Type? pageType;
        lock (_pages)
        {
            if (!_pages.TryGetValue(key, out pageType))
            {
                throw new ArgumentException($"Page not found: {key}. Did you forget to call PageService.Configure?");
            }
        }

        return pageType;
    }

    private void Configure<VM, V>()
        where VM : ObservableObject
        where V : Page
    {
        lock (_pages)
        {
            var key = typeof(VM).FullName!;
            if (_pages.ContainsKey(key))
            {
                throw new ArgumentException($"The key {key} is already configured in PageService");
            }

            var type = typeof(V);
            if (_pages.Any(p => p.Value == type))
            {
                throw new ArgumentException($"This type is already configured with key {_pages.First(p => p.Value == type).Key}");
            }

            _pages.Add(key, type);
        }
    }
}
