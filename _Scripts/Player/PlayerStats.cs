using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class PlayerStats : MonoBehaviour
{
    private PlayerStatsVariables playerStatsVariables;
    private PlayerMovement playerMovement;
    private PlayerSounds playerSounds;

    [HideInInspector] public bool isExhausted;

    //Health
    public float playerHealthMax;
    [HideInInspector] public float playerHealth;

    //Stamina
    public float playerStaminaMax;
    [HideInInspector] public float playerStamina;
    [SerializeField] private float playerStaminaRegenRate;
    [SerializeField] private float playerStaminaDecreaseRate;
    [SerializeField] private float playerStaminaJumpCost;
    private float playerStaminaRegenTimer;
    [SerializeField] private float playerStaminaRegenTimerThreshold;
    [SerializeField] private float playerStaminaRestThreshold;
    [HideInInspector] public bool jumpCost;

    //Hunger
    public float playerHungerMax;
    [HideInInspector] public float playerHunger;

    //Thirst
    public float playerThirstMax;
    [HideInInspector] public float playerThirst;

    //Radiation
    public float playerRadiationMax;
    [HideInInspector] public float playerRadiation;

    private void Start()
    {
        playerMovement = GetComponent<PlayerMovement>();
        playerSounds = GetComponent<PlayerSounds>();
        InitializeStats();
    }

    private void Update()
    {
        SprintStamina();
        JumpStamina();
        RegenerateStamina();
    }

    #region function & methods
    private void SaveStats()
    {
        playerStatsVariables = new PlayerStatsVariables
        {
            savedPlayerHealth = playerHealth,
            savedPlayerStamina = playerStamina,
            savedPlayerHunger = playerHunger,
            savedPlayerThirst = playerThirst,
            savedPlayerRadiation = playerRadiation
        };
        
        string jsonData = JsonUtility.ToJson(playerStatsVariables);
        File.WriteAllText(Application.persistentDataPath + "/PlayerStats.txt", jsonData);
    }

    private void LoadStats()
    {
        playerStatsVariables = new PlayerStatsVariables();
        playerStatsVariables = JsonUtility.FromJson<PlayerStatsVariables>(File.ReadAllText(Application.persistentDataPath + "/PlayerStats.txt"));

        playerHealth = playerStatsVariables.savedPlayerHealth;
        playerStamina = playerStatsVariables.savedPlayerStamina;
        playerHunger = playerStatsVariables.savedPlayerHunger;
        playerThirst = playerStatsVariables.savedPlayerThirst;
        playerRadiation = playerStatsVariables.savedPlayerRadiation;
    }

    private void InitializeStats() //Save, Load sisteminde değişecek
    {
        playerHealth = playerHealthMax ;
        playerStamina = playerStaminaMax;
        playerHunger = playerHungerMax;
        playerThirst = playerThirstMax;
        playerRadiation = 0;
    }

    private void SprintStamina()
    {
        if (playerMovement.isSprinting && playerStamina > 0 && !playerMovement.isSprintJumped && !playerMovement.isFalling)
        {
            playerStamina = Mathf.Clamp(playerStamina - (playerStaminaDecreaseRate * Time.deltaTime), 0, playerStaminaMax);
            playerStaminaRegenTimer = 0;

            if (playerStamina <= 0)
            {
                isExhausted = true;
                playerSounds.Breathing();
            }
        }
    }

    private void RegenerateStamina()
    {
        if (playerStaminaRegenTimer > playerStaminaRegenTimerThreshold)
        {
            playerStamina = Mathf.Clamp(playerStamina + (playerStaminaRegenRate * Time.deltaTime), 0, playerStaminaMax);

            if (playerStamina >= playerStaminaRestThreshold)
                isExhausted = false;
        }
        else
        {
            playerStaminaRegenTimer += Time.deltaTime;
        }
    }

    public void JumpStamina()
    {
        if ((playerMovement.isJumped || playerMovement.isSprintJumped) && !playerMovement.IsGrounded() && jumpCost)
        {
            playerStamina = Mathf.Clamp(playerStamina - playerStaminaJumpCost, 0, playerStaminaMax);
            playerStaminaRegenTimer = 0;
            jumpCost = false;

            if (playerStamina > 0)
                playerSounds.Jump();

            if (playerStamina <= 0)
            {
                isExhausted = true;
                playerSounds.Breathing();
            }
        }
    }
    #endregion
}
