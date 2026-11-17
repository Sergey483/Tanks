using UnityEngine;

public class Troll : Enemy
{
    [SerializeField] private int regenerationPerSecond = 5;

    private float regenerationTimer;

    protected override void Update()
    {
        // Рух за логікою батька + власна регенерація
        base.Update();
        Regenerate();
    }

    public void Regenerate()
    {
        if (Health <= 0 || Health >= maxHealth)
            return;

        regenerationTimer += Time.deltaTime;
        if (regenerationTimer < 1f)
            return;

        regenerationTimer -= 1f;
        Health = Mathf.Min(maxHealth, Health + regenerationPerSecond);
        Debug.Log($"{gameObject.name} регенерує: +{regenerationPerSecond} ХП. Зараз: {Health}");
    }
}
