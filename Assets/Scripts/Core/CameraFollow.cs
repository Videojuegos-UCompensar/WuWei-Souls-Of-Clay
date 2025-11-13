using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float smoothSpeed = 5f;
    [SerializeField] private Vector3 offset;

    private CameraShake shake;

    private void Awake()
    {
        shake = GetComponent<CameraShake>();
    }
    private void FixedUpdate()
    {
        if (target == null) return;

        // Posición deseada con offset
        Vector3 desiredPosition = target.position + offset;

        // Movimiento suave (lerp)
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

        // Apply camera shake offset (if present) on top of the smoothed follow position.
        if (shake != null)
        {
            transform.position = smoothedPosition + shake.CurrentOffset;
        }
        else
        {
            transform.position = smoothedPosition;
        }
    }
}
