using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;


namespace VSX.UniversalVehicleCombat
{

    /// <summary>
    /// This class provides an example control script for a space fighter.
    /// </summary>
    public class PlayerInput_InputSystem_GimballedVehicleControls : VehicleInput
    {

        [Header("Settings")]

        [SerializeField]
        protected float gimbalRotationSpeed = 4;

        protected GimbalController gimbalController;

        protected SCKInputAsset input;
        protected Vector2 rotation;


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
            
            input.GimballedVehicleControls.FreeLook.performed += ctx => rotation = ctx.ReadValue<Vector2>();

        }


        protected override bool Initialize(Vehicle vehicle)
        {
            if (!base.Initialize(vehicle)) return false;
            
            gimbalController = vehicle.GetComponent<GimbalController>();

            if (gimbalController == null)
            {
                if (debugInitialization)
                {
                    Debug.LogWarning(GetType().Name + " failed to initialize - the required " + gimbalController.GetType().Name + " component was not found on the vehicle.");
                }
            }
            else
            {
                if (debugInitialization)
                {
                    Debug.Log(GetType().Name + " successfully initialized.");
                }
            }

            return (gimbalController != null);

        }

        protected override void InputUpdate()
        {

            base.InputUpdate();

            Vector2 nextRotation = rotation;
            if (!Mathf.Approximately(input.GimballedVehicleControls.MouseDelta.ReadValue<Vector2>().magnitude, 0))
            {
                nextRotation = input.GimballedVehicleControls.MouseDelta.ReadValue<Vector2>();
            }

            gimbalController.Rotate(nextRotation.x * gimbalRotationSpeed * Time.deltaTime, -nextRotation.y * gimbalRotationSpeed * Time.deltaTime);

        }

    }
}