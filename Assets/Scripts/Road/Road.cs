using System.Collections.Generic;
using UnityEngine;

namespace MBaske
{
    public class Road : MonoBehaviour
    {
        public const int LayerMask = 1 << 8;
        public Frame AgentFrame { get; private set; }

        [SerializeField]
        private int numChunks = 20;
        [SerializeField]
        private int obstacleInterval = 10;

        [SerializeField]
        private Chunk chunkPrefab;
        private Chunk agentChunk;
        private Stack<Chunk> chunkPool;
        private Dictionary<int, Chunk> activeChunks;
 
        private int latestChunkIndex;
        private int agentChunkIndex;
        private int agentFrameIndex;

        private RoadModulator modulator;
        // Global reference frame, progresses stepwise in z-direction.
        private Frame frame;
        // Left to right [], queues closest to farthest.
        private Queue<int>[] obstacleIndices;   

        public void Initialize()
        {
            InitPool();
            activeChunks = new Dictionary<int, Chunk>();
        }

        public void OnReset()
        {
            ClearChunks();
            ClearObstacles();

            frame = new Frame(transform);
            modulator = new RoadModulator();
            
            agentFrameIndex = 0;
            AgentFrame = frame;

            latestChunkIndex = 0;
            for (int i = 0; i < numChunks; i++)
            {
                AddChunk();
            }

            agentChunkIndex = 0;
            agentChunk = activeChunks[0];
        }

        public bool OnUpdate(Vector3 agentPos)
        {
            bool chunkAdded = false;

            if (Physics.Raycast(agentPos, Vector3.down, out RaycastHit hit, 10, LayerMask))
            {
                agentChunk = hit.collider.GetComponent<Chunk>();

                if (agentChunk.Index > agentChunkIndex)
                {
                    RemoveChunk(agentChunkIndex - 2);
                    agentChunkIndex = agentChunk.Index;

                    AddChunk();
                    chunkAdded = true;
                }

                AgentFrame = agentChunk.GetFrame(hit.point);

                if (AgentFrame.Index != agentFrameIndex)
                {
                    agentFrameIndex = AgentFrame.Index;
                    UpdateObstacleIndices();
                }
            }

            return chunkAdded;
        }

        public IEnumerable<Vector3> GetWayPointDirections(int n, int spacing = Chunk.Length)
        {
            for (int i = 1; i <= n; i++)
            {
                yield return GetWayPointDirection(i * spacing);
            }
        }

        public Vector3 GetWayPointDirection(int offset)
        {
            // Debug.DrawRay(GetFrame(offset).Position, Vector3.up, Color.cyan);
            return (GetFrame(offset).Position - AgentFrame.Position).normalized;
        }

        private Frame GetFrame(int offset)
        {
            int frameIndex = agentFrameIndex + offset;
            int chunkIndex = frameIndex / Chunk.Length;
            return activeChunks[chunkIndex].GetFrame(frameIndex);
        }


        // OBSTACLES

        public IEnumerable<float> GetNormalizedObstacleDistances(float range)
        {
            for (int i = 0; i < obstacleIndices.Length; i++)
            {
                float result = 1f;

                if (obstacleIndices[i].Count > 0)
                {
                    int distanceToClosest = obstacleIndices[i].Peek() - agentFrameIndex;

                    if (distanceToClosest < range)
                    {
                        result = distanceToClosest / range * 2f - 1f;
                        // Debug.DrawRay(GetFrame(distanceToClosest).Position, Vector3.up, Color.magenta);
                    }
                }

                yield return result;
            }
        }

        // Used for heuristic:
        // Demo has obstacles on only one side of the road.
        public bool HasObstacles(int range, out float side)
        {
            for (int i = 0; i < obstacleIndices.Length; i++)
            {
                if (obstacleIndices[i].Count > 0)
                {
                    int distanceToClosest = obstacleIndices[i].Peek() - agentFrameIndex;

                    if (distanceToClosest < range)
                    {
                        side = i < Chunk.MaxObstacles / 2 ? -1f : 1f;
                        return true;
                    }
                }
            }

            side = 0;
            return false;
        }

        private void ClearObstacles()
        {
            if (obstacleIndices == null)
            {
                obstacleIndices = new Queue<int>[Chunk.MaxObstacles];

                for (int i = 0; i < obstacleIndices.Length; i++)
                {
                    obstacleIndices[i] = new Queue<int>();
                }
            }
            else
            {
                for (int i = 0; i < obstacleIndices.Length; i++)
                {
                    obstacleIndices[i].Clear();
                }
            }
        }

        private void AddObstacles(Chunk chunk)
        {
            if (latestChunkIndex > 0 && latestChunkIndex % obstacleInterval == 0)
            {
                int[] indices = chunk.AddObstacles();

                for (int i = 0; i < indices.Length; i++)
                {
                    if (indices[i] > 0)
                    {
                        obstacleIndices[i].Enqueue(indices[i]);
                    }
                }
            }
        }

        private void UpdateObstacleIndices()
        {
            for (int i = 0; i < obstacleIndices.Length; i++)
            {
                if (obstacleIndices[i].Count > 0 && obstacleIndices[i].Peek() <= agentFrameIndex)
                {
                    // Agent passes obstacle.
                    obstacleIndices[i].Dequeue();
                }
            }
        }


        // CHUNKS

        private void AddChunk()
        {
            Chunk chunk = GetPooledChunk();
            chunk.Index = latestChunkIndex;
            chunk.UpdateChunk(frame, modulator);

            AddObstacles(chunk);

            activeChunks.Add(latestChunkIndex, chunk);
            latestChunkIndex++;
        }

        private void RemoveChunk(int index)
        {
            if (activeChunks.TryGetValue(index, out Chunk chunk))
            {
                RecycleChunk(chunk);
                activeChunks.Remove(index);
            }
        }

        private void ClearChunks()
        {
            foreach (Chunk chunk in activeChunks.Values)
            {
                RecycleChunk(chunk);
            }
            activeChunks.Clear();
        }

        private void InitPool()
        {
            chunkPool = new Stack<Chunk>(numChunks);
            for (int i = 0; i < numChunks; i++)
            {
                RecycleChunk(NewChunk);
            }
        }

        private Chunk GetPooledChunk()
        {
            Chunk chunk = chunkPool.Count > 0 ? chunkPool.Pop() : NewChunk;
            chunk.gameObject.SetActive(true);
            return chunk;
        }

        private void RecycleChunk(Chunk chunk)
        {
            chunk.gameObject.SetActive(false);
            chunkPool.Push(chunk);
        }

        private Chunk NewChunk => Instantiate(chunkPrefab, transform);
    }
}