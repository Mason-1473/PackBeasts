using UnityEngine;

public enum RoleCategory { Dominion, Pack, NeutralKiller, Outsider }

public enum AbilityType
{
    None,
    //Analysis abilities - Detective, Sage, Oracle, Arbiter
    Intrude, Vision, Cache, EmergencyCache,
    //Redmedation abilities - Scientist, Scientist, Isolationist
    Synthesis, Administer, Barricade,
    //Excision abilities - Vigilante, Vigilante, Arbiter
    Reload, Shoot, LastResort,
    //Facilitation abilities - Captor, Captor, Captor, Restraint
    Capture, Detain, Execution, Restrain,
    //Subversion abilities - Eclipse, Eclipse, Escapist, Shitfter
    Brilliance, Shade, SwiftFoot, Shift,
    //Elimination abilites - Revenant, Revenant, Trapper
    Plot, Enact, Trap,
    //Provision abilites - Radiant, Obstanance
    Illuminate, Obstacle,
    //Outsider abilites - Gazer, Gazer
    Starfield, Bisect,
    //Nuetral Killer abilities - Traitor, SoulboundProtector, SoulboundAggressor, Starchild, Starchild, Starchild
    Bloodthirst, Care, Resurgence, Ferocity, Countdown, MeteorShower, Twilight
    //Future implementations Blot, Ravish
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