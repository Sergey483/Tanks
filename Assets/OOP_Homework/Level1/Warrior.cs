using UnityEngine;

public class Warrior : BaseCharacter
{
    [SerializeField] private int attackPower = 20;

    private void Start()
    {
        Attack();
    }

    public void Attack()
    {
        Debug.Log($"{characterName} атакує мечем і завдає {attackPower} шкоди! Здоров'я воїна: {Health}");
    }
}
