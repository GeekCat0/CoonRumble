using System;
using TMPro;
using UnityEngine;

public class WeaponSelect : MonoBehaviour
{
    [SerializeField] private ProjectileWeapons weapons;
    [SerializeField] private WeaponData[] weaponsData;
    [SerializeField] private TextMeshProUGUI weaponText;
    private PlayerActionsInput actionsInput;

    private void Start()
    {
        actionsInput = GetComponent<PlayerActionsInput>();
    }
    private void Update()
    {
        SelectWeapon(actionsInput.WeaponNumber);
    }

    private void SelectWeapon(int weaponNumber)
    {
        weapons.SetWeapon(weaponsData[weaponNumber]);
        weaponText.text = weaponsData[weaponNumber].name.ToString();
    }
}
