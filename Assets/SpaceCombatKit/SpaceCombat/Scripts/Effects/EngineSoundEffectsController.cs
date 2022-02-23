using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace VSX.UniversalVehicleCombat.Space
{
    /// <summary>
    /// Controls the sound effects for a space vehicle engine.
    /// </summary>
    public class EngineSoundEffectsController : MonoBehaviour
    {

        [Header("References")]

        [SerializeField]
        protected VehicleEngines3D spaceVehicleEngines;

        [SerializeField]
        protected AudioSource spaceEnginesCruisingAudio;

        [SerializeField]
        protected AudioSource spaceEnginesBoostAudio;

        [Header ("Volume")]

        [SerializeField]
        protected float maxCruisingAudioVolume = 1;

        [SerializeField]
        protected AnimationCurve throttleToEngineVolumeCurve = AnimationCurve.Linear(0f, 0.2f, 1f, 1f);

        [SerializeField]
        protected float lateralThrustContribution = 0.25f;

        [SerializeField]
        protected float verticalThrustContribution = 0.25f;

        [SerializeField]
        protected float forwardThrustContribution = 0.5f;

        [SerializeField]
        protected float maxBoostAudioVolume = 1;
        protected float boostTargetVolume = 0;

        [SerializeField]
        protected float boostPositiveVolumeChangeSpeed = 7;

        [SerializeField]
        protected float boostNegativeVolumeChangeSpeed = 3;


        private void Awake()
        {
            if (spaceVehicleEngines != null)
            {
                spaceVehicleEngines.onEnginesActivated.AddListener(OnEnginesActivated);
                spaceVehicleEngines.onEnginesDeactivated.AddListener(OnEnginesDeactivated);
            }

            UpdateAudio();
        }

        protected virtual void OnEnginesActivated()
        {
            if (spaceEnginesCruisingAudio != null) 
            {
                spaceEnginesCruisingAudio.volume = 0;
                spaceEnginesCruisingAudio.loop = true;
                spaceEnginesCruisingAudio.Play();
            }

            if (spaceEnginesBoostAudio != null)
            {
                spaceEnginesBoostAudio.volume = 0;
                spaceEnginesBoostAudio.loop = true;
                spaceEnginesBoostAudio.Play();
            }
        }

        protected virtual void OnEnginesDeactivated()
        {
            if (spaceEnginesCruisingAudio != null)
            {
                spaceEnginesCruisingAudio.Stop();
            }

            if (spaceEnginesBoostAudio != null)
            {
                spaceEnginesBoostAudio.Stop();
            }
        }

        protected void UpdateAudio()
        {
            if (spaceVehicleEngines != null && spaceVehicleEngines.EnginesActivated)
            {
                if (spaceEnginesCruisingAudio != null)
                {
                    // Calculate engine volume from thrust along each axis
                    float engineVolumeAmount = lateralThrustContribution * Mathf.Abs(spaceVehicleEngines.MovementInputs.x) +
                                        verticalThrustContribution * Mathf.Abs(spaceVehicleEngines.MovementInputs.y) +
                                        forwardThrustContribution * Mathf.Abs(spaceVehicleEngines.MovementInputs.z);

                    spaceEnginesCruisingAudio.volume = throttleToEngineVolumeCurve.Evaluate(engineVolumeAmount) * maxCruisingAudioVolume;
                }

                // Do boost audio
                if (spaceEnginesBoostAudio != null)
                {
                    boostTargetVolume = Mathf.Clamp(spaceVehicleEngines.BoostInputs.magnitude, 0f, 1f);

                    float boostVolumeChangeSpeed = spaceEnginesBoostAudio.volume < boostTargetVolume ? boostPositiveVolumeChangeSpeed : boostNegativeVolumeChangeSpeed;
                    spaceEnginesBoostAudio.volume = Mathf.Lerp(spaceEnginesBoostAudio.volume, boostTargetVolume, boostVolumeChangeSpeed * Time.deltaTime);
                }
            }
        }

        // Called every frame
        protected virtual void Update()
        {
            UpdateAudio();
        }
    }
}