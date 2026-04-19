using UnityEngine;
using UnityEngine.InputSystem;

public class plat_atravesable : MonoBehaviour
{
    private BoxCollider2D platformCollider;
    private Collider2D playerCollider;

    private bool ignoringCollision = false;

    void Start()
    {
        platformCollider = GetComponent<BoxCollider2D>();

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            playerCollider = player.GetComponent<Collider2D>();
    }

    void Update()
    {
        if (playerCollider == null) return;

        bool pressingDown =
            Keyboard.current.sKey.isPressed ||
            Keyboard.current.downArrowKey.isPressed;

        float playerBottom = playerCollider.bounds.min.y;
        float platformTop = platformCollider.bounds.max.y;

        bool playerAbove = playerBottom >= platformTop - 0.05f;

        // activar caída
        if (pressingDown && playerAbove && !ignoringCollision)
        {
            ignoringCollision = true;
            Physics2D.IgnoreCollision(platformCollider, playerCollider, true);
        }

        // restaurar colisión cuando el jugador ya esté debajo
        if (ignoringCollision && playerCollider.bounds.max.y < platformTop - 0.1f)
        {
            ignoringCollision = false;
            Physics2D.IgnoreCollision(platformCollider, playerCollider, false);
        }
    }
}