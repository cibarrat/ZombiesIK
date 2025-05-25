using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using StarterAssets;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine.XR;

public class ThirdPersonShooterController : MonoBehaviour
{
    [SerializeField] private CinemachineVirtualCamera aimVirtualCamera;
    [SerializeField] private CinemachineVirtualCamera followVirtualCamera;
    [SerializeField] private float normalSensitivity;
    [SerializeField] private float aimSensitivity;
    [SerializeField] private GameObject crosshair;
    [SerializeField] private GameObject obstacleCrosshair;
    [SerializeField] private LayerMask aimColliderLayerMask = new LayerMask();
    [SerializeField] private Transform spawnBulletPosition;
    [SerializeField] private GameObject bulletHolePrefab;
    [SerializeField] private AudioClip gunFire;
    [SerializeField] private AudioClip gunClickSound;
    [SerializeField] private AudioClip gunReloadSound;
    [SerializeField] private GameObject bloodSplatter;
    [SerializeField] private Transform gunRestPosition;
    public int LoadedAmmo { get; private set; }
    public float focusTime = 0.5f;
    public float shootRate = 0.5f;
    public float unfocusedCrosshairRadius = 0.5f;
    public float focusedCrosshairRadius = 0.1f;
    public float shootDamage = 15;
    public float reloadTime = 3;
    public float maxIKweight = 0.5f;

    private StarterAssetsInputs starterAssetsInputs;
    private ThirdPersonController thirdPersonController;
    private Animator animator;
    private PlayerStats stats;
    private bool crosshairFocused = false;
    private bool currentSide = true;
    private float shootRateTimeout = 0f;
    private bool gunClick = false;
    private bool isReloading = false;
    public bool CanAim { get; set; } = true;

    private Coroutine FocusCoroutine = null;
    private Coroutine ReloadCoroutine = null;
    private AudioSource audioSource;
    private float ikWeight;

    private void Awake()
    {
        starterAssetsInputs = GetComponent<StarterAssetsInputs>(); 
        thirdPersonController = GetComponent<ThirdPersonController>();
        animator = GetComponent<Animator>();
        stats = GetComponent<PlayerStats>();
        LoadedAmmo = stats.AmmoCapacity;
        audioSource = GetComponent<AudioSource>();
        ikWeight = maxIKweight;
    }

    private void Update()
    {
        SwitchAimSide();
        Aim();
        if (starterAssetsInputs.reload && LoadedAmmo < stats.AmmoCapacity && ReloadCoroutine == null)
        {
            ReloadCoroutine = StartCoroutine(Reload());
        }
    }

