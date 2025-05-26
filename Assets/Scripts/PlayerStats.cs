using StarterAssets;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Cinemachine;
using UnityEngine.SceneManagement;

public class PlayerStats : MonoBehaviour
{
    [SerializeField] private GameObject winScreen;
    [SerializeField] private GameObject loseScreen;
    [SerializeField] private float maxHP = 100;
    [field:SerializeField] public int AmmoCapacity { get; private set; } = 15;
    [SerializeField] private float hitstun = 2;
    [SerializeField] private TextMeshProUGUI ammoText;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private TextMeshProUGUI medkitsText;
    [SerializeField] private AudioClip playerHurt;
    [SerializeField] private AudioClip healEffect;
    [SerializeField] private GameObject canvas;
    private GameObject CamMovement;

    private CinemachineVirtualCamera virtualCamera;
    private Image gameoverMenu;
    private Image victoryMenu;

    private bool isInvincible = false;

    public float Medkits { get; private set; } = 0;
    public int Ammo { get; private set; } = 0;
    public float currentHP { get; private set; }
    private ThirdPersonShooterController tpsController;
    private ThirdPersonController tpController;
    private StarterAssetsInputs inputs;
    private Animator animator;
	//public PostProcessing postProcessingVolume;

    private bool healPressed = false;

    public float delayDeathTime = 0.3f;

    private void Awake()
    {
        tpsController = GetComponent<ThirdPersonShooterController>();
        tpController = GetComponent<ThirdPersonController>();
        currentHP = maxHP;
        inputs = GetComponent<StarterAssetsInputs>();
        Transform imageTransform = canvas.transform.Find("YouDiedMenu");
        Transform imageTransformVictory = canvas.transform.Find("VictoryMenu");
        if (imageTransform != null)
        {
            gameoverMenu = imageTransform.GetComponent<Image>();
        }
        if (imageTransformVictory != null)
        {
            victoryMenu = imageTransformVictory.GetComponent<Image>();
        }
    }
    private void Start()
    {
        Time.timeScale = 1f;

        gameoverMenu.gameObject.SetActive(false);
        victoryMenu.gameObject.SetActive(false);

        CamMovement = GameObject.Find("PlayerFollowCamera");
        virtualCamera = CamMovement.GetComponent<CinemachineVirtualCamera>();
        virtualCamera.enabled = true;
        animator = GetComponent<Animator>();
        inputs = GetComponent<StarterAssetsInputs>();
    }

    private void Update()
    {

        ammoText.text = $"Ammo: {tpsController.LoadedAmmo} | {Ammo}";
        healthText.text = $"Health: {currentHP}";
        medkitsText.text = $"Medkits: {Medkits}";
        if (inputs.heal)
        {
            if (!healPressed && Medkits > 0) {
                healPressed = true;
                Heal(100);
                Medkits--;
                Debug.Log("Healing");
            }
        } else
        {
            healPressed = false;
        }
    }

    public void Damage(float damage)
    {
        if (!isInvincible) 
        {
        	//postProcessingVolume.ActivateDamageVignette();
            tpsController.InterruptAimFocus();
            tpsController.InterruptReload();
            AudioSource.PlayClipAtPoint(playerHurt, transform.position);
            currentHP -= damage;
            if (currentHP <= 0)
            {
                animator.SetTrigger("IsDead");//Death Animation
                tpController.SetCanMove(false);
                tpsController.CanAim = false;
                isInvincible = true;
                GameOver();
            } else
            {
                StartCoroutine(Hitstun(hitstun));
                animator.SetTrigger("IsHit");//Hit Damage Animation
            }
        }
    }

    private IEnumerator Hitstun(float time)
    {
        isInvincible = true;
        tpController.SetCanMove(false);
        tpsController.CanAim = false;
        yield return new WaitForSeconds(time);
        tpController.SetCanMove(true);
        tpsController.CanAim = true;
        isInvincible = false;
    }

    public void Heal(float quantity)
    {
        //postProcessingVolume.ActivateHealVignette();
        AudioSource.PlayClipAtPoint(healEffect, transform.position);
        currentHP += quantity;
        if (currentHP > maxHP)
        {
            currentHP = maxHP;
        }
    }

    public void GameOver()
    {
        virtualCamera.enabled = false;
        StartCoroutine(DeathDelay(delayDeathTime));
    }

    private IEnumerator DeathDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        loseScreen.SetActive(true);
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Victory()
    {
        winScreen.SetActive(true);
        Time.timeScale = 0f;
        virtualCamera.enabled = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public int ReloadAmmo(int quantity)
    {
        int usedAmmo = AmmoCapacity - quantity;
        if (Ammo >= usedAmmo)
        {
            Ammo-=usedAmmo;
            return AmmoCapacity;
        } else
        {
            int newClip = Ammo;
            Ammo = 0;
            return newClip;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Ammo"))
        {
            Ammo += other.gameObject.GetComponent<ItemHandler>().Quantity;
            AudioSource.PlayClipAtPoint(other.gameObject.GetComponent<ItemHandler>().PickupSound, transform.position);
            GameObject.Destroy(other.gameObject);
        }
        if (other.gameObject.CompareTag("Medkit"))
        {
            Medkits += other.gameObject.GetComponent<ItemHandler>().Quantity;
            AudioSource.PlayClipAtPoint(other.gameObject.GetComponent<ItemHandler>().PickupSound, transform.position);
            GameObject.Destroy(other.gameObject);
        }
        if (other.gameObject.CompareTag("Goal"))
        {
            tpsController.CanAim = false;
            tpController.SetCanMove(false);
            Victory();
        }
    }
    public void ChangeScene()
    {

        Time.timeScale = 1f;
        Debug.Log("Changing scene to menu");
        SceneManager.LoadScene("Scenes/MainMenu");
        gameoverMenu.gameObject.SetActive(false);

    }
    public void ReloadLevel()
    {

        Time.timeScale = 1.0f;

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);

    }
}