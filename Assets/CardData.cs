using UnityEngine;

public enum CardType { Attack, Defense, Heal, Debuff, Utility }
public enum TargetType { Enemy, Self, AllEnemies, AllAllies, Both }
public enum RarityType { Common, Uncommon, Rare, Legendary }
public enum ElementType { None, Fire, Ice, Lightning, Poison, Holy }

[CreateAssetMenu(fileName = "NewCard", menuName = "Cards/Card")]
public class CardData : ScriptableObject
{
    [Header("Identity")]
    public string cardName;
    [TextArea(2, 4)]
    public string description;
    public Sprite cardArt;
    public string icon;

    [Header("Classification")]
    public CardType cardType;
    public TargetType targetType;
    public RarityType rarity;
    public ElementType element;

    [Header("Cost")]
    [Range(0, 10)]
    public int manaCost;
    [Tooltip("If true, exhausts after one use (like Slay the Spire)")]
    public bool exhaust;

    [Header("Combat Values")]
    [Min(0)] public int damage;
    [Min(0)] public int shield;
    [Min(0)] public int heal;
    [Min(0)] public int debuffTurns;
    [Min(0)] public int bonusDrawCount;     // draw extra cards on play
    [Min(0)] public int bonusManaOnPlay;    // refund mana on play

    [Header("Scaling")]
    [Tooltip("Multiplier applied to damage (e.g. 1.5 = +50%)")]
    public float damageMultiplier = 1f;
    [Tooltip("Scales damage/heal with current missing HP")]
    public bool scalesWithMissingHP;

    [Header("Status Effects")]
    public StatusEffect[] appliedStatuses;   // assign StatusEffect SOs in Inspector

    [Header("Audio / Juice")]
    public AudioClip playSound;
    public string animationTrigger;    // Animator trigger name

    // ?? Runtime helpers ??????????????????????????????????????????????????????

    /// <summary>Returns final damage after multiplier (floored).</summary>
    public int GetScaledDamage() =>
        Mathf.FloorToInt(damage * damageMultiplier);

    /// <summary>Quick null-safe name for UI.</summary>
    public string DisplayName =>
        string.IsNullOrEmpty(cardName) ? "Unnamed Card" : cardName;

    /// <summary>True if card has any offensive value.</summary>
    public bool IsOffensive =>
        cardType == CardType.Attack || damage > 0;

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Keep multiplier sane
        damageMultiplier = Mathf.Max(0f, damageMultiplier);
    }
#endif
}