using UnityEngine;

[CreateAssetMenu(menuName = "Projectiles/ProjectileData", fileName = "NewProjectileData")]
public class ProjectileData : ScriptableObject
{
    [Header("Damage")]
    public int baseDamage = 0; // additional/static damage for this projectile type

    [Header("Movement")]
    public float speed = 1f;
    public float lifeTime = 10f;

    [Header("Behavior")]
    public bool pierce = false;
    public string element = "";
}
