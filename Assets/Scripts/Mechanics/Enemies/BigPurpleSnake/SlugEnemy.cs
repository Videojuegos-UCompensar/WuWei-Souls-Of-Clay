using UnityEditor.Rendering;
using UnityEngine;

[RequireComponent(typeof(MovementComponent))]
[RequireComponent(typeof(RangedAttackComponent))]
[RequireComponent(typeof(HealthComponent))]
public class SlugEnemy : MonoBehaviour
{
    private MovementComponent movement;
    private RangedAttackComponent rangedAttack;
    private HealthComponent health;
    private Transform player;

    private Animator anim;
    [SerializeField] private bool debugDamage = false;
    [Header("Damage Animation")]
    [SerializeField] private string hitTriggerName = "Hit";
    [SerializeField] private string hitStateName = "Hit";

    [Header("Rangos de comportamiento")]
    [SerializeField] private float detectionRange = 10f;
    [SerializeField] private float escapeRange = 3f;
    [SerializeField] private float shootRange = 4f;
    [SerializeField] private float behaviorHysteresis = 0.4f; // evita oscilaciones entre estados
    



    // Nota: la detección de suelo ahora la realiza el MovementComponent.
    // Evitamos duplicar raycasts guardando la lógica en MovementComponent (SRP/POO).

    private enum EnemyState { Idle, Chasing, Fleeing, Shooting }
    private EnemyState currentState = EnemyState.Idle;

    [Header("Edge handling")]
    [Tooltip("Tiempo mínimo que la babosa dará marcha atrás cuando detecte un borde (s).")]
    [SerializeField] private float forcedRetreatDuration = 0.6f;
    [Tooltip("Distancia mínima (en unidades world) que retrocederá cuando esté acorralada.")]
    [SerializeField] private float forcedRetreatDistance = 1.2f;
    private bool isForcedRetreat = false;
    private Coroutine forcedRetreatCoroutine = null;
    private Vector3 forcedRetreatStart;

    private bool isAttacking = false;
    private Coroutine attackRoutine = null;
    [Header("Ataque")]
    [SerializeField] private float attackAnimDelay = 0.15f; // tiempo desde trigger hasta ejecutar TryAttack (ajusta según anim)
    [SerializeField] private bool useAnimationEventForFire = true; // si true, la animación debe llamar FireProjectile() y allí se ejecuta TryAttack()
    private bool attackExecutedByEvent = false;
    [SerializeField] private string attackStateName = "Attack"; // nombre del estado de animación de ataque
    [SerializeField] private bool debugAttack = false;

    private void Awake()
    {
        movement = GetComponent<MovementComponent>();
        rangedAttack = GetComponent<RangedAttackComponent>();
        health = GetComponent<HealthComponent>();
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        anim = GetComponent<Animator>();

        if (player != null && rangedAttack != null)
            rangedAttack.SetTarget(player);

        // Suscribirse al evento de daño para reproducir animación Hit
        if (health != null)
        {
            try { health.onDamage.AddListener(OnDamaged); } catch { }
        }
    }

    private void OnDisable()
    {
        if (health != null) try { health.onDamage.RemoveListener(OnDamaged); } catch { }
    }

    private void OnDamaged()
    {
        if (debugDamage) Debug.Log($"SlugEnemy.OnDamaged called on {name} (anim present={anim!=null})");

        if (anim != null && debugDamage)
        {
            try {
                var state = anim.GetCurrentAnimatorStateInfo(0);
                Debug.Log($"SlugEnemy animator state: {state.fullPathHash} nameHash={state.shortNameHash} normalizedTime={state.normalizedTime:F2}");
            } catch { }
        }
        // Reproducir animación de golpe de forma segura (trigger)
        SetAnimTriggerSafe(hitTriggerName);

        // Fallback: forzar reproducción directa del estado Hit si la transición por trigger no ocurre
        if (anim != null && !string.IsNullOrEmpty(hitStateName))
        {
            try
            {
                if (debugDamage) Debug.Log($"SlugEnemy: attempting fallback Play({hitStateName})");
                anim.Play(hitStateName, 0, 0f);
            }
            catch { }
        }

        // Marcar como asustado brevemente (opcional)
        SetAnimBoolSafe("IsScared", true);
        try { StopCoroutine("TemporaryScared"); } catch { }
        StartCoroutine(TemporaryScared(0.5f));

        // Indicar que la animación Hit está en curso para bloquear transiciones como Scared
        SetAnimBoolSafe("IsHitPlaying", true);
        try { StopCoroutine("EndHit"); } catch { }
        StartCoroutine(EndHit(0.5f));
    }

