using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using UnityEngine.SceneManagement;

public class PlayerSimulator : MonoBehaviourPunCallbacks
{
    [SerializeField] public bool isSimulationEnabled = false; // Checkbox to enable simulation
    [SerializeField] private int numberOfPlayers = 19; // Number of simulated bots
    [SerializeField] private string roomName = "GameRoom_3418";
    [SerializeField] private GameObject botPrefab; // Assign a prefab for simulated bots

    private int connectedBots = 0;
    private bool isRoomReady = false;

    void Start()
    {
        if (!isSimulationEnabled) return;

        Debug.Log("[PlayerSimulator] Simulation enabled. Connecting to Photon...");
        if (!PhotonNetwork.IsConnected)
        {
            PhotonNetwork.ConnectUsingSettings();
            PhotonNetwork.GameVersion = "1.0";
        }
        else
        {
            JoinOrCreateRoom();
        }
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("[PlayerSimulator] Connected to Master Server. Joining lobby...");
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("[PlayerSimulator] Joined lobby. Creating or joining room...");
        JoinOrCreateRoom();
    }

    private void JoinOrCreateRoom()
    {
        RoomOptions roomOptions = new RoomOptions
        {
            MaxPlayers = (byte)(numberOfPlayers + 1), // +1 for the real player
            IsVisible = true,
            IsOpen = true
        };
        PhotonNetwork.JoinOrCreateRoom(roomName, roomOptions, null);
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("[PlayerSimulator] Joined room: " + PhotonNetwork.CurrentRoom.Name + ". Players: " + PhotonNetwork.CurrentRoom.PlayerCount);
        if (PhotonNetwork.IsMasterClient)
        {
            StartCoroutine(SpawnBotsWithDelay());
        }
        else
        {
            Debug.Log("[PlayerSimulator] Not the Master Client. Waiting for bots to spawn...");
        }
    }

    private IEnumerator SpawnBotsWithDelay()
    {
        Debug.Log("[PlayerSimulator] Starting to spawn bots...");
        for (int i = 0; i < numberOfPlayers; i++)
        {
            if (botPrefab == null)
            {
                Debug.LogError("[PlayerSimulator] botPrefab is not assigned! Cannot spawn bots.");
                yield break;
            }

            GameObject bot = PhotonNetwork.Instantiate(botPrefab.name, Vector3.zero, Quaternion.identity);
            if (bot != null)
            {
                connectedBots++;
                Debug.Log("[PlayerSimulator] Spawned bot " + connectedBots + ". Current player count: " + PhotonNetwork.CurrentRoom.PlayerCount);
            }
            else
            {
                Debug.LogWarning("[PlayerSimulator] Failed to spawn bot " + (i + 1));
            }
            yield return new WaitForSeconds(0.1f); // Small delay between spawns
        }
        yield return new WaitForSeconds(1f); // Additional delay to ensure sync
        CheckRoomReadiness();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log("[PlayerSimulator] Player entered room. Current player count: " + PhotonNetwork.CurrentRoom.PlayerCount);
        StartCoroutine(CheckRoomReadinessAfterDelay());
    }

    private IEnumerator CheckRoomReadinessAfterDelay()
    {
        yield return new WaitForSeconds(1f); // Wait for player count to sync
        CheckRoomReadiness();
    }

    private void CheckRoomReadiness()
    {
        Debug.Log("[PlayerSimulator] Checking room readiness. Current player count: " + PhotonNetwork.CurrentRoom.PlayerCount + ", Connected bots: " + connectedBots + ", Expected: " + (numberOfPlayers + 1));
        if (connectedBots >= numberOfPlayers && !isRoomReady)
        {
            isRoomReady = true;
            Debug.Log("[PlayerSimulator] Room is full with " + (connectedBots + 1) + " players (1 local + " + connectedBots + " bots). Loading RoleReveal...");

            // Check if RoleReveal is in Build Settings
            bool sceneFound = false;
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
                string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
                Debug.Log("[PlayerSimulator] Build Settings Scene " + i + ": " + sceneName);
                if (sceneName == "RoleReveal")
                {
                    sceneFound = true;
                    break;
                }
            }

            if (sceneFound)
            {
                Debug.Log("[PlayerSimulator] RoleReveal found in Build Settings. Loading via Photon...");
                PhotonNetwork.LoadLevel("RoleReveal");
            }
            else
            {
                Debug.LogWarning("[PlayerSimulator] RoleReveal not found in Build Settings. Attempting to load via SceneManager...");
                SceneManager.LoadScene("RoleReveal");
            }
        }
        else
        {
            Debug.Log("[PlayerSimulator] Room not full yet. Connected bots: " + connectedBots + ", Expected: " + numberOfPlayers);
        }
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.Log("[PlayerSimulator] Disconnected: " + cause);
        if (cause == DisconnectCause.MaxCcuReached)
        {
            Debug.LogWarning("Max CCU reached. Returning to MainMenu...");
            SceneManager.LoadScene("MainMenu");
        }
    }
}