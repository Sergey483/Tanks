using UnityEngine;

[CreateAssetMenu(
    fileName = "NewTankConfig",
    menuName = "Tanks/Tank Config"
)]
public class TankConfig : ScriptableObject
{
    [Header("Weapon")]
    [Min(1)]
    public int damage = 25;

    [Min(0.1f)]
    public float projectileSpeed = 18f;

    [Min(0.05f)]
    public float fireCooldown = 0.7f;

    [Min(0.1f)]
    public float projectileLifetime = 5f;

    public Projectile projectilePrefab;
}
