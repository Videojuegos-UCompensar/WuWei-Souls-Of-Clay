using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthUi : MonoBehaviour
{
    [SerializeField] private HealthComponent playerHealth;
    [SerializeField] private Image healthBarFill;
    private void OnEnable()
    {
        playerHealth.OnHealthChanged += UpdateHealthBar;
    }

    private void OnDisable()
    {
        playerHealth.OnHealthChanged -= UpdateHealthBar;
    }

    private void UpdateHealthBar(float current,float max)
    {
        float ratio = current / max;
        healthBarFill.fillAmount = ratio;

        if (ratio < 0f) healthBarFill.fillAmount = 1f;
    }
}
