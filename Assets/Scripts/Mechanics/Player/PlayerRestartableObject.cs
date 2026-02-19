using UnityEngine;

public class PlayerRestartableObject : RestartableObject
{
    private Vector3 savedPosition;
    private Quaternion savedRotation;

    void Awake()
    {
    savedPosition = transform.position;
    savedRotation = transform.rotation;
    }

    public override void OnLevelRestart()
    {
    // Llamamos primero al comportamiento original
    base.OnLevelRestart();

    // Luego aplicas la lógica especial del jugador

    var feedback = GetComponent<DamageFeedback>();
    if (feedback != null)
    {
    feedback.ResetVisual();
    }

    if (storePosition)
        transform.position = savedPosition;

    if (storeRotation)
        transform.rotation = savedRotation;

    if (rb != null && storeVelocity)
    {
        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0;
    }

    var movimiento = GetComponent<Movimiento2D>();
if (movimiento != null)
{
    movimiento.mirandoDrecha = true;

    // Si usas localScale para el flip
    Vector3 scale = transform.localScale;
    scale.x = Mathf.Abs(scale.x);
    transform.localScale = scale;

    // Si usas SpriteRenderer.flipX (mejor opción)
    var sr = GetComponent<SpriteRenderer>();
    if (sr != null)
        sr.flipX = false;
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