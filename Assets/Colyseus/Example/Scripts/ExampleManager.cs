using System;
using System.Collections.Generic;
using Colyseus;
using LucidSightTools;
using UnityEngine;

public class ExampleManager : ColyseusManager<ExampleManager>
{
    public delegate void OnRoomsReceived(ColyseusRoomAvailable[] rooms);

    public static OnRoomsReceived onRoomsReceived;
    private ExampleNetworkedEntityFactory _networkedEntityFactory;

    [SerializeField]
    private ExampleRoomController _roomController;

    public bool autoJoinRoom = true;

    /// <summary>
    ///     Returns a reference to the current networked user.
    /// </summary>
    public ExampleNetworkedEntity CurrentNetworkedEntity;

    private bool isInitialized;

    /// <summary>
    ///     Returns true if there is an active room.
    /// </summary>
    public bool IsInRoom
    {
        get { return _roomController.Room != null; }
    }

    /// <summary>
    ///     Returns the synchronized time from the server in milliseconds.
    /// </summary>
    public double GetServerTime
    {
        get { return _roomController.GetServerTime; }
    }

    /// <summary>
    ///     Returns the synchronized time from the server in seconds.
    /// </summary>
    public double GetServerTimeSeconds
    {
        get { return _roomController.GetServerTimeSeconds; }
    }

    /// <summary>
    ///     The latency in milliseconds between client and server.
    /// </summary>
    public double GetRoundtripTime
    {
        get { return _roomController.GetRoundtripTime; }
    }

    /// <summary>
    ///     Returns a reference to the current networked user.
    /// </summary>
    public ExampleNetworkedUser CurrentUser
    {
        get { return _roomController.CurrentNetworkedUser; }
    }

    public static bool IsReady
    {
        get
        {
            return Instance != null; // && Instance.client != null;
        }
    }

    private string userName;

    /// <summary>
    ///     The display name for the user
    /// </summary>
    public string UserName
    {
        get { return userName; }
        set { userName = value; }
    }

    /// <summary>
    ///     <see cref="MonoBehaviour" /> callback when a script is enabled just before any of the Update methods are called the
    ///     first time.
    /// </summary>
    protected override void Start()
    {
        // For this example we're going to set the target frame rate
        // and allow the app to run in the background for continuous testing.
        Application.targetFrameRate = 60;
        Application.runInBackground = true;
        
    }

    public void BeginServerConnection()
    {
        ConnectToServer();
    }

    public void Initialize(string roomName, Dictionary<string, object> roomOptions)
    {
        if (isInitialized)
        {
            return;
        }

        isInitialized = true;
        // Set up room controller
        _roomController = new ExampleRoomController {roomName = roomName};
        _roomController.SetRoomOptions(roomOptions);
        _roomController.SetDependencies(_colyseusSettings);
        // Set up Networked Entity Factory
        _networkedEntityFactory = new ExampleNetworkedEntityFactory(_roomController.CreationCallbacks,
            _roomController.Entities, _roomController.EntityViews);
    }

    /// <summary>
    ///     Connect to the Colyseus server and either join or create a room.
    /// </summary>
    protected override void ConnectToServer()
    {
        base.ConnectToServer();

        _roomController.SetClient(client);
        if (autoJoinRoom)
        {
            _roomController.JoinOrCreateRoom();
        }
    }

    /// <summary>
    ///     Frame-rate independent message for physics calculations.
    /// </summary>
    protected override void FixedUpdate()
    {
        base.FixedUpdate();

        _roomController.IncrementServerTime();
    }

    public async void GetAvailableRooms()
    {
        ColyseusRoomAvailable[] rooms = await client.GetAvailableRooms(_roomController.roomName);

        onRoomsReceived?.Invoke(rooms);
    }

    public async void JoinExistingRoom(string roomID)
    {
        await _roomController.JoinRoomId(roomID);
    }

    public async void CreateNewRoom(string roomID)
    {
        await _roomController.CreateSpecificRoom(client, _roomController.roomName, roomID);
    }

    public async void LeaveAllRooms(Action onLeave)
    {
        await _roomController.LeaveAllRooms(true, onLeave);
    }

    /// <summary>
    ///     Checks if a <see cref="ColyseusNetworkedEntityView" /> exists for
    ///     the given ID.
    /// </summary>
    /// <param name="entityId">The ID of the <see cref="ExampleNetworkedEntity" /> we're checking for.</param>
    /// <returns></returns>
    public bool HasEntityView(string entityId)
    {
        return _roomController.HasEntityView(entityId);
    }

