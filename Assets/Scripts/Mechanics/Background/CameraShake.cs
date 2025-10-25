using UnityEngine;
using System.Collections;

public class CameraShake : MonoBehaviour
{
    private Vector3 originalPos;
    private Coroutine shakeCoroutine;
    // Offset computed by the shake coroutine. CameraFollow will read this
    // and apply it when computing the final camera position to avoid
    // the follow logic overwriting the shake.
    public Vector3 CurrentOffset { get; private set; } = Vector3.zero;

    public void Shake(float duration)
    {
        // Si ya hay un shake en curso, lo reiniciamos
        if (shakeCoroutine != null)
            StopCoroutine(shakeCoroutine);

        // Record originalPos in case someone needs it; not used to set transform here
        originalPos = transform.localPosition;
        shakeCoroutine = StartCoroutine(DoShake(duration));
    }

    private IEnumerator DoShake(float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float x = Random.Range(-0.05f, 0.05f);
            float y = Random.Range(-0.05f, 0.05f);

            // Update public offset; CameraFollow will apply this offset when setting position
            CurrentOffset = new Vector3(x, y, 0f);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // clear offset
        CurrentOffset = Vector3.zero;
        shakeCoroutine = null;
    }
}
