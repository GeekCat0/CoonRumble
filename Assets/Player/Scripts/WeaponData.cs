using UnityEngine;

[CreateAssetMenu(fileName = "weaponData", menuName = "ScriptableObjects/weaponData")]
public class WeaponData : ScriptableObject
{
    [Header("Components")]
    [field: SerializeField] public GameObject bullet { get; private set; }

    [Header("Weapon Stats")]
    [field: SerializeField] public float shootForce { get; private set; }
    [field: SerializeField] public float upwardForce { get; private set; }
    [field: SerializeField] public float timeBetweenShooting { get; private set; }
    [field: SerializeField] public float spread { get; private set; }
    [field: SerializeField] public float timeBetweenShots { get; private set; }

    [field: SerializeField] public bool fullAuto { get; private set; }
    [field: SerializeField] public bool adaptiveAim { get; private set; }

    [field: SerializeField] public int magazineSize { get; private set; }
    [field: SerializeField] public int bulletsPerTap { get; private set; }
}
