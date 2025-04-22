using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.UI;
using System.Collections;
#if TMPRO
using TMPro;
#endif

public class WaitingRoomManager : MonoBehaviourPunCallbacks
{
    public Vector3 spawnCenter = Vector3.zero;
    public float spawnRadius = 5f;
    public int maxPlayers = 20; //change this to 20 later

    [SerializeField] private RoleManager roleManager;
    [SerializeField] private GameManager gameManager; // Reference to GameManager

#if TMPRO
    [SerializeField] private TMP_Text statusText;
#else
    [SerializeField] private Text statusText;
#endif

    void Start()
    {
        if (!PhotonNetwork.InRoom)
        {
            Debug.LogError("Not in a Photon room. Cannot instantiate player.");
            UpdateStatus("Error: Not connected to a room.");
            return;
        }

        Vector3 randomOffset = Random.insideUnitSphere;
        randomOffset.y = 0;
        randomOffset = randomOffset.normalized * Random.Range(0f, spawnRadius);
        Vector3 spawnPos = spawnCenter + randomOffset;

        GameObject player = PhotonNetwork.Instantiate("PlayerPrefab", spawnPos, Quaternion.identity);
        if (player == null)
        {
            Debug.LogError("Failed to instantiate PlayerPrefab. Ensure it exists in Assets/Resources.");
            UpdateStatus("Error: Failed to spawn player.");
            return;
        }

        UpdateStatus($"Waiting for players... {PhotonNetwork.CurrentRoom.PlayerCount}/{maxPlayers}");

        if (PhotonNetwork.IsMasterClient)
        {
            CheckStartConditions();
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        UpdateStatus($"Player joined: {newPlayer.NickName}. Players: {PhotonNetwork.CurrentRoom.PlayerCount}/{maxPlayers}");

        if (PhotonNetwork.IsMasterClient)
        {
            CheckStartConditions();
        }
    }

    private void CheckStartConditions()
    {
        if (PhotonNetwork.CurrentRoom.PlayerCount >= maxPlayers)
        {
            PhotonNetwork.CurrentRoom.IsOpen = false;
            if (roleManager != null)
            {
                roleManager.AssignRolesToPlayers();
            }
            else
            {
                Debug.LogError("RoleManager reference is null in WaitingRoomManager!");
            }
            photonView.RPC(nameof(BeginCountdown), RpcTarget.All);
        }
    }

    [PunRPC]
    private void BeginCountdown()
    {
        StartCoroutine(Countdown());
    }

    private IEnumerator Countdown()
    {
        int seconds = 5;
        while (seconds > 0)
        {
            UpdateStatus($"Game starts in {seconds}...");
            yield return new WaitForSeconds(1f);
            seconds--;
        }

        UpdateStatus("Starting!");
        if (gameManager != null)
        {
            gameManager.StartGame();
        }
        else
        {
            Debug.LogError("GameManager reference is null in WaitingRoomManager!");
        }
    }

    private void UpdateStatus(string msg)
    {
        if (statusText != null)
        {
            statusText.text = msg;
        }
        Debug.Log(msg);
    }
}