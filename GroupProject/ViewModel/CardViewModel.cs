using System.ComponentModel;

namespace GroupProject.ViewModel
{
    public class CardViewModel : INotifyPropertyChanged
    {
        public int Id { get; }
        public string GateType { get; }
        public string DisplayName => $"{GateType} #{Id}";

        double _x;
        public double X { get => _x; set { _x = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(X))); } }

        double _y;
        public double Y { get => _y; set { _y = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Y))); } }

        public CardViewModel(int id, string gateType, double startX = 50, double startY = 50)
        {
            Id = id;
            GateType = gateType;
            X = startX;
            Y = startY;
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}