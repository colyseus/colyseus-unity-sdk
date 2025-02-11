using System;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// The Resources folder is good for quick prototyping, but it performs poorly with memory management. 
/// Therefore, it is considered good practice to use Addressables for managing game assets.
/// </summary>
public sealed class AssetProviderService : IAssetProviderService
{
    public T Instantiate<T>(string path, Vector3 at) where T : Object
    {
        var prefab = Resources.Load<T>(path);

        if (prefab == null)
        {
            throw new ArgumentException($"Resource not found at path: {path}");
        }

        return Object.Instantiate(prefab, at, Quaternion.identity);
    }

    public T Instantiate<T>(string path) where T : Object
    {
        var prefab = Resources.Load<T>(path);

        if (prefab == null)
        {
            throw new ArgumentException($"Resource not found at path: {path}");
        }

        return Object.Instantiate(prefab);
    }
}