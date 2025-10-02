using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{
    // Singleton para acceder fácilmente desde otros scripts
    public static LevelManager Instance { get; private set; }
    
    [Header("Configuración")]
    public float restartDelay = 1.5f;             // Tiempo de espera antes de reiniciar
    public bool useCheckpoints = true;           // Si se usan checkpoints o no
    public Transform defaultSpawnPoint;           // Punto de spawn predeterminado
    
    [Header("Efectos")]
    public bool useRestartEffect = true;          // Usar efecto de transición al reiniciar
    public GameObject restartEffectPrefab;        // Prefab con efecto visual para reinicio (opcional)
    public AudioClip restartSound;                // Sonido al reiniciar (opcional)
    
    [Header("Guardado")]
    public bool saveProgress = false;             // Si se guarda el progreso entre sesiones
    public bool resetObjectsOnRestart = true;     // Si se resetean objetos al reiniciar
    
    // Variables privadas
    private Transform currentCheckpoint;          // Checkpoint activo
    private Vector3 playerStartPosition;          // Posición inicial del jugador
    private Quaternion playerStartRotation;       // Rotación inicial del jugador
    private List<IRestartable> restartableObjects = new List<IRestartable>(); // Objetos que implementan la interfaz IRestartable
    private GameObject player;                    // Referencia al jugador
    private AudioSource audioSource;              // Componente de audio

    private void Awake()
    {
        // Configurar singleton
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && restartSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    private void Start()
    {
        // Buscar al jugador por su tag
        player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogWarning("No se encontró objeto con tag 'Player'");
        }
        else
        {
            // Guardar posición inicial del jugador
            playerStartPosition = player.transform.position;
            playerStartRotation = player.transform.rotation;
        }
        
        // Si no hay punto de spawn definido, usar posición del jugador
        if (defaultSpawnPoint == null && player != null)
        {
            defaultSpawnPoint = player.transform;
            Debug.Log("No hay punto de spawn definido, usando posición inicial del jugador");
        }
        
        // Buscar todos los objetos que implementan IRestartable
        FindRestartableObjects();
    }
    
    // Método para encontrar todos los objetos que implementan IRestartable
    public void FindRestartableObjects()
    {
        restartableObjects.Clear();
        
        // Encontrar todos los MonoBehaviours que implementan IRestartable
        MonoBehaviour[] scripts = FindObjectsOfType<MonoBehaviour>();
        foreach (MonoBehaviour script in scripts)
        {
            if (script is IRestartable restartable)
            {
                restartableObjects.Add(restartable);
                Debug.Log($"Objeto restartable encontrado: {script.gameObject.name}");
            }
        }
    }
    
    // Método principal para reiniciar el nivel
    public void RestartLevel()
    {
        StartCoroutine(RestartLevelSequence());
    }
    
    // Secuencia de reinicio del nivel
    private IEnumerator RestartLevelSequence()
    {
        // Reproducir sonido si existe
        if (audioSource != null && restartSound != null)
        {
            audioSource.PlayOneShot(restartSound);
        }
        
        // Instanciar efecto visual si existe
        if (useRestartEffect && restartEffectPrefab != null)
        {
            Instantiate(restartEffectPrefab, player != null ? player.transform.position : Vector3.zero, Quaternion.identity);
        }
        
        // Pausar brevemente antes de reiniciar
        yield return new WaitForSeconds(restartDelay);
        
        // Si estamos usando la misma escena para reiniciar
        if (resetObjectsOnRestart)
        {
            ResetLevel();
        }
        else
        {
            // Reiniciar cargando la escena actual de nuevo
            Scene currentScene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(currentScene.buildIndex);
        }
    }
    
    // Método para reiniciar todos los objetos sin recargar la escena
    public void ResetLevel()
    {
        // Reiniciar el jugador
        if (player != null)
        {
            // Determinar posición de spawn
            Vector3 spawnPosition;
            Quaternion spawnRotation;
            
            if (useCheckpoints && currentCheckpoint != null)
            {
                spawnPosition = currentCheckpoint.position;
                spawnRotation = currentCheckpoint.rotation;
            }
            else if (defaultSpawnPoint != null)
            {
                spawnPosition = defaultSpawnPoint.position;
                spawnRotation = defaultSpawnPoint.rotation;
            }
            else
            {
                spawnPosition = playerStartPosition;
                spawnRotation = playerStartRotation;
            }
            
            // Establecer posición del jugador
            player.transform.position = spawnPosition;
            player.transform.rotation = spawnRotation;
            
            // Reactivar al jugador si estaba desactivado
            player.SetActive(true);
            
            // Restaurar la salud del jugador
            PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.RestoreFullHealth();
            }
            
            // Reiniciar componentes del jugador
            Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
            if (playerRb != null)
            {
                playerRb.velocity = Vector2.zero;
            }
            
            // Reactivar colliders si estaban desactivados
            Collider2D[] colliders = player.GetComponents<Collider2D>();
            foreach (Collider2D collider in colliders)
            {
                collider.enabled = true;
            }
            
            // Reactivar scripts de control
            MonoBehaviour[] playerScripts = player.GetComponents<MonoBehaviour>();
            foreach (MonoBehaviour script in playerScripts)
            {
                script.enabled = true;
            }
        }
        
        // Reiniciar todos los objetos que implementan IRestartable
        // Usar una lista temporal para evitar problemas con objetos destruidos
        List<IRestartable> validRestartables = new List<IRestartable>();
        
        foreach (IRestartable restartable in restartableObjects)
        {
            // Verificar si el objeto todavía existe antes de intentar reiniciarlo
            if (IsRestartableValid(restartable))
            {
                validRestartables.Add(restartable);
                restartable.OnLevelRestart();
            }
            else
            {
                Debug.Log("Objeto restartable destruido encontrado, omitiendo");
            }
        }
        
        // Actualizar la lista para eliminar objetos destruidos
        restartableObjects = validRestartables;
        
        Debug.Log("Nivel reiniciado sin recargar la escena");
    }
    
    // Método para verificar si un objeto restartable sigue siendo válido
    private bool IsRestartableValid(IRestartable restartable)
    {
        // Si es un MonoBehaviour, podemos verificar si el gameObject existe
        if (restartable is MonoBehaviour monoBehaviour)
        {
            return monoBehaviour != null && monoBehaviour.gameObject != null;
        }
        
        // Para objetos que no son MonoBehaviour, verificar si no es null
        return restartable != null;
    }
    
    // Método para establecer un nuevo checkpoint
    public void SetCheckpoint(Transform newCheckpoint)
    {
        currentCheckpoint = newCheckpoint;
        Debug.Log($"Nuevo checkpoint establecido: {newCheckpoint.name}");
    }
    
    // Para detección de componentes en el editor
    private void OnValidate()
    {
        if (restartEffectPrefab == null && useRestartEffect)
        {
            Debug.LogWarning("RestartEffectPrefab no asignado pero useRestartEffect está activo");
        }
    }
}

// Interfaz para objetos que necesitan reiniciarse cuando el nivel se reinicia
public interface IRestartable
{
    void OnLevelRestart();
}