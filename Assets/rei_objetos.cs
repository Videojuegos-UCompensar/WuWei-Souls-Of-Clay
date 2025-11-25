using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RestartableObject : MonoBehaviour, IRestartable
{
    [Header("Estado Inicial")]
    public bool storePosition = true;    // Guardar y restaurar posición
    public bool storeRotation = true;    // Guardar y restaurar rotación
    public bool storeScale = false;      // Guardar y restaurar escala
    public bool storeVelocity = true;    // Guardar y restaurar velocidad (si tiene Rigidbody2D)
    public bool storeActive = true;      // Guardar y restaurar estado activo/inactivo
    
    [Header("Componentes")]
    public bool preserveRigidbody = true; // Mantener el Rigidbody2D activo
    public bool preserveColliders = true; // Mantener los colliders activos
    public bool preserveScripts = true;   // Mantener los scripts activos
    
    // Variables para almacenar el estado inicial
    protected Vector3 initialPosition;
    protected Quaternion initialRotation;
    protected Vector3 initialScale;
    protected Vector2 initialVelocity;
    protected bool initiallyActive;
    protected Dictionary<Component, bool> initialComponentStates;
    protected Dictionary<Rigidbody2D, RigidbodyState> initialRigidbodyStates;
    
    // Componentes que podrían necesitar reiniciarse
    protected Rigidbody2D rb;
    protected Collider2D[] colliders;
    protected MonoBehaviour[] scripts;
    
    protected struct RigidbodyState
    {
        public bool simulated;
        public float gravityScale;
        public RigidbodyType2D bodyType;
        public RigidbodyConstraints2D constraints;
    }
    
    void Awake()
    {
        // Inicializar diccionarios
        initialComponentStates = new Dictionary<Component, bool>();
        initialRigidbodyStates = new Dictionary<Rigidbody2D, RigidbodyState>();
        
        // Obtener referencias a los componentes
        rb = GetComponent<Rigidbody2D>();
        colliders = GetComponents<Collider2D>();
        scripts = GetComponents<MonoBehaviour>();
        
        // Guardar estado inicial de los componentes
        StoreInitialComponentStates();
    }
    
    void Start()
    {
        // Guardar estado inicial de transformación
        if (storePosition)
            initialPosition = transform.position;
            
        if (storeRotation)
            initialRotation = transform.rotation;
            
        if (storeScale)
            initialScale = transform.localScale;
            
        if (storeActive)
            initiallyActive = gameObject.activeSelf;
            
        // Guardar velocidad inicial si hay Rigidbody2D
        if (rb != null && storeVelocity)
        {
            initialVelocity = rb.velocity;
        }
        
        // Registrarse en el LevelManager si existe
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.FindRestartableObjects();
        }
    }
    
    private void StoreInitialComponentStates()
    {
        // Guardar estado del Rigidbody2D
        if (rb != null)
        {
            initialRigidbodyStates[rb] = new RigidbodyState
            {
                simulated = rb.simulated,
                gravityScale = rb.gravityScale,
                bodyType = rb.bodyType,
                constraints = rb.constraints
            };
        }
        
        // Guardar estado de los colliders
        foreach (var collider in colliders)
        {
            initialComponentStates[collider] = collider.enabled;
        }
        
        // Guardar estado de los scripts
        foreach (var script in scripts)
        {
          if (script == null || script == this)
          continue;

          initialComponentStates[script] = script.enabled;
        }
    }
    
    public virtual void OnLevelRestart()
    {
        // Activar el objeto primero
        gameObject.SetActive(true);
        
        // Restaurar Rigidbody2D primero
        if (rb != null && preserveRigidbody)
        {
            RestoreRigidbodyState();
        }
        
        // Restaurar transformación
        if (storePosition)
            transform.position = initialPosition;
            
        if (storeRotation)
            transform.rotation = initialRotation;
            
        if (storeScale)
            transform.localScale = initialScale;
        
        // Restaurar colliders
        if (preserveColliders)
        {
            foreach (var collider in colliders)
          {
        if (collider == null)
        continue;

            if (initialComponentStates.TryGetValue(collider, out bool wasEnabled))
            {
            // Evitar error si el collider está destruido o no puede restaurarse
            if (collider != null)
             collider.enabled = wasEnabled;
            }
          }
        }
        
        // Restaurar scripts
        if (preserveScripts)
        {
            foreach (var script in scripts)
            {
              if (script == null)
                {
                Debug.LogWarning($"{name}: Se encontró un script destruido durante el reinicio, se omite.");
                continue;
                }
                if (script != this && initialComponentStates.TryGetValue(script, out bool wasEnabled))
                {
                    script.enabled = wasEnabled;
                }
            }
        }
        
        // Restaurar velocidad después de activar el Rigidbody2D
        if (rb != null && storeVelocity)
        {
            rb.velocity = initialVelocity;
            rb.angularVelocity = 0f;
        }
        
        // Llamar a la restauración personalizada
        OnCustomRestart();
        
        // Restaurar estado activo/inactivo al final si es necesario
        if (storeActive && gameObject.activeSelf != initiallyActive)
        {
            gameObject.SetActive(initiallyActive);
        }
    }
    
    private void RestoreRigidbodyState()
    {
        if (initialRigidbodyStates.TryGetValue(rb, out RigidbodyState state))
        {
            rb.simulated = state.simulated;
            rb.gravityScale = state.gravityScale;
            rb.bodyType = state.bodyType;
            rb.constraints = state.constraints;
        }
    }
    
    protected virtual void OnCustomRestart()
    {
        // Este método puede ser sobrescrito en clases hijas 
        // para comportamientos específicos adicionales
    }
}