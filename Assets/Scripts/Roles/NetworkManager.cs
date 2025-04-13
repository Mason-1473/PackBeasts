using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
#if TMPRO
using TMPro;
#endif

public class NetworkManager : MonoBehaviourPunCallbacks
{
    [SerializeField] private Button joinGameButton;
    [SerializeField] private Button createGameButton;
#if TMPRO
    [SerializeField] private TMP_Text statusText; // For TextMeshPro
#else
    [SerializeField] private Text statusText; // For standard UI Text
#endif
    private const int MAX_PLAYERS = 20; 
    private const string GAME_SCENE = "GameScene";
    private bool isJoining = false;
    private int retryCount = 0;
    private TypedLobby gameLobby = new TypedLobby("GameLobby", LobbyType.Default);

    void Start()
    {
        if (joinGameButton != null) joinGameButton.interactable = false;
        if (createGameButton != null) createGameButton.interactable = false;

        Debug.Log($"statusText assigned: {statusText != null}");
        if (statusText == null)
        {
            Debug.LogWarning("statusText is null! Please assign a UI Text or TMP_Text in Inspector.");
        }

        PhotonNetwork.GameVersion = "1.0";
        PhotonNetwork.ConnectUsingSettings();
        UpdateStatus("Connecting to Photon...");
    }

    public override void OnConnectedToMaster()
    {
        UpdateStatus("Connected! Joining lobby...");
        Debug.Log($"Joining lobby. State: {PhotonNetwork.NetworkClientState}");
        PhotonNetwork.JoinLobby(gameLobby);
    }

    public override void OnJoinedLobby()
    {
        UpdateStatus("In lobby. Ready to join or create a game.");
        Debug.Log($"Joined lobby successfully. State: {PhotonNetwork.NetworkClientState}");
        if (joinGameButton != null) joinGameButton.interactable = true;
        if (createGameButton != null) createGameButton.interactable = true;
    }

    public void OnJoinGameButtonClicked()
    {
        if (isJoining || PhotonNetwork.NetworkClientState != ClientState.JoinedLobby)
        {
            Debug.Log($"Cannot join game. isJoining: {isJoining}, State: {PhotonNetwork.NetworkClientState}");
            return;
        }
        isJoining = true;
        retryCount = 0;
        if (joinGameButton != null) joinGameButton.interactable = false;
        if (createGameButton != null) createGameButton.interactable = false;
        UpdateStatus("Preparing to join a game...");
        Debug.Log($"Preparing to join. State: {PhotonNetwork.NetworkClientState}");
        Invoke(nameof(TryJoinRandomRoom), 4f); // Reduced to 4s
    }

    public void OnCreateGameButtonClicked()
    {
        if (isJoining || PhotonNetwork.NetworkClientState != ClientState.JoinedLobby)
        {
            Debug.Log($"Cannot create game. isJoining: {isJoining}, State: {PhotonNetwork.NetworkClientState}");
            return;
        }
        isJoining = true;
        if (joinGameButton != null) joinGameButton.interactable = false;
        if (createGameButton != null) createGameButton.interactable = false;
        UpdateStatus("Creating a new game...");
        RoomOptions roomOptions = new RoomOptions
        {
            MaxPlayers = MAX_PLAYERS,
            IsVisible = true,
            IsOpen = true
        };
        string roomName = "GameRoom_" + Random.Range(1000, 9999);
        Debug.Log($"Creating room: {roomName} with MaxPlayers={MAX_PLAYERS}");
        PhotonNetwork.CreateRoom(roomName, roomOptions, gameLobby);
    }

