using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public enum GamePhase { Day, Dusk, Night }
public enum DaySubPhase { Discussion, Voting }

public class GameManager : MonoBehaviourPunCallbacks
{
    public static GameManager Instance;
    public GamePhase currentPhase { get; private set; }
    public DaySubPhase currentSubPhase { get; private set; }
    public Text phaseText; // UI Text to display current phase
    public float discussionPhaseDuration = 60f; // 1 minute
    public float votingPhaseDuration = 60f;    // 1 minute
    public float duskPhaseDuration = 30f;       // 30 seconds
    public float nightPhaseDuration = 30f;      // 30 seconds

    private List<int> playersInJudgement = new List<int>(); // ActorNumbers of players in Judgement
    private int currentJudgementPlayer = -1; // ActorNumber of player currently being judged
    private Dictionary<int, int> votes = new Dictionary<int, int>(); // ActorNumber -> voted ActorNumber
    private int dayCount = 0;

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
    }

    public void StartGame()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        dayCount = 1;
        currentPhase = GamePhase.Day;
        currentSubPhase = DaySubPhase.Discussion;
        UpdatePhaseUI();
        photonView.RPC("SyncPhase", RpcTarget.AllBuffered, (int)currentPhase, (int)currentSubPhase, dayCount);
        StartCoroutine(DiscussionPhase());
    }

    [PunRPC]
    void SyncPhase(int phase, int subPhase, int day)
    {
        currentPhase = (GamePhase)phase;
        currentSubPhase = (DaySubPhase)subPhase;
        dayCount = day;
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

    IEnumerator DiscussionPhase()
    {
        yield return new WaitForSeconds(discussionPhaseDuration);
        if (PhotonNetwork.IsMasterClient)
        {
            currentSubPhase = DaySubPhase.Voting;
            photonView.RPC("SyncPhase", RpcTarget.AllBuffered, (int)currentPhase, (int)currentSubPhase, dayCount);
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

        // Count votes
        Dictionary<int, int> voteCounts = new Dictionary<int, int>();
        foreach (var vote in votes.Values)
        {
            if (!voteCounts.ContainsKey(vote)) voteCounts[vote] = 0;
            voteCounts[vote]++;
        }

        // Find player with most votes
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
        // Notify players (e.g., via UI) to make a final vote
    }

    IEnumerator JudgementPhase()
    {
        yield return new WaitForSeconds(30f); // Time for pleading and final vote
        if (PhotonNetwork.IsMasterClient)
        {
            // Count final votes
            Dictionary<int, int> finalVoteCounts = new Dictionary<int, int>();
            foreach (var vote in votes.Values)
            {
                if (!finalVoteCounts.ContainsKey(vote)) finalVoteCounts[vote] = 0;
                finalVoteCounts[vote]++;
            }

            int majority = PhotonNetwork.CurrentRoom.PlayerCount / 2 + 1;
            if (finalVoteCounts.ContainsKey(currentJudgementPlayer) && finalVoteCounts[currentJudgementPlayer] >= majority)
            {
                photonView.RPC("KillPlayer", RpcTarget.All, currentJudgementPlayer);
                TransitionToDusk();
            }
            else if (playersInJudgement.Count < 3)
            {
                votes.Clear();
                currentSubPhase = DaySubPhase.Voting;
                photonView.RPC("SyncPhase", RpcTarget.AllBuffered, (int)currentPhase, (int)currentSubPhase, dayCount);
                StartCoroutine(VotingPhase());
            }
            else
            {
                TransitionToDusk();
            }
        }
    }

    [PunRPC]
    void KillPlayer(int actorNumber)
    {
        Player player = PhotonNetwork.CurrentRoom.Players[actorNumber];
        Debug.Log($"Player {actorNumber} ({player.NickName}) has been executed.");
        // Remove player from game (e.g., destroy GameObject, mark as dead)
        if (player.TagObject is GameObject playerObj)
        {
            PhotonNetwork.Destroy(playerObj);
        }
        CheckWinConditions();
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
        votes[voterActorNumber] = targetActorNumber;
        Debug.Log($"Player {voterActorNumber} voted for Player {targetActorNumber}");
    }

    void TransitionToDusk()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        currentPhase = GamePhase.Dusk;
        votes.Clear();
        playersInJudgement.Clear();
        currentJudgementPlayer = -1;
        photonView.RPC("SyncPhase", RpcTarget.AllBuffered, (int)currentPhase, (int)currentSubPhase, dayCount);
        StartCoroutine(DuskPhase());
    }

    IEnumerator DuskPhase()
    {
        yield return new WaitForSeconds(duskPhaseDuration);
        if (PhotonNetwork.IsMasterClient)
        {
            currentPhase = GamePhase.Night;
            photonView.RPC("SyncPhase", RpcTarget.AllBuffered, (int)currentPhase, (int)currentSubPhase, dayCount);
            StartCoroutine(NightPhase());
        }
    }

    IEnumerator NightPhase()
    {
        yield return new WaitForSeconds(nightPhaseDuration);
        if (PhotonNetwork.IsMasterClient)
        {
            dayCount++;
            currentPhase = GamePhase.Day;
            currentSubPhase = DaySubPhase.Discussion;
            photonView.RPC("SyncPhase", RpcTarget.AllBuffered, (int)currentPhase, (int)currentSubPhase, dayCount);
            StartCoroutine(DiscussionPhase());
        }
    }

    void CheckWinConditions()
    {
        // To be implemented after role system is fully set up
        // Check Dominion, Pack, Neutral Killers, Outsiders win conditions
    }
}