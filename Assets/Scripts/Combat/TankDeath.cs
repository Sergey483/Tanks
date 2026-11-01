using UnityEngine;

[RequireComponent(typeof(TankHealth))]
public class TankDeath : MonoBehaviour
{
    [SerializeField] private GameObject explosionPrefab;
    [SerializeField, Min(0f)] private float destroyDelay = 0.1f;

    private TankHealth health;

    private void Awake()
    {
        health = GetComponent<TankHealth>();
    }

    private void OnEnable()
    {
        health.Died += HandleDeath;
    }

    private void OnDisable()
    {
        health.Died -= HandleDeath;
    }

    private void HandleDeath()
    {
        if (explosionPrefab != null)
        {
            Instantiate(
                explosionPrefab,
                transform.position,
                Quaternion.identity
            );
        }

        DisableTank();
        Destroy(gameObject, destroyDelay);
    }

    private void DisableTank()
    {
        Collider[] colliders =
            GetComponentsInChildren<Collider>();

        foreach (Collider tankCollider in colliders)
            tankCollider.enabled = false;

        TankShooter shooter = GetComponent<TankShooter>();

        if (shooter != null)
            shooter.enabled = false;
    }
}
