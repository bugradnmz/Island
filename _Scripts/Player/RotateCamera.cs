using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateCamera : MonoBehaviour
{
    [SerializeField] private float lookSensitivity;
    [SerializeField] private float smoothing;

    private GameObject player;
    private GameObject fpsCamera;
    private Vector2 smoothedVelocity;
    private Vector2 currentLookPos;

    private void Start()
    {
        player = transform.parent.gameObject;
        fpsCamera = GameObject.Find("FPS Camera");
    }

    private void Update()
    {
        Rotate();
    }

    #region function & methods
    private void Rotate()
    {
        float mouseX = Input.GetAxisRaw("Mouse X");
        float mouseY = Input.GetAxisRaw("Mouse Y");

        Vector2 inputValues = new Vector2(mouseX, mouseY);

        inputValues = Vector2.Scale(inputValues, new Vector2(lookSensitivity * smoothing, lookSensitivity * smoothing));

        smoothedVelocity.x = Mathf.Lerp(smoothedVelocity.x, inputValues.x, 1f / smoothing);
        smoothedVelocity.y = Mathf.Lerp(smoothedVelocity.y, inputValues.y, 1f / smoothing);

        currentLookPos += smoothedVelocity;

        currentLookPos.y = Mathf.Clamp(currentLookPos.y, -90f, 90f);
        fpsCamera.transform.localRotation = Quaternion.AngleAxis(-currentLookPos.y, Vector3.right);

        player.transform.localRotation = Quaternion.AngleAxis(currentLookPos.x, player.transform.up);
    }
    #endregion
}
