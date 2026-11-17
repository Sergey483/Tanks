using UnityEngine;

public class EnemyTest : MonoBehaviour
{
    [SerializeField] private Enemy[] enemies;
    [SerializeField] private int damage = 40;

    private void Start()
    {
        foreach (Enemy enemy in enemies)
        {
            if (enemy == null)
                continue;

            // enemy.Health = 9999; — не скомпілюється через protected set
            enemy.TakeDamage(damage);
        }
    }
}
