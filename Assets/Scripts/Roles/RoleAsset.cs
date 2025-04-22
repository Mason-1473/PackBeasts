using UnityEngine;

public enum RoleCategory { Dominion, Pack, NeutralKiller, Outsider }

public enum AbilityType
{
    None,
    Intrude, Vision, Cache, EmergencyCache,
    Synthesis, Administer, Barricade,
    Reload, Shoot, LastResort,
    Capture, Detain, Execution, Restrain,
    Brilliance, Shade, SwiftFoot, Shift,
    Plot, Enact, Trap,
    Illuminate, Obstacle,
    Starfield, Bisect,
    Bloodthirst, Care, Resurgence, Ferocity, Countdown, MeteorShower, Twilight, Blot, Ravish
}

[CreateAssetMenu(fileName = "NewRole", menuName = "Role Asset")]
public class RoleAsset : ScriptableObject
{
    public string roleName;
    public Sprite icon;
    public RoleCategory category;
    public bool isUnique;
    public bool pairedRole;
    public RoleAsset pairedWith;
    public AbilityType dayAbility;
    public AbilityType duskAbility;
    public AbilityType nightAbility;
    public float abilityValue;
    public float abilityDuration;
    public float abilityRange;
    public int abilityUsageLimit;
}