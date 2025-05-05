using UnityEngine;
using TMPro;
using Photon.Pun;
using Photon.Realtime;

public class RoleDisplayUI : MonoBehaviourPun
{
    [SerializeField] private TextMeshProUGUI roleText;
    [SerializeField] private GameObject continueButton;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private float autoTransitionTime = 10f;

    private float timer;
    private bool hasClickedContinue = false;
    private int readyPlayers = 0;

    private void Start()
    {
        timer = autoTransitionTime;
        GameManager gameManager = GameManager.Instance;
        if (gameManager != null && gameManager.PlayerInfos.TryGetValue(PhotonNetwork.LocalPlayer.ActorNumber, out PlayerInfo playerInfo))
        {
            roleText.text = $"Your Role: {playerInfo.assignedRole.roleName}";
        }
        else
        {
            roleText.text = "Error: Role not assigned!";
            Debug.LogError("GameManager or PlayerInfo not found!");
        }

        statusText.text = PhotonNetwork.IsMasterClient ? "Press Continue to start!" : "Waiting for host...";
    }

    private void Update()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            timer -= Time.deltaTime;
            if (timer <= 0 && !hasClickedContinue)
            {
                photonView.RPC("SignalContinue", RpcTarget.All);
            }
        }
    }

    public void OnContinueButtonClicked()
    {
        if (hasClickedContinue) return;
        hasClickedContinue = true;

        if (PhotonNetwork.IsMasterClient)
        {
            photonView.RPC("SignalContinue", RpcTarget.All);
        }
        else
        {
            photonView.RPC("PlayerReady", RpcTarget.MasterClient);
            statusText.text = "Ready! Waiting for host...";
        }
    }

    [PunRPC]
    private void PlayerReady()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            readyPlayers++;
            if (readyPlayers >= PhotonNetwork.PlayerList.Length)
            {
                photonView.RPC("SignalContinue", RpcTarget.All);
            }
        }
    }

    [PunRPC]
    private void SignalContinue()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.LoadLevel("GameScene");
        }
    }
}