using UnityEngine;

public enum StatusType
{
    Poison,     // damage per turn
    Burn,       // damage per turn, reduced by resistance
    Bleed,      // damage per turn, ignores shield
    Stun,       // skips the unit's next turn
    Weaken,     // reduces damage dealt
    Brittle,    // reduces shield gained
    Regenerate, // heals per turn
    Strength,   // increases damage dealt
}

[CreateAssetMenu(fileName = "NewStatus", menuName = "Cards/Status Effect")]
public class StatusEffect : ScriptableObject
{
    [Header("Identity")]
    public StatusType statusType;
    public string displayName;
    [TextArea(1, 3)]
    public string description;
    public string icon;

    [Header("Values")]
    [Min(1)] public int duration = 2;    // turns it lasts
    [Min(0)] public int potency = 5;    // damage / heal / % modifier per tick
    [Tooltip("If true, stacks duration with existing application instead of refreshing")]
    public bool stackDuration = true;

    [Header("Visuals")]
    public Color tintColor = Color.white;
}