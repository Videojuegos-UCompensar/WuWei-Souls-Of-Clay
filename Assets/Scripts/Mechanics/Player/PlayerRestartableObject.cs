using UnityEngine;

public class PlayerRestartableObject : RestartableObject
{
    private Vector3 savedPosition;
    private Animator animator;
    private Quaternion savedRotation;

    void Awake()
    {
    savedPosition = transform.position;
    savedRotation = transform.rotation;
    animator = GetComponent<Animator>();
    }

    public override void OnLevelRestart()
    {
    // Llamamos primero al comportamiento original
    base.OnLevelRestart();

    // Luego aplicas la lógica especial del jugador

    if (storePosition)
        transform.position = savedPosition;

    if (storeRotation)
        transform.rotation = savedRotation;

    if (rb != null && storeVelocity)
    {
        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0;
    }

    if (animator != null)
{
    animator.Rebind();
    animator.Update(0f);
    animator.Play("Idle", 0, 0f);
}
    // 🔄 Volver visible
foreach (var r in GetComponentsInChildren<Renderer>())
    r.enabled = true;

// 🔄 Reactivar colisiones
foreach (var col in GetComponentsInChildren<Collider2D>())
    col.enabled = true;

// 🔄 Reactivar físicas
if (rb != null)
{
    rb.simulated = true;
    rb.velocity = Vector2.zero;
    rb.angularVelocity = 0f;
}


    OnCustomRestart();
    }

    // Permite que un checkpoint actualice la posición guardada
    public void UpdateCheckpoint(Vector3 newPos)
    {
        savedPosition = newPos;
    }

    public void UpdateCheckpoint(Vector3 newPos, Quaternion newRot)
    {
        savedPosition = newPos;
        savedRotation = newRot;
    }
}