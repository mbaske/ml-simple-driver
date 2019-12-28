using System.Collections.Generic;
using UnityEngine;

public class RoadObserver : MonoBehaviour
{
    [SerializeField] private Road road;

    private int numSegments = 5;
    private int maxSegmentLength = 20;
    private int maxSegmentCurvature = 20; // degrees
    private int numRays = 6; // per road segment
    private float rayHeight = 0.5f; // above road

    private Spline spline;
    private Spline.Point[] waypoints;
    private List<float> normalizedObs; // count = 50 with above settings

    private int lmObstacle = 1 << 9;
    private float rayGap;
    private float rayRadius;
    private Vector3 rayOffset;

    public void Initialize()
    {
        spline = road.GetSpline();
        waypoints = new Spline.Point[numSegments + 1];
        normalizedObs = new List<float>(numSegments * (numRays + 4));

        rayGap = road.Width * 2 / (numRays + 1f);
        rayRadius = rayGap * 0.25f;
        rayOffset = Vector3.up * rayHeight;
    }

    public List<float> GetNormalizedObs(Spline.Point carSP, Transform car)
    {
        CalcWaypoints(carSP);
        normalizedObs.Clear();

        for (int i = 0, n = waypoints.Length - 1; i < n; i++)
        {
            Spline.Point w0 = waypoints[i];
            Spline.Point w1 = waypoints[i + 1];

            Vector3 delta = w1.localPos - w0.localPos;
            float length = delta.magnitude;
            // The normalized length of this road segment relative to max segment length.
            normalizedObs.Add((length / maxSegmentLength) * 2f - 1f);
            // The direction of this road segment in car's local space.
            Vector3 direction = car.InverseTransformVector(delta.normalized); 
            normalizedObs.Add(direction.x);
            normalizedObs.Add(direction.y);
            normalizedObs.Add(direction.z);

            for (int j = 1; j <= numRays; j++)
            {
                Vector3 p0 = w0.globalPos + w0.normal * road.Width - w0.normal * (j * rayGap) + rayOffset;
                Vector3 p1 = w1.globalPos + w1.normal * road.Width - w1.normal * (j * rayGap) + rayOffset;

                if (Physics.SphereCast(p0, rayRadius, p1 - p0, out RaycastHit hit, length, lmObstacle))
                {
                    Debug.DrawLine(p0, hit.point, Color.red);
                    // The normalized obstacle distance relative to current segment length.
                    normalizedObs.Add((hit.distance / length) * 2f - 1f);
                }
                else
                {
                    Debug.DrawRay(p0, p1 - p0, Color.green);
                    normalizedObs.Add(1f); 
                }
            }
        }
        return normalizedObs;
    }

    private void CalcWaypoints(Spline.Point carSP)
    {
        Spline.Point prev = carSP;
        waypoints[0] = carSP;
        int index = carSP.index;
        int segment = 1;
        float length = 0;
        Vector3 tangentXZ = carSP.tangentXZ;
        while (segment <= numSegments)
        {
            index++;
            Spline.Point crnt = spline.GetPoint(index);
            length += (crnt.localPos - prev.localPos).magnitude;
            float curve = Vector3.Angle(tangentXZ, crnt.tangentXZ);
            if (length > maxSegmentLength || curve > maxSegmentCurvature)
            {
                waypoints[segment] = prev;
                segment++;
                length = 0;
                tangentXZ = prev.tangentXZ;
            }
            else
            {
                prev = crnt;
            }
        }
    }
}
