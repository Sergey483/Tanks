using UnityEngine;

// Додаткове завдання ⭐: ремонтний бонус, який викликає RestoreHealth.
[RequireComponent(typeof(Collider))]
public class RepairPickup : MonoBehaviour
{
    [SerializeField, Min(1)] private int repairAmount = 30;
    [SerializeField] private float rotationSpeed = 90f;

    private void Update()
    {
        transform.Rotate(0f, rotationSpeed * Time.deltaTime, 0f, Space.World);
    }

    private void OnTriggerEnter(Collider other)
    {
        TankHealth health = other.GetComponentInParent<TankHealth>();

        if (health == null || health.IsDead)
            return;

        if (health.CurrentHealth >= health.MaxHealth)
            return;

        health.RestoreHealth(repairAmount);
        Destroy(gameObject);
    }
}
