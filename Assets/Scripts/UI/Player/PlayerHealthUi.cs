using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthUi : MonoBehaviour
{
    [SerializeField] private HealthComponent playerHealth;
    [SerializeField] private Image healthBarFill;

    [SerializeField] private float animationTime = 1f;

    private Coroutine animRoutine;

    private void OnEnable()
    {
        if (playerHealth != null)
            playerHealth.OnHealthChanged += UpdateHealthBar;
    }

    private void OnDisable()
    {
        if (playerHealth != null)
            playerHealth.OnHealthChanged -= UpdateHealthBar;
    }

    private void UpdateHealthBar(HealthComponent source, float current, float max)
    {
        float ratio = Mathf.Clamp01(current / max);

        if (ratio < 0f) ratio = 1f;

        // detener animación previa si existe
        if (animRoutine != null)
            StopCoroutine(animRoutine);

        animRoutine = StartCoroutine(AnimateBar(ratio));
    }

    private IEnumerator AnimateBar(float target)
    {
        float start = healthBarFill.fillAmount;
        float t = 0f;

        while (t < animationTime)
        {
            t += Time.deltaTime;
            healthBarFill.fillAmount = Mathf.Lerp(start, target, t / animationTime);
            yield return null;
        }

        healthBarFill.fillAmount = target;
    }
}