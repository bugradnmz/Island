using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private float SkyboxRotationSpeed;

    private void Update()
    {
        RotateSkybox();
    }

    #region function & methods
    private void RotateSkybox()
    {
        RenderSettings.skybox.SetFloat("_Rotation", SkyboxRotationSpeed * Time.time); //To set the speed, just multiply the Time.time with whatever amount you want.
    }
    #endregion
}
