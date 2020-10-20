using UnityEngine;

namespace MBaske
{
    // Slope (x-rot) oscillates regularly (cos).
    // Curvature (y-rot) is random (perlin noise with random offset).
    public class RoadModulator
    {
        private readonly Vector2 scale = new Vector2(50f, 100f);
        private readonly Vector2 strength = new Vector2(0.05f, 2f);
        private readonly Vector2 offset;

        public RoadModulator()
        {
            offset = Random.insideUnitCircle * 256;
        }

        public Quaternion GetRotation(int frameIndex)
        {
            return Quaternion.Euler(
                Mathf.Cos(frameIndex / scale.x) * strength.x,
                Noise(offset.x, offset.y + frameIndex / scale.x) * strength.y,
                0);
        }

        private const float bias = 0.0725f; // TODO

        private static float Noise(float x, float y)
        {
            return (Mathf.PerlinNoise(x, y) - 0.5f) * 2f + bias;
        }
    }
}