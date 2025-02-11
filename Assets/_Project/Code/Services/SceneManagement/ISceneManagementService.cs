using System;
using Cysharp.Threading.Tasks;

public interface ISceneManagementService : IService
{
    public UniTask LoadAsync(string sceneName, Action onLoaded = null);
}
