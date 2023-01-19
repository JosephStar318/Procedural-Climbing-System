using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputHelper : MonoBehaviour
{
    private PlayerInput playerInput;
    private InputAction jumpAction;
    private InputAction moveAction;
    private InputAction sprintAction;
    private InputAction dropAction;

    public static event Action<InputAction.CallbackContext> OnJump;
    public static event Action<Vector2> OnMove;
    public static event Action<bool> OnSprint;
    public static event Action<InputAction.CallbackContext> OnDrop;
    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();

    }
    private void OnEnable()
    {
        jumpAction = playerInput.actions["Jump"];
        moveAction = playerInput.actions["Move"];
        dropAction = playerInput.actions["Drop"];
        sprintAction = playerInput.actions["Sprint"];

        jumpAction.performed += JumpPressed;
        jumpAction.canceled += JumpReleased;

        dropAction.performed += DropPressed;
        dropAction.canceled += DropReleased;

        moveAction.performed += MovePressed;
        moveAction.canceled += MoveReleased;

        sprintAction.performed += SprintPressed;
        sprintAction.canceled += SprintReleased;

    }
    private void OnDisable()
    {
        jumpAction.performed -= JumpPressed;
        jumpAction.canceled -= JumpReleased;

        dropAction.performed -= DropPressed;
        dropAction.canceled -= DropReleased;

        moveAction.performed -= MovePressed;
        moveAction.canceled -= MoveReleased;

        sprintAction.performed -= SprintPressed;
        sprintAction.canceled -= SprintReleased;
    }
    private void SprintPressed(InputAction.CallbackContext obj)
    {
        OnSprint?.Invoke(obj.ReadValueAsButton());
    }

    private void SprintReleased(InputAction.CallbackContext obj)
    {
        OnSprint?.Invoke(obj.ReadValueAsButton());
    }

  
    private void MovePressed(InputAction.CallbackContext obj)
    {
        OnMove?.Invoke(obj.ReadValue<Vector2>());
    }
    private void MoveReleased(InputAction.CallbackContext obj)
    {
        OnMove?.Invoke(obj.ReadValue<Vector2>());
    }
    private void JumpPressed(InputAction.CallbackContext obj)
    {
        OnJump?.Invoke(obj);
    }
    private void JumpReleased(InputAction.CallbackContext obj)
    {
        OnJump?.Invoke(obj);
    }
    private void DropPressed(InputAction.CallbackContext obj)
    {
        OnDrop?.Invoke(obj);
    }
    private void DropReleased(InputAction.CallbackContext obj)
    {
        OnDrop?.Invoke(obj);
    }
}
