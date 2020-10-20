using System.Linq;
using UnityEngine;
using System.Collections.Generic;

namespace Adrenak.Tork {
    public class Vehicle : MonoBehaviour {
        [SerializeField] new Rigidbody rigidbody;
        public Rigidbody Rigidbody { get { return rigidbody; } }

        [Header("Core Components")]
        [SerializeField] Ackermann ackermann;
        public Ackermann Ackermann { get { return ackermann; } }

        [SerializeField] Steering steering;
        public Steering Steering { get { return steering; } }

        [SerializeField] Motor motor;
        public Motor Motor { get { return motor; } }

        [SerializeField] Brakes brake;
        public Brakes Brake { get { return brake; } }

        [Header("Add On Components (Populated on Awake)")]
        [SerializeField] List<VehicleAddOn> addOns;
        public List<VehicleAddOn> AddOns { get { return addOns; } }

        void Awake() {
            addOns = GetComponentsInChildren<VehicleAddOn>().ToList();
        }

        public T GetAddOn<T>() where T : VehicleAddOn {
            foreach (var addOn in addOns)
                if (addOn is T)
                    return addOn as T;
            return null;
        }

        // Agent observations.

        public int CollisionCount { get; set; }
        public Vector3 Inclination => new Vector3(transform.right.y, transform.up.y, transform.forward.y);
        public Vector3 Velocity => Localize(rigidbody.velocity);
        public Vector3 AngularVelocity => Localize(rigidbody.angularVelocity);

        private Vector3 Localize(Vector3 v)
        {
            return transform.InverseTransformVector(v);
        }

        private void OnCollisionEnter(Collision collision)
        {
            CollisionCount++;
        }

        private void OnCollisionStay(Collision collision)
        {
            CollisionCount++;
        }
    }
}
