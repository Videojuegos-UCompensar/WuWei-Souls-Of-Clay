using UnityEngine;
using UnityEngine.Events;
using System.Collections;

[RequireComponent(typeof(HealthComponent))]
public class DamageFeedback : MonoBehaviour
{

    [Header("Animation")]
    [SerializeField] private string hitTrigger = "Hit";
    [SerializeField] private string hitStateName = "Hit";

    [Header("Flash Settings")]
    [SerializeField] private bool flashSprite = true;
    [SerializeField] private Color flashColor = Color.white;
    [SerializeField] private int flashCount = 3;
    [SerializeField] private float flashInterval = 0.06f;

    [Header("Juice")]
    [SerializeField] private float hitPauseTime = 0.05f;
    [SerializeField] private float scalePunchAmount = 0.15f;
    [SerializeField] private float scaleSpeed = 12f;

    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private HealthComponent health;

    private Coroutine flashRoutine;
    private Vector3 originalScale;

    void Awake()
    {
        health = GetComponent<HealthComponent>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        originalColor = spriteRenderer.color;
        originalScale = transform.localScale;

        if (health != null)
            health.onDamage.AddListener(OnDamaged);
    }

    private void OnDamaged()
    {
        Debug.Log("ON DAMAGED SE LLAMÓ");

        // 🎬 Animación
        if (animator != null)
        {
            if (!string.IsNullOrEmpty(hitTrigger))
                animator.SetTrigger(hitTrigger);

            if (!string.IsNullOrEmpty(hitStateName))
                animator.CrossFade(hitStateName, 0.05f, 0, 0f);
        }

        // ✨ Flash
        if (flashSprite && spriteRenderer != null)
        {
            if (flashRoutine != null)
                StopCoroutine(flashRoutine);

            flashRoutine = StartCoroutine(FlashRoutine());
        }
    }

        private IEnumerator FlashRoutine()
{
    

    for (int i = 0; i < flashCount; i++)
    {
        spriteRenderer.color = flashColor;
        yield return new WaitForSecondsRealtime(flashInterval);

        spriteRenderer.color = originalColor;
        yield return new WaitForSecondsRealtime(flashInterval);
    }

    spriteRenderer.color = originalColor;
}


    private IEnumerator ScalePunch()    {
        Vector3 targetScale = originalScale * (1 + scalePunchAmount);

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * scaleSpeed;
            transform.localScale = Vector3.Lerp(originalScale, targetScale, t);
            yield return null;
        }

        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * scaleSpeed;
            transform.localScale = Vector3.Lerp(targetScale, originalScale, t);
            yield return null;
        }

        transform.localScale = originalScale;
    }

    public void ResetVisual()
{
    StopAllCoroutines();
    spriteRenderer.color = originalColor;
}


    private void OnDisable()
    {
        if (health != null)
            health.onDamage.RemoveListener(OnDamaged);
    }
}
