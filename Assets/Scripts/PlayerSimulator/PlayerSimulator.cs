using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using UnityEngine.SceneManagement; // For scene management

public class PlayerSimulator : MonoBehaviourPunCallbacks
{
    [SerializeField] private int numberOfPlayers = 18; // Reduced to 18 to stay under 20 CCU (including the local player)
    private List<SimulatedClient> simulatedClients = new List<SimulatedClient>();
    private string roomName = "GameRoom_3418"; // Match the room name used by Player 1

    void Start()
    {
        // Ensure Photon is not already connected
        if (PhotonNetwork.IsConnected)
        {
            Debug.LogWarning("Photon is already connected. Disconnecting...");
            PhotonNetwork.Disconnect();
        }

        // Start simulation
        Debug.Log($"[PlayerSimulator] Starting simulation for {numberOfPlayers} players...");
        SimulatePlayers();
    }

    void Update()
    {
        // Update each simulated client to process network events
        foreach (var client in simulatedClients)
        {
            client.Update();
        }
    }

    private void SimulatePlayers()
    {
        for (int i = 0; i < numberOfPlayers; i++)
        {
            SimulatedClient client = new SimulatedClient(i + 1, roomName);
            simulatedClients.Add(client);
            client.Connect();
        }
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.Log($"[PlayerSimulator] Disconnected: {cause}");
        if (cause == DisconnectCause.MaxCcuReached)
        {
            Debug.LogWarning("Max CCU reached. Returning to Main Menu...");
            SceneManager.LoadScene("MainMenu"); // Return to main menu instead of crashing
        }
    }
}

[System.Serializable]
public class SimulatedClient : IConnectionCallbacks, IMatchmakingCallbacks
{
    private LoadBalancingClient client;
    private int clientId;
    private string targetRoomName;

    public SimulatedClient(int id, string roomName)
    {
        clientId = id;
        targetRoomName = roomName;
        client = new LoadBalancingClient();
        client.AddCallbackTarget(this);

        if (PhotonNetwork.PhotonServerSettings == null)
        {
            Debug.LogError("PhotonServerSettings is not set up! Please configure it in PhotonServerSettings.");
            return;
        }

        client.AppId = PhotonNetwork.PhotonServerSettings.AppSettings.AppIdRealtime;
        client.AppVersion = PhotonNetwork.AppVersion;
        Debug.Log($"[SimulatedClient {clientId}] Initialized with AppId: {client.AppId}");
    }

    public void Connect()
    {
        Debug.Log($"[SimulatedClient {clientId}] Connecting to Photon...");
        client.ConnectToNameServer();
    }

    public void Update()
    {
        // Process network events
        if (client != null)
        {
            client.Service();
        }
    }

    // IConnectionCallbacks
    public void OnConnected()
    {
        Debug.Log($"[SimulatedClient {clientId}] Connected to Name Server.");
    }

    public void OnConnectedToMaster()
    {
        Debug.Log($"[SimulatedClient {clientId}] Connected to Master Server. Joining lobby...");
        client.OpJoinLobby(null);
    }

    public void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogError($"[SimulatedClient {clientId}] Disconnected: {cause}");
        if (cause == DisconnectCause.MaxCcuReached)
        {
            Debug.LogWarning($"[SimulatedClient {clientId}] Max CCU reached. Consider reducing the number of simulated clients or upgrading your Photon plan.");
            // Optionally, stop further connection attempts for this client
            client.RemoveCallbackTarget(this);
            client = null;
        }
    }

    public void OnRegionListReceived(RegionHandler regionHandler)
    {
        Debug.Log($"[SimulatedClient {clientId}] Received region list: {string.Join(", ", regionHandler.EnabledRegions)}");
    }

    public void OnCustomAuthenticationResponse(Dictionary<string, object> data) { }
    public void OnCustomAuthenticationFailed(string debugMessage) { }

    // IMatchmakingCallbacks
    public void OnJoinedLobby()
    {
        Debug.Log($"[SimulatedClient {clientId}] Joined lobby. Trying to join room '{targetRoomName}'...");
        EnterRoomParams enterRoomParams = new EnterRoomParams
        {
            RoomName = targetRoomName,
            RoomOptions = new RoomOptions { MaxPlayers = 20 },
            Lobby = null // Use default lobby
        };
        client.OpJoinOrCreateRoom(enterRoomParams);
    }

    public void OnJoinedRoom()
    {
        Debug.Log($"[SimulatedClient {clientId}] Joined room '{client.CurrentRoom.Name}'. Players: {client.CurrentRoom.PlayerCount}/{client.CurrentRoom.MaxPlayers}");
    }

    public void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"[SimulatedClient {clientId}] Failed to join room: {message} (Code: {returnCode})");
    }

    public void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.LogError($"[SimulatedClient {clientId}] Failed to join random room: {message} (Code: {returnCode})");
    }

    public void OnCreatedRoom()
    {
        Debug.Log($"[SimulatedClient {clientId}] Created room '{client.CurrentRoom.Name}'.");
    }

    public void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"[SimulatedClient {clientId}] Failed to create room: {message} (Code: {returnCode})");
    }

    public void OnLeftRoom() { }
    public void OnFriendListUpdate(List<FriendInfo> friendList) { }
    public void OnPlayerEnteredRoom(Player newPlayer) { }
    public void OnPlayerLeftRoom(Player otherPlayer) { }
    public void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged) { }
    public void OnMasterClientSwitched(Player newMasterClient) { }
}