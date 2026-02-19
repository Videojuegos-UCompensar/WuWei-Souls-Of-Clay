using UnityEngine;
using UnityEngine.Playables;

public class EjecutarCinematica : MonoBehaviour
{
    public HealthComponent playerHealth;

    public PlayableDirector playableDirector;
    public HealthComponent bossHealth;
    public BossPhase1 bossAI;
    public HomingOrb bossA1;
    public WindSlash bossA2;

    [Range(0f,1f)]
    public float healthThreshold = 0.25f;

    public float timeLimit = 120f;

    private bool cinematicaEjecutada = false;
    private float timer = 0f;

    public Movimiento2D playerMovement;
    public Transform playerTransform;

    private void Start()
{
    playableDirector.stopped += OnCinematicaTerminada;
    bossHealth.OnHealthChanged += CheckHealth;

    if (playerHealth != null)
        playerHealth.onDeath += OnPlayerDeath;
}

    private void Update()
    {
        if (cinematicaEjecutada) return;

        if (bossAI != null && bossAI.IsPlayerDetected())
        {
            timer += Time.deltaTime;

            if (timer >= timeLimit)
            {
                Ejecutar();
            }
        }
    }

    public void DealCinematicDamage()
{
    if (playerHealth == null)
    {
        Debug.LogError("playerHealth no está asignado en EjecutarCinematica");
        return;
    }

    Debug.Log("CINEMÁTICA: aplicando 100 de daño");

    playerHealth.TakeDamage(100);
}



    private void CheckHealth(float current, float max)
    {
        if (cinematicaEjecutada) return;

        float porcentaje = current / max;

        if (porcentaje <= healthThreshold)
        {
            Ejecutar();
        }
    }

private void OnPlayerDeath()
{
    if (cinematicaEjecutada) return;

    // Solo si el boss fue quien lo mató
    if (playerHealth.LastDamageSource == bossAI.gameObject)
    {
        Ejecutar();
    }
}

    private void Ejecutar()
{
    if (cinematicaEjecutada) return;

    cinematicaEjecutada = true;

     if (bossAI != null)
    {
        bossAI.StopBoss();    // 🔥 mata especiales en curso
        bossAI.enabled = false;         // desactiva IA
    }

    // Colocar al jugador en la posición exacta
    if (playerTransform != null)
    {
        playerTransform.position = new Vector3(396.52f, 11.44f, playerTransform.position.z);
    }

    playableDirector.Play();
}





    void OnCinematicaTerminada(PlayableDirector director)
    {
        LevelManager.Instance?.RestartLevel();
    }
}
