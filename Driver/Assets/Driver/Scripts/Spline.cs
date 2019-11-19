using System;
using UnityEngine;

// Adapted from original code by JPBotelho https://github.com/JPBotelho/Catmull-Rom-Splines

/*  
    Catmull-Rom splines are Hermite curves with special tangent values.
    Hermite curve formula:
    (2t^3 - 3t^2 + 1) * p0 + (t^3 - 2t^2 + t) * m0 + (-2t^3 + 3t^2) * p1 + (t^3 - t^2) * m1
    For points p0 and p1 passing through points m0 and m1 interpolated over t = [0, 1]
    Tangent M[k] = (P[k+1] - P[k-1]) / 2
*/
public class Spline
{
    public struct Point
    {
        public Vector3 localPos;
        public Vector3 globalPos;
        public Vector3 tangent;
        public Vector3 normal;
        public Vector3 tangentXZ;
        public int index;

        public Point(Vector3 localPos, Vector3 tangent, Vector3 normal)
        {
            this.localPos = localPos;
            this.tangent = tangent;
            this.normal = normal;

            globalPos = localPos;
            tangentXZ = Vector3.ProjectOnPlane(tangent, Vector3.up).normalized;
            index = 0;
        }
    }

    public int Count => splinePoints.Length;

    private Point[] splinePoints;
    private int resolution; // Amount of points between control points. [Tesselation factor]
    private Vector3[] controlPoints;
    private Vector3 offset;

    public Point GetPoint(int index)
    {
        index = index < 0 ? index + splinePoints.Length : index;
        return splinePoints[index % splinePoints.Length];
    }

    public int FindClosestIndex(Vector3 position, int startIndex = -1, int searchRange = 10)
    {
        int min = startIndex == -1 ? 0 : startIndex - searchRange;
        int max = startIndex == -1 ? splinePoints.Length : startIndex + searchRange;
        return BinSearchClosest(position, min, max);
    }

    // Returns spline points. Count is contorolPoints * resolution + [resolution] points if closed loop.
    public Point[] GetPoints()
    {
        if (!ValidatePoints())
        {
            throw new System.NullReferenceException("Spline not Initialized!");
        }
        return splinePoints;
    }

    public Spline(Vector3[] controlPoints, int resolution, Vector3 offset)
    {
        this.controlPoints = controlPoints;
        this.resolution = resolution;
        this.offset = offset;
        GenerateSplinePoints();
    }

    public void DrawSpline(Color color, float duration = 0.1f)
    {
        if (ValidatePoints())
        {
            for (int i = 0; i < splinePoints.Length; i++)
            {
                if (i < splinePoints.Length - 1)
                {
                    Debug.DrawLine(splinePoints[i].globalPos, splinePoints[i + 1].globalPos, color, duration);
                }
            }
        }
    }

    public void DrawNormals(float extrusion, Color color, float duration = 0.1f)
    {
        if (ValidatePoints())
        {
            for (int i = 0; i < splinePoints.Length; i++)
            {
                Debug.DrawRay(splinePoints[i].globalPos, splinePoints[i].normal * extrusion, color, duration);
            }
        }
    }

    public void DrawTangents(float extrusion, Color color, float duration = 0.1f)
    {
        if (ValidatePoints())
        {
            for (int i = 0; i < splinePoints.Length; i++)
            {
                Debug.DrawRay(splinePoints[i].globalPos, splinePoints[i].tangent * extrusion, color, duration);
            }
        }
    }

    private bool ValidatePoints()
    {
        bool valid = splinePoints != null;
        if (!valid)
        {
            throw new NullReferenceException("Spline not initialized!");
        }
        return valid;
    }

