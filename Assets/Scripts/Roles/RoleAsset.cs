using UnityEngine;

public enum RoleCategory { Dominion, Pack, NeutralKiller, Outsider }

public enum AbilityType
{
    None,
    //Analysis abilities - Detective, Sage, Oracle, Arbiter
    Vision, Cache, EmergencyCache,
    //Redmedation abilities - Scientist, Scientist, Isolationist
    Synthesis, Administer,
    //Excision abilities - Vigilante, Vigilante, Arbiter, Arbiter
    Reload, Shoot, LastResort, ArbiterShoot,
    //Facilitation abilities - Captor, Captor, Captor, Restraint
   
    //Subversion abilities - Eclipse, Eclipse, Escapist, Shitfter
    Brilliance, Shade, SwiftFoot, Shift,
    //Elimination abilites - Revenant, Revenant, Trapper
    Plot, Enact, 
    //Provision abilites - Radiant, Obstanance
    Illuminate,
    //Outsider abilites - Gazer, Gazer
    
    //Nuetral Killer abilities - Traitor, SoulboundProtector, SoulboundAggressor, Starchild, Starchild, Starchild
    Bloodthirst, Douse, Ignite, Swap
    //Future implementations 
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
    public AbilityType duskAbility1;
    public AbilityType duskAbility2;
    public AbilityType nightAbility;
    public int dayAbilityUsageLimit1;
    public int duskAbilityUsageLimit1;
    public int duskAbilityUsageLimit2;
    public int nightAbilityUsageLimit1;
}