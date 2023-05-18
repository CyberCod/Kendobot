using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine;

public class WalljumpGPTexample : MonoBehaviour
{
    [SerializeField] private float wallJumpForce = 5f;
    [SerializeField] private float wallJumpDirection = 1f;
    private bool isTouchingWall;
    private Vector2 wallNormal;

    public void OnMove(InputAction.CallbackContext context)
    {
        RaycastHit2D wallHit = Physics2D.Raycast(transform.position, Vector2.right * wallJumpDirection, 1f);
        isTouchingWall = wallHit.collider != null;

        if (isTouchingWall)
        {
            // Calculate the wall's normal vector
            wallNormal = wallHit.normal;
        }
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (isTouchingWall && context.performed)
        {
            // Apply the wall jump force along the wall's normal
            Vector2 jumpDirection = new Vector2(-wallNormal.y * wallJumpDirection, wallNormal.x);
            Vector2 jumpForce = jumpDirection * wallJumpForce;
            GetComponent<Rigidbody2D>().velocity = jumpForce;
        }
    }
}

