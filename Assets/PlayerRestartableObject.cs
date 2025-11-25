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

    if (storePosition)
        transform.position = savedPosition;

    if (storeRotation)
        transform.rotation = savedRotation;

    if (rb != null && storeVelocity)
    {
        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0;
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