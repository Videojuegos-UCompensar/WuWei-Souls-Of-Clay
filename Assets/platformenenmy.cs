using UnityEngine;

public class PlatformSetup : MonoBehaviour
{
    [Tooltip("Make sure all enemies are on this layer for proper platform collision")]
    public LayerMask enemyLayer;
    
    void Start()
    {
        // Verify that "Enemy" layer exists
        int enemyLayerIndex = LayerMask.NameToLayer("Enemy");
        if (enemyLayerIndex == -1)
        {
            Debug.LogError("The 'Enemy' layer doesn't exist. Please create it in the Layer settings in Unity.");
            Debug.LogError("Go to Edit > Project Settings > Tags and Layers to add the 'Enemy' layer");
        }
        
        // Check if all enemies have the correct layer
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemy in enemies)
        {
            if (enemy.layer != enemyLayerIndex)
            {
                Debug.LogWarning($"Enemy '{enemy.name}' is not on the 'Enemy' layer. Setting it now.");
                enemy.layer = enemyLayerIndex;
            }
            
            // Ensure enemies have required components
            if (enemy.GetComponent<Collider2D>() == null)
            {
                Debug.LogError($"Enemy '{enemy.name}' is missing a Collider2D component!");
            }
            
            if (enemy.GetComponent<Rigidbody2D>() == null)
            {
                Debug.LogError($"Enemy '{enemy.name}' is missing a Rigidbody2D component!");
            }
        }
    }
    
    // This is a utility method to help you set up any existing enemy in the scene
    public void SetupAllEnemies()
    {
        int enemyLayerIndex = LayerMask.NameToLayer("Enemy");
        if (enemyLayerIndex == -1) return;
        
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemy in enemies)
        {
            enemy.layer = enemyLayerIndex;
            
            if (enemy.GetComponent<Collider2D>() == null)
            {
                enemy.AddComponent<CapsuleCollider2D>();
                Debug.Log($"Added CapsuleCollider2D to '{enemy.name}'");
            }
            
            if (enemy.GetComponent<Rigidbody2D>() == null)
            {
                Rigidbody2D rb = enemy.AddComponent<Rigidbody2D>();
                rb.constraints = RigidbodyConstraints2D.FreezeRotation;
                rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                Debug.Log($"Added Rigidbody2D to '{enemy.name}'");
            }
        }
    }
}