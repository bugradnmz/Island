using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSounds : MonoBehaviour
{

    [SerializeField] private float rayLength;  //ChangeFootStep() referansı
    [SerializeField] private float effectsVolume;

    [SerializeField] private AudioClip footStepSand;
    [SerializeField] private AudioClip footStepGrass;
    [SerializeField] private AudioClip footStepWater;
    [SerializeField] private AudioClip footStepConcrete;
    [SerializeField] private AudioClip footStepMetal;

    [SerializeField] private AudioClip effectCrouch;
    [SerializeField] private AudioClip effectGetUp;
    [SerializeField] private AudioClip effectBoneCrush;
    [SerializeField] private AudioClip effectBreathing;
    [SerializeField] private AudioClip effectJump;
    [SerializeField] private AudioClip[] effectHurt;

    private AudioClip currentClip;

    private AudioSource[] audioSource; //audioSource[0] ayak sesleri, audioSource[1] efektler
    private PlayerMovement playerMovement;
    private Tags tags;
    private TerrainDetector terrainDetector;

    private void Awake()
    {
        terrainDetector = new TerrainDetector();
    }

    private void Start()
    {
        playerMovement = GetComponent<PlayerMovement>();
        tags = GameObject.Find("Game Manager").GetComponent<Tags>();
        audioSource = GetComponents<AudioSource>();
    }

    private void Update()
    {
        ChangeFootStep();
        FootSteps();
    }

    #region function & methods
    private void ChangeFootStep()
    {
        int terrainTextureIndex = terrainDetector.GetActiveTerrainTextureIdx(transform.position);
        
        RaycastHit hit;
        
        if(Physics.Raycast(transform.position, Vector3.down, out hit, rayLength))
        {
            Debug.DrawRay(transform.position, Vector3.down * rayLength, Color.red);

            if (hit.collider.CompareTag(tags.tags[0]))
            {
                audioSource[0].clip = footStepWater;
            }            
            else if (hit.collider.CompareTag(tags.tags[2]))
            {
                audioSource[0].clip = footStepConcrete;                
            }
            else if (hit.collider.CompareTag(tags.tags[3]))
            {
                audioSource[0].clip = footStepMetal;
            }
            else if (hit.collider.CompareTag(tags.tags[1]))
            {
                if (terrainTextureIndex == 0 || terrainTextureIndex == 1 || terrainTextureIndex == 2 || terrainTextureIndex == 5)
                {
                    audioSource[0].clip = footStepSand;
                }
                else if (terrainTextureIndex == 3 || terrainTextureIndex == 4)
                {
                    audioSource[0].clip = footStepGrass;
                }
            }
        }        
        currentClip = audioSource[0].clip;
    }

    private void FootSteps()
    {
        if (playerMovement.IsGrounded() && (playerMovement.hAxis != 0 || playerMovement.vAxis != 0) && audioSource[0].isPlaying == false)
        {
            //WALK
            if (playerMovement.movementType == "walk")
            {
                if (currentClip == footStepSand) //Sand
                {
                    audioSource[0].volume = Random.Range(0.075f, 0.125f);
                    audioSource[0].pitch = Random.Range(1.1f, 1.3f);
                }else if (currentClip == footStepGrass)//Grass
                {
                    audioSource[0].volume = Random.Range(0.25f, 0.35f);
                    audioSource[0].pitch = Random.Range(0.6f, 0.8f);
                }
                else if (currentClip == footStepWater)//Water
                {
                    audioSource[0].volume = Random.Range(0.5f, 0.6f);
                    audioSource[0].pitch = Random.Range(0.9f, 1f);
                }
                else if (currentClip == footStepConcrete)//Concrete
                {
                    audioSource[0].volume = Random.Range(0.3f, 0.4f);
                    audioSource[0].pitch = Random.Range(0.95f, 1.2f);
                }
                else if (currentClip == footStepMetal)//Metal
                {
                    audioSource[0].volume = Random.Range(0.2f, 0.3f);
                    audioSource[0].pitch = Random.Range(0.9f, 1.2f);
                }
            }
            //SPRINT
            else if (playerMovement.movementType == "sprint")
            {
                if (currentClip == footStepSand)//Sand
                {
                    audioSource[0].volume = Random.Range(0.225f, 0.3f);
                    audioSource[0].pitch = Random.Range(1.9f, 2.4f);
                }
                else if (currentClip == footStepGrass)//Grass
                {
                    audioSource[0].volume = Random.Range(0.60f, 0.75f);
                    audioSource[0].pitch = Random.Range(1.2f, 1.5f);
                }
                else if (currentClip == footStepWater)//Water
                {
                    audioSource[0].volume = Random.Range(0.9f, 1f);
                    audioSource[0].pitch = Random.Range(1.5f, 1.7f);
                }
                else if (currentClip == footStepConcrete)//Concrete
                {
                    audioSource[0].volume = Random.Range(0.85f, 1f);
                    audioSource[0].pitch = Random.Range(1.65f, 2.1f);
                }
                else if (currentClip == footStepMetal)//Metal
                {
                    audioSource[0].volume = Random.Range(0.75f, 0.85f);
                    audioSource[0].pitch = Random.Range(1.65f, 1.9f);
                }
            }
            //RUN
            else if (playerMovement.movementType == "run")
            {
                if (currentClip == footStepSand)//Sand
                {
                    audioSource[0].volume = Random.Range(0.15f, 0.2f);
                    audioSource[0].pitch = Random.Range(1.5f, 1.8f);
                }
                else if (currentClip == footStepGrass)//Grass
                {
                    audioSource[0].volume = Random.Range(0.40f, 0.55f);
                    audioSource[0].pitch = Random.Range(0.8f, 1.1f);
                }
                else if (currentClip == footStepWater)//Water
                {
                    audioSource[0].volume = Random.Range(0.7f, 0.8f);
                    audioSource[0].pitch = Random.Range(1.1f, 1.2f);
                }
                else if (currentClip == footStepConcrete)//Concrete
                {
                    audioSource[0].volume = Random.Range(0.6f, 0.8f);
                    audioSource[0].pitch = Random.Range(1.25f, 1.55f);
                }
                else if (currentClip == footStepMetal)//Metal
                {
                    audioSource[0].volume = Random.Range(0.4f, 0.5f);
                    audioSource[0].pitch = Random.Range(1.1f, 1.55f);
                }
            }
            else
            {
                audioSource[0].volume = 0.0f;
            }

            audioSource[0].Play();
        }
    }

    public void Crouch() //PlayerMovement
    {
        audioSource[1].volume = effectsVolume * 0.1f;
        audioSource[1].PlayOneShot(effectCrouch);
    }

    public void GetUp() //PlayerMovement
    {
        audioSource[1].volume = effectsVolume * 0.1f;
        audioSource[1].PlayOneShot(effectGetUp);
    }

    public void BoneCrush() //TakeDamage
    {
        audioSource[1].volume = effectsVolume;
        audioSource[1].PlayOneShot(effectBoneCrush);
    }

    public void Breathing() //PlayerStats
    {
        audioSource[1].volume = effectsVolume;
        audioSource[1].PlayOneShot(effectBreathing);
    }

    public void Jump() //PlayerStats
    {
        audioSource[1].volume = effectsVolume;
        audioSource[1].PlayOneShot(effectJump);
    }

    public void TakeDamage() //TakeDamage
    {
        int random = Random.Range(0, effectHurt.Length);
        
        audioSource[1].volume = effectsVolume;
        audioSource[1].PlayOneShot(effectHurt[random]);     
    }
    #endregion

    #region triggers
    private void OnCollisionEnter(Collision collision)
    {
        if((collision.gameObject.CompareTag(tags.tags[0]) ||
            collision.gameObject.CompareTag(tags.tags[1]) ||
            collision.gameObject.CompareTag(tags.tags[2]) ||
            collision.gameObject.CompareTag(tags.tags[3])) && !playerMovement.isMoving && currentClip != null)
        {
            audioSource[0].PlayOneShot(currentClip);
        }
    }
    #endregion
}
