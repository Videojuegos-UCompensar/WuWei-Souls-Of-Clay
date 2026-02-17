using UnityEngine;
using UnityEngine.Playables;

public class EjecutarCinematica : MonoBehaviour
{
    public HealthComponent playerHealth;

    public PlayableDirector playableDirector;
    public HealthComponent bossHealth;
    public BossPhase1 bossAI;

    [Range(0f,1f)]
    public float healthThreshold = 0.25f;

    public float timeLimit = 120f;

    private bool cinematicaEjecutada = false;
    private float timer = 0f;

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

    private void Ejecutar()
{
    if (cinematicaEjecutada) return;

    cinematicaEjecutada = true;

    if (bossAI != null)
        bossAI.enabled = false;

    playableDirector.Play();
}

private void OnPlayerDeath()
{
    if (cinematicaEjecutada) return;

    // Solo si el boss fue quien lo mató
   
        Ejecutar();
    
}



    void OnCinematicaTerminada(PlayableDirector director)
    {
        LevelManager.Instance?.RestartLevel();
    }
}
