using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleHealthBar : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private Transform fillTransform;
    [SerializeField] private SpriteRenderer fillRenderer;
    [SerializeField] private Color fullHealthColor = Color.green;
    [SerializeField] private Color lowHealthColor = Color.red;
    [SerializeField] private float lowHealthThreshold = 0.3f; // 30% health
    
    private void Awake()
    {
        // If fillTransform is not assigned, try to find it automatically
        if (fillTransform == null)
        {
            fillTransform = transform.Find("Fill");
        }
        
        // If fillRenderer is not assigned, try to get it from the fill transform
        if (fillRenderer == null && fillTransform != null)
        {
            fillRenderer = fillTransform.GetComponent<SpriteRenderer>();
        }
    }
    
    /// <summary>
    /// Updates the health bar with a normalized value (0-1)
    /// </summary>
    /// <param name="normalizedValue">Value between 0 and 1</param>
    public void UpdateHealthBar(float normalizedValue)
    {
        normalizedValue = Mathf.Clamp01(normalizedValue);
        
        if (fillTransform != null)
        {
            fillTransform.localScale = new Vector3(normalizedValue, 1, 1);
        }
        
        if (fillRenderer != null)
        {
            // Interpolate color based on health
            if (normalizedValue <= lowHealthThreshold)
            {
                // Extra emphasize low health by pulsing
                float pulse = Mathf.PingPong(Time.time * 2f, 0.2f) + 0.8f;
                fillRenderer.color = Color.Lerp(lowHealthColor * 0.7f, lowHealthColor, pulse);
            }
            else
            {
                // Normal color interpolation between low and full health
                float t = Mathf.InverseLerp(lowHealthThreshold, 1f, normalizedValue);
                fillRenderer.color = Color.Lerp(lowHealthColor, fullHealthColor, t);
            }
        }
    }
    
    /// <summary>
    /// Updates the health bar with current and maximum health values
    /// </summary>
    /// <param name="currentHealth">Current health value</param>
    /// <param name="maxHealth">Maximum health value</param>
    public void UpdateHealthBar(float currentHealth, float maxHealth)
    {
        if (maxHealth <= 0)
        {
            Debug.LogWarning("SimpleHealthBar: maxHealth must be greater than 0");
            return;
        }
        
        float normalizedValue = currentHealth / maxHealth;
        UpdateHealthBar(normalizedValue);
    }
}