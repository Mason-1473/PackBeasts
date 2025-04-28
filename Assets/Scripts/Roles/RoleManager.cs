using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class RoleManager : MonoBehaviourPunCallbacks
{
    public GameObject roleUIPrefab;
    public Transform uiParent;
    private List<RoleAsset> allRoles;
    private Dictionary<int, RoleAsset> playerRoleAssignments;
    private Dictionary<int, int> abilityUsageCounts;
    private Dictionary<int, string> investigationResults; //detective
    private Dictionary<int, string> visionResults; //sage
    private Dictionary<int, int> synthesisProgress; //doctor
    private Dictionary<int, int> killAttemptRecords; //sage
    private Dictionary<int, string> cacheResults; //oracle

    void Awake()
    {
        allRoles = Resources.LoadAll<RoleAsset>("Roles").ToList();
        Debug.Log($"Loaded {allRoles.Count} roles from Resources/Roles");

        if (allRoles.Count < 20)
        {
            Debug.LogError($"Not enough roles! Loaded {allRoles.Count}, need 20.");
            return;
        }

        Debug.Log($"RoleCategory enum check: {RoleCategory.Dominion}");
        Debug.Log($"AbilityType enum check: {AbilityType.Intrude}");

        abilityUsageCounts = new Dictionary<int, int>();
        investigationResults = new Dictionary<int, string>();
        visionResults = new Dictionary<int, string>();
        synthesisProgress = new Dictionary<int, int>();
        killAttemptRecords = new Dictionary<int, int>();
    }

    public void AssignRolesToPlayers()
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            Debug.LogWarning("Only the master client can assign roles.");
            return;
        }

        playerRoleAssignments = new Dictionary<int, RoleAsset>();
        var availableRoles = new List<RoleAsset>(allRoles);

        // Determine faction numbers with weighted randomization
        int packCount = WeightedRandomPackCount();
        packCount = Mathf.Max(1, packCount);

        int maxDominion = 20 - packCount;
        int dominionCount = WeightedRandomDominionCount(maxDominion);
        dominionCount = Mathf.Clamp(dominionCount, 0, maxDominion);

        int remainingPlayers = 20 - (dominionCount + packCount);
        int maxNeutralKillers = remainingPlayers;
        int neutralKillerCount = WeightedRandomNeutralKillerCount(maxNeutralKillers);

        if (packCount < 5)
        {
            int extra = (5 - packCount) * 2;
            neutralKillerCount = Mathf.Min(maxNeutralKillers, neutralKillerCount + Random.Range(0, extra + 1));
        }

        int outsiderCount = remainingPlayers - neutralKillerCount;

        if (outsiderCount > 0)
        {
            int factionCount = 0;
            if (dominionCount > 0) factionCount++;
            if (packCount > 0) factionCount++;
            if (neutralKillerCount > 0) factionCount++;
            if (factionCount < 2)
            {
                outsiderCount = 0;
                neutralKillerCount = remainingPlayers;
            }
        }

        Debug.Log($"Faction Distribution: Dominion={dominionCount}, Pack={packCount}, NeutralKillers={neutralKillerCount}, Outsiders={outsiderCount}");

        List<RoleAsset> selectedRoles = new List<RoleAsset>();

        var dominionRoles = availableRoles.Where(r => r.category == RoleCategory.Dominion).ToList();
        for (int i = 0; i < dominionCount && dominionRoles.Count > 0; i++)
        {
            int idx = Random.Range(0, dominionRoles.Count);
            selectedRoles.Add(dominionRoles[idx]);
            availableRoles.Remove(dominionRoles[idx]);
            dominionRoles.RemoveAt(idx);
        }

        var packRoles = availableRoles.Where(r => r.category == RoleCategory.Pack).ToList();
        for (int i = 0; i < packCount && packRoles.Count > 0; i++)
        {
            int idx = Random.Range(0, packRoles.Count);
            selectedRoles.Add(packRoles[idx]);
            availableRoles.Remove(packRoles[idx]);
            packRoles.RemoveAt(idx);
        }

        var neutralKillerRoles = availableRoles.Where(r => r.category == RoleCategory.NeutralKiller).ToList();
        for (int i = 0; i < neutralKillerCount && neutralKillerRoles.Count > 0; i++)
        {
            int idx = Random.Range(0, neutralKillerRoles.Count);
            selectedRoles.Add(neutralKillerRoles[idx]);
            availableRoles.Remove(neutralKillerRoles[idx]);
            neutralKillerRoles.RemoveAt(idx);
        }

        var outsiderRoles = availableRoles.Where(r => r.category == RoleCategory.Outsider).ToList();
        for (int i = 0; i < outsiderCount && outsiderRoles.Count > 0; i++)
        {
            int idx = Random.Range(0, outsiderRoles.Count);
            selectedRoles.Add(outsiderRoles[idx]);
            availableRoles.Remove(outsiderRoles[idx]);
            outsiderRoles.RemoveAt(idx);
        }

        while (selectedRoles.Count < 20 && availableRoles.Count > 0)
        {
            int idx = Random.Range(0, availableRoles.Count);
            selectedRoles.Add(availableRoles[idx]);
            availableRoles.RemoveAt(idx);
        }

        if (selectedRoles.Count != 20)
        {
            Debug.LogError($"Failed to select 20 roles! Selected {selectedRoles.Count} roles.");
            return;
        }

        var players = PhotonNetwork.PlayerList;
        for (int i = 0; i < players.Length; i++)
        {
            int roleIdx = Random.Range(0, selectedRoles.Count);
            playerRoleAssignments[players[i].ActorNumber] = selectedRoles[roleIdx];
            selectedRoles.RemoveAt(roleIdx);
            Debug.Log($"Assigned role {playerRoleAssignments[players[i].ActorNumber].roleName} to player {players[i].ActorNumber}");
        }

        photonView.RPC("SyncRoleAssignments", RpcTarget.AllBuffered, playerRoleAssignments.Keys.ToArray(), playerRoleAssignments.Values.Select(r => r.roleName).ToArray());
    }

    private int WeightedRandomPackCount()
    {
        float rand = Random.value;
        if (rand < 0.70f) return 5; // 70%
        if (rand < 0.85f) return 4; // 15%
        if (rand < 0.95f) return 3; // 10%
        if (rand < 0.98f) return 2; // 3%
        return 1; // 2%
    }

    private int WeightedRandomDominionCount(int maxDominion)
    {
        float a = 0;
        float b = maxDominion;
        float c = 11;
        float u = Random.value;
        float result;

        if (u < (c - a) / (b - a))
        {
            result = a + Mathf.Sqrt(u * (b - a) * (c - a));
        }
        else
        {
            result = b - Mathf.Sqrt((1 - u) * (b - a) * (b - c));
        }

        return Mathf.RoundToInt(result);
    }

    private int WeightedRandomNeutralKillerCount(int maxNeutralKillers)
    {
        if (maxNeutralKillers <= 0) return 0;

        float a = 0;
        float b = maxNeutralKillers;
        float c = maxNeutralKillers / 2f;
        float u = Random.value;
        float result;

        if (u < (c - a) / (b - a))
        {
            result = a + Mathf.Sqrt(u * (b - a) * (c - a));
        }
        else
        {
            result = b - Mathf.Sqrt((1 - u) * (b - a) * (b - c));
        }

        return Mathf.RoundToInt(result);
    }

    [PunRPC]
    void SyncRoleAssignments(int[] actorNumbers, string[] roleNames)
    {
        playerRoleAssignments = new Dictionary<int, RoleAsset>();
        for (int i = 0; i < actorNumbers.Length; i++)
        {
            RoleAsset role = allRoles.Find(r => r.roleName == roleNames[i]);
            if (role != null)
            {
                playerRoleAssignments[actorNumbers[i]] = role;
            }
        }
        Debug.Log($"Synced roles: {string.Join(", ", playerRoleAssignments.Select(kv => $"Player {kv.Key}: {kv.Value.roleName}"))}");

        DisplayLocalPlayerRole();
    }

    void DisplayLocalPlayerRole()
    {
        if (roleUIPrefab == null)
        {
            Debug.LogError("roleUIPrefab is null!");
            return;
        }
        if (uiParent == null)
        {
            Debug.LogError("uiParent is null!");
            return;
        }

        int localActorNumber = PhotonNetwork.LocalPlayer.ActorNumber;
        if (!playerRoleAssignments.TryGetValue(localActorNumber, out RoleAsset localRole))
        {
            Debug.LogWarning($"No role assigned to local player (ActorNumber: {localActorNumber})");
            return;
        }

        GameObject roleUIObj = Instantiate(roleUIPrefab, uiParent);
        RoleUI roleUI = roleUIObj.GetComponent<RoleUI>();
        if (roleUI == null)
        {
            Debug.LogError("RoleUI component missing on prefab!");
            return;
        }

        roleUI.SetRole(localRole);
        RectTransform rect = roleUIObj.GetComponent<RectTransform>();
        rect.anchoredPosition = Vector2.zero;
    }

    public void UseAbility(AbilityType abilityType)
    {
        int localActorNumber = PhotonNetwork.LocalPlayer.ActorNumber;
        if (playerRoleAssignments == null || !playerRoleAssignments.TryGetValue(localActorNumber, out RoleAsset localRole))
        {
            Debug.LogWarning("Cannot use ability: No role assigned to local player.");
            return;
        }

        bool canUse = false;
        if (GameManager.Instance.currentPhase == GamePhase.Day && abilityType == localRole.dayAbility)
            canUse = true;
        else if (GameManager.Instance.currentPhase == GamePhase.Dusk && abilityType == localRole.duskAbility)
            canUse = true;
        else if (GameManager.Instance.currentPhase == GamePhase.Night && abilityType == localRole.nightAbility)
            canUse = true;

        if (!canUse)
        {
            Debug.LogWarning($"Cannot use ability {abilityType} during {GameManager.Instance.currentPhase} phase.");
            return;
        }

        int usageKey = localActorNumber * 1000 + (int)abilityType;
        if (!abilityUsageCounts.ContainsKey(usageKey)) abilityUsageCounts[usageKey] = 0;
        if (localRole.abilityUsageLimit > 0 && abilityUsageCounts[usageKey] >= localRole.abilityUsageLimit)
        {
            Debug.LogWarning($"Ability {abilityType} has reached its usage limit ({localRole.abilityUsageLimit}).");
            return;
        }

        abilityUsageCounts[usageKey]++;
        Debug.Log($"Player {localActorNumber} using ability: {abilityType} for role {localRole.roleName}");
        photonView.RPC("ExecuteAbility", RpcTarget.All, localActorNumber, (int)abilityType);
    }

    private void RecordKillAttempt(int actorNumber, int currentDay)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            killAttemptRecords[actorNumber] = currentDay;
            photonView.RPC("SyncKillAttempt", RpcTarget.AllBuffered, actorNumber, currentDay);
        }
    }

    [PunRPC]
    void SyncKillAttempt(int actorNumber, int day)
    {
        killAttemptRecords[actorNumber] = day;
        Debug.Log($"Synced kill attempt for Player {actorNumber} on day {day}");
    }

    private bool HasRecentKillAttempt(int actorNumber, int currentDay)
    {
        if (killAttemptRecords.TryGetValue(actorNumber, out int attemptDay))
        {
            return (currentDay - attemptDay) <= 2 && (currentDay - attemptDay) >= 0;
        }
        return false;
    }

    public void CleanUpOldKillAttempts(int currentDay)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            var oldAttempts = killAttemptRecords.Where(kvp => (currentDay - kvp.Value) > 2).Select(kvp => kvp.Key).ToList();
            foreach (var actorNumber in oldAttempts)
            {
                killAttemptRecords.Remove(actorNumber);
                photonView.RPC("RemoveKillAttempt", RpcTarget.AllBuffered, actorNumber);
            }
        }
    }

    [PunRPC]
    void RemoveKillAttempt(int actorNumber)
    {
        killAttemptRecords.Remove(actorNumber);
        Debug.Log($"Removed old kill attempt record for Player {actorNumber}");
    }

    [PunRPC]
    void ExecuteAbility(int actorNumber, int abilityTypeInt)
    {
        AbilityType abilityType = (AbilityType)abilityTypeInt;
        if (!playerRoleAssignments.TryGetValue(actorNumber, out RoleAsset role))
        {
            Debug.LogWarning($"Player {actorNumber} has no assigned role for ability {abilityType}");
            return;
        }

        Debug.Log($"Player {actorNumber} executed ability: {abilityType} for role {role.roleName}");
        GameObject playerObj = PhotonNetwork.CurrentRoom.Players[actorNumber].TagObject as GameObject;
        if (playerObj == null)
        {
            Debug.LogWarning($"Player {actorNumber} GameObject not found.");
            return;
        }

        int currentDay = GameManager.Instance.dayCount;
        var otherPlayers = PhotonNetwork.PlayerList.Where(p => p.ActorNumber != actorNumber).ToList();

        switch (abilityType)
        {
            case AbilityType.Intrude:
                if (otherPlayers.Count > 0)
                {
                    Player target = otherPlayers[Random.Range(0, otherPlayers.Count)];
                    string result = GetIntrudeResult(target.ActorNumber);
                    investigationResults[actorNumber] = $"Intrude result: {result}";
                    Debug.Log($"Player {actorNumber} used Intrude on Player {target.ActorNumber}. Result: {result}");
                }
                break;

            case AbilityType.Vision:
                if (otherPlayers.Count > 0)
                {
                    Player target = otherPlayers[Random.Range(0, otherPlayers.Count)];
                    string result = GetVisionResult(target.ActorNumber, currentDay);
                    visionResults[actorNumber] = $"Vision result: {result}";
                    Debug.Log($"Player {actorNumber} used Vision on Player {target.ActorNumber}. Result: {result}");
                }
                break;

            case AbilityType.Cache:
                if (otherPlayers.Count > 0)
                {
                    Player target = otherPlayers[Random.Range(0, otherPlayers.Count)];
                    string result = GetCacheResult(target.ActorNumber);
                    cacheResults[actorNumber] = $"Cache result: {result}";
                    Debug.Log($"Player {actorNumber} used Cache on Player {target.ActorNumber}. Result: {result}");
                }
                break;

            case AbilityType.Synthesis:
                synthesisProgress[actorNumber] = synthesisProgress.ContainsKey(actorNumber) ? synthesisProgress[actorNumber] + 1 : 1;
                Debug.Log($"Player {actorNumber} started synthesizing a Serum. Progress: {synthesisProgress[actorNumber]}/2");
                break;

            case AbilityType.Administer:
                if (!synthesisProgress.ContainsKey(actorNumber) || synthesisProgress[actorNumber] < 1)
                {
                    Debug.LogWarning($"Player {actorNumber} has no Serum to administer.");
                    return;
                }
                bool isComplex = synthesisProgress[actorNumber] >= 2;
                if (otherPlayers.Count > 0)
                {
                    Player target = otherPlayers[Random.Range(0, otherPlayers.Count)];
                    string defenseLevel = isComplex ? "Fortified" : "Shielded";
                    Debug.Log($"Player {actorNumber} administered a {defenseLevel} Serum to Player {target.ActorNumber}.");
                }
                synthesisProgress.Remove(actorNumber);
                break;

            case AbilityType.Shoot:
                if (otherPlayers.Count > 0)
                {
                    Player target = otherPlayers[Random.Range(0, otherPlayers.Count)];
                    int bulletNumber = abilityUsageCounts[actorNumber * 1000 + (int)abilityType];
                    string attackLevel = bulletNumber == 1 ? "Charged" : "Dominant";
                    Debug.Log($"Player {actorNumber} shot Player {target.ActorNumber} with a {attackLevel} Attack.");
                    RecordKillAttempt(actorNumber, currentDay);
                }
                break;

            case AbilityType.Brilliance:
                Debug.Log($"Player {actorNumber} used Brilliance: Votes hidden, vote doubled.");
                break;
        }
    }

    string GetVisionResult(int targetActorNumber, int currentDay)
    {
        if (!playerRoleAssignments.TryGetValue(targetActorNumber, out RoleAsset targetRole))
            return "Target not found.";

        bool hasKilled = HasRecentKillAttempt(targetActorNumber, currentDay);
        return hasKilled
            ? "Your target has attempted to kill in the past 2 days."
            : "Your target has not attempted to kill in the past 2 days.";
    } 

    string GetCacheResult(int targetActorNumber) 
    {
        if (!playerRoleAssignments.TryGetValue(targetActorNumber, out RoleAsset targetRole))
            return "Target not found.";

        return "";
    }

    string GetIntrudeResult(int targetActorNumber)
    {
        if (!playerRoleAssignments.TryGetValue(targetActorNumber, out RoleAsset targetRole))
            return "Target not found.";

        switch (targetRole.roleName)
        {
            case "Vigilante":
            case "Revenant":
            case "Aggressor":
            case "Traitor":
                return "Your target seems to have weapons lying around.";
            case "Captor":
            case "Restraint":
            case "Trapper":
            case "Gazer":
            case "Starchild":
                return "Your target seems to store many tools.";
            case "Scientist":
            case "Eclipse":
            case "Escapist":
            case "Protector":
                return "Your target seems to protect others.";
            case "Sage":
            case "Oracle":
            case "Radiant":
            case "Vindicator":
                return "Your target seems to investigate others.";
            case "Isolationist":
            case "Obstinance":
            case "Shifter":
            case "Fallicil":
            case "Arbiter":
                return "You couldn’t get a good read on your target.";
            default:
                return "Unknown result.";
        }
    }
}