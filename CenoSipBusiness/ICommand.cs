namespace CenoSipBusiness
{
	public interface ICommand
	{
		string commandstr { get; }
		void commandmethod();
	}
}