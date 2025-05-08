using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.SceneManagement;

public class PlayerSimulator : MonoBehaviourPunCallbacks
{
    public bool isSimulationEnabled = true;
    public int botCount = 19; // Keep 19 bots + 1 player = 20 total
    public GameObject botPrefab;
    public GameObject playerPrefab;
    [SerializeField] private string playerPrefabName = "PlayerPrefab";
    [SerializeField] private string botPrefabName = "BotPrefab";

    void Start()
    {
        Debug.Log("[PlayerSimulator] Subscribing to sceneLoaded event...");
        SceneManager.sceneLoaded += OnSceneLoaded;
        DontDestroyOnLoad(gameObject);
        Debug.Log("[PlayerSimulator] Set to DontDestroyOnLoad.");

        if (isSimulationEnabled)
        {
            Debug.Log("[PlayerSimulator] Simulation enabled. Connecting to Photon...");
            if (!PhotonNetwork.IsConnected)
            {
                PhotonNetwork.ConnectUsingSettings();
                PhotonNetwork.GameVersion = "1.0";
            }
        }

        // Log all prefabs in Resources to debug
        Debug.Log("[PlayerSimulator] Listing all prefabs in Resources folder...");
        GameObject[] allPrefabs = Resources.LoadAll<GameObject>("");
        foreach (var prefab in allPrefabs)
        {
            Debug.Log($"[PlayerSimulator] Found prefab in Resources: {prefab.name}");
        }

        // Verify prefabs
        Debug.Log("[PlayerSimulator] Checking playerPrefab...");
        if (playerPrefab == null)
        {
            Debug.Log($"[PlayerSimulator] playerPrefab is null, attempting to load from Resources: {playerPrefabName}");
            playerPrefab = Resources.Load<GameObject>(playerPrefabName);
            Debug.Log("[PlayerSimulator] Reloaded playerPrefab: " + (playerPrefab != null ? "Success" : "Failed"));
        }
        else
        {
            Debug.Log("[PlayerSimulator] playerPrefab already assigned: " + playerPrefab.name);
        }

        Debug.Log("[PlayerSimulator] Checking botPrefab...");
        if (botPrefab == null)
        {
            Debug.Log($"[PlayerSimulator] botPrefab is null, attempting to load from Resources: {botPrefabName}");
            botPrefab = Resources.Load<GameObject>(botPrefabName);
            Debug.Log("[PlayerSimulator] Reloaded botPrefab: " + (botPrefab != null ? "Success" : "Failed"));
        }
        else
        {
            Debug.Log("[PlayerSimulator] botPrefab already assigned: " + botPrefab.name);
        }
    }

    void OnDestroy()
    {
        Debug.Log("[PlayerSimulator] Unsubscribing from sceneLoaded event...");
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("[PlayerSimulator] Connected to Master Server. Waiting for lobby...");
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("[PlayerSimulator] Joined lobby. Creating or joining room...");
        RoomOptions roomOptions = new RoomOptions { MaxPlayers = 20 };
        PhotonNetwork.JoinOrCreateRoom("SimulationRoom", roomOptions, TypedLobby.Default);
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("[PlayerSimulator] Joined room: " + PhotonNetwork.CurrentRoom.Name);
        if (PhotonNetwork.IsMasterClient && isSimulationEnabled)
        {
            Debug.Log("[PlayerSimulator] Master Client, loading GameScene. IsMasterClient: " + PhotonNetwork.IsMasterClient + ", Simulation: " + isSimulationEnabled);
            PhotonNetwork.LoadLevel("GameScene");
        }
        else
        {
            Debug.Log("[PlayerSimulator] Not master client or simulation disabled. IsMasterClient: " + PhotonNetwork.IsMasterClient + ", Simulation: " + isSimulationEnabled);
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log("[PlayerSimulator] OnSceneLoaded triggered for scene: " + scene.name + ", mode: " + mode + ", IsMasterClient: " + PhotonNetwork.IsMasterClient + ", Simulation: " + isSimulationEnabled);
        if (scene.name == "GameScene" && PhotonNetwork.IsMasterClient && isSimulationEnabled)
        {
            Debug.Log("[PlayerSimulator] GameScene loaded. Spawning player and bots...");
            if (playerPrefab == null)
            {
                Debug.LogError($"[PlayerSimulator] playerPrefab is null! Attempting reload: {playerPrefabName}");
                playerPrefab = Resources.Load<GameObject>(playerPrefabName);
                Debug.Log("[PlayerSimulator] Reloaded playerPrefab in OnSceneLoaded: " + (playerPrefab != null ? "Success" : "Failed"));
            }
            if (playerPrefab != null)
            {
                PhotonNetwork.Instantiate(playerPrefab.name, Vector3.zero, Quaternion.identity);
                Debug.Log("[PlayerSimulator] Player spawned.");
            }
            else
            {
                Debug.LogError("[PlayerSimulator] playerPrefab is still null after reload!");
            }

            if (botPrefab == null)
            {
                Debug.LogError($"[PlayerSimulator] botPrefab is null! Attempting reload: {botPrefabName}");
                botPrefab = Resources.Load<GameObject>(botPrefabName);
                Debug.Log("[PlayerSimulator] Reloaded botPrefab in OnSceneLoaded: " + (botPrefab != null ? "Success" : "Failed"));
            }
            if (botPrefab != null)
            {
                for (int i = 0; i < botCount; i++)
                {
                    GameObject bot = PhotonNetwork.Instantiate(botPrefab.name, Vector3.zero, Quaternion.identity);
                    bot.name = $"Bot_{i + 1}";
                    Debug.Log("[PlayerSimulator] Spawned bot: " + bot.name);
                }
            }
            else
            {
                Debug.LogError("[PlayerSimulator] botPrefab is still null after reload!");
            }
            Debug.Log("[PlayerSimulator] All entities spawned. Starting game...");
            GameManager gm = FindFirstObjectByType<GameManager>();
            if (gm != null)
            {
                gm.StartGame();
                Debug.Log("[PlayerSimulator] Game started.");
            }
            else
            {
                Debug.LogError("[PlayerSimulator] GameManager not found after spawn!");
            }
            Debug.Log("[PlayerSimulator] Active players in room: " + PhotonNetwork.CurrentRoom.PlayerCount);
            RoleManager rm = FindFirstObjectByType<RoleManager>();
            if (rm != null) Debug.Log("[PlayerSimulator] RoleManager found.");
            if (gm != null) Debug.Log("[PlayerSimulator] GameManager found.");
        }
        else
        {
            Debug.Log("[PlayerSimulator] Not spawning - Scene: " + scene.name + ", IsMasterClient: " + PhotonNetwork.IsMasterClient + ", Simulation: " + isSimulationEnabled);
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log("[PlayerSimulator] Player entered room: " + newPlayer.NickName);
    }
}