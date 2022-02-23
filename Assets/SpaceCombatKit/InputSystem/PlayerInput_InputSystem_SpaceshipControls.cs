using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace VSX.UniversalVehicleCombat
{
    /// <summary>
    /// Input script for controlling the steering and movement of a space fighter vehicle.
    /// </summary>
    public class PlayerInput_InputSystem_SpaceshipControls : VehicleInput
    {

        protected SCKInputAsset input;

        protected float acceleration, roll;
        protected Vector2 steering, strafing;

        [Header("Control Scheme")]

        [Tooltip("Whether the vehicle should yaw when rolling.")]
        [SerializeField]
        protected bool linkYawAndRoll = false;

        [Tooltip("How much the vehicle should yaw when rolling.")]
        [SerializeField]
        protected float yawRollRatio = 1;


        [Header("Auto Roll")]

        [SerializeField]
        protected bool autoRollEnabled = true;

        [SerializeField]
        protected float autoRollStrength = 0.04f;

        [SerializeField]
        protected float maxAutoRoll = 0.2f;

        protected float lastRollTime;



        [Header("Mouse Steering")]

        [Tooltip("Whether the mouse position should control the steering.")]
        [SerializeField]
        protected bool mouseSteeringEnabled = true;

        [SerializeField]
        protected MouseSteeringType mouseSteeringType;
        public MouseSteeringType MouseSteeringType
        {
            get { return mouseSteeringType; }
            set { mouseSteeringType = value; }
        }

        /// <summary>
        /// Called to toggle the mouse input in this script.
        /// </summary>
        /// <param name="setEnabled">Whether the mouse input is to be enabled.</param>
        public virtual void SetMouseInputEnabled(bool setEnabled)
        {
            mouseSteeringEnabled = setEnabled;
        }

        [Tooltip("Invert the vertical mouse input.")]
        [SerializeField]
        protected bool mouseVerticalInverted = false;

        [Tooltip("Invert the horizontal mouse input.")]
        [SerializeField]
        protected bool mouseHorizontalInverted = false;


        [Header("Mouse Screen Position Settings")]

        [Tooltip("The fraction of the viewport (based on the screen width) around the screen center inside which the mouse position does not affect the ship steering.")]
        [SerializeField]
        protected float mouseDeadRadius = 0.1f;

        [Tooltip("How far the mouse reticule is allowed to get from the screen center.")]
        [SerializeField]
        protected float maxReticleDistanceFromCenter = 0.475f;

        [SerializeField]
        protected float reticleMovementSpeed = 20f;

        [Tooltip("How much the ship pitches (local X axis rotation) based on the mouse distance from screen center.")]
        [SerializeField]
        protected AnimationCurve mousePositionInputCurve = AnimationCurve.Linear(0, 0, 1, 1);

        [SerializeField]
        protected bool centerCursorOnInputEnabled = true;


        [Header("Mouse Delta Position Settings")]

        [SerializeField]
        protected float mouseDeltaPositionSensitivity = 0.75f;

        [SerializeField]
        protected AnimationCurve mouseDeltaPositionInputCurve = AnimationCurve.Linear(0, 0, 1, 1);

        [Header("Keyboard Steering")]

        [Tooltip("Invert the pitch (local X rotation) input.")]
        [SerializeField]
        protected bool keyboardVerticalInverted = false;

        [Tooltip("Whether keyboard steering is enabled.")]
        [SerializeField]
        protected bool keyboardSteeringEnabled = false;

        [Header("Throttle")]

        [SerializeField]
        protected bool setThrottle = false;

        [SerializeField]
        protected float throttleSensitivity = 1;

        // The rotation, translation and boost inputs that are updated each frame
        protected Vector3 steeringInputs = Vector3.zero;
        protected Vector3 movementInputs = Vector3.zero;
        protected Vector3 boostInputs = Vector3.zero;

        protected bool steeringEnabled = true;

        protected bool movementEnabled = true;

        [Header("Boost")]

        [SerializeField]
        protected float boostChangeSpeed = 3;
        protected Vector3 boostTarget = Vector3.zero;

        // Reference to the engines component on the current vehicle
        protected VehicleEngines3D spaceVehicleEngines;

        protected HUDCursor hudCursor;
        protected Vector3 reticuleViewportPosition = new Vector3(0.5f, 0.5f, 0);

        public bool forceUseMouse = false;


        protected override void Awake()
        {
            base.Awake();

            input = new SCKInputAsset();

            // Steering
            input.SpacefighterControls.Steer.performed += ctx => steering = ctx.ReadValue<Vector2>();

            // Strafing
            input.SpacefighterControls.Strafe.performed += ctx => strafing = ctx.ReadValue<Vector2>();
            
            // Roll
            input.SpacefighterControls.Roll.performed += ctx => GetRollInput(ctx.ReadValue<float>());

            // Acceleration
            input.SpacefighterControls.Throttle.performed += ctx => acceleration = ctx.ReadValue<float>();

            // Boost
            input.SpacefighterControls.Boost.performed += ctx => boostTarget.z = ctx.ReadValue<float>();

        }

        protected void GetRollInput(float rollAmount)
        {
            roll = rollAmount;
        }

        private void OnEnable()
        {
            input.Enable();
        }

        private void OnDisable()
        {
            input.Disable();
        }

        bool CanRunInput()
        {
            return (initialized && inputEnabled && inputUpdateConditions.ConditionsMet);
        }

        /// <summary>
        /// Initialize this input script with a vehicle.
        /// </summary>
        /// <param name="vehicle">The new vehicle.</param>
        /// <returns>Whether the input script succeeded in initializing.</returns>
        protected override bool Initialize(Vehicle vehicle)
        {

            if (!base.Initialize(vehicle)) return false;

            // Clear dependencies
            spaceVehicleEngines = null;

            // Make sure the vehicle has a space vehicle engines component
            spaceVehicleEngines = vehicle.GetComponent<VehicleEngines3D>();

            hudCursor = vehicle.GetComponentInChildren<HUDCursor>();

            if (spaceVehicleEngines == null)
            {
                if (debugInitialization)
                {
                    Debug.LogWarning(GetType().Name + " failed to initialize - the required " + spaceVehicleEngines.GetType().Name + " component was not found on the vehicle.");
                }
                return false;
            }

            if (debugInitialization)
            {
                Debug.Log(GetType().Name + " component successfully initialized.");
            }

            return true;
        }


        public override void EnableInput()
        {
            base.EnableInput();

            if(centerCursorOnInputEnabled && hudCursor != null)
            {
                hudCursor.CenterCursor();
            }
        }


        /// <summary>
        /// Stop the input.
        /// </summary>
        public override void DisableInput()
        {

            base.DisableInput();

            // Reset the space vehicle engines to idle
            if (spaceVehicleEngines != null)
            {
                // Set steering to zero
                steeringInputs = Vector3.zero;
                spaceVehicleEngines.SetSteeringInputs(steeringInputs);

                // Set movement to zero
                movementInputs = Vector3.zero;
                spaceVehicleEngines.SetMovementInputs(movementInputs);

                // Set boost to zero
                boostTarget = Vector3.zero;
                boostInputs = Vector3.zero;
                spaceVehicleEngines.SetBoostInputs(boostInputs);
            }
        }

        /// <summary>
        /// Enable steering input.
        /// </summary>
        public virtual void EnableSteering()
        {
            steeringEnabled = true;
        }


        /// <summary>
        /// Disable steering input.
        /// </summary>
        /// <param name="clearCurrentValues">Whether to clear current steering values.</param>
        public virtual void DisableSteering(bool clearCurrentValues)
        {
            steeringEnabled = false;

            if (clearCurrentValues)
            {
                // Set steering to zero
                steeringInputs = Vector3.zero;
                spaceVehicleEngines.SetSteeringInputs(steeringInputs);
            }
        }


        /// <summary>
        /// Enable movement input.
        /// </summary>
        public virtual void EnableMovement()
        {
            movementEnabled = true;
        }

        /// <summary>
        /// Disable the movement input.
        /// </summary>
        /// <param name="clearCurrentValues">Whether to clear current throttle values.</param>
        public virtual void DisableMovement(bool clearCurrentValues)
        {
            movementEnabled = false;

            if (clearCurrentValues)
            {
                // Set movement to zero
                movementInputs = Vector3.zero;
                spaceVehicleEngines.SetMovementInputs(movementInputs);

                // Set boost to zero
                boostTarget = Vector3.zero;
                boostInputs = Vector3.zero;
                spaceVehicleEngines.SetBoostInputs(boostInputs);
            }
        }


        protected void UpdateReticulePosition(Vector3 mouseDelta)
        {
            if (mouseSteeringType == MouseSteeringType.ScreenPosition)
            {

                // Add the delta 
                reticuleViewportPosition += new Vector3(mouseDelta.x / Screen.width, mouseDelta.y / Screen.height, 0);

                // Center it
                Vector3 centeredReticuleViewportPosition = reticuleViewportPosition - new Vector3(0.5f, 0.5f, 0);

                // Prevent distortion before clamping
                centeredReticuleViewportPosition.x *= (float)Screen.width / Screen.height;

                // Clamp
                centeredReticuleViewportPosition = Vector3.ClampMagnitude(centeredReticuleViewportPosition, maxReticleDistanceFromCenter);

                // Convert back to proper viewport
                centeredReticuleViewportPosition.x /= (float)Screen.width / Screen.height;

                reticuleViewportPosition = centeredReticuleViewportPosition + new Vector3(0.5f, 0.5f, 0);

            }
            else if (mouseSteeringType == MouseSteeringType.DeltaPosition)
            {
                reticuleViewportPosition = new Vector3(0.5f, 0.5f, 0);
            }
        }


        // Do mouse steering
        protected virtual void MouseSteeringUpdate()
        {

            Vector3 inputs = Vector3.zero;

            if (mouseSteeringType == MouseSteeringType.ScreenPosition)
            {
                Vector3 centeredViewportPos = reticuleViewportPosition - new Vector3(0.5f, 0.5f, 0);

                centeredViewportPos.x *= (float)Screen.width / Screen.height;

                float amount = Mathf.Max(centeredViewportPos.magnitude - mouseDeadRadius, 0) / (maxReticleDistanceFromCenter - mouseDeadRadius);

                centeredViewportPos.x /= (float)Screen.width / Screen.height;

                inputs = mousePositionInputCurve.Evaluate(amount) * centeredViewportPos.normalized;
            }
            else if (mouseSteeringType == MouseSteeringType.DeltaPosition)
            {
                inputs = mouseDeltaPositionSensitivity * input.SpacefighterControls.MouseDelta.ReadValue<Vector2>();
                inputs = Mathf.Clamp(mouseDeltaPositionInputCurve.Evaluate(inputs.magnitude), 0, 1) * inputs.normalized;
            }

            inputs.x *= (mouseHorizontalInverted ? -1 : 1);

            inputs.y *= (mouseVerticalInverted ? -1 : 1);

            // Update pitch
            Pitch(-inputs.y);

            // Linked yaw and roll
            if (linkYawAndRoll)
            {
                Roll(-inputs.x);
                Yaw(Mathf.Clamp(-steeringInputs.z * yawRollRatio, -1f, 1f));
            }
            // Separate axes
            else
            {
                // Roll
                Roll(roll);

                // Yaw
                Yaw(inputs.x);
            }

            if (hudCursor != null)
            {
                // Position the reticule
                hudCursor.SetViewportPosition(reticuleViewportPosition);
            }

            spaceVehicleEngines.SetSteeringInputs(steeringInputs);

        }


        // Do keyboard steering
        protected virtual void KeyboardSteeringUpdate()
        {
            Vector3 nextSteeringInputs = Vector3.zero;

            // Pitch
            nextSteeringInputs.x = (keyboardVerticalInverted ? -1 : 1) * steering.y;

            // Linked yaw and roll
            if (linkYawAndRoll)
            {
                // Roll
                nextSteeringInputs.z = Mathf.Clamp(steering.x, -1, 1);
                nextSteeringInputs.y = Mathf.Clamp(-nextSteeringInputs.z * yawRollRatio, -1f, 1f);
            }
            // Separate axes
            else
            {
                // Roll
                nextSteeringInputs.z = Mathf.Clamp(roll, -1, 1);

                // Yaw
                nextSteeringInputs.y = Mathf.Clamp(steering.x, -1f, 1f);
            }

            spaceVehicleEngines.SetSteeringInputs(steeringInputs);

        }


        // Do movement
        protected virtual void MovementUpdate()
        {
            // Forward / backward movement
            Vector3 movementInputs = spaceVehicleEngines.MovementInputs;

            if (setThrottle)
            {
                movementInputs.z = acceleration;
            }
            else
            {
                movementInputs.z += acceleration * throttleSensitivity * Time.deltaTime;
            }

            // Left / right movement
            movementInputs.x = strafing.x;

            // Up / down movement
            movementInputs.y = strafing.y;

            spaceVehicleEngines.SetMovementInputs(movementInputs);

            boostInputs = Vector3.Lerp(boostInputs, boostTarget, boostChangeSpeed * Time.deltaTime);
            if (boostInputs.magnitude < 0.0001f) boostInputs = Vector3.zero;
            spaceVehicleEngines.SetBoostInputs(boostInputs);
        }


        protected void Pitch(float pitchAmount)
        {
            steeringInputs.x = Mathf.Clamp(pitchAmount, -1, 1);
        }


        protected void Yaw(float yawAmount)
        {
            steeringInputs.y = Mathf.Clamp(yawAmount, -1, 1);
        }


        public void Roll(float rollAmount)
        {
            steeringInputs.z = Mathf.Clamp(rollAmount, -1, 1);

            if (Mathf.Abs(rollAmount) > 0.0001f)
            {
                lastRollTime = Time.time;
            }
        }


        public void SetBoost(float boostAmount)
        {
            boostTarget = new Vector3(0f, 0f, boostAmount);
        }


        protected void AutoRoll()
        {
            if (Time.time - lastRollTime < 0.5f) return;

            // Project the forward vector down
            Vector3 flattenedFwd = spaceVehicleEngines.transform.forward;
            flattenedFwd.y = 0;
            flattenedFwd.Normalize();

            // Get the right
            Vector3 right = Vector3.Cross(Vector3.up, flattenedFwd);

            float angle = Vector3.Angle(right, spaceVehicleEngines.transform.right);

            if (Vector3.Dot(spaceVehicleEngines.transform.up, right) > 0)
            {
                angle *= -1;
            }

            Vector3 steeringInputs = spaceVehicleEngines.SteeringInputs;
            steeringInputs.z = Mathf.Clamp(angle * -1 * autoRollStrength, -1, 1);

            steeringInputs.z *= maxAutoRoll;

            steeringInputs.z *= 1 - Mathf.Abs(Vector3.Dot(spaceVehicleEngines.transform.forward, Vector3.up));

            spaceVehicleEngines.SetSteeringInputs(steeringInputs);
        }


        protected override void InputUpdate()
        {
            if (!forceUseMouse && Gamepad.current != null)
            {
                if (hudCursor != null)
                {
                    hudCursor.enabled = false;
                    hudCursor.CenterCursor();
                }

                // Update pitch
                Vector3 steeringInputs = Vector3.zero;
                steeringInputs.x = (mouseVerticalInverted ? 1 : -1) * steering.y;

                // Roll
                steeringInputs.z = roll;

                // Yaw
                steeringInputs.y = steering.x;

                steeringInputs = Vector3.ClampMagnitude(steeringInputs, 1);

                spaceVehicleEngines.SetSteeringInputs(steeringInputs);
            }
            else if (Mouse.current != null)
            {
                if (hudCursor != null)
                {
                    hudCursor.enabled = true;
                }

                UpdateReticulePosition(input.SpacefighterControls.MouseDelta.ReadValue<Vector2>());

                MouseSteeringUpdate();
            }
            else
            {
                KeyboardSteeringUpdate();
            }

            MovementUpdate();

            if (autoRollEnabled) AutoRoll();
        }
    }
}