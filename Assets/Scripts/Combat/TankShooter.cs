using System.Collections.Generic;
using UnityEngine;

public class TankShooter : MonoBehaviour
{
    [SerializeField] private TankConfig config;
    [SerializeField] private Transform firePoint;
    [SerializeField] private Collider ownerCollider;
    [SerializeField] private bool usePlayerInput = true;

    [Header("Feedback")]
    [SerializeField] private ParticleSystem muzzleFlash;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip shootClip;

    public TankConfig Config => config;

    private readonly List<Projectile> activeProjectiles = new List<Projectile>();
    private float nextFireTime;

    private void Update()
    {
        if (usePlayerInput && Input.GetMouseButtonDown(0))
            TryShoot();
    }

    public void SetConfig(TankConfig newConfig)
    {
        if (newConfig != null)
            config = newConfig;
    }

    public bool TryShoot()
    {
        if (config == null ||
            config.projectilePrefab == null ||
            firePoint == null)
        {
            Debug.LogWarning(
                "TankShooter налаштований не повністю."
            );
            return false;
        }

        if (Time.time < nextFireTime)
            return false;

        activeProjectiles.RemoveAll(item => item == null);
        if (config.maxActiveProjectiles > 0 &&
            activeProjectiles.Count >= config.maxActiveProjectiles)
        {
            return false;
        }

        Projectile projectile = Instantiate(
            config.projectilePrefab,
            firePoint.position,
            firePoint.rotation
        );

        projectile.transform.localScale *= config.projectileScale;

        projectile.Initialize(
            config.damage,
            config.projectileSpeed,
            config.projectileLifetime,
            ownerCollider
        );

        activeProjectiles.Add(projectile);

        if (muzzleFlash != null)
            muzzleFlash.Play();

        if (audioSource != null && shootClip != null)
            audioSource.PlayOneShot(shootClip);

        nextFireTime = Time.time + config.fireCooldown;
        return true;
    }
}
