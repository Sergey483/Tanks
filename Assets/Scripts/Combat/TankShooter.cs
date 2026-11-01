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

    private float nextFireTime;

    private void Update()
    {
        if (usePlayerInput && Input.GetMouseButtonDown(0))
            TryShoot();
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

        Projectile projectile = Instantiate(
            config.projectilePrefab,
            firePoint.position,
            firePoint.rotation
        );

        projectile.Initialize(
            config.damage,
            config.projectileSpeed,
            config.projectileLifetime,
            ownerCollider
        );

        if (muzzleFlash != null)
            muzzleFlash.Play();

        if (audioSource != null && shootClip != null)
            audioSource.PlayOneShot(shootClip);

        nextFireTime = Time.time + config.fireCooldown;
        return true;
    }
}
