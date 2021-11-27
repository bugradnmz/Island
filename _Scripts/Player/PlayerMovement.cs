using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float movementSpeed;

    [SerializeField] private float walkSpeed, runSpeed, sprintSpeed, slowSpeed;
    [SerializeField] private float movementLerpSpeed;
    [SerializeField] private float jumpForce;
    [SerializeField] private float groundRayDistance;
    [SerializeField] private float slopeRayDistance;
    [SerializeField] private float slideRayDistance;    
    [SerializeField] private float clampRayDistance;
    [SerializeField] private float ceilingRayDistance;
    [SerializeField] private float ceilingClampRayDistance;
    [SerializeField] private float groundClampDefault;
    [SerializeField] private float slideDuration;
    [SerializeField] private float dropEffectThreshold;
    [SerializeField] private float dropEffectValue;
    [SerializeField] private float dropEffectDuration;
    private float slideTimer;
    private float groundClamp;
    private float slopeAngle;
    private float timeOnAir;

    private bool isAbleToClamp;
    private bool isAbleToSprint;
    private bool clampCeiling;
    private bool dropEffect;

    [HideInInspector] public float hAxis;
    [HideInInspector] public float vAxis;

    public float fallingThreshold;

    [HideInInspector] public bool isFalling;
    [HideInInspector] public bool isCrouched;
    [HideInInspector] public bool isMoving;
    [HideInInspector] public bool isJumped;
    [HideInInspector] public bool isSprinting;
    [HideInInspector] public bool isWalking;
    [HideInInspector] public bool isRunning;
    [HideInInspector] public bool isWalkingCrouched;
    [HideInInspector] public bool isSprintJumped;

    [HideInInspector] public string movementType;

    [HideInInspector] public Rigidbody rb;
    private CapsuleCollider playerCollider;
    private PlayerStats playerStats;
    private PlayerSounds playerSounds;
    private TakeDamage takeDamage;
    private Tags tags;

    public PhysicMaterial stickyMaterial;
    public PhysicMaterial slipperyMaterial;

    public RaycastHit slopeHit;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        playerCollider = GetComponent<CapsuleCollider>();
        playerStats = GetComponent<PlayerStats>();
        playerSounds = GetComponent<PlayerSounds>();
        takeDamage = GetComponent<TakeDamage>();
        tags = GameObject.Find("Game Manager").GetComponent<Tags>();
    }

    private void Update()
    {
        hAxis = Input.GetAxisRaw("Horizontal");
        vAxis = Input.GetAxisRaw("Vertical");

        MovementInput();
        CrouchInput();
        JumpInput();
        Slide();
        CheckSlopeAngle();
        GroundClamp();
        CeilingClamp();
        IsFalling();
    }

    private void FixedUpdate()
    {
        Movement();
    }

    #region function & methods
    private void Movement()
    {
        Vector3 movement = new Vector3(hAxis, -groundClamp, vAxis) * movementSpeed * Time.fixedDeltaTime;
        Vector3 newPosition = rb.position + rb.transform.TransformDirection(movement);

        rb.MovePosition(newPosition);
    }

    private void MovementInput()
    {
        if (isCrouched)
        {            
            if ((vAxis !=0 || hAxis != 0) && !takeDamage.isSlowed && !clampCeiling)
            {
                isWalkingCrouched = true;
                Walk();
            }
            else if (((vAxis != 0 || hAxis != 0) && takeDamage.isSlowed) || clampCeiling)
            {
                isWalkingCrouched = true;
                Slow();
            }
            else
            {
                isWalkingCrouched = false;
                Idle();
            }
        }
        else
        {
            if (Input.GetButton("Sprint") && vAxis > 0 && hAxis == 0 && !playerStats.isExhausted &&
                playerStats.playerStamina > 0 && !isFalling && !isCrouched && isAbleToSprint && !isJumped &&
                !takeDamage.isSlowed && !clampCeiling)
                Sprint();
            else if ((vAxis > 0 || hAxis != 0) && !takeDamage.isSlowed && !clampCeiling)
                Run();
            else if ((vAxis < 0) && !takeDamage.isSlowed && !clampCeiling)
                Walk();
            else if ((takeDamage.isSlowed && (vAxis != 0 || hAxis != 0)) || clampCeiling)
                Slow();
            else
                Idle();
        }
    }

    private void Idle()
    {
        isMoving = false;
        isWalking = false;
        isSprinting = false;
        isRunning = false;
        isWalkingCrouched = false;
        movementType = "none";
        movementSpeed = 0;
    }

    private void Walk()
    {
        movementSpeed = walkSpeed;
        movementType = "walk";
        isMoving = true;
        isWalking = true;
        isSprinting = false;
        isRunning = false;
    }

    private void Run()
    {
        movementSpeed = Mathf.Lerp(movementSpeed, runSpeed, movementLerpSpeed * Time.deltaTime);
        movementType = "run";
        isMoving = true;
        isWalking = false;
        isSprinting = false;
        isWalkingCrouched = false;
        isRunning = true;
    }

    private void Sprint()
    {
        movementSpeed = Mathf.Lerp(movementSpeed, sprintSpeed, movementLerpSpeed * Time.deltaTime);
        movementType = "sprint";
        isMoving = true;
        isWalking = false;
        isSprinting = true;
        isWalkingCrouched = false;
        isRunning = false;
    }

    private void Slow()
    {
        movementSpeed = slowSpeed;
        movementType = "walk";
        isMoving = true;
        isWalking = true;
        isSprinting = false;
        isRunning = false;
    }

    private void CrouchInput()
    {
        if (Input.GetButtonDown("Crouch"))
        {
            if (!isCrouched)
            {
                isCrouched = true;                
                movementSpeed = walkSpeed;
                movementType = "walk";
                playerSounds.Crouch();
            }
            else if (isCrouched && !CheckCeiling())
            {                
                isCrouched = false;
                playerSounds.GetUp();
            }
        }
    }

    public void JumpInput()
    {
        if (Input.GetButtonDown("Jump") && IsGrounded() && playerStats.playerStamina > 0 && !playerStats.isExhausted)
        {
            rb.AddForce(0, jumpForce, 0, ForceMode.Impulse);
            playerStats.jumpCost = true;
            isAbleToClamp = false;
                      
            if (IsGrounded() && isSprinting)
            {
                isJumped = false;
                isSprintJumped = true;
            }
            else
            {
                isJumped = true;
                isSprintJumped = true;
            }
        }
    }

    public bool IsGrounded()
    {
        Debug.DrawRay(transform.position, Vector3.down * groundRayDistance, Color.magenta);
        return Physics.Raycast(transform.position, Vector3.down, groundRayDistance);
    }

    private void IsFalling()  // Düşme efektinide barındırıyor.
    {
        if (!IsGrounded())
        {
            timeOnAir += Time.deltaTime;

            if (timeOnAir >= fallingThreshold)
            {
                isFalling = true;
                playerCollider.material = slipperyMaterial;
            }

            if(timeOnAir >= dropEffectThreshold)
            {
                dropEffect = true;
            }
        }
        else
        {
            isFalling = false;            
            timeOnAir = 0;

            if (dropEffect)
            {
                EZCameraShake.CameraShaker.Instance.ShakeOnce(dropEffectValue, dropEffectValue * 0.4f, dropEffectDuration, dropEffectDuration);
                dropEffect = false;
            }
        }
    }

    private void CheckSlopeAngle()
    {
        RaycastHit slopeHit;
        
        if(Physics.Raycast(transform.position, Vector3.down, out slopeHit, slopeRayDistance))
        {
            Debug.DrawRay(transform.position, Vector3.down * slopeRayDistance, Color.red);

            if (slopeHit.collider.CompareTag(tags.tags[1]) ||
                slopeHit.collider.CompareTag(tags.tags[2]) ||
                slopeHit.collider.CompareTag(tags.tags[3]))
            {
                slopeAngle = Vector3.Angle(slopeHit.normal, Vector3.up);
            }
        }

        if (slopeAngle > 45)
            isAbleToSprint = false;
        else
            isAbleToSprint = true;
    }

    public bool CheckCeiling()
    {
        RaycastHit hit;

        //Middle
        if (Physics.Raycast(transform.position, Vector3.up, out hit, ceilingRayDistance))
        {
            Debug.DrawRay(transform.position, Vector3.up * ceilingRayDistance, Color.black);

            if (hit.collider.CompareTag(tags.tags[0]) ||
                hit.collider.CompareTag(tags.tags[1]) ||
                hit.collider.CompareTag(tags.tags[2]) ||
                hit.collider.CompareTag(tags.tags[3]))
                return true;
        }
        //North
        else if (Physics.Raycast(transform.position + new Vector3(0, 0, 0.525f), Vector3.up, out hit, ceilingRayDistance))
        {
            Debug.DrawRay(transform.position + new Vector3(0, 0, 0.525f), Vector3.up * ceilingRayDistance, Color.black);

            if (hit.collider.CompareTag(tags.tags[0]) ||
                hit.collider.CompareTag(tags.tags[1]) ||
                hit.collider.CompareTag(tags.tags[2]) ||
                hit.collider.CompareTag(tags.tags[3]))
                return true;
        }
        //East
        else if (Physics.Raycast(transform.position + new Vector3(0.525f, 0, 0), Vector3.up, out hit, ceilingRayDistance))
        {
            Debug.DrawRay(transform.position + new Vector3(0.525f, 0, 0), Vector3.up * ceilingRayDistance, Color.black);

            if (hit.collider.CompareTag(tags.tags[0]) ||
                hit.collider.CompareTag(tags.tags[1]) ||
                hit.collider.CompareTag(tags.tags[2]) ||
                hit.collider.CompareTag(tags.tags[3]))
                return true;
        }
        //West
        else if (Physics.Raycast(transform.position + new Vector3(-0.525f, 0, 0), Vector3.up, out hit, ceilingRayDistance))
        {
            Debug.DrawRay(transform.position + new Vector3(-0.525f, 0, 0), Vector3.up * ceilingRayDistance, Color.black);

            if (hit.collider.CompareTag(tags.tags[0]) ||
                hit.collider.CompareTag(tags.tags[1]) ||
                hit.collider.CompareTag(tags.tags[2]) ||
                hit.collider.CompareTag(tags.tags[3]))
                return true;
        }
        //South
        else if (Physics.Raycast(transform.position + new Vector3(0, 0, -0.525f), Vector3.up, out hit, ceilingRayDistance))
        {
            Debug.DrawRay(transform.position + new Vector3(0, 0, -0.525f), Vector3.up * ceilingRayDistance, Color.black);

            if (hit.collider.CompareTag(tags.tags[0]) ||
                hit.collider.CompareTag(tags.tags[1]) ||
                hit.collider.CompareTag(tags.tags[2]) ||
                hit.collider.CompareTag(tags.tags[3]))
                return true;
        }
        //South East
        else if (Physics.Raycast(transform.position + (new Vector3(0.525f, 0, -0.525f).normalized) / 1.95f, Vector3.up, out hit, ceilingRayDistance))
        {       
            Debug.DrawRay(transform.position + (new Vector3(0.525f, 0, -0.525f).normalized) / 1.95f, Vector3.up * ceilingRayDistance, Color.black);

            if (hit.collider.CompareTag(tags.tags[0]) ||
                hit.collider.CompareTag(tags.tags[1]) ||
                hit.collider.CompareTag(tags.tags[2]) ||
                hit.collider.CompareTag(tags.tags[3]))
                return true;
        }
        //South West
        else if (Physics.Raycast(transform.position + (new Vector3(-0.525f, 0, -0.525f).normalized) / 1.95f, Vector3.up, out hit, ceilingRayDistance))
        {
            Debug.DrawRay(transform.position + (new Vector3(-0.525f, 0, -0.525f).normalized) / 1.95f, Vector3.up * ceilingRayDistance, Color.black);

            if (hit.collider.CompareTag(tags.tags[0]) ||
                hit.collider.CompareTag(tags.tags[1]) ||
                hit.collider.CompareTag(tags.tags[2]) ||
                hit.collider.CompareTag(tags.tags[3]))
                return true;
        }
        //North East
        else if (Physics.Raycast(transform.position + (new Vector3(0.525f, 0, 0.525f).normalized) / 1.95f, Vector3.up, out hit, ceilingRayDistance))
        {
            Debug.DrawRay(transform.position + (new Vector3(0.525f, 0, 0.525f).normalized) / 1.95f, Vector3.up * ceilingRayDistance, Color.black);

            if (hit.collider.CompareTag(tags.tags[0]) ||
                hit.collider.CompareTag(tags.tags[1]) ||
                hit.collider.CompareTag(tags.tags[2]) ||
                hit.collider.CompareTag(tags.tags[3]))
                return true;
        }
        //North West
        else if (Physics.Raycast(transform.position + (new Vector3(-0.525f, 0, -0.525f).normalized) / 1.95f, Vector3.up, out hit, ceilingRayDistance))
        {
            Debug.DrawRay(transform.position + (new Vector3(-0.525f, 0, -0.525f).normalized) / 1.95f, Vector3.up * ceilingRayDistance, Color.black);

            if (hit.collider.CompareTag(tags.tags[0]) ||
                hit.collider.CompareTag(tags.tags[1]) ||
                hit.collider.CompareTag(tags.tags[2]) ||
                hit.collider.CompareTag(tags.tags[3]))
                return true;
        }

        return false;        
    }

    private void Slide()
    {
        if (!isCrouched)
        {
            Vector3 topRayOrigin = transform.position + new Vector3(0, 0.5f, 0);
            Vector3 middleRayOrigin = transform.position;
            Vector3 bottomRayOrigin = transform.position - new Vector3(0, 0.5f, 0);

            RaycastHit hit;

            //TOP
            //North
            if (Physics.Raycast(topRayOrigin, transform.forward, out hit, slideRayDistance))
            {
                Debug.DrawRay(topRayOrigin, transform.forward * slideRayDistance, Color.cyan);

                if (hit.collider.CompareTag(tags.tags[1]) ||
                    hit.collider.CompareTag(tags.tags[2]) ||
                    hit.collider.CompareTag(tags.tags[3]))
                {
                    playerCollider.material = slipperyMaterial;
                    slideTimer = 0;
                }
            }
            //East            
            else if(Physics.Raycast(topRayOrigin, transform.right, out hit, slideRayDistance))
            {
                Debug.DrawRay(topRayOrigin, transform.right * slideRayDistance, Color.cyan);

                if (hit.collider.CompareTag(tags.tags[1]) ||
                    hit.collider.CompareTag(tags.tags[2]) ||
                    hit.collider.CompareTag(tags.tags[3]))
                {
                    playerCollider.material = slipperyMaterial;
                    slideTimer = 0;
                }
            }
            //South            
            else if(Physics.Raycast(topRayOrigin, -transform.forward, out hit, slideRayDistance))
            {
                Debug.DrawRay(topRayOrigin, -transform.forward * slideRayDistance, Color.cyan);

                if (hit.collider.CompareTag(tags.tags[1]) ||
                    hit.collider.CompareTag(tags.tags[2]) ||
                    hit.collider.CompareTag(tags.tags[3]))
                {
                    playerCollider.material = slipperyMaterial;
                    slideTimer = 0;
                }
            }
            //West            
            else if(Physics.Raycast(topRayOrigin, -transform.right, out hit, slideRayDistance))
            {
                Debug.DrawRay(topRayOrigin, -transform.right * slideRayDistance, Color.cyan);

                if (hit.collider.CompareTag(tags.tags[1]) ||
                    hit.collider.CompareTag(tags.tags[2]) ||
                    hit.collider.CompareTag(tags.tags[3]))
                {
                    playerCollider.material = slipperyMaterial;
                    slideTimer = 0;
                }
            }
            //North East            
            else if (Physics.Raycast(topRayOrigin, (transform.forward + transform.right).normalized, out hit, slideRayDistance))
            {
                Debug.DrawRay(topRayOrigin, (transform.forward + transform.right).normalized * slideRayDistance, Color.cyan);

                if (hit.collider.CompareTag(tags.tags[1]) ||
                    hit.collider.CompareTag(tags.tags[2]) ||
                    hit.collider.CompareTag(tags.tags[3]))
                {
                    playerCollider.material = slipperyMaterial;
                    slideTimer = 0;
                }
            }
            //North West            
            else if (Physics.Raycast(topRayOrigin, (transform.forward - transform.right).normalized, out hit, slideRayDistance))
            {
                Debug.DrawRay(topRayOrigin, (transform.forward - transform.right).normalized * slideRayDistance, Color.cyan);

                if (hit.collider.CompareTag(tags.tags[1]) ||
                    hit.collider.CompareTag(tags.tags[2]) ||
                    hit.collider.CompareTag(tags.tags[3]))
                {
                    playerCollider.material = slipperyMaterial;
                    slideTimer = 0;
                }
            }
            //South East            
            else if (Physics.Raycast(topRayOrigin, (-transform.forward + transform.right), out hit, slideRayDistance))
            {
                Debug.DrawRay(topRayOrigin, (-transform.forward + transform.right).normalized * slideRayDistance, Color.cyan);

                if (hit.collider.CompareTag(tags.tags[1]) ||
                    hit.collider.CompareTag(tags.tags[2]) ||
                    hit.collider.CompareTag(tags.tags[3]))
                {
                    playerCollider.material = slipperyMaterial;
                    slideTimer = 0;
                }
            }
            //South West            
            else if (Physics.Raycast(topRayOrigin, (-transform.forward - transform.right), out hit, slideRayDistance))
            {
                Debug.DrawRay(topRayOrigin, (-transform.forward - transform.right).normalized * slideRayDistance, Color.cyan);

                if (hit.collider.CompareTag(tags.tags[1]) ||
                    hit.collider.CompareTag(tags.tags[2]) ||
                    hit.collider.CompareTag(tags.tags[3]))
                {
                    playerCollider.material = slipperyMaterial;
                    slideTimer = 0;
                }
            }

            //MIDDLE
            //North
            else if (Physics.Raycast(middleRayOrigin, transform.forward, out hit, slideRayDistance))
            {
                Debug.DrawRay(middleRayOrigin, transform.forward * slideRayDistance, Color.cyan);

                if (hit.collider.CompareTag(tags.tags[1]) ||
                    hit.collider.CompareTag(tags.tags[2]) ||
                    hit.collider.CompareTag(tags.tags[3]))
                {
                    playerCollider.material = slipperyMaterial;
                    slideTimer = 0;
                }
            }
            //East            
            else if (Physics.Raycast(middleRayOrigin, transform.right, out hit, slideRayDistance))
            {
                Debug.DrawRay(middleRayOrigin, transform.right * slideRayDistance, Color.cyan);

                if (hit.collider.CompareTag(tags.tags[1]) ||
                    hit.collider.CompareTag(tags.tags[2]) ||
                    hit.collider.CompareTag(tags.tags[3]))
                {
                    playerCollider.material = slipperyMaterial;
                    slideTimer = 0;
                }
            }
            //South            
            else if (Physics.Raycast(middleRayOrigin, -transform.forward, out hit, slideRayDistance))
            {
                Debug.DrawRay(middleRayOrigin, -transform.forward * slideRayDistance, Color.cyan);

                if (hit.collider.CompareTag(tags.tags[1]) ||
                    hit.collider.CompareTag(tags.tags[2]) ||
                    hit.collider.CompareTag(tags.tags[3]))
                {
                    playerCollider.material = slipperyMaterial;
                    slideTimer = 0;
                }
            }
            //West            
            else if (Physics.Raycast(middleRayOrigin, -transform.right, out hit, slideRayDistance))
            {
                Debug.DrawRay(middleRayOrigin, -transform.right * slideRayDistance, Color.cyan);

                if (hit.collider.CompareTag(tags.tags[1]) ||
                    hit.collider.CompareTag(tags.tags[2]) ||
                    hit.collider.CompareTag(tags.tags[3]))
                {
                    playerCollider.material = slipperyMaterial;
                    slideTimer = 0;
                }
            }
            //North East            
            else if (Physics.Raycast(middleRayOrigin, (transform.forward + transform.right).normalized, out hit, slideRayDistance))
            {
                Debug.DrawRay(middleRayOrigin, (transform.forward + transform.right).normalized * slideRayDistance, Color.cyan);

                if (hit.collider.CompareTag(tags.tags[1]) ||
                    hit.collider.CompareTag(tags.tags[2]) ||
                    hit.collider.CompareTag(tags.tags[3]))
                {
                    playerCollider.material = slipperyMaterial;
                    slideTimer = 0;
                }
            }
            //North West            
            else if (Physics.Raycast(middleRayOrigin, (transform.forward - transform.right).normalized, out hit, slideRayDistance))
            {
                Debug.DrawRay(middleRayOrigin, (transform.forward - transform.right).normalized * slideRayDistance, Color.cyan);

                if (hit.collider.CompareTag(tags.tags[1]) ||
                    hit.collider.CompareTag(tags.tags[2]) ||
                    hit.collider.CompareTag(tags.tags[3]))
                {
                    playerCollider.material = slipperyMaterial;
                    slideTimer = 0;
                }
            }
            //South East            
            else if (Physics.Raycast(middleRayOrigin, (-transform.forward + transform.right), out hit, slideRayDistance))
            {
                Debug.DrawRay(middleRayOrigin, (-transform.forward + transform.right).normalized * slideRayDistance, Color.cyan);

                if (hit.collider.CompareTag(tags.tags[1]) ||
                    hit.collider.CompareTag(tags.tags[2]) ||
                    hit.collider.CompareTag(tags.tags[3]))
                {
                    playerCollider.material = slipperyMaterial;
                    slideTimer = 0;
                }
            }
            //South West            
            else if (Physics.Raycast(middleRayOrigin, (-transform.forward - transform.right), out hit, slideRayDistance))
            {
                Debug.DrawRay(middleRayOrigin, (-transform.forward - transform.right).normalized * slideRayDistance, Color.cyan);

                if (hit.collider.CompareTag(tags.tags[1]) ||
                    hit.collider.CompareTag(tags.tags[2]) ||
                    hit.collider.CompareTag(tags.tags[3]))
                {
                    playerCollider.material = slipperyMaterial;
                    slideTimer = 0;
                }
            }

            //BOTTOM
            //North
            else if (Physics.Raycast(bottomRayOrigin, transform.forward, out hit, slideRayDistance))
            {
                Debug.DrawRay(bottomRayOrigin, transform.forward * slideRayDistance, Color.cyan);

                if (hit.collider.CompareTag(tags.tags[1]) ||
                    hit.collider.CompareTag(tags.tags[2]) ||
                    hit.collider.CompareTag(tags.tags[3]))
                {
                    playerCollider.material = slipperyMaterial;
                    slideTimer = 0;
                }
            }
            //East            
            else if (Physics.Raycast(bottomRayOrigin, transform.right, out hit, slideRayDistance))
            {
                Debug.DrawRay(bottomRayOrigin, transform.right * slideRayDistance, Color.cyan);

                if (hit.collider.CompareTag(tags.tags[1]) ||
                    hit.collider.CompareTag(tags.tags[2]) ||
                    hit.collider.CompareTag(tags.tags[3]))
                {
                    playerCollider.material = slipperyMaterial;
                    slideTimer = 0;
                }
            }
            //South            
            else if (Physics.Raycast(bottomRayOrigin, -transform.forward, out hit, slideRayDistance))
            {
                Debug.DrawRay(bottomRayOrigin, -transform.forward * slideRayDistance, Color.cyan);

                if (hit.collider.CompareTag(tags.tags[1]) ||
                    hit.collider.CompareTag(tags.tags[2]) ||
                    hit.collider.CompareTag(tags.tags[3]))
                {
                    playerCollider.material = slipperyMaterial;
                    slideTimer = 0;
                }
            }
            //West            
            else if (Physics.Raycast(bottomRayOrigin, -transform.right, out hit, slideRayDistance))
            {
                Debug.DrawRay(bottomRayOrigin, -transform.right * slideRayDistance, Color.cyan);

                if (hit.collider.CompareTag(tags.tags[1]) ||
                    hit.collider.CompareTag(tags.tags[2]) ||
                    hit.collider.CompareTag(tags.tags[3]))
                {
                    playerCollider.material = slipperyMaterial;
                    slideTimer = 0;
                }
            }
            //North East            
            else if (Physics.Raycast(bottomRayOrigin, (transform.forward + transform.right).normalized, out hit, slideRayDistance))
            {
                Debug.DrawRay(bottomRayOrigin, (transform.forward + transform.right).normalized * slideRayDistance, Color.cyan);

                if (hit.collider.CompareTag(tags.tags[1]) ||
                    hit.collider.CompareTag(tags.tags[2]) ||
                    hit.collider.CompareTag(tags.tags[3]))
                {
                    playerCollider.material = slipperyMaterial;
                    slideTimer = 0;
                }
            }
            //North West            
            else if (Physics.Raycast(bottomRayOrigin, (transform.forward - transform.right).normalized, out hit, slideRayDistance))
            {
                Debug.DrawRay(bottomRayOrigin, (transform.forward - transform.right).normalized * slideRayDistance, Color.cyan);

                if (hit.collider.CompareTag(tags.tags[1]) ||
                    hit.collider.CompareTag(tags.tags[2]) ||
                    hit.collider.CompareTag(tags.tags[3]))
                {
                    playerCollider.material = slipperyMaterial;
                    slideTimer = 0;
                }
            }
            //South East            
            else if (Physics.Raycast(bottomRayOrigin, (-transform.forward + transform.right), out hit, slideRayDistance))
            {
                Debug.DrawRay(bottomRayOrigin, (-transform.forward + transform.right).normalized * slideRayDistance, Color.cyan);

                if (hit.collider.CompareTag(tags.tags[1]) ||
                    hit.collider.CompareTag(tags.tags[2]) ||
                    hit.collider.CompareTag(tags.tags[3]))
                {
                    playerCollider.material = slipperyMaterial;
                    slideTimer = 0;
                }
            }
            //South West            
            else if (Physics.Raycast(bottomRayOrigin, (-transform.forward - transform.right), out hit, slideRayDistance))
            {
                Debug.DrawRay(bottomRayOrigin, (-transform.forward - transform.right).normalized * slideRayDistance, Color.cyan);

                if (hit.collider.CompareTag(tags.tags[1]) ||
                    hit.collider.CompareTag(tags.tags[2]) ||
                    hit.collider.CompareTag(tags.tags[3]))
                {
                    playerCollider.material = slipperyMaterial;
                    slideTimer = 0;
                }
            }
            else
            {
                slideTimer += Time.deltaTime;

                if (slideTimer >= slideDuration)
                    playerCollider.material = stickyMaterial;
            }
        }
        else
        {
            Vector3 bottomRayOrigin = transform.position - new Vector3(0, 0.5f, 0);

            RaycastHit hit;

            //BOTTOM
            //North
            if (Physics.Raycast(bottomRayOrigin, transform.forward, out hit, slideRayDistance))
            {
                Debug.DrawRay(bottomRayOrigin, transform.forward * slideRayDistance, Color.cyan);

                if (hit.collider.CompareTag(tags.tags[1]) ||
                    hit.collider.CompareTag(tags.tags[2]) ||
                    hit.collider.CompareTag(tags.tags[3]))
                {
                    playerCollider.material = slipperyMaterial;
                    slideTimer = 0;
                }
            }
            //East            
            else if (Physics.Raycast(bottomRayOrigin, transform.right, out hit, slideRayDistance))
            {
                Debug.DrawRay(bottomRayOrigin, transform.right * slideRayDistance, Color.cyan);

                if (hit.collider.CompareTag(tags.tags[1]) ||
                    hit.collider.CompareTag(tags.tags[2]) ||
                    hit.collider.CompareTag(tags.tags[3]))
                {
                    playerCollider.material = slipperyMaterial;
                    slideTimer = 0;
                }
            }
            //South            
            else if (Physics.Raycast(bottomRayOrigin, -transform.forward, out hit, slideRayDistance))
            {
                Debug.DrawRay(bottomRayOrigin, -transform.forward * slideRayDistance, Color.cyan);

                if (hit.collider.CompareTag(tags.tags[1]) ||
                    hit.collider.CompareTag(tags.tags[2]) ||
                    hit.collider.CompareTag(tags.tags[3]))
                {
                    playerCollider.material = slipperyMaterial;
                    slideTimer = 0;
                }
            }
            //West            
            else if (Physics.Raycast(bottomRayOrigin, -transform.right, out hit, slideRayDistance))
            {
                Debug.DrawRay(bottomRayOrigin, -transform.right * slideRayDistance, Color.cyan);

                if (hit.collider.CompareTag(tags.tags[1]) ||
                    hit.collider.CompareTag(tags.tags[2]) ||
                    hit.collider.CompareTag(tags.tags[3]))
                {
                    playerCollider.material = slipperyMaterial;
                    slideTimer = 0;
                }
            }
            //North East            
            else if (Physics.Raycast(bottomRayOrigin, (transform.forward + transform.right).normalized, out hit, slideRayDistance))
            {
                Debug.DrawRay(bottomRayOrigin, (transform.forward + transform.right).normalized * slideRayDistance, Color.cyan);

                if (hit.collider.CompareTag(tags.tags[1]) ||
                    hit.collider.CompareTag(tags.tags[2]) ||
                    hit.collider.CompareTag(tags.tags[3]))
                {
                    playerCollider.material = slipperyMaterial;
                    slideTimer = 0;
                }
            }
            //North West            
            else if (Physics.Raycast(bottomRayOrigin, (transform.forward - transform.right).normalized, out hit, slideRayDistance))
            {
                Debug.DrawRay(bottomRayOrigin, (transform.forward - transform.right).normalized * slideRayDistance, Color.cyan);

                if (hit.collider.CompareTag(tags.tags[1]) ||
                    hit.collider.CompareTag(tags.tags[2]) ||
                    hit.collider.CompareTag(tags.tags[3]))
                {
                    playerCollider.material = slipperyMaterial;
                    slideTimer = 0;
                }
            }
            //South East            
            else if (Physics.Raycast(bottomRayOrigin, (-transform.forward + transform.right), out hit, slideRayDistance))
            {
                Debug.DrawRay(bottomRayOrigin, (-transform.forward + transform.right).normalized * slideRayDistance, Color.cyan);

                if (hit.collider.CompareTag(tags.tags[1]) ||
                    hit.collider.CompareTag(tags.tags[2]) ||
                    hit.collider.CompareTag(tags.tags[3]))
                {
                    playerCollider.material = slipperyMaterial;
                    slideTimer = 0;
                }
            }
            //South West            
            else if (Physics.Raycast(bottomRayOrigin, (-transform.forward - transform.right), out hit, slideRayDistance))
            {
                Debug.DrawRay(bottomRayOrigin, (-transform.forward - transform.right).normalized * slideRayDistance, Color.cyan);

                if (hit.collider.CompareTag(tags.tags[1]) ||
                    hit.collider.CompareTag(tags.tags[2]) ||
                    hit.collider.CompareTag(tags.tags[3]))
                {
                    playerCollider.material = slipperyMaterial;
                    slideTimer = 0;
                }
            }
            else
            {
                slideTimer += Time.deltaTime;

                if (slideTimer >= slideDuration)
                    playerCollider.material = stickyMaterial;
            }
        }
    }

    private void CeilingClamp()
    {
        Vector3 rayOrigin;

        if (isCrouched)
        {
            rayOrigin = transform.position + new Vector3(0, -0.5f, 0);
        }
        else
        {
            rayOrigin = transform.position + new Vector3(0, 0.5f, 0);
        }

        if (vAxis > 0 && hAxis == 0)//North
        {
            RaycastHit hit;

            //North East            
            if(Physics.Raycast(rayOrigin, (transform.forward + transform.right + transform.up * 2).normalized, out hit, ceilingClampRayDistance))
            {
                Debug.DrawRay(rayOrigin, (transform.forward + transform.right + transform.up * 2).normalized * ceilingClampRayDistance, Color.white);

                if (hit.collider.CompareTag(tags.tags[1]) ||
                    hit.collider.CompareTag(tags.tags[2]) ||
                    hit.collider.CompareTag(tags.tags[3]))
                    clampCeiling = true;
            }
            //North            
            else if(Physics.Raycast(rayOrigin, (transform.forward + transform.up * 2).normalized, out hit, ceilingClampRayDistance))
            {
                Debug.DrawRay(rayOrigin, (transform.forward + transform.up * 2).normalized * ceilingClampRayDistance, Color.white);

                if (hit.collider.CompareTag(tags.tags[1]) ||
                    hit.collider.CompareTag(tags.tags[2]) ||
                    hit.collider.CompareTag(tags.tags[3]))
                    clampCeiling = true;
            }
            //North West            
            else if (Physics.Raycast(rayOrigin, (transform.forward - transform.right + transform.up * 2).normalized, out hit, ceilingClampRayDistance))
            {
                Debug.DrawRay(rayOrigin, (transform.forward - transform.right + transform.up * 2).normalized * ceilingClampRayDistance, Color.white);

                if (hit.collider.CompareTag(tags.tags[1]) ||
                    hit.collider.CompareTag(tags.tags[2]) ||
                    hit.collider.CompareTag(tags.tags[3]))
                    clampCeiling = true;
            }
            else
            {
                clampCeiling = false;
            }
        }
        else if (vAxis == 0 && hAxis > 0)//East
        {
            RaycastHit hit;

            //North East            
            if (Physics.Raycast(rayOrigin, (transform.forward + transform.right + transform.up * 2).normalized, out hit, ceilingClampRayDistance))
            {
                Debug.DrawRay(rayOrigin, (transform.forward + transform.right + transform.up * 2).normalized * ceilingClampRayDistance, Color.white);

                if (hit.collider.CompareTag(tags.tags[1]) ||
                    hit.collider.CompareTag(tags.tags[2]) ||
                    hit.collider.CompareTag(tags.tags[3]))
                    clampCeiling = true;
            }
            //East            
            else if (Physics.Raycast(rayOrigin, (transform.right + transform.up * 2).normalized, out hit, ceilingClampRayDistance))
            {
                Debug.DrawRay(rayOrigin, (transform.right + transform.up * 2).normalized * ceilingClampRayDistance, Color.white);

                if (hit.collider.CompareTag(tags.tags[1]) ||
                    hit.collider.CompareTag(tags.tags[2]) ||
                    hit.collider.CompareTag(tags.tags[3]))
                    clampCeiling = true;
            }
            //South East          
            else if (Physics.Raycast(rayOrigin, (-transform.forward + transform.right + transform.up * 2).normalized, out hit, ceilingClampRayDistance))
            {
                Debug.DrawRay(rayOrigin, (-transform.forward + transform.right + transform.up * 2).normalized * ceilingClampRayDistance, Color.white);

                if (hit.collider.CompareTag(tags.tags[1]) ||
                    hit.collider.CompareTag(tags.tags[2]) ||
                    hit.collider.CompareTag(tags.tags[3]))
                    clampCeiling = true;
            }
            else
            {
                clampCeiling = false;
            }
        }
        else if (vAxis < 0 && hAxis == 0)//South
        {
            RaycastHit hit;

            //South East          
            if (Physics.Raycast(rayOrigin, (-transform.forward + transform.right + transform.up * 2).normalized, out hit, ceilingClampRayDistance))
            {
                Debug.DrawRay(rayOrigin, (-transform.forward + transform.right + transform.up * 2).normalized * ceilingClampRayDistance, Color.white);

                if (hit.collider.CompareTag(tags.tags[1]) ||
                    hit.collider.CompareTag(tags.tags[2]) ||
                    hit.collider.CompareTag(tags.tags[3]))
                    clampCeiling = true;
            }
            //South            
            else if (Physics.Raycast(rayOrigin, (-transform.forward + transform.up * 2).normalized, out hit, ceilingClampRayDistance))
            {
                Debug.DrawRay(rayOrigin, (-transform.forward + transform.up * 2).normalized * ceilingClampRayDistance, Color.white);

                if (hit.collider.CompareTag(tags.tags[1]) ||
                    hit.collider.CompareTag(tags.tags[2]) ||
                    hit.collider.CompareTag(tags.tags[3]))
                    clampCeiling = true;
            }
            //South West          
            else if (Physics.Raycast(rayOrigin, (-transform.forward + -transform.right + transform.up * 2).normalized, out hit, ceilingClampRayDistance))
            {
                Debug.DrawRay(rayOrigin, (-transform.forward + -transform.right + transform.up * 2).normalized * ceilingClampRayDistance, Color.white);

                if (hit.collider.CompareTag(tags.tags[1]) ||
                    hit.collider.CompareTag(tags.tags[2]) ||
                    hit.collider.CompareTag(tags.tags[3]))
                    clampCeiling = true;
            }
            else
            {
                clampCeiling = false;
            }
        }
        else if (vAxis == 0 && hAxis < 0)//West
        {
            RaycastHit hit;

            //North West           
            if (Physics.Raycast(rayOrigin, (transform.forward - transform.right + transform.up * 2).normalized, out hit, ceilingClampRayDistance))
            {
                Debug.DrawRay(rayOrigin, (transform.forward - transform.right + transform.up * 2).normalized * ceilingClampRayDistance, Color.white);

                if (hit.collider.CompareTag(tags.tags[1]) ||
                    hit.collider.CompareTag(tags.tags[2]) ||
                    hit.collider.CompareTag(tags.tags[3]))
                    clampCeiling = true;
            }
            //West            
            else if (Physics.Raycast(rayOrigin, (-transform.right + transform.up * 2).normalized, out hit, ceilingClampRayDistance))
            {
                Debug.DrawRay(rayOrigin, (-transform.right + transform.up * 2).normalized * ceilingClampRayDistance, Color.white);

                if (hit.collider.CompareTag(tags.tags[1]) ||
                    hit.collider.CompareTag(tags.tags[2]) ||
                    hit.collider.CompareTag(tags.tags[3]))
                    clampCeiling = true;
            }
            //South West        
            else if (Physics.Raycast(rayOrigin, (-transform.forward + -transform.right + transform.up * 2).normalized, out hit, ceilingClampRayDistance))
            {
                Debug.DrawRay(rayOrigin, (-transform.forward + -transform.right + transform.up * 2).normalized * ceilingClampRayDistance, Color.white);

                if (hit.collider.CompareTag(tags.tags[1]) ||
                    hit.collider.CompareTag(tags.tags[2]) ||
                    hit.collider.CompareTag(tags.tags[3]))
                    clampCeiling = true;
            }
            else
            {
                clampCeiling = false;
            }
        }
        else if (vAxis > 0 && hAxis > 0)//North East
        {
            RaycastHit hit;

            //North            
            if (Physics.Raycast(rayOrigin, (transform.forward + transform.up * 2).normalized, out hit, ceilingClampRayDistance))
            {
                Debug.DrawRay(rayOrigin, (transform.forward  + transform.up * 2).normalized * ceilingClampRayDistance, Color.white);

                if (hit.collider.CompareTag(tags.tags[1]) ||
                    hit.collider.CompareTag(tags.tags[2]) ||
                    hit.collider.CompareTag(tags.tags[3]))
                    clampCeiling = true;
            }
            //North East            
            else if (Physics.Raycast(rayOrigin, (transform.forward + transform.right + transform.up * 2).normalized, out hit, ceilingClampRayDistance))
            {
                Debug.DrawRay(rayOrigin, (transform.forward + transform.right + transform.up * 2).normalized * ceilingClampRayDistance, Color.white);

                if (hit.collider.CompareTag(tags.tags[1]) ||
                    hit.collider.CompareTag(tags.tags[2]) ||
                    hit.collider.CompareTag(tags.tags[3]))
                    clampCeiling = true;
            }
            //East            
            else if (Physics.Raycast(rayOrigin, (transform.right + transform.up * 2).normalized, out hit, ceilingClampRayDistance))
            {
                Debug.DrawRay(rayOrigin, (transform.right + transform.up * 2).normalized * ceilingClampRayDistance, Color.white);

                if (hit.collider.CompareTag(tags.tags[1]) ||
                    hit.collider.CompareTag(tags.tags[2]) ||
                    hit.collider.CompareTag(tags.tags[3]))
                    clampCeiling = true;
            }
            else
            {
                clampCeiling = false;
            }
        }
        else if (vAxis > 0 && hAxis < 0)//North West
        {
            RaycastHit hit;

            //North            
            if (Physics.Raycast(rayOrigin, (transform.forward + transform.up * 2).normalized, out hit, ceilingClampRayDistance))
            {
                Debug.DrawRay(rayOrigin, (transform.forward + transform.up * 2).normalized * ceilingClampRayDistance, Color.white);

                if (hit.collider.CompareTag(tags.tags[1]) ||
                    hit.collider.CompareTag(tags.tags[2]) ||
                    hit.collider.CompareTag(tags.tags[3]))
                    clampCeiling = true;
            }
            //North West            
            else if (Physics.Raycast(rayOrigin, (transform.forward - transform.right + transform.up * 2).normalized, out hit, ceilingClampRayDistance))
            {
                Debug.DrawRay(rayOrigin, (transform.forward - transform.right + transform.up * 2).normalized * ceilingClampRayDistance, Color.white);

                if (hit.collider.CompareTag(tags.tags[1]) ||
                    hit.collider.CompareTag(tags.tags[2]) ||
                    hit.collider.CompareTag(tags.tags[3]))
                    clampCeiling = true;
            }
            //West           
            else if (Physics.Raycast(rayOrigin, (-transform.right + transform.up * 2).normalized, out hit, ceilingClampRayDistance))
            {
                Debug.DrawRay(rayOrigin, (-transform.right + transform.up * 2).normalized * ceilingClampRayDistance, Color.white);

                if (hit.collider.CompareTag(tags.tags[1]) ||
                    hit.collider.CompareTag(tags.tags[2]) ||
                    hit.collider.CompareTag(tags.tags[3]))
                    clampCeiling = true;
            }
            else
            {
                clampCeiling = false;
            }
        }
        else if (vAxis < 0 && hAxis > 0)//South East
        {
            RaycastHit hit;

            //East           
            if (Physics.Raycast(rayOrigin, (transform.right + transform.up * 2).normalized, out hit, ceilingClampRayDistance))
            {
                Debug.DrawRay(rayOrigin, (transform.right + transform.up * 2).normalized * ceilingClampRayDistance, Color.white);

                if (hit.collider.CompareTag(tags.tags[1]) ||
                    hit.collider.CompareTag(tags.tags[2]) ||
                    hit.collider.CompareTag(tags.tags[3]))
                    clampCeiling = true;
            }
            //South East            
            else if (Physics.Raycast(rayOrigin, (-transform.forward + transform.right + transform.up * 2).normalized, out hit, ceilingClampRayDistance))
            {
                Debug.DrawRay(rayOrigin, (-transform.forward + transform.right + transform.up * 2).normalized * ceilingClampRayDistance, Color.white);

                if (hit.collider.CompareTag(tags.tags[1]) ||
                    hit.collider.CompareTag(tags.tags[2]) ||
                    hit.collider.CompareTag(tags.tags[3]))
                    clampCeiling = true;
            }
            //South            
            else if (Physics.Raycast(rayOrigin, (-transform.forward + transform.up * 2).normalized, out hit, ceilingClampRayDistance))
            {
                Debug.DrawRay(rayOrigin, (-transform.forward + transform.up * 2).normalized * ceilingClampRayDistance, Color.white);

                if (hit.collider.CompareTag(tags.tags[1]) ||
                    hit.collider.CompareTag(tags.tags[2]) ||
                    hit.collider.CompareTag(tags.tags[3]))
                    clampCeiling = true;
            }
            else
            {
                clampCeiling = false;
            }
        }
        else if (vAxis < 0 && hAxis < 0)//South West
        {
            RaycastHit hit;

            //West         
            if (Physics.Raycast(rayOrigin, (-transform.right + transform.up * 2).normalized, out hit, ceilingClampRayDistance))
            {
                Debug.DrawRay(rayOrigin, (-transform.right + transform.up * 2).normalized * ceilingClampRayDistance, Color.white);

                if (hit.collider.CompareTag(tags.tags[1]) ||
                    hit.collider.CompareTag(tags.tags[2]) ||
                    hit.collider.CompareTag(tags.tags[3]))
                    clampCeiling = true;
            }
            //South West          
            else if (Physics.Raycast(rayOrigin, (-transform.forward - transform.right + transform.up * 2).normalized, out hit, ceilingClampRayDistance))
            {
                Debug.DrawRay(rayOrigin, (-transform.forward - transform.right + transform.up * 2).normalized * ceilingClampRayDistance, Color.white);

                if (hit.collider.CompareTag(tags.tags[1]) ||
                    hit.collider.CompareTag(tags.tags[2]) ||
                    hit.collider.CompareTag(tags.tags[3]))
                    clampCeiling = true;
            }
            //South            
            else if (Physics.Raycast(rayOrigin, (-transform.forward + transform.up * 2).normalized, out hit, ceilingClampRayDistance))
            {
                Debug.DrawRay(rayOrigin, (-transform.forward + transform.up * 2).normalized * ceilingClampRayDistance, Color.white);

                if (hit.collider.CompareTag(tags.tags[1]) ||
                    hit.collider.CompareTag(tags.tags[2]) ||
                    hit.collider.CompareTag(tags.tags[3]))
                    clampCeiling = true;
            }
            else
            {
                clampCeiling = false;
            }
        }
        else
        {
            clampCeiling = false;
        }
    }

    private void GroundClamp()
    {
        Vector3 rayOrigin = transform.position - new Vector3(0, 0.75f, 0);

        if (isAbleToClamp)
        {
            if (vAxis > 0 && hAxis == 0)//North
            {
                RaycastHit hit;
                
                if (Physics.Raycast(rayOrigin, (transform.forward - transform.up), out hit, clampRayDistance))
                {
                    Debug.DrawRay(rayOrigin, (transform.forward - transform.up) * clampRayDistance, Color.grey);

                    if (hit.collider.CompareTag(tags.tags[1]) ||
                        hit.collider.CompareTag(tags.tags[2]) ||
                        hit.collider.CompareTag(tags.tags[3]))
                        groundClamp = 0;
                }
                else
                {
                    groundClamp = groundClampDefault;
                }
            }
            else if (vAxis == 0 && hAxis > 0)//East
            {
                RaycastHit hit;

                if (Physics.Raycast(rayOrigin, (transform.right - transform.up), out hit, clampRayDistance))
                {
                    Debug.DrawRay(rayOrigin, (transform.right - transform.up) * clampRayDistance, Color.grey);

                    if (hit.collider.CompareTag(tags.tags[1]) ||
                        hit.collider.CompareTag(tags.tags[2]) ||
                        hit.collider.CompareTag(tags.tags[3]))
                        groundClamp = 0;
                }
                else
                {
                    groundClamp = groundClampDefault;
                }
            }
            else if (vAxis < 0 && hAxis == 0)//South
            {
                RaycastHit hit;

                if (Physics.Raycast(rayOrigin, (-transform.forward - transform.up), out hit, clampRayDistance))
                {
                    Debug.DrawRay(rayOrigin, (-transform.forward - transform.up) * clampRayDistance, Color.grey);

                    if (hit.collider.CompareTag(tags.tags[1]) ||
                        hit.collider.CompareTag(tags.tags[2]) ||
                        hit.collider.CompareTag(tags.tags[3]))
                        groundClamp = 0;
                }
                else
                {
                    groundClamp = groundClampDefault;
                }
            }
            else if (vAxis == 0 && hAxis < 0)//West
            {
                RaycastHit hit;

                if (Physics.Raycast(rayOrigin, (-transform.right - transform.up), out hit, clampRayDistance))
                {
                    Debug.DrawRay(rayOrigin, (-transform.right - transform.up) * clampRayDistance, Color.grey);

                    if (hit.collider.CompareTag(tags.tags[1]) ||
                        hit.collider.CompareTag(tags.tags[2]) ||
                        hit.collider.CompareTag(tags.tags[3]))
                        groundClamp = 0;
                }
                else
                {
                    groundClamp = groundClampDefault;
                }
            }
            else if (vAxis > 0 && hAxis > 0)//North East
            {
                RaycastHit hit;
                
                if(Physics.Raycast(rayOrigin, (transform.forward - transform.up), out hit, clampRayDistance))
                {
                    Debug.DrawRay(rayOrigin, (transform.forward - transform.up) * clampRayDistance, Color.grey);

                    if (hit.collider.CompareTag(tags.tags[1]) ||
                        hit.collider.CompareTag(tags.tags[2]) ||
                        hit.collider.CompareTag(tags.tags[3]))
                        groundClamp = 0;
                }
                else if (Physics.Raycast(rayOrigin, (transform.right - transform.up), out hit, clampRayDistance))
                {
                    Debug.DrawRay(rayOrigin, (transform.right - transform.up) * clampRayDistance, Color.grey);

                    if (hit.collider.CompareTag(tags.tags[1]) ||
                        hit.collider.CompareTag(tags.tags[2]) ||
                        hit.collider.CompareTag(tags.tags[3]))
                        groundClamp = 0;
                }
                else
                {
                    groundClamp = groundClampDefault;
                }
            }
            else if (vAxis > 0 && hAxis < 0)//North West
            {
                RaycastHit hit;

                if (Physics.Raycast(rayOrigin, (transform.forward - transform.up), out hit, clampRayDistance))
                {
                    Debug.DrawRay(rayOrigin, (transform.forward - transform.up) * clampRayDistance, Color.grey);

                    if (hit.collider.CompareTag(tags.tags[1]) ||
                        hit.collider.CompareTag(tags.tags[2]) ||
                        hit.collider.CompareTag(tags.tags[3]))
                        groundClamp = 0;
                }
                else if (Physics.Raycast(rayOrigin, (-transform.right - transform.up), out hit, clampRayDistance))
                {
                    Debug.DrawRay(rayOrigin, (-transform.right - transform.up) * clampRayDistance, Color.grey);

                    if (hit.collider.CompareTag(tags.tags[1]) ||
                        hit.collider.CompareTag(tags.tags[2]) ||
                        hit.collider.CompareTag(tags.tags[3]))
                        groundClamp = 0;
                }
                else
                {
                    groundClamp = groundClampDefault;
                }
            }
            else if (vAxis < 0 && hAxis > 0)//South East
            {
                RaycastHit hit;

                if (Physics.Raycast(rayOrigin, (-transform.forward - transform.up), out hit, clampRayDistance))
                {
                    Debug.DrawRay(rayOrigin, (-transform.forward - transform.up) * clampRayDistance, Color.grey);

                    if (hit.collider.CompareTag(tags.tags[1]) ||
                        hit.collider.CompareTag(tags.tags[2]) ||
                        hit.collider.CompareTag(tags.tags[3]))
                        groundClamp = 0;
                }
                else if (Physics.Raycast(rayOrigin, (transform.right - transform.up), out hit, clampRayDistance))
                {
                    Debug.DrawRay(rayOrigin, (transform.right - transform.up) * clampRayDistance, Color.grey);

                    if (hit.collider.CompareTag(tags.tags[1]) ||
                        hit.collider.CompareTag(tags.tags[2]) ||
                        hit.collider.CompareTag(tags.tags[3]))
                        groundClamp = 0;
                }
                else
                {
                    groundClamp = groundClampDefault;
                }
            }
            else if (vAxis < 0 && hAxis < 0)//South West
            {
                RaycastHit hit;

                if (Physics.Raycast(rayOrigin, (-transform.forward - transform.up), out hit, clampRayDistance))
                {
                    Debug.DrawRay(rayOrigin, (-transform.forward - transform.up) * clampRayDistance, Color.grey);

                    if (hit.collider.CompareTag(tags.tags[1]) ||
                        hit.collider.CompareTag(tags.tags[2]) ||
                        hit.collider.CompareTag(tags.tags[3]))
                        groundClamp = 0;
                }
                else if (Physics.Raycast(rayOrigin, (-transform.right - transform.up), out hit, clampRayDistance))
                {
                    Debug.DrawRay(rayOrigin, (-transform.right - transform.up) * clampRayDistance, Color.grey);

                    if (hit.collider.CompareTag(tags.tags[1]) ||
                        hit.collider.CompareTag(tags.tags[2]) ||
                        hit.collider.CompareTag(tags.tags[3]))
                        groundClamp = 0;
                }
                else
                {
                    groundClamp = groundClampDefault;
                }
            }
        }
        else
        {
            groundClamp = 0;
        }
    }
    #endregion

    #region triggers
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag(tags.tags[1]) ||
            collision.gameObject.CompareTag(tags.tags[2]) ||
            collision.gameObject.CompareTag(tags.tags[3]))
        {
            isAbleToClamp = true;
            isJumped = false;
            isSprintJumped = false;
        }
    }

    /*private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag(tags.tags[1]) ||
            collision.gameObject.CompareTag(tags.tags[2]) ||
            collision.gameObject.CompareTag(tags.tags[3]))
        {
            
        }
    }*/
    #endregion
}