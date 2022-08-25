using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    private static string gameName = null;
    private static string hostname = null;
    private static string port = null;
    private static bool secureProtocol = false;

    public string GameName
    {
        get => string.IsNullOrEmpty(gameName) ? "my_room" : gameName;
        set => gameName = value;
    }

    public string HostName
    {
        get => string.IsNullOrEmpty(hostname) ? "localhost" : hostname;
        set => hostname = value;
    }

    public string Port
    {
        get => string.IsNullOrEmpty(port) ? "2567" : port;
        set => port = value;
    }

    public string Protocol
    {
        get => secureProtocol ? "wss" : "ws";
        set => secureProtocol = !secureProtocol;
    }

    public string HostAddress => $"{Protocol}://{HostName}:{Port}";

    public void Play()
    {
        SceneManager.LoadScene("Game", LoadSceneMode.Single);
    }
}
