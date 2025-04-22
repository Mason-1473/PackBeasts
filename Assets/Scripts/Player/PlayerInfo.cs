using UnityEngine;

public class PlayerInfo : MonoBehaviour
{
    public string playerName;
    public int actorNumber;
    public RoleAsset assignedRole;
    public string faction;

    // Status Effects
    public bool isDetained;
    public bool isBarricaded;

    // Persistent Status Effects
    public bool isBlotted;
    public bool isShifted;

    // Attack Info
    public AttackInstance attackInstance = new AttackInstance(BaseAttackLevel.None);

    // Methods to clear effects
    public void ClearTemporaryStatusEffects()
    {
        isDetained = false;
        isBarricaded = false;
    }

    public void ClearPersistentStatusEffects()
    {
        isBlotted = false;
        isShifted = false;
    }

    public void ClearAllStatusEffects() // Only call if you're sure
    {
        ClearTemporaryStatusEffects();
        ClearPersistentStatusEffects();
    }
}
