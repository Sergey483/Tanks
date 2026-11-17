using UnityEngine;

// Тестова мішень для тиру: отримує шкоду і змінює колір після кожного влучання.
public class TestTarget : MonoBehaviour, IDamageable
{
    [SerializeField, Min(1)] private int maxHealth = 100;
    [SerializeField] private Color fullColor = Color.white;
    [SerializeField] private Color hitColor = new Color(0.9f, 0.15f, 0.1f);

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    private int currentHealth;
    private Renderer targetRenderer;
    private MaterialPropertyBlock propertyBlock;

    private void Awake()
    {
        currentHealth = maxHealth;
        targetRenderer = GetComponentInChildren<Renderer>();
        propertyBlock = new MaterialPropertyBlock();
        UpdateColor();
    }

    public void TakeDamage(int amount)
    {
        if (amount <= 0 || currentHealth <= 0)
            return;

        currentHealth = Mathf.Max(0, currentHealth - amount);
        Debug.Log($"{name} отримав {amount} шкоди. Залишилося HP: {currentHealth}");
        UpdateColor();

        if (currentHealth == 0)
            Destroy(gameObject);
    }

    private void UpdateColor()
    {
        if (targetRenderer == null)
            return;

        float health01 = (float)currentHealth / maxHealth;
        propertyBlock.SetColor(BaseColorId, Color.Lerp(hitColor, fullColor, health01));
        targetRenderer.SetPropertyBlock(propertyBlock);
    }
}