    public void TakeDamage(int amount)
    {
        if (health != null)
        {
            health.TakeDamage(amount);
        }
        else
        {
            Debug.LogWarning($"{name}: TakeDamage called but HealthComponent missing");
        }
        // reproducir feedback inmediatamente
        OnDamaged();
    }

    private System.Collections.IEnumerator TemporaryScared(float duration)
    {
        yield return new WaitForSeconds(duration);
        SetAnimBoolSafe("IsScared", false);
    }

    private System.Collections.IEnumerator EndHit(float duration)
    {
        yield return new WaitForSeconds(duration);
        SetAnimBoolSafe("IsHitPlaying", false);
    }

    // Método público para Animation Event al final del clip Hit (más preciso que temporizador)
    public void OnHitEnd()
    {
        SetAnimBoolSafe("IsHitPlaying", false);
    }

    private void Update()
    {
        if (player == null) return;

        float dist = Vector2.Distance(transform.position, player.position);
        bool inShootRange = dist <= shootRange;

        // Iniciar o detener rutina de ataque según estado deseado
        if (currentState == EnemyState.Shooting && attackRoutine == null)
        {
            attackRoutine = StartCoroutine(AttackRoutine());
        }
        else if (currentState != EnemyState.Shooting && attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
            attackRoutine = null;
            isAttacking = false;
        }

        // Decide desired state with hysteresis to prevent rapid flipping
        EnemyState desired = DetermineDesiredState(dist);

        // If desired differs, switch; otherwise execute current state's behavior
        currentState = desired;

        // If we are executing a forced retreat (edge escape), override normal behaviors
        if (isForcedRetreat)
        {
            // Ensure movement is active while retreating; coroutine drives actual movement
            SetAnimBoolSafe("IsScared", true);
            SetAnimBoolSafe("IsMoving", true);
            return;
        }

        switch (currentState)
        {
            case EnemyState.Fleeing:
                HandleFlee();
                break;
            case EnemyState.Shooting:
                HandleShoot();
                break;
            case EnemyState.Chasing:
                HandleChase();
                break;
            default:
                HandleIdle();
                break;
        }

        // no hay flags previas ahora
    }

    private EnemyState DetermineDesiredState(float dist)
    {
        // Apply hysteresis: expand ranges slightly if we're inside them
        float fleeThreshold = (currentState == EnemyState.Fleeing) ? escapeRange + behaviorHysteresis : escapeRange;
        if (dist <= fleeThreshold) return EnemyState.Fleeing;

        // Ahora: cualquier distancia hasta detectionRange provoca Shooting (a menos que estemos en flee)
        float detectionThreshold = (currentState == EnemyState.Shooting) ? detectionRange + behaviorHysteresis : detectionRange;
        if (dist <= detectionThreshold) return EnemyState.Shooting;

        return EnemyState.Idle;
    }

    private void HandleIdle()
    {
        SetAnimBoolSafe("IsMoving", false);
        SetAnimBoolSafe("IsScared", false);
        movement?.Idle();
    }

