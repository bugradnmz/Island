using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public Text healthText;
    public Text staminaText;
    public Text hungerText;
    public Text thirstText;
    public Text radiationText;

    public Slider healthSlider;
    public Slider staminaSlider;
    public Slider hungerSlider;
    public Slider thirstSlider;
    public Slider radiationSlider;

    public Image crouchImage;

    private PlayerStats playerStats;
    private PlayerMovement playerMovement;

    private void Start()
    {
        playerStats = GameObject.Find("Player").gameObject.GetComponent<PlayerStats>();
        playerMovement = GameObject.Find("Player").gameObject.GetComponent<PlayerMovement>();

        LockCursor();
        InitializeStatsUI();
    }

    private void Update()
    {
        SyncStatsUI();
        CrouchImage();
    }


    #region function & methods
    private void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void InitializeStatsUI()
    {
        healthSlider.maxValue = playerStats.playerHealthMax;
        staminaSlider.maxValue = playerStats.playerStaminaMax;
        hungerSlider.maxValue = playerStats.playerHungerMax;
        thirstSlider.maxValue = playerStats.playerThirstMax;
        radiationSlider.maxValue = playerStats.playerRadiationMax;
    }

    private void SyncStatsUI()
    {
        healthSlider.value = playerStats.playerHealth;
        staminaSlider.value = playerStats.playerStamina;
        hungerSlider.value = playerStats.playerHunger;
        thirstSlider.value = playerStats.playerThirst;
        radiationSlider.value = playerStats.playerRadiation;

        healthText.text = Mathf.Ceil(Mathf.Clamp((playerStats.playerHealth),0,playerStats.playerHealthMax)).ToString();
        staminaText.text = Mathf.Ceil(Mathf.Clamp((playerStats.playerStamina), 0, playerStats.playerStaminaMax)).ToString();
        hungerText.text = Mathf.Ceil(Mathf.Clamp((playerStats.playerHunger), 0, playerStats.playerHungerMax)).ToString();
        thirstText.text = Mathf.Ceil(Mathf.Clamp((playerStats.playerThirst), 0, playerStats.playerThirstMax)).ToString();
        radiationText.text = Mathf.Ceil(Mathf.Clamp((playerStats.playerRadiation), 0, playerStats.playerRadiationMax)).ToString();
    }

    private void CrouchImage()
    {
        if (playerMovement.isCrouched)
        {
            crouchImage.enabled = true;
        }
        else
        {
            crouchImage.enabled = false;
        }
    }
    #endregion
}
