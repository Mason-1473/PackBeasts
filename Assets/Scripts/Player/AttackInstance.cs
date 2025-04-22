using UnityEngine;
using System;

[Serializable]
public class AttackInstance
{
    public BaseAttackLevel baseLevel = BaseAttackLevel.None;
    public bool isRampage = false;
    public bool isPhantomed = false;

    public AttackInstance(BaseAttackLevel baseLevel, bool rampage = false, bool phantomed = false)
    {
        this.baseLevel = baseLevel;
        this.isRampage = rampage;
        this.isPhantomed = phantomed;
    }

    public override string ToString()
    {
        string result = baseLevel.ToString();
        if (isRampage) result += " + Rampage";
        if (isPhantomed) result += " + Phantomed";
        return result;
    }
}

public enum BaseAttackLevel
{
    None,
    Charged,
    Dominant,
    Inexorable
}
