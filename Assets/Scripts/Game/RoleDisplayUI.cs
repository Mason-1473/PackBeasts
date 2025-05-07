using UnityEngine;
using TMPro;
using Photon.Pun;
using Photon.Realtime;

public class RoleDisplayUI : MonoBehaviourPun
{
    [SerializeField] private TextMeshProUGUI roleText;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private float autoTransitionTime = 10f;

    private float timer;

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

        statusText.text = $"Transitioning to GameScene in {Mathf.CeilToInt(timer)} seconds...";
    }

    private void Update()
    {
        timer -= Time.deltaTime;
        statusText.text = $"Transitioning to GameScene in {Mathf.CeilToInt(timer)} seconds...";

        if (PhotonNetwork.IsMasterClient && timer <= 0)
        {
            photonView.RPC("SignalContinue", RpcTarget.All);
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