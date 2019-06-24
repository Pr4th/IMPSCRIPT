using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityStandardAssets.Characters.FirstPerson;

public enum FireMod {
    SemiAuto,
    FullAuto
}

public class WeaponBase : MonoBehaviour {

    protected Animator animator;

    protected AudioSource audioSource;
    protected FirstPersonController controller;
    protected bool fireLock = false;
    protected bool canShoot = false;

    protected bool isReloading = false;

    [Header("Object References")]
    public ParticleSystem muzzleflash;

    public Transform shootPoint;
    public GameObject sparkPrefab;

    public GameObject bloodPrefab;

    [Header("UI Reference")]
    public Text weaponNameText;
    public Text ammoText;

    [Header("Sound References")]

    public AudioClip fireSound;
    public AudioClip dryFireSound;

    public AudioClip drawSound;
    public AudioClip MagOutSound;
    public AudioClip MagInSound;
    public  AudioClip boltSound;

    

    [Header("Weapon Attributes")]
    public float damage = 20f;

    public FireMod fireMode = FireMod.SemiAuto;

    public float fireRate = 1.0f;

    public int bulletsInClip;
    public int clipSize = 12;

    public int bulletsLeft;
    public int maxAmmo = 100;

    public float spread = 0.7f;
    public float recoil = 0.5f;


    void Start() {
        controller = GameObject.Find("Player").GetComponent<FirstPersonController>();
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();

        bulletsInClip = clipSize;
        bulletsLeft = maxAmmo;

        Invoke("EnableWeapon", 1f);
        UpdateTexts();
    }

    public void UpdateTexts() {
        weaponNameText.text = GetWeaponName();
        ammoText.text = "Ammo: " + bulletsInClip + " / " + bulletsLeft;
    }

    string GetWeaponName() {
        string weaponName = "";

        if(this is Police9mm) {
            weaponName = "Police 9mm";
        }
        else if(this is Magnum) {
            weaponName = "Revolver";
        }
        else if(this is Compact9mm) {
            weaponName = "MP5";
        }

        else {
            throw new System.Exception("Unknown Weapon");
        }

        return weaponName;
    }

    void EnableWeapon() {
        canShoot = true;
    }

    void Update() {
        if(fireMode == FireMod.FullAuto && Input.GetButtonDown("Fire1")) {
            CheckFire();
        }
        else if(fireMode == FireMod.SemiAuto && Input.GetButtonDown("Fire1")) {
            CheckFire();
        }

        if (Input.GetButtonDown("Reload")) {
            CheckReload();
        }
    }


    void CheckFire () {
        if (!canShoot) return;
        if (isReloading) return;
        if (fireLock) return;


        if(bulletsInClip > 0) {
            Fire();
        }

        else {
            DryFire();
        }
    }

    void Fire() {
        audioSource.PlayOneShot(fireSound);
        fireLock = true;

        DetectHit();

        Recoil();

        muzzleflash.Stop();
        muzzleflash.Play();

        PlayFireAnimation();

        bulletsInClip--;
        UpdateTexts();

        StartCoroutine(CoResetFireLock());
    }
    public void CreateBlood(Vector3 pos, Quaternion rot) {
        GameObject blood = Instantiate(bloodPrefab, pos, rot);
        Destroy(blood, 1f);
    }

    public virtual void PlayFireAnimation() {
        animator.CrossFadeInFixedTime("Fire", 0.1f);
    }

    void Recoil() {
        controller.mouseLook.Recoil(recoil);
    }

    void DetectHit() {
        RaycastHit hit;

        if(Physics.Raycast(shootPoint.position, CalculateSpread(spread, shootPoint), out hit)) {
            if (hit.transform.CompareTag("Enemy")) {
                Health health = hit.transform.GetComponent<Health>();

                if (health == null) {
                    throw new System.Exception("Cannot found Health Component on Enemy");
                }

                else {
                    health.TakeDamage(damage);
                    CreateBlood(hit.point, hit.transform.rotation);
                }
            }
            else {
                GameObject spark = Instantiate(sparkPrefab, hit.point, hit.transform.rotation);
                Destroy(spark, 1);
            }
        }
    }

    Vector3 CalculateSpread(float spread, Transform shootPoint) {
        return Vector3.Lerp(shootPoint.TransformDirection(Vector3.forward * 100), Random.onUnitSphere, spread);
    }


    protected virtual void ReloadAmmo()
    {
        int bulletsToLoad = clipSize - bulletsInClip;
        int bulletsToSub = (bulletsLeft >= bulletsToLoad) ? bulletsToLoad : bulletsLeft;

        bulletsLeft -= bulletsToSub;
        bulletsInClip += bulletsToSub;  

    }

    void DryFire() {
        audioSource.PlayOneShot(dryFireSound);
        fireLock = true;

        StartCoroutine(CoResetFireLock());
    }

    IEnumerator CoResetFireLock() {
        yield return new WaitForSeconds(fireRate);
        fireLock = false;
    }

    void CheckReload() {
        if (bulletsLeft > 0 && bulletsInClip < clipSize) {
            Reload();
        }
    }

    void Reload() {
        if (isReloading) return;

        isReloading = true;
        animator.CrossFadeInFixedTime("Reload", 0.1f);
    }

    void ReloadaAmmo() {
        int bulletsToLoad = clipSize - bulletsInClip;
        int bulletsToSub = (bulletsLeft >= bulletsToLoad) ? bulletsToLoad : bulletsLeft;

        bulletsLeft -= bulletsToSub;
        bulletsInClip += bulletsToLoad;

        UpdateTexts();
    }

    public void OnDraw() {
        audioSource.PlayOneShot(drawSound);
    }

    public void OnMagOut() {
        audioSource.PlayOneShot(MagOutSound);
    }

    public void OnMagIn() {
        ReloadAmmo();
        audioSource.PlayOneShot(MagInSound);
    }

    public void OnBoltForwarded() {
        audioSource.PlayOneShot(boltSound);
        Invoke("ResetIsReloading", 1f);
    }

    void ResetIsReloading() {
        isReloading = false;
    }
}
