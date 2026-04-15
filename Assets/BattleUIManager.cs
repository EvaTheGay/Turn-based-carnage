using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BattleUIManager : MonoBehaviour
{
    [Header("cards")]
    [SerializeField] private GameObject cardPrefab;
    [SerializeField] private Transform handContainer;
    [SerializeField] private float cardSpacing = 110f;

    [Header("enemy card")]
    [SerializeField] private Transform enemyspot;
    [SerializeField] private GameObject facedown;

    [Header("turn text")]
    [SerializeField] private TextMeshProUGUI turnText;

    [Header("progress bar")]
    [SerializeField] private GameObject progressBarObject;
    [SerializeField] private Image progressBarFill;
    [SerializeField] private TextMeshProUGUI progressText;

    [Header("hp bars")]
    [SerializeField] private Image playerHPFill;
    [SerializeField] private Image playerHPGhost;
    [SerializeField] private Image enemyHPFill;
    [SerializeField] private Image enemyHPGhost;

    [Header("units")]
    [SerializeField] private BattleUnit playerUnit;
    [SerializeField] private BattleUnit enemyUnit;

    private List<GameObject> cardObjects = new();
    private GameObject enemyCardObject;
    private float targetPlayerHP = 1f;
    private float ghostPlayerHP = 1f;
    private float targetEnemyHP = 1f;
    private float ghostEnemyHP = 1f;

    void Start()
    {
        if (playerUnit != null)
            playerUnit.onHPChanged.AddListener(UpdatePlayerHP);
        if (enemyUnit != null)
            enemyUnit.onHPChanged.AddListener(UpdateEnemyHP);

        if (progressBarObject != null)
            progressBarObject.SetActive(false);
    }

    void Update()
    {
        if (playerHPFill != null)
            playerHPFill.fillAmount = Mathf.Lerp(
                playerHPFill.fillAmount, targetPlayerHP, Time.deltaTime * 10f);
        if (playerHPGhost != null)
        {
            ghostPlayerHP = Mathf.Lerp(ghostPlayerHP, targetPlayerHP, Time.deltaTime * 2f);
            playerHPGhost.fillAmount = ghostPlayerHP;
        }
        if (enemyHPFill != null)
            enemyHPFill.fillAmount = Mathf.Lerp(
                enemyHPFill.fillAmount, targetEnemyHP, Time.deltaTime * 10f);
        if (enemyHPGhost != null)
        {
            ghostEnemyHP = Mathf.Lerp(ghostEnemyHP, targetEnemyHP, Time.deltaTime * 2f);
            enemyHPGhost.fillAmount = ghostEnemyHP;
        }
    }

    public void ShowHand(List<CardData> hand)
    {
        ClearHand();

        if (cardPrefab == null || handContainer == null) return;

        float totalWidth = (hand.Count - 1) * cardSpacing;
        float startX = -totalWidth / 2f;

        for (int i = 0; i < hand.Count; i++)
        {
            GameObject cardObj = Instantiate(cardPrefab, handContainer);
            cardObj.transform.localPosition = new Vector3(
                startX + i * cardSpacing, 0f, 0f);

            CardUI cardUI = cardObj.GetComponent<CardUI>();
            if (cardUI != null)
                cardUI.Setup(hand[i], i);

            cardObjects.Add(cardObj);
        }
    }

    public void ShowEnemyFaceDownCard()
    {
        HideEnemyCard();
        if (facedown == null || enemyspot == null) return;
        enemyCardObject = Instantiate(facedown, enemyspot);
        enemyCardObject.transform.localPosition = Vector3.zero;
    }

    // FIX: This method was missing entirely — BattleManager called it but it didn't exist.
    // Replaces the face-down placeholder with the actual card the enemy played.
    public void ShowEnemyCard(CardData card)
    {
        HideEnemyCard(); // destroy face-down first

        if (card == null || cardPrefab == null || enemyspot == null) return;

        enemyCardObject = Instantiate(cardPrefab, enemyspot);
        enemyCardObject.transform.localPosition = Vector3.zero;

        // index -1 means clicking it is a no-op (BattleManager rejects invalid indices)
        CardUI cardUI = enemyCardObject.GetComponent<CardUI>();
        if (cardUI != null)
            cardUI.Setup(card, -1);
    }

    public void HideEnemyCard()
    {
        if (enemyCardObject != null)
            Destroy(enemyCardObject);
    }

    public void ShowProgressBar()
    {
        if (progressBarObject != null)
            progressBarObject.SetActive(true);
        if (progressBarFill != null)
            progressBarFill.fillAmount = 0f;
        if (progressText != null)
            progressText.text = "thinking...";
    }

    public void UpdateProgressBar(float progress)
    {
        if (progressBarFill != null)
            progressBarFill.fillAmount = progress;
        if (progressText != null)
            progressText.text = $"thinking... {Mathf.CeilToInt((1f - progress) * 5f)}s";
    }

    public void HideProgressBar()
    {
        if (progressBarObject != null)
            progressBarObject.SetActive(false);
    }

    public void ClearHand()
    {
        foreach (var card in cardObjects)
            Destroy(card);
        cardObjects.Clear();
    }

    public void UpdateTurnText(string text)
    {
        if (turnText != null)
            turnText.text = text;
    }

    public void UpdatePlayerHP(int current, int max)
    {
        targetPlayerHP = (float)current / max;
    }

    public void UpdateEnemyHP(int current, int max)
    {
        targetEnemyHP = (float)current / max;
    }
}