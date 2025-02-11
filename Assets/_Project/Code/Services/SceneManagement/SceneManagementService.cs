using System;
using System.Collections;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class SceneManagementService : ISceneManagementService
{
    private static IEnumerator LoadScene(string nextScene, Action onLoaded = null)
    {
        AsyncOperation waitNextScene = SceneManager.LoadSceneAsync(nextScene);

        while (!waitNextScene.isDone)
        {
            yield return null;
        }

        onLoaded?.Invoke();
    }

    public UniTask LoadAsync(string name, Action onLoaded = null)
    {
        MonoBehaviour sceneContext = UnityEngine.Object.FindFirstObjectByType<InitialScope>(); // MonoBehaviour
        sceneContext.StartCoroutine(LoadScene(name, onLoaded));

        return UniTask.CompletedTask;
    }
}