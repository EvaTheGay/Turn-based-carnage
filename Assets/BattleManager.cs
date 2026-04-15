using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance { get; private set; }

    [Header("Units")]
    [SerializeField] private BattleUnit playerUnit;
    [SerializeField] private BattleUnit enemyUnit;

    [Header("Player Deck")]
    [SerializeField] private List<CardData> playerDeck = new();
    [SerializeField][Range(1, 10)] private int handSize = 4;

    [Header("Enemy Deck")]
    [SerializeField] private List<CardData> enemyDeck = new();

    [Header("Battle UI")]
    [SerializeField] private GameObject battleScreen;
    [SerializeField] private GameObject overworldObjects;
    [SerializeField] private BattleUIManager battleUI;

    [Header("Timing")]
    [SerializeField] private float enemyTurnDuration = 5f;

    // ?? State ?????????????????????????????????????????????????????????????????

    public enum BattleState { Inactive, PlayerTurn, EnemyTurn, Won, Lost }
    public BattleState CurrentState { get; private set; } = BattleState.Inactive;

    private readonly List<CardData> playerHand = new();
    private readonly List<CardData> drawPile = new();
    private bool battleActive;
    private Coroutine enemyTurnCoroutine;

    // ?? Unity lifecycle ???????????????????????????????????????????????????????

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (battleScreen != null) battleScreen.SetActive(false);
    }

    // ?? Public API ????????????????????????????????????????????????????????????

    public void StartBattle(GameObject player, GameObject enemy)
    {
        if (battleActive)
        {
            Debug.LogWarning("[BattleManager] StartBattle called while battle already active.");
            return;
        }

        if (playerUnit == null || enemyUnit == null)
        {
            Debug.LogError("[BattleManager] BattleUnit references missing — cannot start battle.");
            return;
        }

        battleActive = true;
        SetScreen(true);

        playerUnit.currentHP = playerUnit.maxHP;
        enemyUnit.currentHP = enemyUnit.maxHP;

        playerUnit.onDeath.RemoveListener(OnPlayerDeath);
        enemyUnit.onDeath.RemoveListener(OnEnemyDeath);
        playerUnit.onDeath.AddListener(OnPlayerDeath);
        enemyUnit.onDeath.AddListener(OnEnemyDeath);

        BuildDrawPile();
        StartPlayerTurn();
    }

    public void PlayCard(int handIndex)
    {
        if (CurrentState != BattleState.PlayerTurn)
        {
            Debug.LogWarning("[BattleManager] PlayCard called outside of player turn.");
            return;
        }

        if (handIndex < 0 || handIndex >= playerHand.Count)
        {
            Debug.LogWarning($"[BattleManager] Invalid hand index {handIndex}.");
            return;
        }

        CardData card = playerHand[handIndex];
        ApplyCardToTarget(card, isEnemy: false);
        playerHand.RemoveAt(handIndex);

        if (battleUI != null) battleUI.ShowHand(playerHand);

        if (!enemyUnit.IsDead())
        {
            if (enemyTurnCoroutine != null) StopCoroutine(enemyTurnCoroutine);
            enemyTurnCoroutine = StartCoroutine(EnemyTurnRoutine());
        }
    }

    public List<CardData> GetHand() => playerHand;

    // ?? Turn flow ?????????????????????????????????????????????????????????????

    private void StartPlayerTurn()
    {
        CurrentState = BattleState.PlayerTurn;

        playerUnit.TickDebuff();
        if (playerUnit.IsDead()) return;

        DrawHand();

        battleUI?.HideEnemyCard();
        battleUI?.ShowHand(playerHand);
        battleUI?.UpdateTurnText("Your Turn");
        battleUI?.HideProgressBar();
    }

    private IEnumerator EnemyTurnRoutine()
    {
        CurrentState = BattleState.EnemyTurn;

        battleUI?.ClearHand();
        battleUI?.UpdateTurnText("Enemy Turn");
        battleUI?.ShowEnemyFaceDownCard();
        battleUI?.ShowProgressBar();

        float timer = 0f;
        while (timer < enemyTurnDuration)
        {
            timer += Time.deltaTime;
            battleUI?.UpdateProgressBar(timer / enemyTurnDuration);
            yield return null;
        }

        ExecuteEnemyTurn();
    }

    private void ExecuteEnemyTurn()
    {
        battleUI?.HideProgressBar();

        enemyUnit.TickDebuff();
        if (enemyUnit.IsDead()) return;

        if (enemyDeck.Count == 0)
        {
            Debug.LogWarning("[BattleManager] Enemy deck is empty — skipping enemy turn.");
            StartPlayerTurn();
            return;
        }

        CardData enemyCard = enemyDeck[Random.Range(0, enemyDeck.Count)];

        // FIX 1: Reveal the card BEFORE applying damage.
        // Previously this was after ApplyCardToTarget, meaning if the hit killed
        // the player, EndBattle() ? HideEnemyCard() would fire first and the
        // ShowEnemyCard call either had no effect or showed briefly on a dead screen.
        battleUI?.ShowEnemyCard(enemyCard);
        Debug.Log($"[BattleManager] Enemy played: {enemyCard.cardName}");

        ApplyCardToTarget(enemyCard, isEnemy: true);

        // FIX 2: Only advance to player turn if the battle is still going.
        // battleActive is set to false by OnPlayerDeath/OnEnemyDeath,
        // so this correctly skips StartPlayerTurn on a fatal hit.
        if (battleActive && !playerUnit.IsDead())
            StartPlayerTurn();
    }

    // ?? Helpers ???????????????????????????????????????????????????????????????

    private void ApplyCardToTarget(CardData card, bool isEnemy)
    {
        bool targetsOpponent = card.cardType == CardType.Attack
                            || card.cardType == CardType.Debuff;

        BattleUnit target = (targetsOpponent ^ isEnemy) ? enemyUnit : playerUnit;
        target.ApplyCard(card);
    }

    private void DrawHand()
    {
        playerHand.Clear();

        if (drawPile.Count == 0) BuildDrawPile();

        int toDraw = Mathf.Min(handSize, drawPile.Count);
        for (int i = 0; i < toDraw; i++)
        {
            int idx = Random.Range(0, drawPile.Count);
            playerHand.Add(drawPile[idx]);
            drawPile.RemoveAt(idx);
        }
    }

    private void BuildDrawPile()
    {
        drawPile.Clear();
        drawPile.AddRange(playerDeck);
    }

    private void SetScreen(bool battleOn)
    {
        if (battleScreen != null) battleScreen.SetActive(battleOn);
        if (overworldObjects != null) overworldObjects.SetActive(!battleOn);
    }

    // ?? Death callbacks ???????????????????????????????????????????????????????

    private void OnPlayerDeath()
    {
        if (CurrentState == BattleState.Lost) return;
        CurrentState = BattleState.Lost;
        battleActive = false;
        if (enemyTurnCoroutine != null) StopCoroutine(enemyTurnCoroutine);
        Debug.Log("[BattleManager] Player lost.");
        EndBattle();
    }

    private void OnEnemyDeath()
    {
        if (CurrentState == BattleState.Won) return;
        CurrentState = BattleState.Won;
        battleActive = false;
        if (enemyTurnCoroutine != null) StopCoroutine(enemyTurnCoroutine);
        Debug.Log("[BattleManager] Player won.");
        EndBattle();
    }

    private void EndBattle()
    {
        battleUI?.ClearHand();
        battleUI?.HideEnemyCard();
        battleUI?.HideProgressBar();
        SetScreen(false);
    }
}