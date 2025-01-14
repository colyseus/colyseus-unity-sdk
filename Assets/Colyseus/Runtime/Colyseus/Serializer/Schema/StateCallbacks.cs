
namespace Colyseus.Schema
{
	public class Callbacks<T> where T : Schema
	{
		T state;

		public Callbacks(T state)
		{
			this.state = state;
		}

		public static Callbacks<T> Get(T state)
		{
			return new Callbacks<T>(state);
		}
	}
}
