using UnityEngine;

public interface IAssetProviderService : IService
{
    public T Instantiate<T>(string path, Vector3 at) where T : Object;
    public T Instantiate<T>(string path) where T : Object;
}