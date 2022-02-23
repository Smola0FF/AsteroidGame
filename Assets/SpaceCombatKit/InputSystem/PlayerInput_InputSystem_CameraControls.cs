using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using VSX.CameraSystem;

namespace VSX.UniversalVehicleCombat
{
    /// <summary>
    /// Input script for controlling the steering and movement of a space fighter vehicle.
    /// </summary>
    public class PlayerInput_InputSystem_CameraControls : VehicleInput
    {
        
        protected SCKInputAsset input;

        protected CameraTarget cameraTarget;

        protected CameraEntity cameraEntity;

        protected GimbalController cameraGimbalController;

        protected VehicleEngines3D engines;

        [Header("Free Look Mode")]

        [Tooltip("How fast the camera rotates in free look mode.")]
        [SerializeField]
        protected float freeLookSpeed = 0.1f;

        [Tooltip("Whether the engine controls should be disabled in free look mode.")]
        [SerializeField]
        protected bool disableEngineControlsInFreeLookMode = true;

        [Tooltip("Whether any steering inputs should be cleared in free look mode (otherwise ship will continue to turn).")]
        [SerializeField]
        protected bool clearSteeringInputsInFreeLookMode = true;

        [Tooltip("Whether to invert the vertical axis of the camera rotation in free look mode.")]
        [SerializeField]
        protected bool invertFreeLookVertical = false;

        protected bool isFreeLookMode = false;

        [SerializeField]
        protected List<CameraView> freeLookModeCameraViews = new List<CameraView>();


        protected override void Awake()
        {

            base.Awake();

            input = new SCKInputAsset();

            // Cycle the camera view forward
            input.CameraControls.CycleCameraViewForward.performed += ctx => CycleCameraView(true);

            // Cycle the camera view backward
            input.CameraControls.CycleCameraViewBack.performed += ctx => CycleCameraView(false);

            input.CameraControls.FreeLookMode.started += ctx => EnterFreeLookMode();

            input.CameraControls.FreeLookMode.canceled += ctx => ExitFreeLookMode();
        }


        protected override bool Initialize(Vehicle vehicle)
        {

            if (!base.Initialize(vehicle)) return false;

            // Unlink from previous camera target
            if (cameraTarget != null)
            {
                cameraTarget.onCameraEntityTargeting.RemoveListener(OnCameraFollowingVehicle);
            }

            // Link to camera following camera target
            cameraTarget = vehicle.GetComponent<CameraTarget>();
            if (cameraTarget != null)
            {
                // Link to camera already following
                if (cameraTarget.CameraEntity != null)
                {
                    OnCameraFollowingVehicle(cameraTarget.CameraEntity);
                }

                // Link to any new camera following
                cameraTarget.onCameraEntityTargeting.AddListener(OnCameraFollowingVehicle);
            }

            engines = vehicle.GetComponent<VehicleEngines3D>();

            return true;
        }


        protected virtual void OnCameraFollowingVehicle(CameraEntity cameraEntity)
        {
            this.cameraEntity = cameraEntity;

            if (cameraEntity != null)
            {
                cameraGimbalController = cameraEntity.GetComponent<GimbalController>();
            }
        }


        protected virtual void FreeLookModeUpdate()
        {
            if (!isFreeLookMode) return;

            // Free look mode
            if (cameraGimbalController != null)
            {
                cameraGimbalController.Rotate(Mouse.current.delta.x.ReadValue() * freeLookSpeed, -Mouse.current.delta.y.ReadValue() * (invertFreeLookVertical ? -1 : 1) * freeLookSpeed);

                if (engines != null)
                {
                    if (clearSteeringInputsInFreeLookMode) engines.SetSteeringInputs(Vector3.zero);
                    if (disableEngineControlsInFreeLookMode) engines.ControlsDisabled = true;
                }
            }
        }

        protected virtual void EnterFreeLookMode()
        {
            if (isFreeLookMode) return;

            if (freeLookModeCameraViews.Count != 0 && freeLookModeCameraViews.IndexOf(cameraEntity.CurrentView) == -1) return;

            isFreeLookMode = true;
            
            if (engines != null)
            {
                if (disableEngineControlsInFreeLookMode)
                {
                    engines.ControlsDisabled = false;
                }

                if (clearSteeringInputsInFreeLookMode)
                {
                    engines.SetSteeringInputs(Vector3.zero);
                }
            }
        }

        protected virtual void ExitFreeLookMode()
        {
            if (!isFreeLookMode) return;

            isFreeLookMode = false;
            
            if (cameraGimbalController != null)
            {
                cameraGimbalController.ResetGimbal(true);
            }

            if (engines != null)
            {
                if (disableEngineControlsInFreeLookMode)
                {
                    engines.ControlsDisabled = false;
                }
            }
        }


        protected virtual void CycleCameraView(bool forward)
        {
            if (cameraEntity != null)
            {
                cameraEntity.CycleCameraView(forward);
            }
        }


        protected virtual void OnEnable()
        {
            input.Enable();
        }


        protected virtual void OnDisable()
        {
            input.Disable();
        }

        protected override void Update()
        {
            base.Update();
            FreeLookModeUpdate();
        }
    }
}