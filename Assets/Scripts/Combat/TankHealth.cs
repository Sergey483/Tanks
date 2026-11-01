using System;
using UnityEngine;

public class TankHealth : MonoBehaviour, IDamageable
{
    [Header("Health")]
    [SerializeField, Min(1)] private int maxHealth = 100;
    [SerializeField] private int currentHealth;

    public int MaxHealth => maxHealth;
    public int CurrentHealth => currentHealth;
    public bool IsDead { get; private set; }

    public event Action<int, int> HealthChanged;
    public event Action Died;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    private void Start()
    {
        HealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void TakeDamage(int amount)
    {
        if (IsDead || amount <= 0)
            return;

        currentHealth = Mathf.Clamp(
            currentHealth - amount,
            0,
            maxHealth
        );

        HealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth == 0)
            Die();
    }

    public void RestoreHealth(int amount)
    {
        if (IsDead || amount <= 0)
            return;

        currentHealth = Mathf.Clamp(
            currentHealth + amount,
            0,
            maxHealth
        );

        HealthChanged?.Invoke(currentHealth, maxHealth);
    }

    private void Die()
    {
        if (IsDead)
            return;

        IsDead = true;
        Died?.Invoke();
    }

    private void OnValidate()
    {
        maxHealth = Mathf.Max(1, maxHealth);

        if (!Application.isPlaying)
            currentHealth = maxHealth;
    }
}
