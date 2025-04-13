using UnityEngine;
using UnityEngine.UI;

public class RoleUI : MonoBehaviour
{
    public Image roleIcon;

    public void SetRole(RoleAsset role)
    {
        if (roleIcon == null) Debug.LogError("roleIcon is null!");
        if (role.icon == null) Debug.LogError($"No icon for role: {role.roleName}");
        roleIcon.sprite = role.icon;
        Debug.Log($"Set role {role.roleName} with icon {role.icon?.name}");
    }
}