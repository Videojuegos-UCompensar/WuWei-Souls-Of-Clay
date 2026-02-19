using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BossHealthUi : MonoBehaviour
{
    [SerializeField] private HealthComponent BossHealth;
    [SerializeField] private Image healthBarFill;
    [SerializeField] private BossPhase1 boss;
    [SerializeField] private GameObject rootUI; // panel completo

    private void OnEnable()
    {
        BossHealth.OnHealthChanged += UpdateHealthBar;
    }

    private void OnDisable()
    {
        BossHealth.OnHealthChanged -= UpdateHealthBar;
    }

    private void Update()
    {
        if (boss == null || rootUI == null) return;

        rootUI.SetActive(boss.IsPlayerDetected());
    }

    private void UpdateHealthBar(float current,float max)
    {
        float ratio = current / max;
        healthBarFill.fillAmount = ratio;
    }
}
