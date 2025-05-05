using UnityEngine;

public class PlayerInfo : MonoBehaviour
{
    public string playerName;
    public int actorNumber;
    public RoleAsset assignedRole;
    public string faction;
    public string defenseLevel;
    public bool skipNextDusk;
    public bool hasWon;

    // Status Effects
    // public bool isDetained;
    // public bool isBarricaded;

    // Attack Info
    public AttackInstance attackInstance = new AttackInstance(BaseAttackLevel.None);

    // public void ClearTemporaryStatusEffects()
    // {
    //     isDetained = false;
    //     isBarricaded = false;
    // }
}