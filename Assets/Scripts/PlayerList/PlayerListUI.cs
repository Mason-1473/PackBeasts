using UnityEngine;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using System.Text;

public class PlayerListUI : MonoBehaviourPun
{
    [SerializeField] private TextMeshProUGUI livingPlayersList;
    [SerializeField] private TextMeshProUGUI deadPlayersList;

    private void Start()
    {
        UpdatePlayerLists();
    }

    private void Update()
    {
        UpdatePlayerLists();
    }

    private void UpdatePlayerLists()
    {
        StringBuilder living = new StringBuilder("Living Players:\n");
        StringBuilder dead = new StringBuilder("Dead Players:\n");

        foreach (Player player in PhotonNetwork.PlayerList)
        {
            bool isAlive = GameManager.Instance.PlayerInfos.TryGetValue(player.ActorNumber, out PlayerInfo info) && !info.hasWon;
            if (isAlive)
                living.AppendLine(info.playerName);
            else
                dead.AppendLine(player.NickName);
        }

        livingPlayersList.text = living.ToString();
        deadPlayersList.text = dead.ToString();
    }

    [PunRPC]
    public void NotifyPlayerDeath(int actorNumber)
    {
        UpdatePlayerLists();
    }
}