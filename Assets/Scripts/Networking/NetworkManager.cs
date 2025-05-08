using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.SceneManagement;
using System.Collections;

public class NetworkManager : MonoBehaviourPunCallbacks
{
    private bool shouldJoinLobby = false;

    void Start()
    {
        if (!PhotonNetwork.IsConnected)
        {
            PhotonNetwork.ConnectUsingSettings();
            PhotonNetwork.GameVersion = "1.0";
            Debug.Log("[NetworkManager] Connecting to Photon...");
        }
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("[NetworkManager] Connected to Master Server. Preparing to join lobby...");
        shouldJoinLobby = true;
        StartCoroutine(TryJoinLobby());
    }

    private IEnumerator TryJoinLobby()
    {
        // Wait until the client is fully ready
        while (!PhotonNetwork.IsConnectedAndReady || PhotonNetwork.NetworkClientState != ClientState.ConnectedToMaster)
        {
            Debug.Log($"[NetworkManager] Waiting for client to be ready. Current state: {PhotonNetwork.NetworkClientState}");
            yield return new WaitForSeconds(0.1f);
        }

        if (shouldJoinLobby)
        {
            shouldJoinLobby = false;
            Debug.Log("[NetworkManager] Client ready. Joining lobby...");
            PhotonNetwork.JoinLobby();
        }
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("[NetworkManager] Joined lobby.");
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("[NetworkManager] Joined room: " + PhotonNetwork.CurrentRoom.Name);
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.Log("[NetworkManager] Disconnected: " + cause);
        if (cause == DisconnectCause.MaxCcuReached)
        {
            Debug.LogWarning("Max CCU reached. Returning to MainMenu...");
            SceneManager.LoadScene("MainMenu");
        }
    }
}