namespace GroupProject.Model.LogicModel
{
	public interface IOutputProvider
	{
		bool Output { get; }
		int Id { get; set; }
	}
}