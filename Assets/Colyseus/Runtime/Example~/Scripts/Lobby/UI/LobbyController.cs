using System;
using System.Collections;
using System.Collections.Generic;
using Colyseus;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LobbyController : MonoBehaviour
{
    [SerializeField]
    private GameObject connectingCover = null;

    [SerializeField]
    private CreateUserMenu createUserMenu = null;

    public int minRequiredPlayers = 2;

    //Variables to initialize the room controller
    public string roomName = "my_room";
    public string gameSceneName = "ExampleScene";
    [SerializeField]
    private RoomSelectionMenu selectRoomMenu = null;

    private void Awake()
    {
        createUserMenu.gameObject.SetActive(true);
        selectRoomMenu.gameObject.SetActive(false);
        connectingCover.SetActive(true);
    }

    private IEnumerator Start()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        while (!ExampleManager.IsReady)
        {
            yield return new WaitForEndOfFrame();
        }

        Dictionary<string, object> roomOptions = new Dictionary<string, object>
        {
            ["YOUR_ROOM_OPTION_1"] = "option 1",
            ["YOUR_ROOM_OPTION_2"] = "option 2"
        };

        ExampleManager.Instance.Initialize(roomName, roomOptions);
        ExampleManager.onRoomsReceived += OnRoomsReceived;
        connectingCover.SetActive(false);
    }

    private void OnDestroy()
    {
        ExampleManager.onRoomsReceived -= OnRoomsReceived;
    }

    public void CreateUser()
    {
        string desiredUserName = createUserMenu.UserName;
        PlayerPrefs.SetString("UserName", desiredUserName);

        ColyseusSettings clonedSettings = ExampleManager.Instance.CloneSettings();
        clonedSettings.colyseusServerAddress = createUserMenu.ServerURL;
        clonedSettings.colyseusServerPort = createUserMenu.ServerPort;
        clonedSettings.useSecureProtocol= createUserMenu.UseSecure;

        ExampleManager.Instance.OverrideSettings(clonedSettings);

        ExampleManager.Instance.BeginServerConnection();

        ExampleManager.Instance.UserName = desiredUserName;

        //Do user creation stuff
        createUserMenu.gameObject.SetActive(false);
        selectRoomMenu.gameObject.SetActive(true);
        selectRoomMenu.GetAvailableRooms();
    }

    public void CreateRoom()
    {
        connectingCover.SetActive(true);
        string desiredRoomName = selectRoomMenu.RoomCreationName;
        LoadGallery(() => { ExampleManager.Instance.CreateNewRoom(desiredRoomName); });
    }

    public void JoinRoom(string id)
    {
        connectingCover.SetActive(true);
        LoadGallery(() => { ExampleManager.Instance.JoinExistingRoom(id); });
    }

    public void OnConnectedToServer()
    {
        connectingCover.SetActive(false);
    }

    private void OnRoomsReceived(ColyseusRoomAvailable[] rooms)
    {
        selectRoomMenu.HandRooms(rooms);
    }

    private void LoadGallery(Action onComplete)
    {
        StartCoroutine(LoadSceneAsync(gameSceneName, onComplete));
    }

    private IEnumerator LoadSceneAsync(string scene, Action onComplete)
    {
        Scene currScene = SceneManager.GetActiveScene();
        AsyncOperation op = SceneManager.LoadSceneAsync(scene, LoadSceneMode.Additive);
        while (op.progress <= 0.9f)
        {
            //Wait until the scene is loaded
            yield return new WaitForEndOfFrame();
        }

        onComplete.Invoke();
        op.allowSceneActivation = true;
        SceneManager.UnloadSceneAsync(currScene);
    }
}