    // Maneja el comportamiento de perseguir al jugador.
    // El enemigo avanzará hacia el jugador solo si MovementComponent indica que hay suelo
    // en la dirección de movimiento. Esto evita duplicar raycasts en este script.
    private void HandleChase()
    {
        SetAnimBoolSafe("IsScared", false);

        // Intentar moverse hacia el jugador. MovementComponent calcula la dirección y
        // hace el flip del sprite. Después preguntamos si hay suelo delante.
        movement?.MoveTowards(player.position);
        if (movement != null && movement.IsGroundAhead())
        {
            SetAnimBoolSafe("IsMoving", true);
        }
        else
        {
            // No hay suelo delante: iniciar un retreat forzado para alejarse del borde
            Vector2 awayDir = (transform.position - player.position).normalized;
            StartForcedRetreat(awayDir);
        }
    }

    // Maneja la lógica de huida: intenta alejarse del jugador si hay suelo atrás.
    private void HandleFlee()
    {
        SetAnimBoolSafe("IsScared", true);
        Vector2 awayDir = (transform.position - player.position).normalized;
        if (movement != null && movement.IsGroundAheadInDirection(awayDir))
        {
            // Forzamos un paso en la dirección awayDir (signo X). Usa MoveInDirection para un paso simple.
            movement.MoveInDirection(Mathf.Sign(awayDir.x));
            SetAnimBoolSafe("IsMoving", true);
        }
        else
        {
            // No hay suelo atrás: iniciar un retreat forzado (moverse hacia el interior de la plataforma)
            StartForcedRetreat(awayDir);
        }
    }

    private void HandleShoot()
    {
        SetAnimBoolSafe("IsScared", false);
        // Disparar mientras se mueve: intentar avanzar hacia el jugador si hay suelo
        movement?.MoveTowards(player.position);
        if (movement != null && movement.IsGroundAhead())
        {
            SetAnimBoolSafe("IsMoving", true);
        }
        else
        {
            // Sin suelo: iniciar retreat forzado para evitar que se quede en el borde
            Vector2 awayDir = (transform.position - player.position).normalized;
            StartForcedRetreat(awayDir);
            SetAnimBoolSafe("IsMoving", true);
        }
        // La lógica de disparo real corre en la coroutine AttackRoutine (iniciada/terminada por Update)
    }

    private void StartForcedRetreat(Vector2 awayDir)
    {
        if (isForcedRetreat) return;
        if (forcedRetreatCoroutine != null) StopCoroutine(forcedRetreatCoroutine);
        forcedRetreatCoroutine = StartCoroutine(ForcedRetreatRoutine(awayDir));
    }

