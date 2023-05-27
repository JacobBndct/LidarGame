using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Windows;

public class PlayerMove : MonoBehaviour
{
    [Header("Player Movement Parameters")]
    [SerializeField] float movementSpeed;
    
    Vector2 playerDirection;
    Rigidbody playerRigidbody;

    void Awake()
    {
        playerRigidbody = GetComponent<Rigidbody>();
        playerDirection = Vector2.zero;
    }

    void FixedUpdate()
    {
        MovePlayer();
    }

    public void PlayerMovement(InputAction.CallbackContext context)
    {
        playerDirection = context.ReadValue<Vector2>();
    }

    /// <summary>
    /// Update the player's velocity according to the recorded input of the user
    /// </summary>
    void MovePlayer()
    {
        Vector3 playerMovement = new Vector3(playerDirection.x * movementSpeed * Time.fixedDeltaTime, playerRigidbody.velocity.y, playerDirection.y * movementSpeed * Time.fixedDeltaTime);
        playerRigidbody.velocity = transform.TransformDirection(playerMovement);
    }
}