    private void GenerateSplinePoints()
    {
        splinePoints = new Point[resolution * controlPoints.Length];

        Vector3 p0, p1; // Start point, end point
        Vector3 m0, m1; // Tangents

        //  First for loop goes through each individual control point and connects it to the next, so 0-1, 1-2, 2-3 and so on
        for (int currentPoint = 0; currentPoint < controlPoints.Length; currentPoint++)
        {
            p0 = controlPoints[currentPoint];

            if (currentPoint == controlPoints.Length - 1)
            {
                p1 = controlPoints[0];
            }
            else
            {
                p1 = controlPoints[currentPoint + 1];
            }

            if (currentPoint == 0) //  Tangent M[k] = (P[k+1] - P[k-1]) / 2
            {
                m0 = p1 - controlPoints[controlPoints.Length - 1];
            }
            else
            {
                m0 = p1 - controlPoints[currentPoint - 1];
            }

            if (currentPoint == controlPoints.Length - 1)
            {
                m1 = controlPoints[(currentPoint + 2) % controlPoints.Length] - p0;
            }
            else if (currentPoint == 0)
            {
                m1 = controlPoints[currentPoint + 2] - p0;
            }
            else
            {
                m1 = controlPoints[(currentPoint + 2) % controlPoints.Length] - p0;
            }

            m0 *= 0.5f;
            m1 *= 0.5f;

            float pointStep = 1.0f / resolution;

            //  Creates [resolution] points between this control point and the next
            for (int tesselatedPoint = 0; tesselatedPoint < resolution; tesselatedPoint++)
            {
                float t = tesselatedPoint * pointStep;

                Point point = Evaluate(p0, p1, m0, m1, t);
                point.globalPos = offset + point.localPos;
                point.index = currentPoint * resolution + tesselatedPoint;
                splinePoints[point.index] = point;
            }
        }
    }

    // Evaluates curve at t[0, 1]. Returns point/normal/tan struct. [0, 1] means clamped between 0 and 1.
    public static Point Evaluate(Vector3 start, Vector3 end, Vector3 tanPoint1, Vector3 tanPoint2, float t)
    {
        Vector3 position = CalculatePosition(start, end, tanPoint1, tanPoint2, t);
        Vector3 tangent = CalculateTangent(start, end, tanPoint1, tanPoint2, t);
        Vector3 normal = NormalFromTangent(tangent);

        return new Point(position, tangent, normal);
    }

    // Calculates curve position at t[0, 1]
    public static Vector3 CalculatePosition(Vector3 start, Vector3 end, Vector3 tanPoint1, Vector3 tanPoint2, float t)
    {
        //  Hermite curve formula:
        //  (2t^3 - 3t^2 + 1) * p0 + (t^3 - 2t^2 + t) * m0 + (-2t^3 + 3t^2) * p1 + (t^3 - t^2) * m1
        Vector3 position = (2.0f * t * t * t - 3.0f * t * t + 1.0f) * start
            + (t * t * t - 2.0f * t * t + t) * tanPoint1
            + (-2.0f * t * t * t + 3.0f * t * t) * end
            + (t * t * t - t * t) * tanPoint2;

        return position;
    }

    // Calculates tangent at t[0, 1]
    public static Vector3 CalculateTangent(Vector3 start, Vector3 end, Vector3 tanPoint1, Vector3 tanPoint2, float t)
    {
        //  Calculate tangents
        //  p'(t) = (6t² - 6t)p0 + (3t² - 4t + 1)m0 + (-6t² + 6t)p1 + (3t² - 2t)m1
        Vector3 tangent = (6 * t * t - 6 * t) * start
            + (3 * t * t - 4 * t + 1) * tanPoint1
            + (-6 * t * t + 6 * t) * end
            + (3 * t * t - 2 * t) * tanPoint2;

        return tangent.normalized;
    }

    // Calculates normal vector from tangent
    public static Vector3 NormalFromTangent(Vector3 tangent)
    {
        return Vector3.Cross(tangent, Vector3.up).normalized / 2;
    }

    private int BinSearchClosest(Vector3 pos, int min, int max)
    {
        bool b = (pos - GetPoint(min).localPos).sqrMagnitude < (pos - GetPoint(max).localPos).sqrMagnitude;
        int delta = max - min;
        int iMid = min + delta / 2;
        return delta == 1 ? (b ? min : max) : BinSearchClosest(pos, b ? min : iMid, b ? iMid : max);
    }
}