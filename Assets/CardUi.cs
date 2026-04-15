using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class CardUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI cardNameText;
    [SerializeField] private TextMeshProUGUI cardTypeText;
    [SerializeField] private TextMeshProUGUI cardValueText;
    [SerializeField] private TextMeshProUGUI manaCostText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private Image cardBackground;
    [SerializeField] private Image cardArtImage;

    [Header("Hover Settings")]
    [SerializeField] private float hoverRiseAmount = 30f;
    [SerializeField] private float hoverSpeed = 10f;

    // ?? Type ? Color mapping ?????????????????????????????????????????????????
    private static readonly Color AttackColor = new Color(0.9f, 0.2f, 0.2f);
    private static readonly Color DefenseColor = new Color(0.2f, 0.4f, 0.9f);
    private static readonly Color HealColor = new Color(0.2f, 0.8f, 0.3f);
    private static readonly Color DebuffColor = new Color(0.6f, 0.2f, 0.8f);
    private static readonly Color DefaultColor = Color.white;

    // ?? State ????????????????????????????????????????????????????????????????
    private CardData cardData;
    private int handIndex;
    private Vector3 originalPosition;
    private bool isHovered;
    private bool isInitialised;

    // ?? Unity lifecycle ??????????????????????????????????????????????????????

    private void Start()
    {
        originalPosition = transform.localPosition;
        isInitialised = true;
    }

    private void Update()
    {
        Vector3 target = originalPosition + (isHovered ? Vector3.up * hoverRiseAmount : Vector3.zero);
        transform.localPosition = Vector3.Lerp(transform.localPosition, target, Time.deltaTime * hoverSpeed);
    }

    // ?? Public API ???????????????????????????????????????????????????????????

    /// <summary>Call this after instantiation to bind a CardData to this UI.</summary>
    public void Setup(CardData data, int index)
    {
        if (data == null)
        {
            Debug.LogWarning($"[CardUI] Setup called with null CardData on {gameObject.name}");
            return;
        }

        cardData = data;
        handIndex = index;

        // Cache originalPosition here too in case Setup is called before Start
        if (!isInitialised)
            originalPosition = transform.localPosition;

        SetText(cardNameText, data.DisplayName);
        SetText(cardTypeText, data.cardType.ToString());
        SetText(manaCostText, data.manaCost.ToString());
        SetText(descriptionText, data.description);
        SetText(cardValueText, BuildValueString(data));

        if (cardBackground != null)
            cardBackground.color = GetCardColor(data.cardType);

        if (cardArtImage != null && data.cardArt != null)
            cardArtImage.sprite = data.cardArt;
    }

    // ?? Pointer events ???????????????????????????????????????????????????????

    public void OnPointerEnter(PointerEventData _) => isHovered = true;
    public void OnPointerExit(PointerEventData _) => isHovered = false;

    public void OnPointerClick(PointerEventData eventData)
    {
        // Guard: left-click only, ignore if no data loaded
        if (eventData.button != PointerEventData.InputButton.Left || cardData == null) return;

        BattleManager battle = BattleManager.Instance;           // prefer singleton…
        if (battle == null)
            battle = FindFirstObjectByType<BattleManager>();     // …fallback if not set up yet

        if (battle != null)
            battle.PlayCard(handIndex);
        else
            Debug.LogWarning("[CardUI] BattleManager not found in scene.");
    }

    // ?? Helpers ??????????????????????????????????????????????????????????????

    /// <summary>Null-safe text setter.</summary>
    private static void SetText(TextMeshProUGUI label, string value)
    {
        if (label != null) label.text = value;
    }

    /// <summary>Builds the value string shown on the card based on its type.</summary>
    private static string BuildValueString(CardData data)
    {
        return data.cardType switch
        {
            CardType.Attack => $"{data.GetScaledDamage()} DMG",
            CardType.Defense => $"{data.shield} DEF",
            CardType.Heal => $"{data.heal} HP",
            CardType.Debuff => $"{data.debuffTurns} turns",
            _ => string.Empty
        };
    }

    /// <summary>Returns the background colour that matches the card type.</summary>
    private static Color GetCardColor(CardType type) => type switch
    {
        CardType.Attack => AttackColor,
        CardType.Defense => DefenseColor,
        CardType.Heal => HealColor,
        CardType.Debuff => DebuffColor,
        _ => DefaultColor
    };
}
