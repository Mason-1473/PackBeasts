using UnityEngine;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using System.Linq;

public class PackMarkManager : MonoBehaviourPun
{
    [SerializeField] private TMP_Dropdown voteDropdown;
    [SerializeField] private GameObject votePanel;

    private RoleManager roleManager;
    private GameManager gameManager;
    private Dictionary<int, int> voteCounts = new Dictionary<int, int>();
    private List<Player> packMembers = new List<Player>();

    private void Start()
    {
        roleManager = FindFirstObjectByType<RoleManager>();
        gameManager = GameManager.Instance;
        UpdateVoteUI();
    }

    private void Update()
    {
        bool isPackMember = gameManager.PlayerInfos.TryGetValue(PhotonNetwork.LocalPlayer.ActorNumber, out PlayerInfo playerInfo) &&
                           playerInfo.faction == "Pack";
        votePanel.SetActive(isPackMember && gameManager.currentPhase == GamePhase.Dusk);
    }

    private void UpdateVoteUI()
    {
        packMembers.Clear();
        voteDropdown.ClearOptions();
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            bool isAlive = gameManager.PlayerInfos.TryGetValue(player.ActorNumber, out PlayerInfo info) && !info.hasWon;
            if (isAlive && info.faction == "Pack")
            {
                packMembers.Add(player);
            }
        }
        voteDropdown.AddOptions(packMembers.Select(p => p.NickName).ToList());
    }

    public void SubmitVote()
    {
        int selectedIndex = voteDropdown.value;
        if (selectedIndex >= 0 && selectedIndex < packMembers.Count)
        {
            int votedActorNumber = packMembers[selectedIndex].ActorNumber;
            photonView.RPC("RecordVote", RpcTarget.MasterClient, votedActorNumber);
        }
    }

    [PunRPC]
    private void RecordVote(int votedActorNumber)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        if (!voteCounts.ContainsKey(votedActorNumber))
            voteCounts[votedActorNumber] = 0;
        voteCounts[votedActorNumber]++;

        int packMemberCount = packMembers.Count;
        if (voteCounts.Values.Sum() >= packMemberCount)
        {
            int markedActorNumber = voteCounts.OrderByDescending(kv => kv.Value).First().Key;
            Player markedPlayer = PhotonNetwork.PlayerList.First(p => p.ActorNumber == markedActorNumber);
            photonView.RPC("ApplyMark", RpcTarget.All, markedActorNumber);
        }
    }

    [PunRPC]
    private void ApplyMark(int markedActorNumber)
    {
        Player markedPlayer = PhotonNetwork.PlayerList.First(p => p.ActorNumber == markedActorNumber);
        roleManager.GrantChargedAttack(markedPlayer);
        Debug.Log($"Player {markedPlayer.NickName} marked with Charged Attack.");
    }

    private PlayerInfo FindPlayerInfo(Player player)
    {
        return GameManager.Instance.PlayerInfos.TryGetValue(player.ActorNumber, out PlayerInfo info) ? info : null;
    }
}