using UnityEngine;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using System.Text;

public class ChatManager : MonoBehaviourPun
{
    [SerializeField] private TextMeshProUGUI chatLog;
    [SerializeField] private TMP_InputField messageInput;
    [SerializeField] private GameObject publicChatButton;
    [SerializeField] private GameObject packChatButton;

    private StringBuilder chatMessages = new StringBuilder();

    private void Start()
    {
        UpdateChatButtonVisibility();
    }

    private void Update()
    {
        UpdateChatButtonVisibility();
    }

    private void UpdateChatButtonVisibility()
    {
        GameManager gameManager = GameManager.Instance;
        bool isPackMember = gameManager.PlayerInfos.TryGetValue(PhotonNetwork.LocalPlayer.ActorNumber, out PlayerInfo playerInfo) &&
                           playerInfo.faction == "Pack";
        bool isDuskOrNight = gameManager.currentPhase == GamePhase.Dusk || gameManager.currentPhase == GamePhase.Night;
        packChatButton.SetActive(isPackMember && isDuskOrNight);
        publicChatButton.SetActive(true);
    }

    public void SendPublicMessage()
    {
        string message = messageInput.text.Trim();
        if (!string.IsNullOrEmpty(message))
        {
            photonView.RPC("ReceiveMessage", RpcTarget.All, PhotonNetwork.LocalPlayer.NickName, message, false);
            messageInput.text = "";
        }
    }

    public void SendPackMessage()
    {
        string message = messageInput.text.Trim();
        if (!string.IsNullOrEmpty(message))
        {
            foreach (Player player in PhotonNetwork.PlayerList)
            {
                if (GameManager.Instance.PlayerInfos.TryGetValue(player.ActorNumber, out PlayerInfo info) &&
                    info.faction == "Pack")
                {
                    photonView.RPC("ReceiveMessage", player, PhotonNetwork.LocalPlayer.NickName, message, true);
                }
            }
            messageInput.text = "";
        }
    }

    [PunRPC]
    private void ReceiveMessage(string senderName, string message, bool isPackMessage)
    {
        string prefix = isPackMessage ? "[Pack] " : "";
        chatMessages.AppendLine($"{prefix}{senderName}: {message}");
        chatLog.text = chatMessages.ToString();
    }
}