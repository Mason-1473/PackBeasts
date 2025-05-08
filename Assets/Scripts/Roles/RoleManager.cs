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
    private Dictionary<int, string> visionResults; // Sage
    private Dictionary<int, int> synthesisProgress; // Scientist
    private Dictionary<int, int> killAttemptRecords; // Sage
    private Dictionary<int, string> cacheResults; // Oracle
    private Dictionary<int, string> emergencyCacheResults; // Arbiter
    private Dictionary<int, int> lastResortBullets; // Arbiter
    private Dictionary<int, bool> arbiterUsedReloadThisNight; // Arbiter
    private Dictionary<int, bool> arbiterUsedEmergencyCache; // Arbiter
    private Dictionary<int, int> arbiterEmergencyCacheDay; // Arbiter
    private Dictionary<int, int> vigilanteBullets; // Vigilante 
    private Dictionary<int, bool> vigilanteUsedReloadThisNight; // Vigilante
    private Dictionary<int, int> oracleLastCacheDay; // Oracle
    private Dictionary<int, bool> scientistSelfAdministered; // Scientist
    private Dictionary<int, string> delayedVisionResults; // Sage
    private Dictionary<int, string> delayedCacheResults; // Oracle
    private Dictionary<int, string> delayedEmergencyCacheResults; // Arbiter
    private Dictionary<int, string> delayedIlluminateResults; // Radiant
    private Dictionary<int, int> shadeTargets; // Eclipse
    private Dictionary<int, int> swiftFootTargets; // Escapist
    private Dictionary<int, string> plotChoices; // Revenant
    private Dictionary<int, int> plotProgress; // Revenant
    private Dictionary<int, bool> isMarked; // Revenant
    private Dictionary<int, int> vindicatorMissions; // Vindicator
    private Dictionary<int, int> traitorAttackCounts; // Traitor
    private Dictionary<int, int> firebirdCurrentLine; // Firebird
    private Dictionary<int, List<int>> firebirdDousedTargets; // Firebird

    void Awake()
    {
        if (GetComponent<PhotonView>() == null)
        {
            gameObject.AddComponent<PhotonView>();
        }
        DontDestroyOnLoad(gameObject); // Ensure RoleManager persists across scenes
        allRoles = Resources.LoadAll<RoleAsset>("Roles").ToList();
        Debug.Log($"Loaded {allRoles.Count} roles from Resources/Roles");

        playerRoleAssignments = new Dictionary<int, RoleAsset>();
        abilityUsageCounts = new Dictionary<int, int>();
        visionResults = new Dictionary<int, string>();
        synthesisProgress = new Dictionary<int, int>();
        killAttemptRecords = new Dictionary<int, int>();
        cacheResults = new Dictionary<int, string>();
        emergencyCacheResults = new Dictionary<int, string>();
        lastResortBullets = new Dictionary<int, int>();
        arbiterUsedReloadThisNight = new Dictionary<int, bool>();
        arbiterUsedEmergencyCache = new Dictionary<int, bool>();
        arbiterEmergencyCacheDay = new Dictionary<int, int>();
        vigilanteBullets = new Dictionary<int, int>();
        vigilanteUsedReloadThisNight = new Dictionary<int, bool>();
        oracleLastCacheDay = new Dictionary<int, int>();
        scientistSelfAdministered = new Dictionary<int, bool>();
        delayedVisionResults = new Dictionary<int, string>();
        delayedCacheResults = new Dictionary<int, string>();
        delayedEmergencyCacheResults = new Dictionary<int, string>();
        delayedIlluminateResults = new Dictionary<int, string>();
        shadeTargets = new Dictionary<int, int>();
        swiftFootTargets = new Dictionary<int, int>();
        plotChoices = new Dictionary<int, string>();
        plotProgress = new Dictionary<int, int>();
        isMarked = new Dictionary<int, bool>();
        vindicatorMissions = new Dictionary<int, int>();
        traitorAttackCounts = new Dictionary<int, int>();
        firebirdCurrentLine = new Dictionary<int, int>();
        firebirdDousedTargets = new Dictionary<int, List<int>>();
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
        var players = GameManager.Instance.PlayerInfos.Keys.ToArray(); // Use all registered players from GameManager
        int playerCount = players.Length;
        Debug.Log($"Assigning roles for {playerCount} players");

        // Step 1: Assign Dominion (target ~11, range [0-19])
        int dominionCount = 11; // Target
        if (Random.value < 0.3f) // 30% chance to deviate significantly from target
        {
            dominionCount = Random.Range(0, Mathf.Min(20, playerCount + 1)); // Range [0-19], capped by player count
        }
        else if (Random.value < 0.6f) // 30% chance for slight deviation (60% total deviation chance)
        {
            dominionCount = Random.Range(9, 14); // Slight deviation around 11 (9-13)
        }
        // Ensure Dominion doesn't exceed available players
        dominionCount = Mathf.Min(dominionCount, playerCount);

        // Step 2: Assign Pack (target ~5, range [1-5])
        int packCount = 5; // Target
        if (Random.value < 0.3f) // 30% chance to deviate
        {
            packCount = Random.Range(1, 6); // Range [1-5]
        }
        else if (Random.value < 0.6f) // 30% chance for slight deviation
        {
            packCount = Random.Range(4, 6); // Slight deviation around 5 (4-5)
        }
        // Ensure Pack doesn't exceed remaining players after Dominion and meets minimum of 1
        packCount = Mathf.Min(packCount, playerCount - dominionCount);
        packCount = Mathf.Max(packCount, 1);

        // Step 3: Calculate remaining players for Neutral Killers and Outsiders
        int remainingPlayers = playerCount - dominionCount - packCount;

        // Step 4: Assign Neutral Killers (more frequent if Pack < 5)
        int neutralKillerCount = 0;
        if (remainingPlayers > 0)
        {
            // Base allocation: Split remaining players between Neutral Killers and Outsiders
            neutralKillerCount = remainingPlayers / 2;
            // Increase Neutral Killers if Pack is less than 5
            if (packCount < 5)
            {
                float packDeficitFactor = (5 - packCount) / 5f; // E.g., if Pack=3, factor=0.4
                int additionalNK = Mathf.RoundToInt(remainingPlayers * packDeficitFactor * 0.5f); // Add up to 50% more based on deficit
                neutralKillerCount += additionalNK;
            }
            neutralKillerCount = Mathf.Min(neutralKillerCount, remainingPlayers); // Cap by remaining players
        }

        // Step 5: Assign Outsiders (more frequent if Pack < 5, only if 2+ other factions)
        int outsiderCount = 0;
        int remainingAfterNK = remainingPlayers - neutralKillerCount;

        // Check if at least 2 other factions are present
        int factionCount = 0;
        if (dominionCount > 0) factionCount++;
        if (packCount > 0) factionCount++; // Already guaranteed to be at least 1
        if (neutralKillerCount > 0) factionCount++;

        if (factionCount >= 2 && remainingAfterNK > 0)
        {
            outsiderCount = remainingAfterNK; // Allocate remaining players to Outsiders
            // Increase Outsiders if Pack is less than 5
            if (packCount < 5)
            {
                float packDeficitFactor = (5 - packCount) / 5f;
                int additionalOutsiders = Mathf.RoundToInt(remainingAfterNK * packDeficitFactor * 0.5f);
                outsiderCount += additionalOutsiders;
            }
            outsiderCount = Mathf.Min(outsiderCount, remainingAfterNK); // Cap by remaining players
        }

        // Step 6: Adjust Neutral Killers to fill any remaining slots (since they have no upper limit)
        int finalRemaining = playerCount - dominionCount - packCount - neutralKillerCount - outsiderCount;
        if (finalRemaining > 0)
        {
            neutralKillerCount += finalRemaining; // Assign remaining slots to Neutral Killers
        }

        Debug.Log($"Faction Distribution: Dominion={dominionCount}, Pack={packCount}, NeutralKillers={neutralKillerCount}, Outsiders={outsiderCount}");

        List<RoleAsset> selectedRoles = new List<RoleAsset>();
        var dominionRoles = availableRoles.Where(r => r.category == RoleCategory.Dominion).ToList();
        var packRoles = availableRoles.Where(r => r.category == RoleCategory.Pack).ToList();
        var neutralKillerRoles = availableRoles.Where(r => r.category == RoleCategory.NeutralKiller).ToList();
        var outsiderRoles = availableRoles.Where(r => r.category == RoleCategory.Outsider).ToList();

        // Step 7: Ensure unique roles (Eclipse, Escapist, Revenant)
        var uniqueRoles = new List<string> { "Eclipse", "Escapist", "Revenant" };
        foreach (var roleName in uniqueRoles)
        {
            var role = availableRoles.FirstOrDefault(r => r.roleName == roleName && r.isUnique);
            if (role != null && Random.value > 0.5f && playerCount > 0)
            {
                selectedRoles.Add(role);
                availableRoles.Remove(role);
                if (role.category == RoleCategory.Pack)
                    packCount--;
                else if (role.category == RoleCategory.NeutralKiller)
                    neutralKillerCount--;
                Debug.Log($"Assigned unique role: {role.roleName}");
            }
            else
            {
                Debug.LogWarning($"Unique role {roleName} not included (not found, random skip, or no players left).");
            }
        }

        // Step 8: Assign Dominion roles
        for (int i = 0; i < dominionCount && dominionRoles.Count > 0 && selectedRoles.Count < playerCount; i++)
        {
            int idx = Random.Range(0, dominionRoles.Count);
            var role = dominionRoles[idx];
            selectedRoles.Add(role);
            if (role.isUnique)
            {
                availableRoles.Remove(role);
                dominionRoles.RemoveAt(idx);
            }
            Debug.Log($"Assigned Dominion role: {role.roleName} (Unique: {role.isUnique})");
        }

        // Step 9: Assign Pack roles
        for (int i = 0; i < packCount && packRoles.Count > 0 && selectedRoles.Count < playerCount; i++)
        {
            int idx = Random.Range(0, packRoles.Count);
            var role = packRoles[idx];
            selectedRoles.Add(role);
            if (role.isUnique)
            {
                availableRoles.Remove(role);
                packRoles.RemoveAt(idx);
            }
            Debug.Log($"Assigned Pack role: {role.roleName} (Unique: {role.isUnique})");
        }

        // Step 10: Assign NeutralKiller roles
        for (int i = 0; i < neutralKillerCount && neutralKillerRoles.Count > 0 && selectedRoles.Count < playerCount; i++)
        {
            int idx = Random.Range(0, neutralKillerRoles.Count);
            var role = neutralKillerRoles[idx];
            selectedRoles.Add(role);
            if (role.isUnique)
            {
                availableRoles.Remove(role);
                neutralKillerRoles.RemoveAt(idx);
            }
            Debug.Log($"Assigned NeutralKiller role: {role.roleName} (Unique: {role.isUnique})");
        }

        // Step 11: Assign Outsider roles
        for (int i = 0; i < outsiderCount && outsiderRoles.Count > 0 && selectedRoles.Count < playerCount; i++)
        {
            int idx = Random.Range(0, outsiderRoles.Count);
            var role = outsiderRoles[idx];
            selectedRoles.Add(role);
            if (role.isUnique)
            {
                availableRoles.Remove(role);
                outsiderRoles.RemoveAt(idx);
            }
            Debug.Log($"Assigned Outsider role: {role.roleName} (Unique: {role.isUnique})");
        }

        // Step 12: Fill remaining slots with non-unique roles
        while (selectedRoles.Count < playerCount && availableRoles.Count > 0)
        {
            var nonUniqueRoles = availableRoles.Where(r => !r.isUnique).ToList();
            if (nonUniqueRoles.Count == 0)
            {
                Debug.LogError("No non-unique roles available to fill remaining slots. Check your role assets in Resources/Roles.");
                break;
            }
            int idx = Random.Range(0, nonUniqueRoles.Count);
            var role = nonUniqueRoles[idx];
            selectedRoles.Add(role);
            Debug.Log($"Assigned additional non-unique role: {role.roleName}");
        }

        if (selectedRoles.Count < playerCount)
        {
            Debug.LogError($"Failed to select {playerCount} roles! Selected {selectedRoles.Count}. Ensure enough RoleAssets are available in Resources/Roles.");
            return;
        }

        // Step 13: Assign roles to players
        for (int i = 0; i < players.Length; i++)
        {
            int roleIdx = Random.Range(0, selectedRoles.Count);
            playerRoleAssignments[players[i]] = selectedRoles[roleIdx];
            if (selectedRoles[roleIdx].roleName == "Vindicator")
            {
                var dominionPlayers = playerRoleAssignments
                    .Where(kv => kv.Value.category == RoleCategory.Dominion && kv.Value.roleName != "Captor")
                    .Select(kv => kv.Key)
                    .ToList();
                if (dominionPlayers.Count > 0)
                {
                    vindicatorMissions[players[i]] = dominionPlayers[Random.Range(0, dominionPlayers.Count)];
                }
            }
            Debug.Log($"Assigned role {(selectedRoles[roleIdx] != null ? selectedRoles[roleIdx].roleName : "None")} to player {players[i]}");
            selectedRoles.RemoveAt(roleIdx);
        }

        photonView.RPC("SyncRoleAssignments", RpcTarget.AllBuffered, playerRoleAssignments.Keys.ToArray(), playerRoleAssignments.Values.Select(r => r.roleName).ToArray());
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
            else
            {
                Debug.LogWarning($"Failed to sync role {roleNames[i]} for ActorNumber {actorNumbers[i]}. Role not found in allRoles.");
            }
        }
        Debug.Log($"Synced roles: {string.Join(", ", playerRoleAssignments.Select(kv => $"Player {kv.Key}: {kv.Value.roleName}"))}");
        DisplayLocalPlayerRole();
    }

    void DisplayLocalPlayerRole()
    {
        Debug.Log($"Displaying role for {PhotonNetwork.LocalPlayer.NickName}, ActorNumber: {PhotonNetwork.LocalPlayer.ActorNumber}");
        if (roleUIPrefab == null)
        {
            roleUIPrefab = Resources.Load<GameObject>("RoleUIPrefab");
            if (roleUIPrefab == null)
            {
                Debug.LogError("Could not load RoleUIPrefab from Resources! Ensure 'RoleUIPrefab' is in Assets/Resources/ and named exactly 'RoleUIPrefab'.");
                return;
            }
        }
        if (uiParent == null)
        {
            uiParent = GameObject.Find("Canvas")?.transform;
            if (uiParent == null)
            {
                Debug.LogError("Canvas not found for uiParent! Ensure a Canvas exists in the scene.");
                return;
            }
        }

        int localActorNumber = PhotonNetwork.LocalPlayer.ActorNumber;
        if (!playerRoleAssignments.TryGetValue(localActorNumber, out RoleAsset localRole))
        {
            Debug.LogWarning($"No role assigned to local player (ActorNumber: {localActorNumber})");
            return;
        }

        Debug.Log($"Instantiating role UI for role: {localRole.roleName}");
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
        rect.localScale = Vector3.one;
        Debug.Log("Role UI instantiated and positioned");
    }

    public void UseAbility(AbilityType abilityType, int targetActorNumber = -1, bool isComplexSerum = false, string plotChoice = "")
    {
        int localActorNumber = PhotonNetwork.LocalPlayer.ActorNumber;
        if (!playerRoleAssignments.TryGetValue(localActorNumber, out RoleAsset localRole))
        {
            Debug.LogWarning("Cannot use ability: No role assigned to local player.");
            return;
        }

        if (GameManager.Instance.PlayerInfos.TryGetValue(localActorNumber, out PlayerInfo playerInfo) && playerInfo.hasWon)
        {
            Debug.LogWarning($"Player {localActorNumber} has already won and cannot use abilities.");
            return;
        }

        bool requiresTarget = abilityType == AbilityType.Vision || abilityType == AbilityType.Cache || 
                             abilityType == AbilityType.Administer || abilityType == AbilityType.Shoot || 
                             abilityType == AbilityType.EmergencyCache || abilityType == AbilityType.ArbiterShoot ||
                             abilityType == AbilityType.Shade || abilityType == AbilityType.SwiftFoot ||
                             abilityType == AbilityType.Illuminate || abilityType == AbilityType.Bloodthirst ||
                             abilityType == AbilityType.Douse;
        if (requiresTarget && targetActorNumber == -1)
        {
            Debug.LogWarning($"Ability {abilityType} requires a valid target.");
            return;
        }

        if (localRole.roleName == "Vigilante")
        {
            if (abilityType == AbilityType.Reload)
            {
                if (GameManager.Instance.dayCount == 1 && GameManager.Instance.currentPhase == GamePhase.Dusk)
                {
                    Debug.LogWarning("Cannot use Reload on first Dusk!");
                    return;
                }
                if (GameManager.Instance.currentPhase != GamePhase.Dusk)
                {
                    Debug.LogWarning($"Reload can only be used during Dusk phase, not {GameManager.Instance.currentPhase}");
                    return;
                }
                if (vigilanteBullets.GetValueOrDefault(localActorNumber, 0) >= 2)
                {
                    Debug.LogWarning("Vigilante already has 2 bullets!");
                    return;
                }
                vigilanteUsedReloadThisNight[localActorNumber] = true;
            }
            else if (abilityType == AbilityType.Shoot)
            {
                if (vigilanteBullets.GetValueOrDefault(localActorNumber, 0) <= 0)
                {
                    Debug.LogWarning("No bullets available to Shoot!");
                    return;
                }
            }
            if (vigilanteUsedReloadThisNight.GetValueOrDefault(localActorNumber, false) && abilityType != AbilityType.Reload)
            {
                Debug.LogWarning("Cannot use other abilities on the same night as Reload!");
                return;
            }
        }
        if (localRole.roleName == "Arbiter")
        {
            if (abilityType == AbilityType.EmergencyCache)
            {
                if (GameManager.Instance.dayCount == 1 && GameManager.Instance.currentPhase == GamePhase.Dusk)
                {
                    Debug.LogWarning("Cannot use Emergency Cache on first Dusk!");
                    return;
                }
                if (GameManager.Instance.currentPhase != GamePhase.Dusk)
                {
                    Debug.LogWarning($"Emergency Cache can only be used during Dusk phase, not {GameManager.Instance.currentPhase}");
                    return;
                }
                if (arbiterUsedEmergencyCache.GetValueOrDefault(localActorNumber, false))
                {
                    Debug.LogWarning("Emergency Cache can only be used once per game!");
                    return;
                }
            }
            else if (abilityType == AbilityType.LastResort)
            {
                if (!arbiterUsedEmergencyCache.GetValueOrDefault(localActorNumber, false))
                {
                    Debug.LogWarning("Cannot use Last Resort before using Emergency Cache!");
                    return;
                }
                if (GameManager.Instance.dayCount == 1 && GameManager.Instance.currentPhase == GamePhase.Dusk)
                {
                    Debug.LogWarning("Cannot use Last Resort on first Dusk!");
                    return;
                }
                if (GameManager.Instance.currentPhase != GamePhase.Dusk)
                {
                    Debug.LogWarning($"Last Resort can only be used during Dusk phase, not {GameManager.Instance.currentPhase}");
                    return;
                }
                if (arbiterEmergencyCacheDay.ContainsKey(localActorNumber) && GameManager.Instance.dayCount == arbiterEmergencyCacheDay[localActorNumber] + 1)
                {
                    Debug.LogWarning("Cannot use Last Resort the Dusk after Emergency Cache!");
                    return;
                }
                if (lastResortBullets.GetValueOrDefault(localActorNumber, 0) >= 1)
                {
                    Debug.LogWarning("Arbiter already has 1 bullet!");
                    return;
                }
                arbiterUsedReloadThisNight[localActorNumber] = true;
            }
            else if (abilityType == AbilityType.ArbiterShoot)
            {
                if (lastResortBullets.GetValueOrDefault(localActorNumber, 0) <= 0)
                {
                    Debug.LogWarning("No bullets available to Shoot!");
                    return;
                }
            }
            if (arbiterUsedReloadThisNight.GetValueOrDefault(localActorNumber, false) && abilityType != AbilityType.LastResort)
            {
                Debug.LogWarning("Cannot use other abilities on the same night as Reload!");
                return;
            }
        }
        if (localRole.roleName == "Oracle" && abilityType == AbilityType.Cache)
        {
            if (GameManager.Instance.dayCount == 1 && GameManager.Instance.currentPhase == GamePhase.Dusk)
            {
                Debug.LogWarning("Cannot use Cache on first Dusk!");
                return;
            }
            if (GameManager.Instance.currentPhase != GamePhase.Dusk)
            {
                Debug.LogWarning($"Cache can only be used during Dusk phase, not {GameManager.Instance.currentPhase}");
                return;
            }
            if (oracleLastCacheDay.ContainsKey(localActorNumber) && GameManager.Instance.dayCount == oracleLastCacheDay[localActorNumber] + 1)
            {
                Debug.LogWarning("Cannot use Cache on consecutive Dusks!");
                return;
            }
        }
        if (localRole.roleName == "Scientist")
        {
            if (abilityType == AbilityType.Synthesis)
            {
                if (GameManager.Instance.currentPhase != GamePhase.Dusk)
                {
                    Debug.LogWarning($"Synthesis can only be used during Dusk phase, not {GameManager.Instance.currentPhase}");
                    return;
                }
                if (synthesisProgress.ContainsKey(localActorNumber))
                {
                    Debug.LogWarning("Already synthesizing a Serum!");
                    return;
                }
            }
            else if (abilityType == AbilityType.Administer)
            {
                if (GameManager.Instance.currentPhase != GamePhase.Night)
                {
                    Debug.LogWarning($"Administer can only be used during Night phase, not {GameManager.Instance.currentPhase}");
                    return;
                }
                if (!synthesisProgress.ContainsKey(localActorNumber) || synthesisProgress[localActorNumber] < 1)
                {
                    Debug.LogWarning("No Serum available to administer!");
                    return;
                }
                if (targetActorNumber == localActorNumber)
                {
                    if (synthesisProgress[localActorNumber] >= 2)
                    {
                        Debug.LogWarning("Cannot administer Complex Serum to self!");
                        return;
                    }
                    if (scientistSelfAdministered.GetValueOrDefault(localActorNumber, false))
                    {
                        Debug.LogWarning("Can only administer Simple Serum to self once per game!");
                        return;
                    }
                }
            }
        }
        if (localRole.roleName == "Eclipse" && abilityType == AbilityType.Shade)
        {
            if (GameManager.Instance.currentPhase != GamePhase.Dusk)
            {
                Debug.LogWarning($"Shade can only be used during Dusk phase, not {GameManager.Instance.currentPhase}");
                return;
            }
            if (!playerRoleAssignments.TryGetValue(targetActorNumber, out RoleAsset targetRole) || targetRole.category != RoleCategory.Pack)
            {
                Debug.LogWarning("Shade can only target Pack members!");
                return;
            }
        }
        if (localRole.roleName == "Escapist" && abilityType == AbilityType.SwiftFoot)
        {
            if (GameManager.Instance.currentPhase != GamePhase.Dusk)
            {
                Debug.LogWarning($"Swift Foot can only be used during Dusk phase, not {GameManager.Instance.currentPhase}");
                return;
            }
            if (!playerRoleAssignments.TryGetValue(targetActorNumber, out RoleAsset targetRole) || targetRole.category != RoleCategory.Pack)
            {
                Debug.LogWarning("Swift Foot can only target Pack members!");
                return;
            }
        }
        if (localRole.roleName == "Revenant" && abilityType == AbilityType.Plot)
        {
            if (GameManager.Instance.currentPhase != GamePhase.Dusk)
            {
                Debug.LogWarning($"Plot can only be used during Dusk phase, not {GameManager.Instance.currentPhase}");
                return;
            }
            if (string.IsNullOrEmpty(plotChoice) || (plotChoice != "Rampage" && plotChoice != "Dominant"))
            {
                Debug.LogWarning("Plot requires a valid choice: Rampage or Dominant!");
                return;
            }
            if (plotProgress.GetValueOrDefault(localActorNumber, 0) > 0)
            {
                Debug.LogWarning("Already plotting!");
                return;
            }
        }
        if (localRole.roleName == "Revenant" && abilityType == AbilityType.Enact)
        {
            if (GameManager.Instance.currentPhase != GamePhase.Night)
            {
                Debug.LogWarning($"Enact can only be used during Night phase, not {GameManager.Instance.currentPhase}");
                return;
            }
            if (!isMarked.GetValueOrDefault(localActorNumber, false))
            {
                Debug.LogWarning("Cannot Enact: Player is not Marked!");
                return;
            }
            if (plotProgress.GetValueOrDefault(localActorNumber, 0) < 2)
            {
                Debug.LogWarning("Cannot Enact: Plot not ready!");
                return;
            }
        }
        if (localRole.roleName == "Radiant" && abilityType == AbilityType.Illuminate)
        {
            if (GameManager.Instance.currentPhase != GamePhase.Dusk)
            {
                Debug.LogWarning($"Illuminate can only be used during Dusk phase, not {GameManager.Instance.currentPhase}");
                return;
            }
        }
        if (localRole.roleName == "Firebird" && abilityType == AbilityType.Swap)
        {
            if (GameManager.Instance.currentPhase != GamePhase.Dusk)
            {
                Debug.LogWarning($"Swap can only be used during Dusk phase, not {GameManager.Instance.currentPhase}");
                return;
            }
        }
        if (localRole.roleName == "Firebird" && (abilityType == AbilityType.Douse || abilityType == AbilityType.Ignite))
        {
            if (GameManager.Instance.currentPhase != GamePhase.Night)
            {
                Debug.LogWarning($"{abilityType} can only be used during Night phase, not {GameManager.Instance.currentPhase}");
                return;
            }
        }

        bool canUse = false;
        int usageLimit = 0;

        if (GameManager.Instance.currentPhase == GamePhase.Day && abilityType == localRole.dayAbility)
        {
            canUse = true;
            usageLimit = localRole.dayAbilityUsageLimit1;
        }
        else if (GameManager.Instance.currentPhase == GamePhase.Dusk && (abilityType == localRole.duskAbility1 || abilityType == localRole.duskAbility2))
        {
            canUse = true;
            usageLimit = (abilityType == localRole.duskAbility1) ? localRole.duskAbilityUsageLimit1 : localRole.duskAbilityUsageLimit2;
        }
        else if (GameManager.Instance.currentPhase == GamePhase.Night && abilityType == localRole.nightAbility1)
        {
            canUse = true;
            usageLimit = localRole.nightAbilityUsageLimit1;
        }
        else if (GameManager.Instance.currentPhase == GamePhase.Night && abilityType == localRole.nightAbility2)
        {
            canUse = true;
            usageLimit = localRole.nightAbilityUsageLimit2;
        }

        if (!canUse)
        {
            Debug.LogWarning($"Cannot use ability {abilityType} during {GameManager.Instance.currentPhase} phase.");
            return;
        }

        int usageKey = localActorNumber * 1000 + (int)abilityType;
        if (!abilityUsageCounts.ContainsKey(usageKey)) abilityUsageCounts[usageKey] = 0;

        if (localRole.roleName == "Oracle" && abilityType == AbilityType.Cache)
        {
            if (abilityUsageCounts.GetValueOrDefault(usageKey, 0) >= 2)
            {
                Debug.LogWarning("Cache can only be used twice per game!");
                return;
            }
        }

        if (usageLimit > 0 && abilityUsageCounts[usageKey] >= usageLimit)
        {
            Debug.LogWarning($"Ability {abilityType} has reached its usage limit ({usageLimit}).");
            return;
        }

        abilityUsageCounts[usageKey]++;
        Debug.Log($"Player {localActorNumber} using ability: {abilityType} for role {localRole.roleName}");
        photonView.RPC("ExecuteAbility", RpcTarget.All, localActorNumber, (int)abilityType, targetActorNumber, isComplexSerum, plotChoice);
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
    void ExecuteAbility(int actorNumber, int abilityTypeInt, int targetActorNumber, bool isComplexSerum, string plotChoice)
    {
        AbilityType abilityType = (AbilityType)abilityTypeInt;
        if (!playerRoleAssignments.TryGetValue(actorNumber, out RoleAsset role))
        {
            Debug.LogWarning($"Player {actorNumber} has no assigned role for ability {abilityType}");
            return;
        }

        Debug.Log($"Player {actorNumber} executed ability: {abilityType} for role {role.roleName}");
        GameObject playerObj = PhotonNetwork.CurrentRoom.Players.ContainsKey(actorNumber) ? PhotonNetwork.CurrentRoom.Players[actorNumber].TagObject as GameObject : null;
        if (playerObj == null && GameManager.Instance.PlayerInfos.ContainsKey(actorNumber))
        {
            playerObj = GameManager.Instance.PlayerInfos[actorNumber].gameObject; // Fallback to PlayerInfo gameObject
        }
        if (playerObj == null)
        {
            Debug.LogWarning($"Player {actorNumber} GameObject not found.");
            return;
        }

        int currentDay = GameManager.Instance.dayCount;

        switch (abilityType)
        {
            case AbilityType.Reload:
                if (role.roleName == "Vigilante" && PhotonNetwork.IsMasterClient)
                {
                    vigilanteBullets[actorNumber] = vigilanteBullets.GetValueOrDefault(actorNumber, 0) + 1;
                    Debug.Log($"Player {actorNumber} reloaded. Bullets: {vigilanteBullets[actorNumber]}");
                    photonView.RPC("SyncVigilanteBullets", RpcTarget.AllBuffered, actorNumber, vigilanteBullets[actorNumber]);
                }
                break;

            case AbilityType.Shoot:
                if (role.roleName == "Vigilante")
                {
                    if (vigilanteBullets.GetValueOrDefault(actorNumber, 0) <= 0)
                    {
                        Debug.LogWarning($"Player {actorNumber} has no bullets to Shoot!");
                        return;
                    }
                    if (targetActorNumber != -1 && (PhotonNetwork.CurrentRoom.Players.ContainsKey(targetActorNumber) || GameManager.Instance.PlayerInfos.ContainsKey(targetActorNumber)) && targetActorNumber != actorNumber)
                    {
                        int bulletNumber = vigilanteBullets[actorNumber];
                        BaseAttackLevel attackLevel = bulletNumber == 2 ? BaseAttackLevel.Charged : BaseAttackLevel.Dominant;
                        vigilanteBullets[actorNumber]--;
                        photonView.RPC("SyncVigilanteBullets", RpcTarget.AllBuffered, actorNumber, vigilanteBullets[actorNumber]);
                        RecordKillAttempt(actorNumber, currentDay);
                        GameManager.Instance.ResolveAttack(actorNumber, targetActorNumber, new AttackInstance(attackLevel));

                        if (bulletNumber == 2 && playerRoleAssignments.TryGetValue(targetActorNumber, out RoleAsset targetRole) && targetRole.category == RoleCategory.Dominion)
                        {
                            vigilanteBullets[actorNumber] = 0;
                            photonView.RPC("SyncVigilanteBullets", RpcTarget.AllBuffered, actorNumber, 0);
                            Debug.Log($"Player {actorNumber} killed a Dominion member. Second bullet lost.");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"Invalid target {targetActorNumber} for Shoot.");
                    }
                }
                break;

            case AbilityType.EmergencyCache:
                if (role.roleName == "Arbiter")
                {
                    if (targetActorNumber != -1 && (PhotonNetwork.CurrentRoom.Players.ContainsKey(targetActorNumber) || GameManager.Instance.PlayerInfos.ContainsKey(targetActorNumber)) && targetActorNumber != actorNumber)
                    {
                        string result = GetEmergencyCacheResult(targetActorNumber);
                        delayedEmergencyCacheResults[actorNumber] = $"Emergency Cache result: {result}";
                        arbiterUsedEmergencyCache[actorNumber] = true;
                        arbiterEmergencyCacheDay[actorNumber] = currentDay;
                        Debug.Log($"Player {actorNumber} used Emergency Cache on Player {targetActorNumber}. Result pending.");
                        if (PhotonNetwork.IsMasterClient && playerRoleAssignments.TryGetValue(targetActorNumber, out RoleAsset targetRole) && targetRole.category != RoleCategory.Dominion)
                        {
                            photonView.RPC("NotifyTargetOfArbiter", RpcTarget.AllBuffered, targetActorNumber, actorNumber, role.roleName);
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"Invalid target {targetActorNumber} for Emergency Cache.");
                    }
                }
                break;

            case AbilityType.LastResort:
                if (role.roleName == "Arbiter" && PhotonNetwork.IsMasterClient)
                {
                    lastResortBullets[actorNumber] = 1;
                    Debug.Log($"Player {actorNumber} reloaded. Bullets: {lastResortBullets[actorNumber]}");
                    photonView.RPC("SyncArbiterBullets", RpcTarget.AllBuffered, actorNumber, lastResortBullets[actorNumber]);
                }
                break;

            case AbilityType.ArbiterShoot:
                if (role.roleName == "Arbiter")
                {
                    if (lastResortBullets.GetValueOrDefault(actorNumber, 0) <= 0)
                    {
                        Debug.LogWarning($"Player {actorNumber} has no bullets to Shoot!");
                        return;
                    }
                    if (targetActorNumber != -1 && (PhotonNetwork.CurrentRoom.Players.ContainsKey(targetActorNumber) || GameManager.Instance.PlayerInfos.ContainsKey(targetActorNumber)) && targetActorNumber != actorNumber)
                    {
                        lastResortBullets[actorNumber]--;
                        photonView.RPC("SyncArbiterBullets", RpcTarget.AllBuffered, actorNumber, lastResortBullets[actorNumber]);
                        RecordKillAttempt(actorNumber, currentDay);
                        GameManager.Instance.ResolveAttack(actorNumber, targetActorNumber, new AttackInstance(BaseAttackLevel.Charged));
                    }
                    else
                    {
                        Debug.LogWarning($"Invalid target {targetActorNumber} for ArbiterShoot.");
                    }
                }
                break;

            case AbilityType.Vision:
                if (role.roleName == "Sage")
                {
                    if (targetActorNumber != -1 && (PhotonNetwork.CurrentRoom.Players.ContainsKey(targetActorNumber) || GameManager.Instance.PlayerInfos.ContainsKey(targetActorNumber)) && targetActorNumber != actorNumber)
                    {
                        string result = GetVisionResult(targetActorNumber, currentDay);
                        delayedVisionResults[actorNumber] = $"Vision result: {result}";
                        Debug.Log($"Player {actorNumber} used Vision on Player {targetActorNumber}. Result pending.");
                    }
                    else
                    {
                        Debug.LogWarning($"Invalid target {targetActorNumber} for Vision.");
                    }
                }
                break;

            case AbilityType.Cache:
                if (role.roleName == "Oracle")
                {
                    if (targetActorNumber != -1 && (PhotonNetwork.CurrentRoom.Players.ContainsKey(targetActorNumber) || GameManager.Instance.PlayerInfos.ContainsKey(targetActorNumber)) && targetActorNumber != actorNumber)
                    {
                        string result = GetCacheResult(targetActorNumber);
                        delayedCacheResults[actorNumber] = $"Cache result: {result}";
                        oracleLastCacheDay[actorNumber] = currentDay;
                        Debug.Log($"Player {actorNumber} used Cache on Player {targetActorNumber}. Result pending.");
                        if (PhotonNetwork.IsMasterClient && playerRoleAssignments.TryGetValue(targetActorNumber, out RoleAsset targetRole) && targetRole.category != RoleCategory.Dominion)
                        {
                            photonView.RPC("NotifyTargetOfOracle", RpcTarget.AllBuffered, targetActorNumber, actorNumber, role.roleName);
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"Invalid target {targetActorNumber} for Cache.");
                    }
                }
                break;

            case AbilityType.Synthesis:
                if (role.roleName == "Scientist")
                {
                    synthesisProgress[actorNumber] = isComplexSerum ? 1 : 2;
                    Debug.Log($"Player {actorNumber} started synthesizing a {(isComplexSerum ? "Complex" : "Simple")} Serum. Progress: {synthesisProgress[actorNumber]}/{(isComplexSerum ? 2 : 1)}");
                }
                break;

            case AbilityType.Administer:
                if (role.roleName == "Scientist")
                {
                    if (!synthesisProgress.ContainsKey(actorNumber) || synthesisProgress[actorNumber] < 1)
                    {
                        Debug.LogWarning($"Player {actorNumber} has no Serum to administer.");
                        return;
                    }
                    bool isComplex = synthesisProgress[actorNumber] >= 2;
                    if ((targetActorNumber != -1 && (PhotonNetwork.CurrentRoom.Players.ContainsKey(targetActorNumber) || GameManager.Instance.PlayerInfos.ContainsKey(targetActorNumber))) || targetActorNumber == actorNumber)
                    {
                        string defenseLevel = isComplex ? "Fortified" : "Shielded";
                        GameManager.Instance.SetDefenseLevel(targetActorNumber, defenseLevel);
                        Debug.Log($"Player {actorNumber} administered a {defenseLevel} Serum to Player {targetActorNumber}.");
                        if (targetActorNumber == actorNumber)
                        {
                            scientistSelfAdministered[actorNumber] = true;
                        }
                        synthesisProgress.Remove(actorNumber);
                    }
                    else
                    {
                        Debug.LogWarning($"Invalid target {targetActorNumber} for Administer.");
                    }
                }
                break;

            case AbilityType.Brilliance:
                if (role.roleName == "Eclipse")
                {
                    photonView.RPC("HideVotesAndDouble", RpcTarget.All, actorNumber);
                }
                break;

            case AbilityType.Shade:
                if (role.roleName == "Eclipse")
                {
                    shadeTargets[actorNumber] = targetActorNumber;
                    Debug.Log($"Player {actorNumber} used Shade on Player {targetActorNumber}.");
                }
                break;

            case AbilityType.SwiftFoot:
                if (role.roleName == "Escapist")
                {
                    swiftFootTargets[actorNumber] = targetActorNumber;
                    Debug.Log($"Player {actorNumber} used Swift Foot on Player {targetActorNumber}.");
                }
                break;

            case AbilityType.Plot:
                if (role.roleName == "Revenant")
                {
                    plotChoices[actorNumber] = plotChoice;
                    plotProgress[actorNumber] = 1;
                    Debug.Log($"Player {actorNumber} started Plot: {plotChoice}. Progress: 1/2");
                }
                break;

            case AbilityType.Enact:
                if (role.roleName == "Revenant")
                {
                    if (targetActorNumber != -1 && (PhotonNetwork.CurrentRoom.Players.ContainsKey(targetActorNumber) || GameManager.Instance.PlayerInfos.ContainsKey(targetActorNumber)) && targetActorNumber != actorNumber)
                    {
                        string choice = plotChoices.GetValueOrDefault(actorNumber, "Dominant");
                        bool isRampage = choice == "Rampage";
                        BaseAttackLevel attackLevel = isRampage ? BaseAttackLevel.Dominant : BaseAttackLevel.Dominant;
                        GameManager.Instance.ResolveAttack(actorNumber, targetActorNumber, new AttackInstance(attackLevel, isRampage));
                        plotChoices.Remove(actorNumber);
                        plotProgress.Remove(actorNumber);
                        RecordKillAttempt(actorNumber, currentDay);
                    }
                    else
                    {
                        Debug.LogWarning($"Invalid target {targetActorNumber} for Enact.");
                    }
                }
                break;

            case AbilityType.Illuminate:
                if (role.roleName == "Radiant")
                {
                    if (targetActorNumber != -1 && (PhotonNetwork.CurrentRoom.Players.ContainsKey(targetActorNumber) || GameManager.Instance.PlayerInfos.ContainsKey(targetActorNumber)) && targetActorNumber != actorNumber)
                    {
                        string result = GetIlluminateResult(targetActorNumber);
                        delayedIlluminateResults[actorNumber] = $"Illuminate result: {result}";
                        Debug.Log($"Player {actorNumber} used Illuminate on Player {targetActorNumber}. Result pending.");
                    }
                    else
                    {
                        Debug.LogWarning($"Invalid target {targetActorNumber} for Illuminate.");
                    }
                }
                break;

            case AbilityType.Bloodthirst:
                if (role.roleName == "Traitor")
                {
                    if (targetActorNumber != -1 && (PhotonNetwork.CurrentRoom.Players.ContainsKey(targetActorNumber) || GameManager.Instance.PlayerInfos.ContainsKey(targetActorNumber)) && targetActorNumber != actorNumber)
                    {
                        if (playerRoleAssignments.TryGetValue(targetActorNumber, out RoleAsset targetRole) && targetRole.roleName == "Traitor")
                        {
                            photonView.RPC("RevealTraitors", RpcTarget.All, actorNumber, targetActorNumber);
                        }
                        else
                        {
                            int attackCount = traitorAttackCounts.GetValueOrDefault(actorNumber, 0) + 1;
                            traitorAttackCounts[actorNumber] = attackCount;
                            BaseAttackLevel attackLevel = attackCount >= 4 ? BaseAttackLevel.Inexorable : (attackCount >= 2 ? BaseAttackLevel.Dominant : BaseAttackLevel.Charged);
                            GameManager.Instance.ResolveAttack(actorNumber, targetActorNumber, new AttackInstance(attackLevel));
                            RecordKillAttempt(actorNumber, currentDay);
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"Invalid target {targetActorNumber} for Bloodthirst.");
                    }
                }
                break;

            case AbilityType.Swap:
                if (role.roleName == "Firebird")
                {
                    int currentLine = firebirdCurrentLine.GetValueOrDefault(actorNumber, 1);
                    currentLine = (currentLine % 3) + 1;
                    firebirdCurrentLine[actorNumber] = currentLine;
                    Debug.Log($"Player {actorNumber} swapped to Line {currentLine}.");
                }
                break;

            case AbilityType.Douse:
                if (role.roleName == "Firebird")
                {
                    if (targetActorNumber != -1 && (PhotonNetwork.CurrentRoom.Players.ContainsKey(targetActorNumber) || GameManager.Instance.PlayerInfos.ContainsKey(targetActorNumber)))
                    {
                        int currentLine = firebirdCurrentLine.GetValueOrDefault(actorNumber, 1);
                        if (!firebirdDousedTargets.ContainsKey(currentLine))
                        {
                            firebirdDousedTargets[currentLine] = new List<int>();
                        }
                        firebirdDousedTargets[currentLine].Add(targetActorNumber);
                        Debug.Log($"Player {actorNumber} doused Player {targetActorNumber} on Line {currentLine}.");
                    }
                    else
                    {
                        Debug.LogWarning($"Invalid target {targetActorNumber} for Douse.");
                    }
                }
                break;

            case AbilityType.Ignite:
                if (role.roleName == "Firebird")
                {
                    foreach (var line in firebirdDousedTargets)
                    {
                        foreach (var target in line.Value)
                        {
                            if (PhotonNetwork.CurrentRoom.Players.ContainsKey(target) || GameManager.Instance.PlayerInfos.ContainsKey(target))
                            {
                                GameManager.Instance.ResolveAttack(actorNumber, target, new AttackInstance(BaseAttackLevel.Inexorable, false, true));
                            }
                        }
                    }
                    firebirdDousedTargets.Clear();
                    Debug.Log($"Player {actorNumber} ignited all doused targets.");
                    RecordKillAttempt(actorNumber, currentDay);
                }
                break;
        }
    }

    [PunRPC]
    void SyncVigilanteBullets(int actorNumber, int bulletCount)
    {
        vigilanteBullets[actorNumber] = bulletCount;
        Debug.Log($"Synced Vigilante bullets for Player {actorNumber}: {bulletCount}");
    }

    [PunRPC]
    void SyncArbiterBullets(int actorNumber, int bulletCount)
    {
        lastResortBullets[actorNumber] = bulletCount;
        Debug.Log($"Synced Arbiter bullets for Player {actorNumber}: {bulletCount}");
    }

    [PunRPC]
    void NotifyTargetOfArbiter(int targetActorNumber, int arbiterActorNumber, string arbiterRoleName)
    {
        if (PhotonNetwork.LocalPlayer.ActorNumber == targetActorNumber)
        {
            Debug.Log($"Player {arbiterActorNumber} (Arbiter, {arbiterRoleName}) used Emergency Cache on you!");
        }
    }

    [PunRPC]
    void NotifyTargetOfOracle(int targetActorNumber, int oracleActorNumber, string oracleRoleName)
    {
        if (PhotonNetwork.LocalPlayer.ActorNumber == targetActorNumber)
        {
            Debug.Log($"Player {oracleActorNumber} (Oracle, {oracleRoleName}) used Cache on you!");
        }
    }

    [PunRPC]
    void HideVotesAndDouble(int actorNumber)
    {
        Debug.Log($"Player {actorNumber} used Brilliance: Votes hidden, vote doubled.");
    }

    [PunRPC]
    void RevealTraitors(int actorNumber, int targetActorNumber)
    {
        if (PhotonNetwork.LocalPlayer.ActorNumber == actorNumber)
        {
            Debug.Log($"Player {targetActorNumber} is also a Traitor!");
        }
        if (PhotonNetwork.LocalPlayer.ActorNumber == targetActorNumber)
        {
            Debug.Log($"Player {actorNumber} is also a Traitor!");
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
        if (shadeTargets.ContainsValue(targetActorNumber))
        {
            return "Your target's role is a Dominion role.";
        }
        return $"Your target's role is {targetRole.roleName}";
    }

    string GetEmergencyCacheResult(int targetActorNumber)
    {
        if (!playerRoleAssignments.TryGetValue(targetActorNumber, out RoleAsset targetRole))
            return "Target not found.";
        return $"Your target's role is {targetRole.roleName}";
    }

    string GetIlluminateResult(int targetActorNumber)
    {
        if (!playerRoleAssignments.TryGetValue(targetActorNumber, out RoleAsset targetRole))
            return "Target not found.";
        if (shadeTargets.ContainsValue(targetActorNumber))
        {
            return "Your target's role is a Dominion role.";
        }
        return $"Your target's role is {targetRole.roleName}";
    }

    public bool IsVindicatorMissionComplete(int actorNumber)
    {
        if (vindicatorMissions.TryGetValue(actorNumber, out int targetActorNumber))
        {
            return !PhotonNetwork.CurrentRoom.Players.ContainsKey(targetActorNumber) || 
                   (GameManager.Instance.PlayerInfos.TryGetValue(targetActorNumber, out PlayerInfo targetInfo) && targetInfo.hasWon);
        }
        return false;
    }

    public void ResetVigilanteReloadFlag()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            vigilanteUsedReloadThisNight.Clear();
            arbiterUsedReloadThisNight.Clear();
            photonView.RPC("SyncVigilanteReloadFlag", RpcTarget.All);
        }
    }

    [PunRPC]
    void SyncVigilanteReloadFlag()
    {
        vigilanteUsedReloadThisNight.Clear();
        arbiterUsedReloadThisNight.Clear();
        Debug.Log("Reset Vigilante and Arbiter Reload flags");
    }

    public void DeliverDelayedResults()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            foreach (var kvp in delayedVisionResults)
            {
                int actorNumber = kvp.Key;
                string result = kvp.Value;
                visionResults[actorNumber] = result;
                photonView.RPC("SyncDelayedResult", RpcTarget.AllBuffered, actorNumber, "Vision", result);
            }
            foreach (var kvp in delayedCacheResults)
            {
                int actorNumber = kvp.Key;
                string result = kvp.Value;
                cacheResults[actorNumber] = result;
                photonView.RPC("SyncDelayedResult", RpcTarget.AllBuffered, actorNumber, "Cache", result);
            }
            foreach (var kvp in delayedEmergencyCacheResults)
            {
                int actorNumber = kvp.Key;
                string result = kvp.Value;
                emergencyCacheResults[actorNumber] = result;
                photonView.RPC("SyncDelayedResult", RpcTarget.AllBuffered, actorNumber, "EmergencyCache", result);
            }
            foreach (var kvp in delayedIlluminateResults)
            {
                int actorNumber = kvp.Key;
                string result = kvp.Value;
                delayedIlluminateResults[actorNumber] = result;
                photonView.RPC("SyncDelayedResult", RpcTarget.AllBuffered, actorNumber, "Illuminate", result);
            }
            delayedVisionResults.Clear();
            delayedCacheResults.Clear();
            delayedEmergencyCacheResults.Clear();
            delayedIlluminateResults.Clear();
        }
    }

    [PunRPC]
    void SyncDelayedResult(int actorNumber, string abilityType, string result)
    {
        if (abilityType == "Vision")
            visionResults[actorNumber] = result;
        else if (abilityType == "Cache")
            cacheResults[actorNumber] = result;
        else if (abilityType == "EmergencyCache")
            emergencyCacheResults[actorNumber] = result;
        else if (abilityType == "Illuminate")
            delayedIlluminateResults[actorNumber] = result;
        if (PhotonNetwork.LocalPlayer.ActorNumber == actorNumber)
        {
            Debug.Log($"Received {abilityType} result: {result}");
        }
    }

    public void GrantChargedAttack(Player player)
    {
        if (GameManager.Instance.PlayerInfos.TryGetValue(player.ActorNumber, out PlayerInfo playerInfo))
        {
            playerInfo.attackInstance.baseLevel = BaseAttackLevel.Charged;
            photonView.RPC("SyncAttackLevel", RpcTarget.All, player.ActorNumber, "Charged");
        }
    }

    [PunRPC]
    private void SyncAttackLevel(int actorNumber, string attackLevel)
    {
        Player player = PhotonNetwork.PlayerList.FirstOrDefault(p => p.ActorNumber == actorNumber);
        if (player != null && GameManager.Instance.PlayerInfos.TryGetValue(actorNumber, out PlayerInfo playerInfo))
        {
            playerInfo.attackInstance.baseLevel = (BaseAttackLevel)System.Enum.Parse(typeof(BaseAttackLevel), attackLevel);
        }
    }
}