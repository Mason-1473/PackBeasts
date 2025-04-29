using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
#if TMPRO
using TMPro;
#endif

public class NetworkManager : MonoBehaviourPunCallbacks
{

    public static NetworkManager Instance { get; private set; }

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

    if (FindObjectsOfType<NetworkManager>().Length > 1)
    {
        Destroy(gameObject);
        return;
    }
    DontDestroyOnLoad(gameObject);
    }

    [SerializeField] private Button joinGameButton;
    [SerializeField] private Button createGameButton;
#if TMPRO
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text countdownText;
#else
    [SerializeField] private Text statusText;
    [SerializeField] private Text countdownText;
#endif

    private const int MAX_PLAYERS = 20;
    private const string GAME_SCENE = "GameScene";
    private bool isJoining = false;
    private int retryCount = 0;
    private TypedLobby gameLobby = new TypedLobby("GameLobby", LobbyType.Default);

    void Start() {
    if (joinGameButton != null) joinGameButton.interactable = false;
    if (createGameButton != null) createGameButton.interactable = false;
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
        joinGameButton.interactable = false;
        createGameButton.interactable = false;
        UpdateStatus("Preparing to join a game...");
        Invoke(nameof(TryJoinRandomRoom), 4f);
    }

    public void OnCreateGameButtonClicked()
    {
        if (isJoining || PhotonNetwork.NetworkClientState != ClientState.JoinedLobby) return;

        isJoining = true;
        joinGameButton.interactable = false;
        createGameButton.interactable = false;
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
        joinGameButton.interactable = true;
        createGameButton.interactable = true;
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        isJoining = false;
        UpdateStatus($"Failed to join room: {message}");
        joinGameButton.interactable = true;
        createGameButton.interactable = true;
    }

    public override void OnLeftLobby()
{
    UpdateStatus("Left lobby. Rejoining...");
    CancelInvoke(nameof(TryJoinRandomRoom));
    PhotonNetwork.JoinLobby(gameLobby);
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
    }
}