    private void TryJoinRandomRoom()
    {
        if (PhotonNetwork.NetworkClientState != ClientState.JoinedLobby)
        {
            UpdateStatus("Not in lobby. Rejoining...");
            Debug.Log($"Cannot join, state: {PhotonNetwork.NetworkClientState}. Rejoining lobby...");
            PhotonNetwork.JoinLobby(gameLobby);
            Invoke(nameof(TryJoinRandomRoom), 2f);
            return;
        }
        UpdateStatus("Trying to join a random game...");
        Debug.Log($"Joining random room. State: {PhotonNetwork.NetworkClientState}, Retry: {retryCount}");
        PhotonNetwork.JoinRandomRoom();
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        retryCount++;
        UpdateStatus($"No rooms found. Retry {retryCount}/2...");
        Debug.Log($"JoinRandomRoom failed: {message} (Code: {returnCode}). Retry {retryCount}/2...");
        if (retryCount >= 2) // Reduced to 2 retries
        {
            UpdateStatus("No rooms available. Creating a new game...");
            Debug.Log("Max retries reached. Creating room...");
            RoomOptions roomOptions = new RoomOptions
            {
                MaxPlayers = MAX_PLAYERS,
                IsVisible = true,
                IsOpen = true
            };
            string roomName = "GameRoom_" + Random.Range(1000, 9999);
            Debug.Log($"Creating room: {roomName} with MaxPlayers={MAX_PLAYERS}");
            PhotonNetwork.CreateRoom(roomName, roomOptions, gameLobby);
            retryCount = 0;
            return;
        }
        Invoke(nameof(TryJoinRandomRoom), 2f);
    }

    public override void OnJoinedRoom()
    {
        isJoining = false;
        retryCount = 0;
        string roomName = PhotonNetwork.CurrentRoom.Name;
        UpdateStatus($"Joined room '{roomName}'! Players: {PhotonNetwork.CurrentRoom.PlayerCount}/{PhotonNetwork.CurrentRoom.MaxPlayers}");
        Debug.Log($"Joined room '{roomName}' with {PhotonNetwork.CurrentRoom.PlayerCount}/{PhotonNetwork.CurrentRoom.MaxPlayers} players");
        if (PhotonNetwork.IsMasterClient)
        {
            CheckPlayerCount();
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        UpdateStatus($"Player joined! Players: {PhotonNetwork.CurrentRoom.PlayerCount}/{PhotonNetwork.CurrentRoom.MaxPlayers}");
        Debug.Log($"Player {newPlayer.NickName} joined. Total: {PhotonNetwork.CurrentRoom.PlayerCount}/{PhotonNetwork.CurrentRoom.MaxPlayers}");
        if (PhotonNetwork.IsMasterClient)
        {
            CheckPlayerCount();
        }
    }

    private void CheckPlayerCount()
    {
        Debug.Log($"Current players: {PhotonNetwork.CurrentRoom.PlayerCount}/{PhotonNetwork.CurrentRoom.MaxPlayers}");
        if (PhotonNetwork.CurrentRoom.PlayerCount >= MAX_PLAYERS)
        {
            UpdateStatus("Room full! Starting game...");
            Debug.Log("Room full. Loading GameScene...");
            PhotonNetwork.CurrentRoom.IsOpen = false;
            if (PhotonNetwork.IsMasterClient)
            {
                PhotonNetwork.LoadLevel(GAME_SCENE);
            }
        }
    }

    public override void OnCreatedRoom()
    {
        string roomName = PhotonNetwork.CurrentRoom.Name;
        UpdateStatus($"Created room '{roomName}'! Waiting for players...");
        Debug.Log($"Created room '{roomName}' with MaxPlayers={PhotonNetwork.CurrentRoom.MaxPlayers}");
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        isJoining = false;
        UpdateStatus($"Failed to create room: {message}");
        Debug.LogError($"CreateRoom failed: {message}");
        if (joinGameButton != null) joinGameButton.interactable = true;
        if (createGameButton != null) createGameButton.interactable = true;
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        isJoining = false;
        UpdateStatus($"Failed to join room: {message}");
        Debug.LogError($"JoinRoom failed: {message}");
        if (joinGameButton != null) joinGameButton.interactable = true;
        if (createGameButton != null) createGameButton.interactable = true;
    }

    public override void OnLeftLobby()
    {
        UpdateStatus("Left lobby. Rejoining...");
        Debug.Log($"Left lobby. State: {PhotonNetwork.NetworkClientState}");
        PhotonNetwork.JoinLobby(gameLobby);
    }

    private void UpdateStatus(string message)
    {
        Debug.Log($"Status Update: {message}");
        if (statusText != null)
        {
#if TMPRO
            statusText.text = message; // TMP_Text
#else
            statusText.text = message; // UI Text
#endif
        }
        else
        {
            Debug.LogWarning("statusText is null! Please assign in Inspector.");
        }
    }
}