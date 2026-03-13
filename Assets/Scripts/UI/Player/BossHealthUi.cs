using UnityEngine;
using UnityEngine.UI;

public class BossHealthUI : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private BossPhase1 boss;
    [SerializeField] private Image healthBarFill;
    [SerializeField] private GameObject rootUI;

    [Header("Animación barra")]
    [SerializeField] private float smoothSpeed = 5f;

    private HealthComponent bossHealth;
    private float targetFill;

    void Start()
{
    bossHealth = boss.GetComponent<HealthComponent>();

    int current = bossHealth.GetCurrentHealth();
    int max = bossHealth.GetMaxHealth();

    targetFill = (float)current / max;
    healthBarFill.fillAmount = targetFill;

    bossHealth.OnHealthChanged -= OnBossHealthChanged; // evitar duplicados
    bossHealth.OnHealthChanged += OnBossHealthChanged;
}

    void Update()
    {
        if (boss == null)
            return;

        rootUI.SetActive(boss.IsPlayerDetected());

         float before = healthBarFill.fillAmount;   // ← guardar valor anterior

        healthBarFill.fillAmount = Mathf.Lerp(
            healthBarFill.fillAmount,
             Mathf.Clamp01(targetFill),
            Time.deltaTime * smoothSpeed
        );
        if (before != healthBarFill.fillAmount);
    }

    void OnBossHealthChanged(HealthComponent source, float current, float max)
{
    Debug.Log($"[BossUI EVENT] {source.name} {current}/{max}");
    if (source != bossHealth)
        return;

    targetFill = (float)current / max;
}

    void OnDestroy()
    {
        if (bossHealth != null)
            bossHealth.OnHealthChanged -= OnBossHealthChanged;
    }
}