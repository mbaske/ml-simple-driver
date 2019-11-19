using UnityEngine;

public class RoadDetection : MonoBehaviour
{
    private int lmRoad = 1 << 8;

    public bool IsOnRoad(out Vector3 position)
    {
        position = Vector3.zero;
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 5, lmRoad))
        {
            position = hit.point;
            return true;
        }
        return false;
    }
}
