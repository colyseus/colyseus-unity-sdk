using Cysharp.Threading.Tasks;

namespace NativeWebSocket
{
	public class WaitForBackgroundThread
	{
		public UniTask.Awaiter GetAwaiter()
		{
			return UniTask.RunOnThreadPool(() => { }).GetAwaiter();
		}
	}
}