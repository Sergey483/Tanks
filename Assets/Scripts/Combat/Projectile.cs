using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Projectile : MonoBehaviour
{
    [SerializeField] private ImpactEffect impactEffectPrefab;

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
        if (impactEffectPrefab == null ||
            collision.contactCount == 0)
        {
            return;
        }

        ContactPoint contact = collision.GetContact(0);

        Instantiate(
            impactEffectPrefab,
            contact.point,
            Quaternion.LookRotation(contact.normal)
        );
    }
}
