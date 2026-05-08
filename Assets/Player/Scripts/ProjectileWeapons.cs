using UnityEngine;

public class ProjectileWeapons : MonoBehaviour
{
    [SerializeField] private WeaponData weaponData;

    [Header("Components")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Transform attackPoint;

    private int bulletsLeft = 1;
    private int bulletsShot = 0;

    private bool shooting = false;
    private bool readyToShoot = true;

    private PlayerState playerState;
    private PlayerActionsInput playerActionsInput;
    
    private void Awake()
    {
        playerState = GetComponent<PlayerState>();
        playerActionsInput = GetComponent<PlayerActionsInput>();
        bulletsLeft = weaponData.magazineSize;
    }

    private void Update()
    {
        HandleShooting();
    }
    private void HandleShooting() // Logic for choosing if the player can shoot or not
    {
        if (weaponData.fullAuto)
             shooting = playerActionsInput.AttackHeld > 0;

        if (!weaponData.fullAuto)
                shooting = playerActionsInput.AttackPressed;

        if (readyToShoot && shooting && bulletsLeft > 0 && playerState.CurrentPlayerActionState != PlayerActionState.Dashing)
        {
            bulletsShot = 0;
            Shoot();
        }
    }

    private void Shoot()
    {
        readyToShoot = false;

        // Find a point going straight from the middle of the screen to know a location we want to hit
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        // Check if ray hit anything
        Vector3 targetPoint;
        if (Physics.Raycast(ray, out hit) && weaponData.adaptiveAim)
            targetPoint = hit.point;
        else
            targetPoint = ray.GetPoint(50);

        // Calculate direction from attackPoint to targetPoint
        Vector3 directionWithoutSpread = targetPoint - attackPoint.position;

        // Add spread
        float x = Random.Range(-weaponData.spread, weaponData.spread);
        float y = Random.Range(-weaponData.spread, weaponData.spread);
        Vector3 directionWithSpread = directionWithoutSpread + new Vector3(x, y, 0);

        // Instantiate projectile
        GameObject currentBullet = Instantiate(weaponData.bullet, attackPoint.position, Quaternion.identity);

        // Rotate towards the shoot direction
        currentBullet.transform.forward = directionWithSpread.normalized;

        // Add forces to bullet
        currentBullet.GetComponent<Rigidbody>().AddForce(directionWithSpread.normalized * weaponData.shootForce, ForceMode.Impulse);
        currentBullet.GetComponent<Rigidbody>().AddForce(playerCamera.transform.up * weaponData.upwardForce, ForceMode.Impulse);

        bulletsLeft--;
        bulletsShot++;

        Invoke(nameof(ResetShot), weaponData.timeBetweenShooting);

        if (bulletsShot < weaponData.bulletsPerTap && bulletsLeft > 0)
            Invoke(nameof(Shoot), weaponData.timeBetweenShots);

    }
    private void ResetShot()
    {
        readyToShoot = true;
    }
    public void SetWeapon(WeaponData data)
    {
        weaponData = data;
    }
}
