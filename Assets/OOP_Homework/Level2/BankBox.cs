using UnityEngine;

public class BankBox : MonoBehaviour
{
    // Читати баланс може будь-хто, а змінити — лише сам BankBox
    public int Gold { get; private set; } = 100;

    public void AddGold(int amount)
    {
        if (amount <= 0)
        {
            Debug.LogWarning("Додати можна лише додатну кількість золота.");
            return;
        }

        Gold += amount;
        Debug.Log($"Додано {amount} золота. Баланс: {Gold}");
    }
}
