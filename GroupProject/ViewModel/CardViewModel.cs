using System.ComponentModel;
using System.Runtime.CompilerServices;
using GroupProject.Model.LogicModel;
using GroupProject.Services;

namespace GroupProject.ViewModel;

public class CardViewModel : INotifyPropertyChanged
{
    private readonly XmlStateService _xmlService;
    public int Id { get; set; }
    public GateTypeEnum GateType { get; }

    private string? _currentValue;
    public string DisplayName => $"{GateType}";

    public string? CurrentValue
    {
        get => _currentValue;
        set
        {
            if (_currentValue == value) return;

            _currentValue = value;
            OnPropertyChanged();
        }
    }

    private double _x, _y;

    public double X
    {
        get => _x;
        set
        {
            _x = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(X)));
        }
    }

    public double Y
    {
        get => _y;
        set
        {
            _y = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Y)));
        }
    }

    public CardViewModel(XmlStateService xmlService, int id, GateTypeEnum gateType, double startX = 50,
        double startY = 50)
    {
        Id = id;
        GateType = gateType;
        X = startX;
        Y = startY;
        _xmlService = xmlService;

        CurrentValue = gateType == GateTypeEnum.Output ? "0" : string.Empty;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public void Delete()
    {
        switch (GateType)
        {
            case GateTypeEnum.Input:
                _xmlService.DeleteInputCard(Id);
                break;
            case GateTypeEnum.Output:
                _xmlService.DeleteOutputCard(Id);
                break;
            default:
                _xmlService.DeleteLogicGateCard(Id);
                break;
        }
    }

    public void UpdateInputCardValue(bool value)
    {
        _xmlService.UpdateInputCardValue(Id, value);
    }
}