using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TakeDamage : MonoBehaviour
{
    private Vector3 oldPosition;
    private Vector3 currentPosition;

    [SerializeField] private float fallDamagePosDifThreshold;
    [SerializeField] private float fallDamageVelocityThreshold;
    [SerializeField] private float fallDistanceLowThreshold;
    [SerializeField] private float fallDistanceMediumThreshold;
    [SerializeField] private float fallDistanceHighThreshold;
    [SerializeField] private float slowDuration;
    [SerializeField] private float cameraShakeValue;
    [SerializeField] private float damageEffectCD;
    [SerializeField] private float bloodAmount;

    private float positionDifferenceOnY;
    private float maxVelocity;
    private float oldPlayerHealth;
    private float healthChange;
    private float slowTimer;
    private float damageEffectTimer;

    private bool playDamageEffects;

    [HideInInspector] public bool isFallDamageTaken;
    [HideInInspector] public bool isDamageTaken;    
    [HideInInspector] public bool isSlowed;    

    private PlayerMovement playerMovement;
    private PlayerStats playerStats;
    private PlayerSounds playerSounds;

    private void Start()
    {
        playerMovement = GetComponent<PlayerMovement>();
        playerStats = GetComponent<PlayerStats>();
        playerSounds = GetComponent<PlayerSounds>();

        isFallDamageTaken = true;
        playDamageEffects = true;
        oldPlayerHealth = playerStats.playerHealth;
    }

    private void Update()
    {
        TakeFallDamage();
        CheckTakenDamage();
        SlowTimer();
        DamageEffectTimer();

        if (Input.GetKey(KeyCode.E)) //Simulate Damage
        {
            playerStats.playerHealth -= 1;
        }
    }

    private void FixedUpdate()
    {
        CalculateMaxVelocity();
    }

    #region function & methods
    private void CheckTakenDamage()
    {
        if (playerStats.playerHealth != oldPlayerHealth)
        {
            healthChange = oldPlayerHealth - playerStats.playerHealth;
            oldPlayerHealth = playerStats.playerHealth;

            if (healthChange > 0)
            {   
                isDamageTaken = true;

                if (playerMovement.IsGrounded())
                {
                    isSlowed = true;
                    slowTimer = 0;
                }

                if (playDamageEffects)
                {
                    playerSounds.TakeDamage();
                    ShakeCamera();
                    BleedBehavior.BloodAmount = bloodAmount;

                    damageEffectTimer = 0;
                    playDamageEffects = false;
                }
            }
            else
            {
                isDamageTaken = false;                    
            }
        }
    }

    private void SlowTimer()
    {
        if (isSlowed)
        {
            if (slowTimer < slowDuration)
            {
                slowTimer += Time.deltaTime;
            }
            else
            {
                isSlowed = false;
            }
        }
    }

    private void DamageEffectTimer()
    {
        if (!playDamageEffects)
        {
            if (damageEffectTimer < damageEffectCD)
            {
                damageEffectTimer += Time.deltaTime;
            }
            else
            {
                playDamageEffects = true;
            }
        }
    }

    private void CalculateMaxVelocity()
    {
        if (playerMovement.isFalling)
        {
            if (-playerMovement.rb.velocity.y > maxVelocity)
                maxVelocity = playerMovement.rb.velocity.y;
        }
        else
        {
            maxVelocity = 0;
        }
    }

    private void TakeFallDamage()
    {
        currentPosition = transform.position;
        positionDifferenceOnY = oldPosition.y - currentPosition.y;        

        float fallDamage = -maxVelocity * positionDifferenceOnY;

        if (positionDifferenceOnY >= fallDamagePosDifThreshold &&
            maxVelocity <= -fallDamageVelocityThreshold &&
            !playerMovement.IsGrounded())
        {
            isFallDamageTaken = false;
        }

        if (!isFallDamageTaken && playerMovement.IsGrounded())
        {
            if (positionDifferenceOnY < fallDistanceLowThreshold)
                fallDamage = fallDamage * 0.5f;
            else if (positionDifferenceOnY < fallDistanceMediumThreshold)
                fallDamage = fallDamage * 1f;
            else if (positionDifferenceOnY < fallDistanceHighThreshold)
                fallDamage = fallDamage * 2;
            else
                fallDamage = fallDamage * 4;

            if (fallDamage >= 0.50f)
            {
                playerSounds.BoneCrush();                
                playerStats.playerHealth -= Mathf.Round(fallDamage);
                isFallDamageTaken = true;
            }
        }
    }

    private void ShakeCamera()
    {
        EZCameraShake.CameraShaker.Instance.ShakeOnce(cameraShakeValue, cameraShakeValue * 0.4f, slowDuration, slowDuration);
    }

    #endregion
    #region triggers
    private void OnCollisionStay(Collision collision)
    {
        if (!playerMovement.isFalling)
            oldPosition = currentPosition;
    }

    private void OnCollisionExit(Collision collision)
    {
        if (!playerMovement.isFalling)
            oldPosition = currentPosition;
    }
    #endregion
}
