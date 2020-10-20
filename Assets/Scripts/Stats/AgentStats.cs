using System.Collections.Generic;
using UnityEngine;

namespace MBaske
{
    public class AgentStats : MonoBehaviour
    {
        [SerializeField]
        private int bufferSize = 128;

        private Dictionary<string, StatsBuffer> buffers;

        private void Start()
        {
            DriverAgent agent = GetComponent<DriverAgent>();
            agent.ResetEvent += OnAgentReset;
            agent.StatsEvent += OnAgentStats;

            buffers = new Dictionary<string, StatsBuffer>()
            {
                {  "Action Accelerate", new StatsBuffer(bufferSize, "Normalized") },
                {  "Action Steer", new StatsBuffer(bufferSize, "Normalized") },
                {  "Current Steer", new StatsBuffer(bufferSize, "Normalized") },
                {  "Forward Speed", new StatsBuffer(bufferSize, "Meters/Second") },
                {  "Offset", new StatsBuffer(bufferSize, "Meters") },
                {  "Orientation", new StatsBuffer(bufferSize, "Angle") },
                {  "Collisions", new StatsBuffer(bufferSize, "Count") },
                {  "Reward", new StatsBuffer(bufferSize, "Sum") }
            };

            int i = 0;
            var col = Colors.Palette(buffers.Count);
            var gf = FindObjectOfType<GraphFactory>(true);
            foreach (var kvp in buffers)
            {
                gf.AddGraph(kvp.Key).Add(kvp.Value, col[i++], true);
            }
        }

        private void OnAgentReset(string name)
        {
            foreach (var buffer in buffers.Values)
            {
                buffer.Clear();
            }
        }

        private void OnAgentStats(string name, DriverAgent.Stats stats)
        {
            buffers["Action Accelerate"].Add(stats.action0);
            buffers["Action Steer"].Add(stats.action1);
            buffers["Current Steer"].Add(stats.steer);
            buffers["Forward Speed"].Add(stats.speed);
            buffers["Offset"].Add(stats.offset);
            buffers["Orientation"].Add(stats.orientation);
            buffers["Collisions"].Add(stats.collisions);
            buffers["Reward"].Add(stats.reward);
        }
    }
}
