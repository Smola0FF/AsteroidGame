using UnityEngine;
using UnityEngine.UI;
using System;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;


namespace VSX.UniversalVehicleCombat
{

    /// <summary>
    /// This class provides an example control script for a space fighter.
    /// </summary>
    public class PlayerInput_InputSystem_CharacterControls : VehicleInput
    {

        [Header("Settings")]

        protected SCKInputAsset input;
        protected Vector2 movement;
        protected float run;

        protected FirstPersonCharacterController characterController;


        private void OnEnable()
        {
            input.Enable();
        }

        private void OnDisable()
        {
            input.Disable();
        }

        protected override void Awake()
        {
            base.Awake();

            input = new SCKInputAsset();

            // Steering
            input.CharacterControls.Move.performed += ctx => movement = ctx.ReadValue<Vector2>();
            input.CharacterControls.Jump.performed += ctx => Jump();
            input.CharacterControls.Run.performed += ctx => run = ctx.ReadValue<float>();
        }


        /// <summary>
        /// Initialize this input script with a vehicle.
        /// </summary>
        /// <param name="vehicle">The vehicle.</param>
        /// <returns>Whether initialization succeeded</returns>
        protected override bool Initialize(Vehicle vehicle)
        {

            characterController = vehicle.GetComponent<FirstPersonCharacterController>();
            if (characterController == null)
            {
                if (debugInitialization)
                {
                    Debug.LogWarning(GetType().Name + " failed to initialize - the required " + characterController.GetType().Name + " component was not found on the vehicle.");
                }
                return false;
            }

            if (debugInitialization)
            {
                Debug.Log(GetType().Name + " successfully initialized.");
            }

            return true;
        }


        protected virtual void Jump()
        {
            if (initialized)
            {
                characterController.Jump();
            }
        }



        // Update is called once per frame
        protected override void InputUpdate()
        {

            // Moving
            float horizontal = movement.x;
            float forward = movement.y;

            characterController.SetMovementInputs(horizontal, 0, forward);

            characterController.SetRunning(run > 0.9f);

        }

    }
}