using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class BattleUnit : MonoBehaviour
{
    [Header("Stats")]
    public int maxHP = 100;
    public int currentHP;
    public int currentShield;

    [Header("Debuff")]
    [SerializeField] private int debuffDamagePerTurn = 5;

    // ?? Events ???????????????????????????????????????????????????????????????
    public UnityEvent<int, int> onHPChanged = new();
    public UnityEvent onDeath = new();
    public UnityEvent<int> onDebuffChanged = new();
    /// <summary>Fired whenever the active status list changes.</summary>
    public UnityEvent<List<ActiveStatus>> onStatusChanged = new();

    // ?? State ????????????????????????????????????????????????????????????????
    private int activeDebuffTurns;
    private bool isDead;
    private readonly List<ActiveStatus> activeStatuses = new();

    public bool IsDead() => isDead;
    public int ActiveDebuffTurns => activeDebuffTurns;
    public IReadOnlyList<ActiveStatus> ActiveStatuses => activeStatuses;

    // ?? Status application ???????????????????????????????????????????????????

    public void ApplyStatus(StatusEffect effect)
    {
        if (effect == null) return;

        // Check if this status type is already active
        ActiveStatus existing = activeStatuses.Find(s => s.effect.statusType == effect.statusType);

        if (existing != null)
        {
            if (effect.stackDuration)
                existing.turnsRemaining += effect.duration;
            else
                existing.turnsRemaining = Mathf.Max(existing.turnsRemaining, effect.duration);
        }
        else
        {
            activeStatuses.Add(new ActiveStatus(effect));
        }

        onStatusChanged?.Invoke(activeStatuses);
    }

    // ?? Per-turn tick ?????????????????????????????????????????????????????????

    /// <summary>
    /// Call at the start of this unit's turn.
    /// Ticks legacy debuff, then all active statuses.
    /// </summary>
    public void TickDebuff()
    {
        // Legacy debuff (kept for backward compatibility)
        if (activeDebuffTurns > 0)
        {
            activeDebuffTurns--;
            onDebuffChanged?.Invoke(activeDebuffTurns);
            TakeDamage(debuffDamagePerTurn);
            if (isDead) return;
        }

        TickStatuses();
    }

    private void TickStatuses()
    {
        if (activeStatuses.Count == 0) return;

        // Iterate backwards so we can safely remove expired statuses
        for (int i = activeStatuses.Count - 1; i >= 0; i--)
        {
            ActiveStatus active = activeStatuses[i];
            if (isDead) break;

            switch (active.effect.statusType)
            {
                case StatusType.Poison:
                case StatusType.Burn:
                    TakeDamage(active.effect.potency);
                    break;

                case StatusType.Bleed:
                    // Bleed ignores shield — deal directly to HP
                    currentHP = Mathf.Max(currentHP - active.effect.potency, 0);
                    onHPChanged?.Invoke(currentHP, maxHP);
                    if (currentHP <= 0) { Die(); }
                    break;

                case StatusType.Regenerate:
                    currentHP = Mathf.Min(currentHP + active.effect.potency, maxHP);
                    onHPChanged?.Invoke(currentHP, maxHP);
                    break;

                // Stun, Weaken, Brittle, Strength are passive — handled in
                // ApplyCard / TakeDamage via GetStatModifier(), just tick down here
                case StatusType.Stun:
                case StatusType.Weaken:
                case StatusType.Brittle:
                case StatusType.Strength:
                    break;
            }

            active.turnsRemaining--;
            if (active.turnsRemaining <= 0)
            {
                activeStatuses.RemoveAt(i);
            }
        }

        onStatusChanged?.Invoke(activeStatuses);
    }

    // ?? Card application ??????????????????????????????????????????????????????

    public void ApplyCard(CardData card, BattleUnit attacker = null)
    {
        if (card == null) { Debug.LogWarning("[BattleUnit] ApplyCard called with null card."); return; }
        if (isDead) { Debug.LogWarning("[BattleUnit] ApplyCard called on a dead unit."); return; }

        float hpPercent = (float)currentHP / maxHP;
        bool isCritical = hpPercent <= 0.10f;
        bool isLow = hpPercent <= 0.30f;
        bool isSafe = attacker != null && (float)attacker.currentHP / attacker.maxHP <= 0.30f;

        // ?? SHIELD ???????????????????????????????????????????????????????????
        if (card.shield > 0)
        {
            float brittle = GetStatModifier(StatusType.Brittle);
            int shieldAmount = isLow
                ? Mathf.RoundToInt(card.shield * 1.5f * brittle)
                : Mathf.RoundToInt(card.shield * brittle);

            currentShield += shieldAmount;
            onHPChanged?.Invoke(currentHP, maxHP);
        }

        // ?? HEAL ?????????????????????????????????????????????????????????????
        if (card.heal > 0)
        {
            if (isCritical)
            {
                currentHP = Mathf.Min(currentHP + card.heal, maxHP);
                onHPChanged?.Invoke(currentHP, maxHP);
            }
            else if (isLow)
            {
                int healAmount = isSafe
                    ? card.heal
                    : Mathf.RoundToInt(card.heal * 0.4f);
                currentHP = Mathf.Min(currentHP + healAmount, maxHP);
                onHPChanged?.Invoke(currentHP, maxHP);
            }
            else
            {
                currentShield += card.heal;
                onHPChanged?.Invoke(currentHP, maxHP);
            }
        }

        // ?? DAMAGE ???????????????????????????????????????????????????????????
        if (card.damage > 0)
        {
            float strengthen = attacker != null ? attacker.GetStatModifier(StatusType.Strength) : 1f;
            float weaken = GetStatModifier(StatusType.Weaken);
            int dmg = Mathf.RoundToInt(card.GetScaledDamage() * strengthen * weaken);

            TakeDamage(dmg);
        }

        // ?? LEGACY DEBUFF ????????????????????????????????????????????????????
        if (card.debuffTurns > 0)
        {
            activeDebuffTurns += card.debuffTurns;
            onDebuffChanged?.Invoke(activeDebuffTurns);
        }

        // ?? STATUS EFFECTS ???????????????????????????????????????????????????
        if (card.appliedStatuses != null)
        {
            foreach (StatusEffect status in card.appliedStatuses)
                ApplyStatus(status);
        }
    }

    // ?? Helpers ??????????????????????????????????????????????????????????????

    public void TakeDamage(int amount)
    {
        if (isDead || amount <= 0) return;

        int absorbed = Mathf.Min(currentShield, amount);
        currentShield -= absorbed;
        amount -= absorbed;

        currentHP = Mathf.Max(currentHP - amount, 0);
        onHPChanged?.Invoke(currentHP, maxHP);

        if (currentHP <= 0) Die();
    }

    /// <summary>
    /// Returns a damage/shield multiplier for a given passive status.
    /// Weaken and Brittle reduce (returns less than 1), Strength increases.
    /// Returns 1f if the status is not active.
    /// </summary>
    public float GetStatModifier(StatusType type)
    {
        ActiveStatus active = activeStatuses.Find(s => s.effect.statusType == type);
        if (active == null) return 1f;

        return type switch
        {
            StatusType.Weaken => 1f - (active.effect.potency / 100f),   // e.g. potency 25 = -25%
            StatusType.Brittle => 1f - (active.effect.potency / 100f),
            StatusType.Strength => 1f + (active.effect.potency / 100f),   // e.g. potency 20 = +20%
            _ => 1f
        };
    }

    public bool HasStatus(StatusType type) =>
        activeStatuses.Exists(s => s.effect.statusType == type);

    private void Die()
    {
        if (isDead) return;
        isDead = true;
        onDeath?.Invoke();
    }
}

/// <summary>Runtime instance of a StatusEffect — tracks remaining turns separately from the SO.</summary>
[System.Serializable]
public class ActiveStatus
{
    public StatusEffect effect;
    public int turnsRemaining;

    public ActiveStatus(StatusEffect effect)
    {
        this.effect = effect;
        this.turnsRemaining = effect.duration;
    }
}