using CommunityToolkit.Mvvm.ComponentModel;
using Magneto.Desktop.WinUI.Core.Services;
using Microsoft.UI.Xaml;

namespace Magneto.Desktop.WinUI.ViewModels;

public class TestPrintViewModel : ObservableRecipient
{
    private string? _distanceText;

    private double? _distance;

    private string? _positionText;

    private double? _position;

    public TestPrintViewModel()
    {
        // Set default distance to move motors
        SetDistance(10);
    }

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

    public string PositionText
    {
        get
        {
            return _positionText;
        }
        set
        {
            _positionText = value;
            OnPropertyChanged(nameof(PositionText)); // Implement INotifyPropertyChanged
        }
    }

    private void SetDistance(int distance)
    {
        _distance = distance;
        _distanceText = distance.ToString();
    }

}
