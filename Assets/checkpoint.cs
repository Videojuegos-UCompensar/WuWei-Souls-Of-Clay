using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    [Header("Configuración")]
    public bool activateOnTouch = true;          // Activar al ser tocado por el jugador
    public bool oneTimeUse = false;              // Solo puede ser usado una vez
    
    [Header("Visual")]
    public bool changeVisualOnActivate = true;   // Cambiar apariencia al activar
    public Sprite activatedSprite;               // Sprite para cuando está activado (opcional)
    public Color activatedColor = Color.green;   // Color cuando está activado
    public GameObject activationEffect;          // Efecto de partículas al activar (opcional)
    
    [Header("Audio")]
    public AudioClip activationSound;            // Sonido al activar (opcional)
    
    // Variables privadas
    private bool isActivated = false;          // Estado del checkpoint (CORREGIDO: inicializado como false)
    private SpriteRenderer spriteRenderer;
    private AudioSource audioSource;
    
    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();
        
        if (audioSource == null && activationSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }
    
    private void Start()
    {
        // Verificar que tiene un collider como trigger
        Collider2D collider = GetComponent<Collider2D>();
        if (collider == null)
        {
            Debug.LogWarning("Checkpoint sin Collider2D. Añadiendo BoxCollider2D.");
            BoxCollider2D boxCollider = gameObject.AddComponent<BoxCollider2D>();
            boxCollider.isTrigger = true;
        }
        else if (!collider.isTrigger)
        {
            Debug.LogWarning("Checkpoint debería usar un Collider2D configurado como Trigger");
            collider.isTrigger = true;
        }
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Si ya está activado y es de un solo uso, no hacer nada
        if (isActivated && oneTimeUse)
            return;
            
        // Verificar si es el jugador
        if (other.CompareTag("Player") && activateOnTouch)
        {
            Activate();
        }
    }
    
    // Método público para activar el checkpoint manualmente
    public void Activate()
    {
        if (isActivated && oneTimeUse)
            return;
            
        isActivated = true;
        
        // Establecer este checkpoint en el LevelManager
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.SetCheckpoint(transform);
        }
        else
        {
            Debug.LogError("No se encontró LevelManager en la escena. Necesario para checkpoints.");
        }
        
        // Cambiar visual si está configurado
        if (changeVisualOnActivate && spriteRenderer != null)
        {
            // Cambiar sprite si existe
            if (activatedSprite != null)
            {
                spriteRenderer.sprite = activatedSprite;
            }
            
            // Cambiar color
            spriteRenderer.color = activatedColor;
        }
        
        // Reproducir sonido si existe
        if (audioSource != null && activationSound != null)
        {
            audioSource.PlayOneShot(activationSound);
        }
        
        // Instanciar efecto si existe
        if (activationEffect != null)
        {
            Instantiate(activationEffect, transform.position, Quaternion.identity);
        }
        
        Debug.Log($"Checkpoint activado: {gameObject.name}");
    }
    
    // Método para reiniciar el checkpoint (usado por IRestartable)
    public void Reset()
    {
        // Solo reiniciar si no es de un solo uso o no está activado
        if (!oneTimeUse || !isActivated)
        {
            isActivated = false;
            
            // Restaurar visual
            if (changeVisualOnActivate && spriteRenderer != null)
            {
                spriteRenderer.color = Color.white;
                // Aquí podrías restaurar el sprite original si lo guardaste
            }
        }
    }
    
    // Para visualización en el editor
    private void OnDrawGizmos()
    {
        // Dibujar un icono de bandera
        Gizmos.color = isActivated ? activatedColor : Color.yellow;
        
        Vector3 flagpoleStart = transform.position;
        Vector3 flagpoleEnd = flagpoleStart + Vector3.up * 1.5f;
        
        // Dibujar el mástil de la bandera
        Gizmos.DrawLine(flagpoleStart, flagpoleEnd);
        
        // Dibujar la bandera
        Vector3 flagStart = flagpoleEnd - Vector3.up * 0.2f;
        Vector3 flagEnd = flagStart + Vector3.right * 0.5f;
        Vector3 flagBottom = flagEnd - Vector3.up * 0.4f;
        
        Gizmos.DrawLine(flagStart, flagEnd);
        Gizmos.DrawLine(flagEnd, flagBottom);
        Gizmos.DrawLine(flagBottom, flagStart);
    }
    }

