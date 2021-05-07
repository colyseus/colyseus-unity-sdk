using UnityEngine;

public class DontDestroyTag : MonoBehaviour
{
    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }
}