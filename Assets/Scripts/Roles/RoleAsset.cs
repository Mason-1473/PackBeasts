using UnityEngine;

[CreateAssetMenu(fileName = "NewRole", menuName = "Game/Role")]
public class RoleAsset : ScriptableObject
{
    public string roleName;
    public RoleCategory category;
    public bool isUnique;
    public bool isAnalysis;
    public RoleAsset pairedRole;
    public Sprite icon;
}

