using UnityEngine;

// Показує на екрані здоров'я ворогів і баланс BankBox.
public class OopDebugOverlay : MonoBehaviour
{
    [SerializeField] private Enemy[] enemies;
    [SerializeField] private BankBox bankBox;

    private GUIStyle style;

    private void OnGUI()
    {
        if (style == null)
            style = new GUIStyle(GUI.skin.label) { fontSize = 22 };

        float y = 15f;
        GUI.Label(new Rect(15f, y, 600f, 30f), "WASD — рух гравця", style);

        foreach (Enemy enemy in enemies)
        {
            if (enemy == null)
                continue;

            y += 30f;
            GUI.Label(new Rect(15f, y, 600f, 30f), $"{enemy.name} ({enemy.GetType().Name}): {enemy.Health} HP", style);
        }

        if (bankBox != null)
        {
            y += 30f;
            GUI.Label(new Rect(15f, y, 600f, 30f), $"BankBox: {bankBox.Gold} золота", style);
        }
    }
}