    /// <summary>
    ///     Returns a <see cref="ExampleNetworkedEntityView" /> given <see cref="entityId" />
    /// </summary>
    /// <param name="entityId"></param>
    /// <returns>
    ///     Returns <see cref="ExampleNetworkedEntityView" /> if one exists for the given <see cref="entityId" />
    /// </returns>
    public ExampleNetworkedEntityView GetEntityView(string entityId)
    {
        return _roomController.GetEntityView(entityId);
    }

    /// <summary>
    ///     On detection of <see cref="OnApplicationQuit" /> will disconnect
    ///     from all <see cref="rooms" />.
    /// </summary>
    private void CleanUpOnAppQuit()
    {
        if (client == null)
        {
            return;
        }

        _roomController.CleanUp();
    }

    /// <summary>
    ///     <see cref="MonoBehaviour" /> callback that gets called just before app exit.
    /// </summary>
    protected override void OnApplicationQuit()
    {
        base.OnApplicationQuit();

        _roomController.LeaveAllRooms(true);

        CleanUpOnAppQuit();
    }

#if UNITY_EDITOR
    public void OnEditorQuit()
    {
        OnApplicationQuit();
    }
#endif

#region Remote Function Call

    /// <summary>
    ///     Send a Remote Function Call
    /// </summary>
    /// <param name="entity">The entity we want to send the RFC</param>
    /// <param name="function">The name of the function to call</param>
    /// <param name="param">The parameters of the function to call</param>
    /// <param name="target">Who should receive this RFC</param>
    public static void RFC(ColyseusNetworkedEntityView entity, string function, object[] param,
        ExampleRFCTargets target = ExampleRFCTargets.ALL)
    {
        RFC(entity.Id, function, param, target);
    }

    /// <summary>
    ///     Send a Remote Function Call
    /// </summary>
    /// <param name="entityId">The ID of the entity we want to send the RFC</param>
    /// <param name="function">The name of the function to call</param>
    /// <param name="param">The parameters of the function to call</param>
    /// <param name="target">Who should receive this RFC</param>
    public static void RFC(string entityId, string function, object[] param,
        ExampleRFCTargets target = ExampleRFCTargets.ALL)
    {
        NetSend("remoteFunctionCall",
            new ExampleRFCMessage {entityId = entityId, function = function, param = param, target = target});
    }

    public static void CustomServerMethod(string methodName, object[] param)
    {
        NetSend("customMethod", new ExampleCustomMethodMessage {method = methodName, param = param});
    }

    /// <summary>
    ///     Send an action and message object to the room.
    /// </summary>
    /// <param name="action">The action to take</param>
    /// <param name="message">The message object to pass along to the room</param>
    public static void NetSend(string action, object message = null)
    {
        if (Instance._roomController.Room == null)
        {
            LSLog.LogError($"Error: Not in room for action {action} msg {message}");
            return;
        }

        _ = message == null
            ? Instance._roomController.Room.Send(action)
            : Instance._roomController.Room.Send(action, message);
    }

    /// <summary>
    ///     Send an action and message object to the room.
    /// </summary>
    /// <param name="actionByte">The action to take</param>
    /// <param name="message">The message object to pass along to the room</param>
    public static void NetSend(byte actionByte, object message = null)
    {
        if (Instance._roomController.Room == null)
        {
            LSLog.LogError(
                $"Error: Not in room for action bytes msg {(message != null ? message.ToString() : "No Message")}");
            return;
        }

        _ = message == null
            ? Instance._roomController.Room.Send(actionByte)
            : Instance._roomController.Room.Send(actionByte, message);
    }

#endregion Remote Function Call

#region Networked Entity Creation

    /// <summary>
    ///     Creates a new <see cref="ExampleNetworkedEntity" /> with the given prefab and attributes.
    /// </summary>
    /// <param name="prefab">Prefab you would like to use to create the entity</param>
    /// <param name="attributes">Entity attributes</param>
    public void InstantiateNetworkedEntity(string prefab, Dictionary<string, object> attributes = null)
    {
        InstantiateNetworkedEntity(prefab, Vector3.zero, Quaternion.identity, attributes);
    }

    /// <summary>
    ///     Creates a new <see cref="ExampleNetworkedEntity" /> with the given prefab and attributes
    ///     and places it at the provided position.
    /// </summary>
    /// <param name="prefab">Prefab you would like to use to create the entity</param>
    /// <param name="position">Position for the new entity</param>
    /// <param name="attributes">Entity attributes</param>
    public static void InstantiateNetworkedEntity(string prefab, Vector3 position,
        Dictionary<string, object> attributes = null)
    {
        Instance._networkedEntityFactory.InstantiateNetworkedEntity(Instance._roomController.Room, prefab, position,
            Quaternion.identity, attributes);
    }

