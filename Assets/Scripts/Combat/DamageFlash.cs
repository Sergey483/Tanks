using System.Collections;
using UnityEngine;

// Додаткове завдання ⭐: коротке миготіння матеріалу танка після отримання шкоди.
[RequireComponent(typeof(TankHealth))]
public class DamageFlash : MonoBehaviour
{
    [SerializeField] private Color flashColor = Color.white;
    [SerializeField, Min(0.01f)] private float flashDuration = 0.1f;

    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private TankHealth health;
    private Renderer[] tankRenderers;
    private MaterialPropertyBlock propertyBlock;
    private int lastHealth;

    private void Awake()
    {
        health = GetComponent<TankHealth>();
        tankRenderers = GetComponentsInChildren<MeshRenderer>();
        propertyBlock = new MaterialPropertyBlock();
        lastHealth = health.MaxHealth;
    }

    private void OnEnable()
    {
        health.HealthChanged += HandleHealthChanged;
    }

    private void OnDisable()
    {
        health.HealthChanged -= HandleHealthChanged;
    }

    private void HandleHealthChanged(int currentHealth, int maxHealth)
    {
        if (currentHealth < lastHealth)
        {
            StopAllCoroutines();
            StartCoroutine(Flash());
        }

        lastHealth = currentHealth;
    }

    private IEnumerator Flash()
    {
        propertyBlock.SetColor(ColorId, flashColor);

        foreach (Renderer tankRenderer in tankRenderers)
            tankRenderer.SetPropertyBlock(propertyBlock);

        yield return new WaitForSeconds(flashDuration);

        foreach (Renderer tankRenderer in tankRenderers)
            tankRenderer.SetPropertyBlock(null);
    }
}
