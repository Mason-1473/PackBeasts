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
    public int maxPlayers = 20;

#if TMPRO
    [SerializeField] private TMP_Text statusText;
#else
    [SerializeField] private Text statusText;
#endif

    void Start()
    {
        Vector3 randomOffset = Random.insideUnitSphere;
        randomOffset.y = 0;
        randomOffset = randomOffset.normalized * Random.Range(0f, spawnRadius);
        Vector3 spawnPos = spawnCenter + randomOffset;

        PhotonNetwork.Instantiate("PlayerPrefab", spawnPos, Quaternion.identity);

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

        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.LoadLevel("GameScene"); 
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
