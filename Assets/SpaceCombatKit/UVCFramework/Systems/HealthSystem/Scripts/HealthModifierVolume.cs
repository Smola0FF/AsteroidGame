using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VSX.UniversalVehicleCombat
{
    /// <summary>
    /// This component represents a 3D space (represented by a trigger collider) that causes a change in health to objects that enter it.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class HealthModifierVolume : MonoBehaviour
    {
        [System.Serializable]
        public class HealthTypeChangeRate
        {
            [Tooltip("The Health Type.")]
            public HealthType healthType;

            [Tooltip("The change rate (either per second, or applied in a single instance, depending on whether Continuous Over Time is checked).")]
            public float changeRate;
        }

        [Tooltip("The type of health change that this volume will cause.")]
        public HealthModifierType healthModifierType;

        [Tooltip("The change rates for different health types.")]
        public List<HealthTypeChangeRate> healthTypeChangeRates = new List<HealthTypeChangeRate>();

        [Tooltip("If True, the health change is applied per second for as long as the object is inside the trigger collider. If False, the health change is applied once upon entering the trigger collider.")]
        [SerializeField]
        protected bool continuousPerSecond = true;


        // Called when the component is first added to a gameobject or reset in the inspector.
        protected virtual void Reset()
        {
            // Check/add rigidbody
            if (GetComponent<Rigidbody>() == null)
            {
                Rigidbody m_Rigidbody = gameObject.AddComponent(typeof(Rigidbody)) as Rigidbody;
                m_Rigidbody.isKinematic = true;
            }

            // Check/add collider
            if (GetComponent<Collider>() == null)
            {
                SphereCollider addedCollider = gameObject.AddComponent(typeof(SphereCollider)) as SphereCollider;
                addedCollider.radius = 100;
            }

            // Make sure the collider is a trigger collider
            Collider col = GetComponent<Collider>();
            col.isTrigger = true;
        }


        // Called when another collider enters the trigger collider
        protected virtual void OnTriggerEnter(Collider other)
        {
            if (continuousPerSecond) return;

            DamageReceiver damageReceiver = other.GetComponent<DamageReceiver>();
            if (damageReceiver != null)
            {
                for (int i = 0; i < healthTypeChangeRates.Count; ++i)
                {
                    if (healthTypeChangeRates[i].healthType == damageReceiver.HealthType)
                    {
                        float change = healthTypeChangeRates[i].changeRate;

                        if (change < 0)
                        {
                            damageReceiver.Damage(change, damageReceiver.GetClosestPoint(transform.position), healthModifierType, transform);
                        }
                        else if (change > 0)
                        {
                            damageReceiver.Heal(change, damageReceiver.GetClosestPoint(transform.position), healthModifierType, transform);
                        }

                        return;
                    }
                }
            }
        }

        // Called every frame that another collider is within the trigger collider
        protected virtual void OnTriggerStay(Collider other)
        {
            if (!continuousPerSecond) return;

            DamageReceiver damageReceiver = other.GetComponent<DamageReceiver>();
            if (damageReceiver != null)
            {
                for (int i = 0; i < healthTypeChangeRates.Count; ++i)
                {
                    if (healthTypeChangeRates[i].healthType == damageReceiver.HealthType)
                    {
                        float change = healthTypeChangeRates[i].changeRate * Time.deltaTime;

                        if (change < 0)
                        {
                            damageReceiver.Damage(change, damageReceiver.GetClosestPoint(transform.position), healthModifierType, transform);
                        }
                        else if (change > 0)
                        {
                            damageReceiver.Heal(change, damageReceiver.GetClosestPoint(transform.position), healthModifierType, transform);
                        }
                        
                        return;
                    }
                }
            }
        }
    }
}