    private void Aim()
    {
        Vector2 screenCenterPoint = new Vector2(Screen.width / 2f, Screen.height / 2f);
        if (CanAim && starterAssetsInputs.aim)
        {
            thirdPersonController.SetCanRun(false);
            if (shootRateTimeout > 0f)
            {
                shootRateTimeout -= Time.deltaTime;
            }
            aimVirtualCamera.gameObject.SetActive(true);
            crosshair.SetActive(true);
            animator.SetBool("isAiming", true);
            //thirdPersonController.SetRotateOnMove(false);
            //thirdPersonController.SetSensitivity(aimSensitivity);
            if (!isReloading)
            {
                //animator.SetLayerWeight(1, Mathf.Lerp(animator.GetLayerWeight(1), 1f, Time.deltaTime * 10f));//set aim layer  to weight 1 aim
            }
            if (!crosshairFocused && starterAssetsInputs.move == Vector2.zero && FocusCoroutine == null)
            {
                FocusCoroutine = StartCoroutine(FocusCrosshair(focusTime));
            }
            else if (crosshairFocused && starterAssetsInputs.move != Vector2.zero)
            {
                UnfocusCrosshair();
            }
            // Sub Crosshair Aim
            GenerateSubCrosshair();
            float shootArea = crosshairFocused ? focusedCrosshairRadius : unfocusedCrosshairRadius;
            Vector2 shotVariation = new Vector2(Random.Range(-shootArea, shootArea), Random.Range(-shootArea, shootArea));

            // Aim point
            Ray ray = Camera.main.ScreenPointToRay(screenCenterPoint + shotVariation);
            if (Physics.Raycast(ray, out RaycastHit raycastHit, 999f, aimColliderLayerMask))
            {
                Vector3 mouseWorldPosition = raycastHit.point;
                Vector3 worldAimTarget = mouseWorldPosition;
                worldAimTarget.y = transform.position.y;
                Vector3 aimDirection = (worldAimTarget - transform.position).normalized;

                transform.forward = Vector3.Lerp(transform.forward, aimDirection, Time.deltaTime * 20f);

                animator.GetBoneTransform(HumanBodyBones.Spine).LookAt(aimDirection);
                Vector3 shootAimDirection = (mouseWorldPosition - spawnBulletPosition.position).normalized;
                // Shoot
                Shoot(shootAimDirection);
            }
        }
        else
        {
            Ray ray = Camera.main.ScreenPointToRay(screenCenterPoint);
            if (Physics.Raycast(ray, out RaycastHit raycastHit, 999f, aimColliderLayerMask))
            {
                animator.GetBoneTransform(HumanBodyBones.Neck).forward = Vector3.Lerp(animator.GetBoneTransform(HumanBodyBones.Neck).forward, raycastHit.point, Time.deltaTime * 20f);
            }
            animator.SetBool("isAiming", false);
            thirdPersonController.SetCanRun(true);
            //thirdPersonController.SetRotateOnMove(true);
            aimVirtualCamera.gameObject.SetActive(false);
            crosshair.SetActive(false);
            obstacleCrosshair.SetActive(false);
            //thirdPersonController.SetSensitivity(normalSensitivity);
            //animator.SetLayerWeight(1, Mathf.Lerp(animator.GetLayerWeight(1), 0f, Time.deltaTime * 10f)); //set aim layer to 0 - idle
            animator.GetBoneTransform(HumanBodyBones.Spine).rotation = Quaternion.identity;
            UnfocusCrosshair();
        }
        if (starterAssetsInputs.move != Vector2.zero && FocusCoroutine != null)
        {
            UnfocusCrosshair();
        }
    }
    private void Shoot(Vector3 shootAimDirection)
    {
        if (starterAssetsInputs.shoot)
        {
            if (LoadedAmmo > 0 && !isReloading)
            {
                if (shootRateTimeout <= 0f && Physics.Raycast(spawnBulletPosition.position, shootAimDirection, out RaycastHit hitInfo, 999f, aimColliderLayerMask))
                {
                    animator.SetTrigger("isRecoil");//Pistol Recoil animation
                    AudioSource.PlayClipAtPoint(gunFire, spawnBulletPosition.position);
                    shootRateTimeout = shootRate;
                    if (hitInfo.collider.gameObject.CompareTag("Enemy"))
                    {
                        float damage = shootDamage * Random.Range(1, 1.3f);
                        if (crosshairFocused)
                        {
                            damage *= Random.Range(1.1f, 1.5f);
                        }
                        Instantiate(bloodSplatter, hitInfo.point, Quaternion.identity);
                        hitInfo.collider.gameObject.GetComponentInParent<ZombieController>().Damage(damage, hitInfo.collider);
                    }
                    else
                    {
                        GameObject bulletHole = Instantiate(bulletHolePrefab, hitInfo.point + hitInfo.normal * 0.01f, Quaternion.identity);
                        bulletHole.transform.rotation = Quaternion.LookRotation(hitInfo.normal);
                        bulletHole.transform.SetParent(hitInfo.collider.transform);
                        GameObject.Destroy(bulletHole, 5);
                    }
                    UnfocusCrosshair();
                    LoadedAmmo--;
                }
            }
            else if (shootRateTimeout <= 0f && !isReloading)
            {
                shootRateTimeout = shootRate;
                if (gunClick && ReloadCoroutine == null && stats.Ammo > 0)
                {
                    ReloadCoroutine = StartCoroutine(Reload());
                }
                else
                {
                    AudioSource.PlayClipAtPoint(gunClickSound, spawnBulletPosition.transform.position);
                    gunClick = true;
                }
            }
        }
        
    }

    private IEnumerator Reload()
    {
        
        if (stats.Ammo > 0)
        {
            audioSource.PlayOneShot(gunReloadSound);
            //animator.SetLayerWeight(1, Mathf.Lerp(animator.GetLayerWeight(1), 0f, Time.deltaTime * 10f));//set aim layer 1
            animator.SetTrigger("IsReload");//set Bool True <---AQUI ESTA REVISANDO SI AMMO O NO
            isReloading = true;
            yield return new WaitForSeconds(reloadTime);
            LoadedAmmo = stats.ReloadAmmo(LoadedAmmo);
            gunClick = false;
        }
        isReloading = false;
        // animator.SetTrigger("IsReload", false); //set Bool False
       // animator.SetLayerWeight(2, Mathf.Lerp(animator.GetLayerWeight(2), 0f, Time.deltaTime * 10f));//set aim layer 1
        ReloadCoroutine = null;
    }

