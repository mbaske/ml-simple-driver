using UnityEngine;

[System.Serializable]
public struct CurveModifier
{
    [Range(0f, 20f)] public float amplitude;
    [Range(0, 10)] public int frequency;

    public float GetOffset(float angle)
    {
        return Mathf.Sin(Mathf.Deg2Rad * angle * (float)frequency) * amplitude;
    }
}