    /// <summary>
    ///     Creates a new <see cref="ExampleNetworkedEntity" /> with the given prefab and attributes
    ///     and places it at the provided position and rotation.
    /// </summary>
    /// <param name="prefab">Prefab you would like to use to create the entity</param>
    /// <param name="position">Position for the new entity</param>
    /// <param name="rotation">Position for the new entity</param>
    /// <param name="attributes">Position for the new entity</param>
    public static void InstantiateNetworkedEntity(string prefab, Vector3 position, Quaternion rotation,
        Dictionary<string, object> attributes = null)
    {
        Instance._networkedEntityFactory.InstantiateNetworkedEntity(Instance._roomController.Room, prefab, position,
            rotation, attributes);
    }

    /// <summary>
    ///     Creates a new <see cref="ExampleNetworkedEntity" /> with the given <see cref="ColyseusNetworkedEntityView" /> and
    ///     attributes
    ///     and places it at the provided position and rotation.
    /// </summary>
    /// <param name="position">Position for the new entity</param>
    /// <param name="rotation">Position for the new entity</param>
    /// <param name="attributes">Position for the new entity</param>
    /// <param name="viewToAssign">
    ///     The provided view that will be assigned to the new <see cref="ExampleNetworkedEntity" />
    /// </param>
    /// <param name="callback">
    ///     Callback that will be invoked with the newly created <see cref="ExampleNetworkedEntity" />
    /// </param>
    public static void CreateNetworkedEntityWithTransform(Vector3 position, Quaternion rotation,
        Dictionary<string, object> attributes = null, ColyseusNetworkedEntityView viewToAssign = null,
        Action<ExampleNetworkedEntity> callback = null)
    {
        Instance._networkedEntityFactory.CreateNetworkedEntityWithTransform(Instance._roomController.Room, position,
            rotation, attributes, viewToAssign, callback);
    }

    /// <summary>
    ///     Creates a new <see cref="ExampleNetworkedEntity" /> with the given prefab, attributes, and
    ///     <see cref="ColyseusNetworkedEntityView" />.
    /// </summary>
    /// <param name="prefab">Prefab you would like to use</param>
    /// <param name="attributes">Position for the new entity</param>
    /// <param name="viewToAssign">
    ///     The provided view that will be assigned to the new <see cref="ExampleNetworkedEntity" />
    /// </param>
    /// <param name="callback">
    ///     Callback that will be invoked with the newly created <see cref="ExampleNetworkedEntity" />
    /// </param>
    public static void CreateNetworkedEntity(string prefab, Dictionary<string, object> attributes = null,
        ColyseusNetworkedEntityView viewToAssign = null, Action<ExampleNetworkedEntity> callback = null)
    {
        Instance._networkedEntityFactory.CreateNetworkedEntity(Instance._roomController.Room, prefab, attributes,
            viewToAssign, callback);
    }

    /// <summary>
    ///     Creates a new <see cref="ExampleNetworkedEntity" /> attributes and <see cref="ColyseusNetworkedEntityView" />.
    /// </summary>
    /// <param name="attributes">Position for the new entity</param>
    /// <param name="viewToAssign">
    ///     The provided view that will be assigned to the new <see cref="ExampleNetworkedEntity" />
    /// </param>
    /// <param name="callback">
    ///     Callback that will be invoked with the newly created <see cref="ExampleNetworkedEntity" />
    /// </param>
    public static void CreateNetworkedEntity(Dictionary<string, object> attributes = null,
        ColyseusNetworkedEntityView viewToAssign = null, Action<ExampleNetworkedEntity> callback = null)
    {
        Instance._networkedEntityFactory.CreateNetworkedEntity(Instance._roomController.Room, attributes, viewToAssign,
            callback);
    }

    /// <summary>
    ///     Registers the <see cref="ColyseusNetworkedEntityView" /> with the manager for tracking.
    ///     <para>Initializes the <see cref="ColyseusNetworkedEntityView" /> if it has not yet been initialized.</para>
    /// </summary>
    /// <param name="model"></param>
    /// <param name="view"></param>
    public void RegisterNetworkedEntityView(ExampleNetworkedEntity model, ColyseusNetworkedEntityView view)
    {
        _networkedEntityFactory.RegisterNetworkedEntityView(model, view);
    }

    /// <summary>
    ///     Creates a GameObject using the <see cref="ColyseusNetworkedEntityView" />'s prefab.
    ///     <para>Requires that the entity has a "prefab" attribute defined.</para>
    /// </summary>
    /// <param name="entity"></param>
    private static async void CreateFromPrefab(ExampleNetworkedEntity entity)
    {
        await Instance._networkedEntityFactory.CreateFromPrefab(entity);
    }

#endregion Networked Entity Creation
}