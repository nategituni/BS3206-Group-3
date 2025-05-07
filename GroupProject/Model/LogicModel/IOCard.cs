namespace GroupProject.Model.LogicModel
{
	public class IOCard : Card, IOutputProvider
	{
		private bool _input;
		public bool Output
		{
			get { return _input; }
			private set { _input = value; }
		}

		public void SetValue(bool value)
		{
			Output = value;
		}
	}
}