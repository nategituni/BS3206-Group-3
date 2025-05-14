using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GroupProject.ViewModel
{
    public class CardViewModel : INotifyPropertyChanged
    {
        public int Id { get; set;}
        public string GateType { get; }
        public string DisplayName => $"{GateType}"; // #{Id}"; // Temp remove id

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

			CurrentValue = gateType == "Output" ? "0" : string.Empty;
        }

		// protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
		// {
		// 	PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		// }

        public event PropertyChangedEventHandler PropertyChanged;


		// Output card stuff

		private string _currentValue;
		public string CurrentValue
		{
			get => _currentValue;
			set
			{
				if(_currentValue != value)
				{
					_currentValue = value;
					OnPropertyChanged();
				}
			}
		}

		protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
    }
}