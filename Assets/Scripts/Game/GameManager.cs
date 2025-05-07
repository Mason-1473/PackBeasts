using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;
#if TMPRO
using TMPro;
#endif

public enum GamePhase { Day, Dusk, Night }
public enum DaySubPhase { Discussion, Voting }

public class GameManager : MonoBehaviourPunCallbacks
{
    public static GameManager Instance { get; private set; }
    public GamePhase currentPhase { get; private set; }
    public DaySubPhase currentSubPhase { get; private set; }
    public int dayCount { get; private set; }
    public float discussionPhaseDuration = 60f;
    public float votingPhaseDuration = 30f;
    public float duskPhaseDuration = 30f;
    public float nightPhaseDuration = 30f;
    public TextMeshProUGUI phaseText;
    private float phaseTimeRemaining;

    private RoleManager roleManager;
    private Dictionary<int, PlayerInfo> playerInfos;
    public IReadOnlyDictionary<int, PlayerInfo> PlayerInfos => playerInfos;

    private List<int> playersInJudgement = new List<int>();
    private int currentJudgementPlayer = -1;
    private Dictionary<int, int> votes = new Dictionary<int, int>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        roleManager = FindObjectOfType<RoleManager>();
        if (roleManager == null)
        {
            Debug.LogError("RoleManager not found in scene!");
        }

