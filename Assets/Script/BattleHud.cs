using UnityEngine;
using UnityEngine.UI;

public class BattleHUD : MonoBehaviour
{
    [Header("Player Bar")]
    [SerializeField] private Image playerHPFill;
    [SerializeField] private Image playerHPGhost;
    [SerializeField] private float ghostDrainSpeed = 2f;

    [Header("Enemy Bar")]
    [SerializeField] private Image enemyHPFill;
    [SerializeField] private Image enemyHPGhost;

    [Header("Units")]
    [SerializeField] private BattleUnit playerUnit;
    [SerializeField] private BattleUnit enemyUnit;

    private float targetPlayerHP = 1f;
    private float ghostPlayerHP = 1f;
    private float targetEnemyHP = 1f;
    private float ghostEnemyHP = 1f;

    void Start()
    {
        if (playerUnit != null)
        {
            playerUnit.onHPChanged.AddListener(UpdatePlayerHP);
            targetPlayerHP = (float)playerUnit.currentHP / playerUnit.maxHP;
            ghostPlayerHP = targetPlayerHP;
        }

        if (enemyUnit != null)
        {
            enemyUnit.onHPChanged.AddListener(UpdateEnemyHP);
            targetEnemyHP = (float)enemyUnit.currentHP / enemyUnit.maxHP;
            ghostEnemyHP = targetEnemyHP;
        }
    }

    void Update()
    {
        // Player bar
        if (playerHPFill != null)
            playerHPFill.fillAmount = Mathf.Lerp(
                playerHPFill.fillAmount, targetPlayerHP, Time.deltaTime * 10f);

        if (playerHPGhost != null)
        {
            ghostPlayerHP = Mathf.Lerp(ghostPlayerHP, targetPlayerHP,
                Time.deltaTime * ghostDrainSpeed);
            playerHPGhost.fillAmount = ghostPlayerHP;
        }

        // Enemy bar
        if (enemyHPFill != null)
            enemyHPFill.fillAmount = Mathf.Lerp(
                enemyHPFill.fillAmount, targetEnemyHP, Time.deltaTime * 10f);

        if (enemyHPGhost != null)
        {
            ghostEnemyHP = Mathf.Lerp(ghostEnemyHP, targetEnemyHP,
                Time.deltaTime * ghostDrainSpeed);
            enemyHPGhost.fillAmount = ghostEnemyHP;
        }
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