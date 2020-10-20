using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using Adrenak.Tork;
using System;

namespace MBaske
{
    public class DriverAgent : Agent
    {
        public event Action<string> ResetEvent;
        public event Action<string, Stats> StatsEvent;

        public struct Stats
        {
            public float action0;
            public float action1;
            public float speed;
            public float steer;
            public float offset;
            public float orientation;
            public float collisions;
            public float reward;
        }
        private Stats stats;

        private Road road;
        private Vehicle vehicle;
        private TorkWheel[] wheels;
        private Resetter resetter;
        private float updateTime;

        private const float timeout = 5;
        
        public override void Initialize()
        {
            resetter = new Resetter(transform);
            vehicle = GetComponentInChildren<Vehicle>();
            wheels = GetComponentsInChildren<TorkWheel>();
            road = GetComponentInChildren<Road>();
            road.Initialize();
        }

        public override void OnEpisodeBegin()
        {
            ResetAgent();
        }

        private void ResetAgent()
        {
            vehicle.CollisionCount = 0;
            updateTime = Time.time;
            stats = default;
            resetter.Reset();
            road.OnReset();
            
            ResetEvent?.Invoke(name);
        }

        public override void CollectObservations(VectorSensor sensor)
        {
            if (sensor != null)
            {
                sensor.AddObservation(stats.steer);

                float speed = Mathf.Clamp(stats.speed, 0f, 40f);
                sensor.AddObservation(speed / 20f - 1f);

                float angle = Mathf.Clamp(stats.orientation, -45f, 45f);
                sensor.AddObservation(angle / 45f);

                float offset = Mathf.Clamp(stats.offset, -Chunk.RoadExtent, Chunk.RoadExtent);
                sensor.AddObservation(offset / Chunk.RoadExtent);

                sensor.AddObservation(vehicle.Inclination);
                sensor.AddObservation(Normalization.Sigmoid(vehicle.Velocity));
                sensor.AddObservation(Normalization.Sigmoid(vehicle.AngularVelocity));

                sensor.AddObservation(road.GetNormalizedObstacleDistances(60));

                foreach (var dir in road.GetWayPointDirections(5, 16))
                {
                    sensor.AddObservation(dir);
                }
            }
        }

        public override void OnActionReceived(ActionBuffers actionBuffers)
        {
            var actions = actionBuffers.ContinuousActions.Array;

            vehicle.Motor.value = actions[0];
            vehicle.Steering.value = actions[1];

            Vector3 fwd = vehicle.transform.forward;
            Vector3 pos = vehicle.transform.position;

            bool chunkAdded = road.OnUpdate(pos);
            Frame frame = road.AgentFrame;

            stats.action0 = actions[0];
            stats.action1 = actions[1];
            stats.speed = Vector3.Dot(frame.Forward, vehicle.Rigidbody.velocity);
            stats.steer = vehicle.Steering.CrntNormValue;
            stats.offset = frame.LocalPosition(pos).x;
            stats.orientation = Vector3.SignedAngle(frame.Forward, fwd, Vector3.up);
            stats.collisions = vehicle.CollisionCount;
            vehicle.CollisionCount = 0;

            updateTime = chunkAdded ? Time.time : updateTime;
            bool hasTimedOut = Time.time - updateTime > timeout;

            if (hasTimedOut || IsOffMesh())
            {
                AddReward(-1);
                ResetAgent();
            }
            else
            {
                float offRoad = Mathf.Abs(stats.offset) - Chunk.RoadExtent + 1;
                stats.reward = offRoad > 0 ? -offRoad : stats.speed * 0.02f;
                stats.reward -= stats.collisions * 0.5f;
                AddReward(stats.reward);
            }

            StatsEvent?.Invoke(name, stats);
        }

        private bool IsOffMesh()
        {
            int n = 0;
            foreach (var wheel in wheels)
            {
                n += Physics.Raycast(wheel.transform.position, Vector3.down, 10, Road.LayerMask) ? 1 : 0;
            }
            return n < 3;
        }

        public override void Heuristic(float[] actions) 
        {
            actions[0] = Input.GetAxis("Vertical");
            actions[1] = Input.GetAxis("Horizontal");
        }
    }
}

