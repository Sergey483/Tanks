using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Projectile : MonoBehaviour
{
    [SerializeField] private ImpactEffect impactEffectPrefab;
    [SerializeField] private ImpactEffect metalImpactEffectPrefab;

    private Rigidbody projectileRigidbody;
    private int damage;
    private float lifetime;
    private Collider ownerCollider;
    private bool isInitialized;

    private void Awake()
    {
        projectileRigidbody = GetComponent<Rigidbody>();
    }

    public void Initialize(
        int newDamage,
        float speed,
        float newLifetime,
        Collider newOwnerCollider
    )
    {
        damage = newDamage;
        lifetime = newLifetime;
        ownerCollider = newOwnerCollider;
        isInitialized = true;

        if (ownerCollider != null &&
            TryGetComponent(out Collider projectileCollider))
        {
            Physics.IgnoreCollision(
                projectileCollider,
                ownerCollider
            );
        }

        projectileRigidbody.linearVelocity =
            transform.forward * speed;

        Destroy(gameObject, lifetime);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!isInitialized)
            return;

        if (collision.collider == ownerCollider)
            return;

        IDamageable damageable =
            collision.collider.GetComponentInParent<IDamageable>();

        if (damageable != null)
            damageable.TakeDamage(damage);

        CreateImpactEffect(collision);

        Destroy(gameObject);
    }

    private void CreateImpactEffect(Collision collision)
    {
        ImpactSurface surface = collision.collider.GetComponentInParent<ImpactSurface>();
        bool isMetal = surface != null && surface.Type == ImpactSurface.SurfaceType.Metal;

        // Для металу — іскри, для ґрунту та бетону — звичайний ефект.
        ImpactEffect effect = isMetal && metalImpactEffectPrefab != null
            ? metalImpactEffectPrefab
            : impactEffectPrefab;

        if (effect == null ||
            collision.contactCount == 0)
        {
            return;
        }

        ContactPoint contact = collision.GetContact(0);

        Instantiate(
            effect,
            contact.point,
            Quaternion.LookRotation(contact.normal)
        );
    }
}