    private IEnumerator FocusCrosshair(float time)
    {
        yield return new WaitForSeconds(time);
        if (starterAssetsInputs.aim && !crosshairFocused && starterAssetsInputs.move == Vector2.zero)
        {
            Transform[] crosshairs = crosshair.GetComponentsInChildren<Transform>();
            crosshairFocused = true;
            for (int i = 0; i < crosshairs.Length; i++)
            {
                crosshairs[i].localPosition *= 0.5f; 
            }
        } else
        {
            FocusCoroutine = null;
        }
    }

    private void GenerateSubCrosshair()
    {
        Vector2 screenCenterPoint = new Vector2(Screen.width / 2f, Screen.height / 2f);
        Ray ray = Camera.main.ScreenPointToRay(screenCenterPoint);
        if (Physics.Raycast(ray, out RaycastHit staticRaycastHit, 999f, aimColliderLayerMask))
        {
            Vector3 shootAimDirection = (staticRaycastHit.point - spawnBulletPosition.position).normalized;
            if (Physics.Raycast(spawnBulletPosition.position, shootAimDirection, out RaycastHit hitInfo, 999f, aimColliderLayerMask))
            {
                Vector3 screenPosition = Camera.main.WorldToScreenPoint(hitInfo.point);
                RectTransform obstacleTransform = obstacleCrosshair.GetComponent<RectTransform>();
                obstacleTransform.anchoredPosition = new Vector2(
                        screenPosition.x - (Screen.width / 2),
                        screenPosition.y - (Screen.height / 2)
                    );
                if (Mathf.Abs(obstacleTransform.anchoredPosition.x) > 30 && Mathf.Abs(obstacleTransform.anchoredPosition.y) > 30)
                {
                    obstacleCrosshair.SetActive(true);
                }
                else
                {
                    obstacleCrosshair.SetActive(false);
                }
            }
        }
    }
    private void UnfocusCrosshair()
    {
        if (FocusCoroutine != null)
        {
            StopCoroutine(FocusCoroutine);
            FocusCoroutine = null;
        }
        if (crosshairFocused)
        {
            Transform[] crosshairs = crosshair.GetComponentsInChildren<Transform>();
            crosshairFocused = false;
            for (int i = 0; i < crosshairs.Length; i++)
            {
                crosshairs[i].localPosition *= 2f;
            }
        }
    }

    private void SwitchAimSide()
    {
        if (starterAssetsInputs.switchSides)
        {
            if (currentSide)
            {
                bool cameraSide = aimVirtualCamera.GetCinemachineComponent<Cinemachine3rdPersonFollow>().CameraSide == 1;
                Cinemachine3rdPersonFollow aimCameraFollow = aimVirtualCamera.GetCinemachineComponent<Cinemachine3rdPersonFollow>();
                aimCameraFollow.CameraSide = !cameraSide ? 1 : 0;
                Cinemachine3rdPersonFollow followCameraFollow = followVirtualCamera.GetCinemachineComponent<Cinemachine3rdPersonFollow>();
                followCameraFollow.CameraSide = !cameraSide ? 1 : 0;
                spawnBulletPosition.localPosition = new Vector3(spawnBulletPosition.localPosition.x * -1, spawnBulletPosition.localPosition.y, spawnBulletPosition.localPosition.z);
                currentSide = false;
            }
        } else
        {
            currentSide = true;
        }
    }

    public void InterruptReload()
    {
        if (ReloadCoroutine != null)
        {
            StopCoroutine(ReloadCoroutine);
            audioSource.Stop();
            isReloading = false;
            ReloadCoroutine = null;
        }
    }

    public void InterruptAimFocus()
    {
        if (FocusCoroutine != null)
        {
            StopCoroutine(FocusCoroutine);
            crosshairFocused = false;
            FocusCoroutine = null;
        }
    }

    private void OnAnimatorIK(int layerIndex)
    {
        if (animator.GetBool("isAiming") && ikWeight > 0)
        {
            ikWeight = ikWeight < 0? 0 : ikWeight - 2 * Time.deltaTime;
        } else if (ikWeight < maxIKweight)
        {
            ikWeight = ikWeight > maxIKweight? 0.8f: ikWeight + 2 * Time.deltaTime;
        }
        animator.SetIKPosition(AvatarIKGoal.RightHand, gunRestPosition.position + gunRestPosition.right * .05f);
        animator.SetIKPositionWeight(AvatarIKGoal.RightHand, ikWeight);
        animator.SetIKPosition(AvatarIKGoal.LeftHand, gunRestPosition.position - gunRestPosition.right * .05f);
        animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, ikWeight);
    }
}
