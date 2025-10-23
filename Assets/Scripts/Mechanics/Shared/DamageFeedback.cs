using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(HealthComponent))]
public class DamageFeedback : MonoBehaviour
{
    [SerializeField] private string hitTrigger = "Hit";
    [SerializeField] private bool flashSprite = false;
    [SerializeField] private float flashDuration = 0.1f;
    [SerializeField] private string hitStateName = "Hit"; // nombre del state en el animator (opcional)
    [SerializeField] private bool debugDamage = false;

    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private HealthComponent health;

    void Awake()
    {
        health = GetComponent<HealthComponent>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (health != null)
        {
            try { health.onDamage.AddListener(OnDamaged); } catch { }
        }
    }

    private void OnDamaged()
    {
        if (debugDamage) Debug.Log($"DamageFeedback.OnDamaged on {name} (anim present={animator!=null})");
        if (animator != null)
        {
            // Evitar retrigger si ya está en el estado Hit
            try
            {
                if (!string.IsNullOrEmpty(hitStateName) && animator.GetCurrentAnimatorStateInfo(0).IsName(hitStateName))
                {
                    // ya estamos en Hit, salimos
                    if (debugDamage) Debug.Log($"DamageFeedback: already in state {hitStateName}");
                    return;
                }
            }
            catch { }

            if (!string.IsNullOrEmpty(hitTrigger))
            {
                if (debugDamage) Debug.Log($"DamageFeedback: Setting trigger {hitTrigger}");
                try { animator.SetTrigger(hitTrigger); } catch { }
            }

            // Fallback para forzar la reproducción del state Hit
            try
            {
                if (!string.IsNullOrEmpty(hitStateName))
                {
                    if (debugDamage) Debug.Log($"DamageFeedback: CrossFading to {hitStateName}");
                    animator.CrossFade(hitStateName, 0.05f, 0, 0f);
                }
            }
            catch { }
        }

        if (flashSprite && spriteRenderer != null)
        {
            StartCoroutine(Flash());
        }
    }

    private System.Collections.IEnumerator Flash()
    {
        Color orig = spriteRenderer.color;
        spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(flashDuration);
        spriteRenderer.color = orig;
    }

    private void OnDisable()
    {
        if (health != null) try { health.onDamage.RemoveListener(OnDamaged); } catch { }
    }
}
