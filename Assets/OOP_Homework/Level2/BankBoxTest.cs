using UnityEngine;

public class BankBoxTest : MonoBehaviour
{
    [SerializeField] private BankBox bankBox;

    private void Start()
    {
        if (bankBox == null)
            return;

        // ❌ Пряма зміна не компілюється:
        // bankBox.Gold = 1000;
        // error CS0272: The property or indexer 'BankBox.Gold' cannot be used in this context
        // because the set accessor is inaccessible

        // ✅ Безпечне додавання через метод
        bankBox.AddGold(900);
        bankBox.AddGold(-50);

        Debug.Log($"Фінальний баланс: {bankBox.Gold}");
    }
}
