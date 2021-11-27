using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAnimations : MonoBehaviour
{
    private PlayerMovement playerMovement; //playerMovementScript
    private Animator playerAnimator;

    private void Start()
    {
        playerMovement = GetComponent<PlayerMovement>();
        playerAnimator = GetComponent<Animator>();;
    }

    private void Update()
    {
        SetAnimations();
    }

    #region function & methods
    private void SetAnimations()
    {
        if (playerMovement.isCrouched) //eğilik iken
        {
            playerAnimator.SetBool("Sprint", false);
            playerAnimator.SetBool("Run", false);
            playerAnimator.SetBool("Walk", false);
            playerAnimator.SetBool("Crouch", true);

            if (!playerMovement.isWalkingCrouched)
            {
                playerAnimator.SetBool("WalkCrouched", false);           
            }
            else
            {
                playerAnimator.SetBool("WalkCrouched", true);
            }
        }
        else //eğilik değil iken
        {
            playerAnimator.SetBool("Crouch", false);
            playerAnimator.SetBool("WalkCrouched", false);

            if (playerMovement.isSprinting)
            {
                playerAnimator.SetBool("Sprint", true);
                playerAnimator.SetBool("Run", false);
                playerAnimator.SetBool("Walk", false);

                if (!playerMovement.IsGrounded())
                    playerAnimator.SetBool("OnAir", true);
                else
                    playerAnimator.SetBool("OnAir", false);
            }
            else if (playerMovement.isWalking)
            {
                playerAnimator.SetBool("Sprint", false);
                playerAnimator.SetBool("Run", false);
                playerAnimator.SetBool("Walk", true);
            }
            else if(playerMovement.isRunning)
            {
                playerAnimator.SetBool("Sprint", false);
                playerAnimator.SetBool("Run", true);
                playerAnimator.SetBool("Walk", false);
            }
            else
            {
                playerAnimator.SetBool("Sprint", false);
                playerAnimator.SetBool("Run", false);
                playerAnimator.SetBool("Walk", false);
            }
        }
    }
    #endregion
}
