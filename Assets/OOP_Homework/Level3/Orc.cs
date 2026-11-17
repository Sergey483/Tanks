using UnityEngine;

public class Orc : Enemy
{
    [SerializeField] private int axeDamage = 25;

    protected override void Start()
    {
        base.Start();
        Attack();
    }

    public void Attack()
    {
        Debug.Log($"{gameObject.name} б'є сокирою на {axeDamage} шкоди!");
    }
}
