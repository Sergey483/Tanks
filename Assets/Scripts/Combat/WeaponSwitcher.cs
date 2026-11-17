using UnityEngine;

// Перемикає конфіги зброї гравця клавішами 1, 2, 3.
[RequireComponent(typeof(TankShooter))]
public class WeaponSwitcher : MonoBehaviour
{
    [SerializeField] private TankConfig[] weapons;

    private TankShooter shooter;

    private void Awake()
    {
        shooter = GetComponent<TankShooter>();
    }

    private void Update()
    {
        for (int i = 0; i < weapons.Length && i < 9; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                Select(i);
        }
    }

    private void Select(int index)
    {
        TankConfig weapon = weapons[index];
        if (weapon == null || weapon == shooter.Config)
            return;

        shooter.SetConfig(weapon);
        Debug.Log($"Зброя: {weapon.weaponName} (damage {weapon.damage}, cooldown {weapon.fireCooldown})");
    }
}
