using System;
using System.Collections;
using System.Runtime.CompilerServices;
using UnityEngine;

public class WaitForUpdate : CustomYieldInstruction
{
	public override bool keepWaiting
	{
		get { return false; }
	}

	public MainThreadAwaiter GetAwaiter()
	{
		var awaiter = new MainThreadAwaiter();
		MainThreadUtil.Run(CoroutineWrapper(this, awaiter));
		return awaiter;
	}

	public class MainThreadAwaiter : INotifyCompletion
	{
		Action continuation;

		public bool IsCompleted { get; set; }

		public void GetResult() { }

		public void Complete()
		{
			IsCompleted = true;
			continuation?.Invoke();
		}

		void INotifyCompletion.OnCompleted(Action continuation)
		{
			this.continuation = continuation;
		}
	}

	public static IEnumerator CoroutineWrapper(IEnumerator theWorker, MainThreadAwaiter awaiter)
	{
		yield return theWorker;
		awaiter.Complete();
	}
}