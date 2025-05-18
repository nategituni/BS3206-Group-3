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
    public bool IsLocked { get; }
    public bool ExpectedValue { get; set; }
    private bool _inputValue;




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

   public bool InputValue
{
    get => _inputValue;
    set
    {
        if (_inputValue == value) return;
        _inputValue = value;
        OnPropertyChanged();

        // Update XML state
        UpdateInputCardValue(_inputValue);
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

    public CardViewModel(XmlStateService xmlService, int id, GateTypeEnum gateType, bool isLocked = false, double startX = 50, double startY = 50, bool inputValue = false)
    {
        Id = id;
        GateType = gateType;
        X = startX;
        Y = startY;
        _xmlService = xmlService;
        IsLocked = isLocked;

        CurrentValue = gateType == GateTypeEnum.Output ? "0" : string.Empty;

        if (gateType == GateTypeEnum.Input)
        InputValue = inputValue;
    }


    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public void Delete()
{
    if (IsLocked) return;

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