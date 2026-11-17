using UnityEngine;

public class BaseCharacter : MonoBehaviour
{
    // Здоров'я можна читати звідусіль, а змінювати — тільки цей клас і його нащадки
    public int Health { get; protected set; } = 100;

    public string characterName = "Герой";
}
