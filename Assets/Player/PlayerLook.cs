using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerLook : MonoBehaviour
{
    [Header("Camera Movement Parameters")]
    [SerializeField] float horizontalSensitivity, verticalSensitivity;
    [SerializeField] bool invertHorizontal, invertVertical;
    [SerializeField] int cameraRange;

    [Header("Camera Obeject Parameters")]
    [SerializeField] Transform rotatableObjects;
    Rigidbody playerRigidbody;

    void Awake()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        playerRigidbody = GetComponent<Rigidbody>();
    }

    public void PlayerLookAt(InputAction.CallbackContext context)
    {
        Vector2 delta = context.ReadValue<Vector2>();

        HorizontalRotation(delta.x);
        VerticalRotation(delta.y);
    }

    /// <summary>
    /// Updates the x rotation of player
    /// </summary>
    /// <param name="deltaX">Change in x rotation</param>
    void HorizontalRotation(float deltaX)
    {
        float horizontalRotation = deltaX * horizontalSensitivity * Invert(invertHorizontal);
        Quaternion horizontalRotationQuaternion = Quaternion.AngleAxis(horizontalRotation, transform.up) * playerRigidbody.rotation;
        playerRigidbody.MoveRotation(horizontalRotationQuaternion);
    }

    /// <summary>
    /// Updates the y rotation of camera and other rotatable objects
    /// </summary>
    /// <param name="deltaY">Change in y rotation</param>
    void VerticalRotation(float deltaY)
    {
        float verticalRotation = -(deltaY * verticalSensitivity) * Invert(invertVertical);

        float currentVerticalRotation = RelativeToForward(rotatableObjects.localEulerAngles.x);

        float min = -cameraRange - currentVerticalRotation;
        float max = cameraRange - currentVerticalRotation;
        verticalRotation = Mathf.Clamp(verticalRotation, min, max);

        Vector3 verticalRotationVector = new Vector3(verticalRotation, 0f, 0f);
        rotatableObjects.Rotate(verticalRotationVector);
    }

    /// <summary>
    /// Changes the range of a euler angle from 0 - 360 degrees to -180 - 180. This works so that 0 is in the direction of the local forward of the transform.
    /// </summary>
    /// <param name="eulerRotation">Euler angle from 0 - 360 degrees</param>
    /// <returns>Euleur angle from -180 - 180</returns>
    float RelativeToForward(float eulerRotation)
    {
        return (eulerRotation >= 180) ? eulerRotation - 360 : eulerRotation;
    }

    /// <summary>
    /// Inverts equation on true and leaves the same on false
    /// </summary>
    /// <param name="isInverted">bool to determine if should invert</param>
    /// <returns>Returns -1 on true and 1 on false</returns>
    float Invert(bool isInverted)
    {
        return Mathf.Pow(-1, isInverted ? 1 : 0);
    }
}