        playerInfos = new Dictionary<int, PlayerInfo>();
    }

    public void StartGame()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        // Instead of checking for exactly 20 players, allow game to start with any number of players for testing
        // if (PhotonNetwork.CurrentRoom.PlayerCount != 20)
        // {
        //     Debug.LogWarning("Game requires exactly 20 players to start.");
        //     return;
        // }

        dayCount = 1;
        currentPhase = GamePhase.Day;
        currentSubPhase = DaySubPhase.Discussion;
        phaseTimeRemaining = discussionPhaseDuration;
        UpdatePhaseUI();
        photonView.RPC("SyncPhase", RpcTarget.AllBuffered, (int)currentPhase, (int)currentSubPhase, dayCount, phaseTimeRemaining);
        roleManager.AssignRolesToPlayers();
        StartCoroutine(DiscussionPhase());
    }

    [PunRPC]
    void SyncPhase(int phase, int subPhase, int day, float timeRemaining)
    {
        currentPhase = (GamePhase)phase;
        currentSubPhase = (DaySubPhase)subPhase;
        dayCount = day;
        phaseTimeRemaining = timeRemaining;
        UpdatePhaseUI();
    }

    void UpdatePhaseUI()
    {
        string phaseString = currentPhase.ToString();
        if (currentPhase == GamePhase.Day)
        {
            phaseString += $" ({currentSubPhase}) - Day {dayCount}";
        }
        if (phaseText != null)
        {
            phaseText.text = phaseString;
        }
        Debug.Log($"Phase: {phaseString}");
    }

    public float GetPhaseRemainingTime()
    {
        return phaseTimeRemaining;
    }

    private void Update()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            phaseTimeRemaining -= Time.deltaTime;
            photonView.RPC("SyncTimer", RpcTarget.All, phaseTimeRemaining);
        }
    }

    [PunRPC]
    private void SyncTimer(float timeRemaining)
    {
        phaseTimeRemaining = timeRemaining;
    }

    IEnumerator DiscussionPhase()
    {
        yield return new WaitForSeconds(discussionPhaseDuration);
        if (PhotonNetwork.IsMasterClient)
        {
            currentSubPhase = DaySubPhase.Voting;
            phaseTimeRemaining = votingPhaseDuration;
            photonView.RPC("SyncPhase", RpcTarget.AllBuffered, (int)currentPhase, (int)currentSubPhase, dayCount, phaseTimeRemaining);
            StartCoroutine(VotingPhase());
        }
    }

    IEnumerator VotingPhase()
    {
        yield return new WaitForSeconds(votingPhaseDuration);
        if (PhotonNetwork.IsMasterClient)
        {
            ProcessVotes();
        }
    }

    void ProcessVotes()
    {
        if (votes.Count == 0)
        {
            TransitionToDusk();
            return;
        }

        Dictionary<int, int> voteCounts = new Dictionary<int, int>();
        foreach (var vote in votes.Values)
        {
            if (!voteCounts.ContainsKey(vote)) voteCounts[vote] = 0;
            voteCounts[vote]++;
        }

        int majority = PhotonNetwork.CurrentRoom.PlayerCount / 2 + 1;
        int mostVotedPlayer = -1;
        int highestVotes = 0;
        foreach (var pair in voteCounts)
        {
            if (pair.Value >= majority && pair.Value > highestVotes)
            {
                mostVotedPlayer = pair.Key;
                highestVotes = pair.Value;
            }
        }

        if (mostVotedPlayer != -1 && playersInJudgement.Count < 3)
        {
            playersInJudgement.Add(mostVotedPlayer);
            currentJudgementPlayer = mostVotedPlayer;
            photonView.RPC("EnterJudgement", RpcTarget.All, mostVotedPlayer);
            votes.Clear();
            StartCoroutine(JudgementPhase());
        }
        else
        {
            TransitionToDusk();
        }
    }

    [PunRPC]
    void EnterJudgement(int actorNumber)
    {
        Debug.Log($"Player {actorNumber} has entered the Judgement State.");
    }

    IEnumerator JudgementPhase()
    {
        yield return new WaitForSeconds(30f);
        if (PhotonNetwork.IsMasterClient)
        {
            Dictionary<int, int> finalVoteCounts = new Dictionary<int, int>();
            foreach (var vote in votes.Values)
            {
                if (!finalVoteCounts.ContainsKey(vote)) finalVoteCounts[vote] = 0;
                finalVoteCounts[vote]++;
            }

            int majority = PhotonNetwork.CurrentRoom.PlayerCount / 2 + 1;
            if (finalVoteCounts.ContainsKey(currentJudgementPlayer) && finalVoteCounts[currentJudgementPlayer] >= majority)
            {
                photonView.RPC("KillPlayer", RpcTarget.All, currentJudgementPlayer, "Execution");
                TransitionToDusk();
            }
            else if (playersInJudgement.Count < 3)
            {
                votes.Clear();
                currentSubPhase = DaySubPhase.Voting;
                phaseTimeRemaining = votingPhaseDuration;
                photonView.RPC("SyncPhase", RpcTarget.AllBuffered, (int)currentPhase, (int)currentSubPhase, dayCount, phaseTimeRemaining);
                StartCoroutine(VotingPhase());
            }
            else
            {
                TransitionToDusk();
            }
        }
    }

    void TransitionToDusk()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        currentPhase = GamePhase.Dusk;
        votes.Clear();
        playersInJudgement.Clear();
        currentJudgementPlayer = -1;
        phaseTimeRemaining = duskPhaseDuration;
        photonView.RPC("SyncPhase", RpcTarget.AllBuffered, (int)currentPhase, (int)currentSubPhase, dayCount, phaseTimeRemaining);
        StartCoroutine(DuskPhase());
    }

    IEnumerator DuskPhase()
    {
        roleManager.ResetVigilanteReloadFlag();
        yield return new WaitForSeconds(duskPhaseDuration);
        if (PhotonNetwork.IsMasterClient)
        {
            currentPhase = GamePhase.Night;
            phaseTimeRemaining = nightPhaseDuration;
            photonView.RPC("SyncPhase", RpcTarget.AllBuffered, (int)currentPhase, (int)currentSubPhase, dayCount, phaseTimeRemaining);
            StartCoroutine(NightPhase());
        }
    }

    IEnumerator NightPhase()
    {
        yield return new WaitForSeconds(nightPhaseDuration);
        if (PhotonNetwork.IsMasterClient)
        {
            roleManager.DeliverDelayedResults();
            roleManager.CleanUpOldKillAttempts(dayCount);
            dayCount++;
            currentPhase = GamePhase.Day;
            currentSubPhase = DaySubPhase.Discussion;
            phaseTimeRemaining = discussionPhaseDuration;
            photonView.RPC("SyncPhase", RpcTarget.AllBuffered, (int)currentPhase, (int)currentSubPhase, dayCount, phaseTimeRemaining);
            StartCoroutine(DiscussionPhase());
        }
    }

    public void SubmitVote(int targetActorNumber)
    {
        if (currentPhase != GamePhase.Day || currentSubPhase != DaySubPhase.Voting) return;
        int voterActorNumber = PhotonNetwork.LocalPlayer.ActorNumber;
        photonView.RPC("RegisterVote", RpcTarget.All, voterActorNumber, targetActorNumber);
    }

    [PunRPC]
    void RegisterVote(int voterActorNumber, int targetActorNumber)
    {
        if (playerInfos.TryGetValue(voterActorNumber, out PlayerInfo voterInfo) && voterInfo.hasWon)
        {
            Debug.Log($"Player {voterActorNumber} has already won and cannot vote.");
            return;
        }
        votes[voterActorNumber] = targetActorNumber;
        Debug.Log($"Player {voterActorNumber} voted for Player {targetActorNumber}");
    }

    public void RegisterPlayer(Player player, GameObject playerObject)
    {
        if (!playerInfos.ContainsKey(player.ActorNumber))
        {
            PlayerInfo info = playerObject.GetComponent<PlayerInfo>();
            if (info != null)
            {
                info.actorNumber = player.ActorNumber;
                info.playerName = player.NickName;
                if (info.assignedRole != null && info.assignedRole.roleName == "Fallicil")
                {
                    info.defenseLevel = "Shielded";
                }
                else if (info.assignedRole != null && info.assignedRole.roleName == "Traitor")
                {
                    info.defenseLevel = "Shielded";
                }
                else
                {
                    info.defenseLevel = "None";
                }
                playerInfos[player.ActorNumber] = info;
            }
        }
    }

    [PunRPC]
    void KillPlayer(int actorNumber, string cause)
    {
        if (!playerInfos.TryGetValue(actorNumber, out PlayerInfo playerInfo))
        {
            Debug.LogWarning($"PlayerInfo not found for ActorNumber {actorNumber}");
            return;
        }

        RoleAsset role = playerInfo.assignedRole;
        if (role == null) return;

        bool isFallicilWin = role.roleName == "Fallicil" && cause == "Execution" && dayCount > 2;
        bool isVindicatorWin = role.roleName == "Vindicator" && cause == "Execution" && roleManager.IsVindicatorMissionComplete(actorNumber);

        if (isFallicilWin || isVindicatorWin)
        {
            playerInfo.hasWon = true;
            playerInfo.skipNextDusk = true;
            photonView.RPC("PlayerWins", RpcTarget.All, actorNumber, role.roleName);
            Debug.Log($"Player {actorNumber} ({role.roleName}) has won and will skip further phases.");
        }
        else
        {
            if (playerInfo.defenseLevel == "Shielded" && role.roleName == "Fallicil")
            {
                playerInfo.defenseLevel = "None";
                Debug.Log($"Player {actorNumber} (Fallicil) lost Shielded defense.");
            }
            else
            {
                PhotonNetwork.Destroy(playerInfo.gameObject);
                playerInfos.Remove(actorNumber);
                photonView.RPC("NotifyPlayerDeath", RpcTarget.All, actorNumber);
            }
        }

        if (PhotonNetwork.IsMasterClient)
        {
            CheckWinConditions();
        }
    }

    [PunRPC]
    void PlayerWins(int actorNumber, string roleName)
    {
        Debug.Log($"Player {actorNumber} ({roleName}) has won!");
    }

    void CheckWinConditions()
    {
        var activePlayers = playerInfos.Values.Where(p => !p.hasWon).ToList();
        var remainingRoles = activePlayers.Select(p => p.assignedRole).ToList();

        if (activePlayers.Count == 0)
        {
            photonView.RPC("GameOver", RpcTarget.All, "No active players");
            return;
        }

        if (remainingRoles.All(r => r.category == RoleCategory.Dominion || r.category == RoleCategory.Outsider))
        {
            photonView.RPC("GameOver", RpcTarget.All, "Dominion");
        }
        else if (remainingRoles.All(r => r.category == RoleCategory.Pack || r.category == RoleCategory.Outsider))
        {
            photonView.RPC("GameOver", RpcTarget.All, "Pack");
        }
        else if (remainingRoles.All(r => r.category == RoleCategory.Outsider || r.category == RoleCategory.NeutralKiller))
        {
            var nkRoles = remainingRoles.Where(r => r.category == RoleCategory.NeutralKiller).Select(r => r.roleName).Distinct();
            if (nkRoles.Count() == 1)
            {
                photonView.RPC("GameOver", RpcTarget.All, nkRoles.First());
            }
        }
        else if (remainingRoles.All(r => r.roleName == "Traitor" || r.category == RoleCategory.Outsider))
        {
            photonView.RPC("GameOver", RpcTarget.All, "Traitor");
        }
    }

    [PunRPC]
    void GameOver(string winner)
    {
        Debug.Log($"Game Over! {winner} wins!");
    }

    public void ResolveAttack(int attackerActorNumber, int targetActorNumber, AttackInstance attack)
    {
        if (!playerInfos.TryGetValue(targetActorNumber, out PlayerInfo targetInfo))
        {
            Debug.LogWarning($"Target PlayerInfo not found for ActorNumber {targetActorNumber}");
            return;
        }

        if (targetInfo.hasWon)
        {
            Debug.Log($"Player {targetActorNumber} has already won and cannot be attacked.");
            return;
        }

        string targetDefense = targetInfo.defenseLevel ?? "None";
        bool survives = false;

        if (targetInfo.assignedRole.roleName == "Escapist" && !attack.isPhantomed)
        {
            survives = true;
        }
        else
        {
            switch (attack.baseLevel)
            {
                case BaseAttackLevel.Charged:
                    survives = targetDefense != "None";
                    break;
                case BaseAttackLevel.Dominant:
                    survives = targetDefense == "Fortified" || targetDefense == "Invincible";
                    break;
                case BaseAttackLevel.Inexorable:
                    survives = targetDefense == "Invincible";
                    break;
                default:
                    survives = true;
                    break;
            }
        }

        if (!survives)
        {
            if (targetInfo.assignedRole.roleName == "Fallicil" && targetDefense == "Shielded")
            {
                targetInfo.defenseLevel = "None";
                Debug.Log($"Player {targetActorNumber} (Fallicil) lost Shielded defense.");
            }
            else
            {
                photonView.RPC("KillPlayer", RpcTarget.All, targetActorNumber, "Ability");
            }
        }
        else
        {
            Debug.Log($"Player {targetActorNumber} survived the attack!");
        }

        if (attack.isRampage)
        {
            // Simplified: requires visitor tracking
        }

        if (targetInfo.assignedRole.roleName == "Traitor")
        {
            targetInfo.defenseLevel = targetDefense == "Shielded" ? "Fortified" : "Invincible";
            Debug.Log($"Player {targetActorNumber} (Traitor) defense upgraded to {targetInfo.defenseLevel}");
        }
    }

    public void SetDefenseLevel(int actorNumber, string defenseLevel)
    {
        if (playerInfos.TryGetValue(actorNumber, out PlayerInfo playerInfo))
        {
            if (!playerInfo.hasWon)
            {
                playerInfo.defenseLevel = defenseLevel;
                Debug.Log($"Player {actorNumber} defense set to {defenseLevel}");
            }
        }
    }
}