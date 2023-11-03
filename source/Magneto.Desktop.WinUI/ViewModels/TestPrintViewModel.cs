using CommunityToolkit.Mvvm.ComponentModel;
using Magneto.Desktop.WinUI.Core.Services;
using Microsoft.UI.Xaml;

namespace Magneto.Desktop.WinUI.ViewModels;

public class TestPrintViewModel : ObservableRecipient
{
    private string _distanceText;

    private double _distance;

    public string DistanceText
    {
        get
        {
            return _distanceText;
        }
        set
        {
            _distanceText = value;
            OnPropertyChanged(nameof(DistanceText)); // Implement INotifyPropertyChanged
        }
    }

    public TestPrintViewModel()
    {
        // Set default distance to move motors
        SetDistance(10);
    }

    private void SetDistance(int distance)
    {
        _distance = distance;
        _distanceText = distance.ToString();
    }

    
}
