using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
#if TMPRO
using TMPro;
#endif

public class NetworkManager : MonoBehaviourPunCallbacks
{
    public static NetworkManager Instance { get; private set; }

    [SerializeField] private Button joinGameButton;
    [SerializeField] private Button createGameButton;
#if TMPRO
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text countdownText;
#else
    [SerializeField] private Text statusText;
    [SerializeField] private Text countdownText;
#endif
    [SerializeField] private GameObject playerPrefab; // Assign a prefab with PlayerInfo component

    private const int MAX_PLAYERS = 20; // Updated to 20 for Pack Beasts
    private const string GAME_SCENE = "GameScene";
    private bool isJoining = false;
    private int retryCount = 0;
    private TypedLobby gameLobby = new TypedLobby("GameLobby", LobbyType.Default);

    private void Awake()
    {
        // Singleton setup
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject); // Keep across scene loads
    }

    void Start()
    {
        if (joinGameButton != null)
            joinGameButton.interactable = false;
        else
            Debug.LogWarning("joinGameButton is not assigned in NetworkManager.");
        
        if (createGameButton != null)
            createGameButton.interactable = false;
        else
            Debug.LogWarning("createGameButton is not assigned in NetworkManager.");

        if (playerPrefab == null)
            Debug.LogWarning("playerPrefab is not assigned in NetworkManager. Please assign a prefab with PlayerInfo component.");

        PhotonNetwork.AutomaticallySyncScene = true;
        PhotonNetwork.GameVersion = "1.0";
        PhotonNetwork.ConnectUsingSettings();
        UpdateStatus("Connecting to Photon...");
        
        Debug.Log($"PhotonView in Start: {photonView != null}");
    }

    public override void OnConnectedToMaster()
    {
        UpdateStatus("Connected! Joining lobby...");
        PhotonNetwork.JoinLobby(gameLobby);
    }

    public override void OnJoinedLobby()
    {
        UpdateStatus("In lobby. Ready to join or create a game.");
        if (joinGameButton != null) joinGameButton.interactable = true;
        if (createGameButton != null) createGameButton.interactable = true;
    }

    public void OnJoinGameButtonClicked()
    {
        if (isJoining || PhotonNetwork.NetworkClientState != ClientState.JoinedLobby) return;

        isJoining = true;
        retryCount = 0;
        if (joinGameButton != null) joinGameButton.interactable = false;
        if (createGameButton != null) createGameButton.interactable = false;
        UpdateStatus("Preparing to join a game...");
        Invoke(nameof(TryJoinRandomRoom), 4f);
    }

    public void OnCreateGameButtonClicked()
    {
        if (isJoining || PhotonNetwork.NetworkClientState != ClientState.JoinedLobby) return;

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
        PhotonNetwork.CreateRoom(roomName, roomOptions, gameLobby);
    }

    private void TryJoinRandomRoom()
    {
        if (PhotonNetwork.NetworkClientState != ClientState.JoinedLobby)
        {
            UpdateStatus("Not in lobby. Rejoining...");
            PhotonNetwork.JoinLobby(gameLobby);
            Invoke(nameof(TryJoinRandomRoom), 2f);
            return;
        }

        UpdateStatus("Trying to join a random game...");
        PhotonNetwork.JoinRandomRoom();
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        retryCount++;
        UpdateStatus($"No rooms found. Retry {retryCount}/2...");
        if (retryCount >= 2)
        {
            UpdateStatus("No rooms available. Creating a new game...");
            RoomOptions roomOptions = new RoomOptions
            {
                MaxPlayers = MAX_PLAYERS,
                IsVisible = true,
                IsOpen = true
            };

            string roomName = "GameRoom_" + Random.Range(1000, 9999);
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

        // Spawn player prefab
        if (playerPrefab != null)
        {
            GameObject playerObject = PhotonNetwork.Instantiate(playerPrefab.name, Vector3.zero, Quaternion.identity);
            if (playerObject.GetComponent<PlayerInfo>() == null)
            {
                Debug.LogError("Player prefab must have a PlayerInfo component!");
            }
            else
            {
                GameManager.Instance.RegisterPlayer(PhotonNetwork.LocalPlayer, playerObject);
                Debug.Log($"Registered player {PhotonNetwork.LocalPlayer.ActorNumber} with GameManager.");
            }
        }
        else
        {
            Debug.LogError("playerPrefab is not assigned in NetworkManager. Cannot spawn player.");
        }

        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.LoadLevel("WaitingRoom");
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        UpdateStatus($"Player joined! Players: {PhotonNetwork.CurrentRoom.PlayerCount}/{PhotonNetwork.CurrentRoom.MaxPlayers}");

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
            UpdateStatus("Room full! Starting game in 5 seconds...");
            if (PhotonNetwork.IsMasterClient)
            {
                if (photonView == null)
                {
                    Debug.LogError("PhotonView is null on NetworkManager!");
                    return;
                }
                photonView.RPC("StartCountdownRPC", RpcTarget.All);
            }
        }
    }

    [PunRPC]
    private void StartCountdownRPC()
    {
        StartCoroutine(CountdownCoroutine());
    }

    private IEnumerator CountdownCoroutine()
    {
        int seconds = 5;
        while (seconds > 0)
        {
            SetCountdownText($"Game starts in {seconds}...");
            yield return new WaitForSeconds(1f);
            seconds--;
        }

        SetCountdownText("Starting!");

        if (PhotonNetwork.IsMasterClient)
        {
            StartGame();
        }
    }

    private void SetCountdownText(string msg)
    {
        if (countdownText != null)
        {
#if TMPRO
            countdownText.text = msg;
#else
            countdownText.text = msg;
#endif
            Debug.Log($"Countdown text updated: {msg}");
        }
        else
        {
            Debug.LogWarning("countdownText is not assigned in NetworkManager.");
        }
    }

    private void StartGame()
    {
        PhotonNetwork.CurrentRoom.IsOpen = false;
        PhotonNetwork.LoadLevel(GAME_SCENE);
    }

    public override void OnCreatedRoom()
    {
        string roomName = PhotonNetwork.CurrentRoom.Name;
        UpdateStatus($"Created room '{roomName}'! Waiting for players...");
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        isJoining = false;
        UpdateStatus($"Failed to create room: {message}");
        if (joinGameButton != null) joinGameButton.interactable = true;
        if (createGameButton != null) createGameButton.interactable = true;
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        isJoining = false;
        UpdateStatus($"Failed to join room: {message}");
        if (joinGameButton != null) joinGameButton.interactable = true;
        if (createGameButton != null) createGameButton.interactable = true;
    }

    public override void OnLeftLobby()
    {
        UpdateStatus("Left lobby. Rejoining...");
        CancelInvoke(nameof(TryJoinRandomRoom));
        PhotonNetwork.JoinLobby(gameLobby);
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        Debug.Log($"Room list updated. Available rooms: {roomList.Count}");
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        UpdateStatus($"Player left. Players: {PhotonNetwork.CurrentRoom.PlayerCount}/{PhotonNetwork.CurrentRoom.MaxPlayers}");
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        Debug.Log($"Master client switched to player {newMasterClient.ActorNumber}");
        if (PhotonNetwork.IsMasterClient)
        {
            CheckPlayerCount();
        }
    }

    private void UpdateStatus(string message)
    {
        Debug.Log($"Status Update: {message}");
        if (statusText != null)
        {
#if TMPRO
            statusText.text = message;
#else
            statusText.text = message;
#endif
        }
        else
        {
            Debug.LogWarning("statusText is not assigned in NetworkManager.");
        }
    }

    // Called when the GameScene is loaded
    void OnLevelWasLoaded(int level)
    {
        if (SceneManager.GetActiveScene().name == GAME_SCENE && PhotonNetwork.IsMasterClient)
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.StartGame();
                Debug.Log("Started game via GameManager.");
            }
            else
            {
                Debug.LogError("GameManager.Instance is null in GameScene. Ensure GameManager is present.");
            }
        }
    }
}