    private System.Collections.IEnumerator ForcedRetreatRoutine(Vector2 awayDir)
    {
        isForcedRetreat = true;
        forcedRetreatStart = transform.position;
        float elapsed = 0f;

        // Strategy:
        // 1) Preferimos dar un paso hacia el jugador la PRIMERA vez si hay suelo en esa dirección
        // 2) Si no es seguro moverse hacia el jugador, intentar retroceder (awayDir) como antes
        // 3) En todo caso, respetar la duración máxima y distancia mínima configuradas

        // Calculamos la dirección hacia el jugador
        Vector2 toPlayerDir = (player != null) ? ((Vector2)player.position - (Vector2)transform.position).normalized : -awayDir;

        bool moved = false;

        // Intentar mover hacia el jugador si hay suelo en esa dirección
        if (movement != null && movement.IsGroundAheadInDirection(toPlayerDir))
        {
            // Mover hacia el jugador hasta alcanzar la distancia mínima o tiempo límite
            while (elapsed < forcedRetreatDuration && Vector2.Distance(transform.position, forcedRetreatStart) < forcedRetreatDistance)
            {
                movement.MoveTowards(player.position);
                SetAnimBoolSafe("IsScared", true);
                SetAnimBoolSafe("IsMoving", true);

                // Si en algún momento delante ya no hay suelo, abortar para no caer
                if (!movement.IsGroundAhead()) break;

                elapsed += Time.deltaTime;
                moved = true;
                yield return null;
            }
        }

        // Si no pudimos movernos hacia el jugador (no seguro), intentamos retroceder como fallback
        if (!moved)
        {
            elapsed = 0f;
            forcedRetreatStart = transform.position; // reiniciamos punto de referencia
            while (elapsed < forcedRetreatDuration && Vector2.Distance(transform.position, forcedRetreatStart) < forcedRetreatDistance)
            {
                if (movement != null && movement.IsGroundAheadInDirection(awayDir))
                {
                    // Forzamos un paso simple hacia awayDir para garantizar separación del borde
                    movement.MoveInDirection(Mathf.Sign(awayDir.x));
                }
                else
                {
                    // Si no hay suelo ni adelante ni atrás, detenemos el intento para evitar caer
                    break;
                }

                SetAnimBoolSafe("IsScared", true);
                SetAnimBoolSafe("IsMoving", true);

                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        // Finalizar retreat: volver a Idle y permitir comportamiento normal
        isForcedRetreat = false;
        forcedRetreatCoroutine = null;
        movement?.Idle();
        SetAnimBoolSafe("IsMoving", false);
    }

    // Anim event: opcional, se puede usar para efectos (no necesario para disparo)
    public void FireProjectile()
    {
        // si usamos Animation Event como sincronización, ejecuta TryAttack aquí
        if (useAnimationEventForFire && rangedAttack != null)
        {
            if (rangedAttack.IsReadyToFire())
            {
                rangedAttack.TryAttack();
            }
            attackExecutedByEvent = true;
            return;
        }

        // si quieres efectos VFX/Sound en el frame exacto, dispara aquí.
    }

    private System.Collections.IEnumerator AttackRoutine()
    {
        isAttacking = true;
        while (currentState == EnemyState.Shooting)
        {
            if (rangedAttack != null && rangedAttack.IsReadyToFire())
            {
                // Reproducir animación
                attackExecutedByEvent = false;
                SetAnimTriggerSafe("Attack");

                if (useAnimationEventForFire)
                {
                    // Esperar a que el Animator entre en el estado de ataque (para asegurar que la anim se reprodujo)
                    float waitTime = 0f;
                    float timeout = 1.5f; // seguridad: no esperar indefinidamente
                    if (anim != null && !string.IsNullOrEmpty(attackStateName))
                    {
                        while (!anim.GetCurrentAnimatorStateInfo(0).IsName(attackStateName) && waitTime < timeout && currentState == EnemyState.Shooting)
                        {
                            waitTime += Time.deltaTime;
                            yield return null;
                        }
                    }

                    // Ahora esperar al Animation Event que ejecuta FireProjectile
                    waitTime = 0f;
                    while (!attackExecutedByEvent && waitTime < timeout && currentState == EnemyState.Shooting)
                    {
                        waitTime += Time.deltaTime;
                        yield return null;
                    }
                    if (debugAttack) Debug.Log($"AttackRoutine: waited {waitTime:F2}s for animation event (animPresent={anim!=null})");
                }
                else
                {
                    // Esperar un pequeño delay para sincronizar trigger -> impacto
                    yield return new WaitForSeconds(attackAnimDelay);
                    // Ejecutar el ataque (TryAttack maneja cooldown)
                    rangedAttack.TryAttack();
                }
            }

            // Esperar un frame antes de comprobar otra vez (evita loop tight)
            yield return null;
        }

        isAttacking = false;
        attackRoutine = null;
    }


    private void ChangeState(EnemyState newState)
    {
        currentState = newState;
    }

    // Seguridad al setear parámetros del Animator (por si falta o el parámetro no existe)
    private void SetAnimBoolSafe(string param, bool value)
    {
        if (anim == null) return;
        try
        {
            anim.SetBool(param, value);
        }
        catch (System.ArgumentException)
        {
        }
    }

    private void SetAnimTriggerSafe(string param)
    {
        if (anim == null) return;
        try
        {
            anim.SetTrigger(param);
        }
        catch (System.ArgumentException)
        {
        }
    }


    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, escapeRange);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, shootRange);

        // Si MovementComponent expone un groundCheck (no público), podemos dibujar una aproximación.
        // Para evitar duplicación, omitimos los raycasts específicos aquí.
    }
}
