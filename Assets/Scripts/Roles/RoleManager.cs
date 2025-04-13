using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RoleManager : MonoBehaviour  // Or RoleList if renamed
{
    public int totalRoles = 20;
    public GameObject roleUIPrefab;
    public Transform uiParent;
    private List<RoleAsset> selectedRoles;
    private List<RoleAsset> allRoles;

    void Awake()
    {
        allRoles = Resources.LoadAll<RoleAsset>("Roles").ToList();
        Debug.Log($"Loaded {allRoles.Count} roles from Resources/Roles");
    }

    void Start()
{
    if (roleUIPrefab == null) Debug.LogError("roleUIPrefab is null!");
    if (uiParent == null) Debug.LogError("uiParent is null!");
    selectedRoles = GetBalancedRoles(totalRoles);
    Debug.Log("Assigned Roles:");

    // Calculate total width and starting position
    float iconWidth = 60f; // Distance between icons
    float totalWidth = (selectedRoles.Count - 1) * iconWidth; // Total width of all icons
    float startX = -totalWidth / 2; // Start from left to center the group

    for (int i = 0; i < selectedRoles.Count; i++)
    {
        GameObject roleUIObj = Instantiate(roleUIPrefab, uiParent);
        RoleUI roleUI = roleUIObj.GetComponent<RoleUI>();
        if (roleUI == null) Debug.LogError("RoleUI component missing on prefab!");
        roleUI.SetRole(selectedRoles[i]);
        RectTransform rect = roleUIObj.GetComponent<RectTransform>();
        rect.anchoredPosition = new Vector2(startX + (i * iconWidth), 0f);
    }
}
    List<RoleAsset> GetBalancedRoles(int count)
    {
        List<RoleAsset> selected = new();
        HashSet<RoleAsset> seenUnique = new();  // Tracks unique roles (across all categories)
        HashSet<RoleAsset> selectedPackRoles = new();  // Tracks distinct Pack roles

        // Step 1: Pick exactly 5 distinct Pack roles
        var packRoles = allRoles.Where(r => r.category == RoleCategory.Pack).ToList();
        int maxDistinctPackRoles = Mathf.Min(5, packRoles.Count);  // Limit to 5 or fewer if not enough Pack roles
        while (selectedPackRoles.Count < maxDistinctPackRoles)
        {
            var r = packRoles[Random.Range(0, packRoles.Count)];
            if (!selectedPackRoles.Contains(r))  // Ensure distinct Pack roles
            {
                Debug.Log($"Selected distinct Pack Role: {r.roleName}");
                selectedPackRoles.Add(r);
                AddWithPair(r, selected, seenUnique, count);
            }
        }

        // Step 2: Fill remaining Pack slots with duplicates from the 5 selected Pack roles (if needed)
        int numPack = Random.Range(1, 6);  // Still use 1–5 Pack roles total
        var availablePackRoles = selectedPackRoles.ToList();  // Only use the 5 selected Pack roles
        while (selected.Count < numPack)
        {
            var r = availablePackRoles[Random.Range(0, availablePackRoles.Count)];
            Debug.Log($"Trying to add Pack Role (duplicate allowed): {r.roleName}");
            if (r.isUnique && seenUnique.Contains(r))
            {
                Debug.Log($"Skipping unique Pack role: {r.roleName} (already added)");
                continue;
            }
            AddWithPair(r, selected, seenUnique, count);
        }

        // Step 3: Fill rest of roles with non-Pack roles
        var nonPackRoles = allRoles.Where(r => r.category != RoleCategory.Pack).ToList();
        while (selected.Count < count)
        {
            var r = nonPackRoles[Random.Range(0, nonPackRoles.Count)];
            Debug.Log($"Trying to add Non-Pack Role: {r.roleName}");
            if (r.isUnique && seenUnique.Contains(r))
            {
                Debug.Log($"Skipping unique role: {r.roleName} (already added)");
                continue;
            }
            if (r.pairedRole && selected.Count + 1 > count) continue;
            AddWithPair(r, selected, seenUnique, count);
        }

        Debug.Log($"Final selected roles: {string.Join(", ", selected.Select(r => r.roleName))}");
        return selected;
    }

    void AddWithPair(RoleAsset r, List<RoleAsset> list, HashSet<RoleAsset> seen, int maxCount)
    {
        list.Add(r);

        if (r.pairedRole && !seen.Contains(r.pairedRole) && list.Count + 1 <= maxCount)
        {
            list.Add(r.pairedRole);
            if (r.pairedRole.isUnique)
            {
                seen.Add(r.pairedRole);
            }
        }

        if (r.isUnique)
        {
            seen.Add(r);
        }
    }
}