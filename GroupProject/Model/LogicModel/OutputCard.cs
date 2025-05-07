namespace GroupProject.Model.LogicModel
{
	public class OutputCard : IOCard
	{
		public bool Input1 { get; set; }
		public IOutputProvider Input1Card { get; set; }
	}
}