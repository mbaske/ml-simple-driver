using UnityEngine;

namespace MBaske
{
    public class Chunk : MonoBehaviour
    {
        public int Index { get; set; }

        // Nominal length (no curvature), assuming frame progression step length = 1;
        public const int Length = 12;
        // Distance from road center to edge.
        public const float RoadExtent = 3.4f;
        public const float MeshExtent = 6f;

        // Obstacle placement.
        private static readonly int numObstacles = Length - 1; // per side
        private static readonly float obstacleXSpacing = RoadExtent / (Length + 1f);

        public static int MaxObstacles => numObstacles * 2; // both sides

        private Mesh mesh;
        private MeshFilter mf;
        private MeshCollider mc;
        private Vector3[] vertices;
        private Vector3[] normals;
        // Reference frames for each rounded z-pos, forward = road tangent.
        private Frame[] frames;
        // Frame with origin at transfrom.position. 
        // Used for quick lookup of individual frames based on agent's local z-pos.
        // Points towards next chunk's pos which fits curvature better than transform.forward.
        private Frame chunkFrame;

        // TODO pool
        private Transform obstacles;
        [SerializeField]
        private GameObject conePrefab;
        [SerializeField]
        private GameObject barrelPrefab;
        [SerializeField]
        private GameObject blockPrefab;

        
        private void Awake()
        {
            mf = GetComponent<MeshFilter>();
            mc = GetComponent<MeshCollider>();
            mesh = GenerateMesh();
            mesh.MarkDynamic();

            vertices = mesh.vertices;
            normals = mesh.normals;
            frames = new Frame[Length + 1];
        }


        // MESH & FRAMES

        public void UpdateChunk(Frame frame, RoadModulator modulator)
        {
            ClearObstacles();

            transform.position = frame.Position;
            transform.rotation = frame.Rotation;

            UpdateMesh(0, frame);
            Vector3 pos = frame.Position;

            for (int z = 1; z <= Length; z++)
            {
                frame.Advance(modulator.GetRotation(frame.Index));
                UpdateMesh(z, frame);

                frames[z - 1] = new Frame(pos, frame.Rotation, frame.Index - 1);
                pos = frame.Position;
            }
            // Same position as next chunk's first frame.
            frames[Length] = frame.Copy();

            chunkFrame = new Frame(transform.position, Quaternion.LookRotation(
                frame.Position - transform.position,
                (frame.Up + transform.up) * 0.5f));

            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.RecalculateBounds();
            mf.sharedMesh = mesh;
            mc.sharedMesh = mesh;
        }

        private void UpdateMesh(int z, Frame frame)
        {
            vertices[z * 2]     = transform.InverseTransformPoint(frame.Position - frame.Right * MeshExtent);
            vertices[z * 2 + 1] = transform.InverseTransformPoint(frame.Position + frame.Right * MeshExtent);

            normals[z * 2]      = frame.Up;
            normals[z * 2 + 1]  = frame.Up;
        }

        public Frame GetFrame(int frameIndex)
        {
            return frames[frameIndex % Length];
        }

        public Frame GetFrame(Vector3 agentPos)
        {
            // Not exact, but good enough, since each individual chunk's curvature is minimal.
            Vector3 p = chunkFrame.LocalPosition(agentPos);
            return frames[Mathf.Clamp(Mathf.RoundToInt(p.z), 0, Length)];
        }


        // OBSTACLES

        public int[] AddObstacles()
        {
            if (obstacles == null)
            {
                obstacles = new GameObject("Obstacles").transform;
                obstacles.parent = transform;
                obstacles.localPosition = Vector3.zero;
            }

            // Frame indices, left to right.
            int[] indices = new int[MaxObstacles];

            bool left = Util.RandomBool();
            bool block = Util.RandomBool(0.25f);

            if (block)
            {
                int z = Length / 2;
                var p = GetSpawnPoint(z, z, left ? -1 : 1);
                Instantiate(blockPrefab, p, frames[z].Rotation, obstacles);

                for (int x = 0; x < numObstacles; x++)
                {
                    indices[x + (left ? 0 : numObstacles)] = frames[z].Index;
                }
            }
            else
            {
                var prefab = Util.RandomBool() ? conePrefab : barrelPrefab;
                for (int z = 1; z < Length; z++)
                {
                    int x = Length - z;
                    var p = GetSpawnPoint(z, x, left ? -1 : 1);
                    Instantiate(prefab, p, frames[z].Rotation, obstacles);

                    x = left ? numObstacles - x : numObstacles + x - 1;
                    indices[x] = frames[z].Index;
                }
            }

            return indices;
        }

        private void ClearObstacles()
        {
            if (obstacles != null)
            {
                Destroy(obstacles.gameObject);
                obstacles = null;
            }
        }

        private Vector3 GetSpawnPoint(int x, int z, int sign)
        {
            return frames[z].Position + frames[z].Right * obstacleXSpacing * x * sign;
        }



        private static Mesh GenerateMesh()
        {
            int n = (Length + 1) * 2;
            Vector3[] vertices = new Vector3[n];
            Vector3[] normals = new Vector3[n];
            Vector2[] uvs = new Vector2[n];

            int[] triangles = new int[Length * 6];

            for (int i = 0; i <= Length; i++)
            {
                int iL = i * 2;
                int iR = iL + 1;

                vertices[iL] = new Vector3(-MeshExtent, 0, i);
                vertices[iR] = new Vector3(MeshExtent, 0, i);

                normals[iL] = Vector3.up;
                normals[iR] = Vector3.up;

                float y = i / (float)Length;
                uvs[iL] = new Vector2(0, y);
                uvs[iR] = new Vector2(1, y);
            }

            for (int i = 0; i < Length; i++)
            {
                int iL0 = i * 2;
                int iR0 = iL0 + 1;
                int iL1 = iL0 + 2;
                int iR1 = iR0 + 2;

                int t = i * 6;
                triangles[t] = iL0;
                triangles[t + 1] = iL1;
                triangles[t + 2] = iR1;
                triangles[t + 3] = iL0;
                triangles[t + 4] = iR1;
                triangles[t + 5] = iR0;
            }

            return new Mesh
            {
                vertices = vertices,
                normals = normals,
                uv = uvs,
                triangles = triangles
            };
        }
    }
}