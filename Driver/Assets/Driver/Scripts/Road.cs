using UnityEngine;
using System.Linq;

public class Road : MonoBehaviour
{
    [Range(4, 12)] public float Width = 8;
    [SerializeField] private CurveModifier[] curveModifiers;
    [SerializeField] private CurveModifier[] heightModifiers;
    [Space]
    [SerializeField] [InspectorButton("UpdateMesh")] private bool update;
    [SerializeField] private bool cloneMesh;

    private Spline spline;
    private const float radius = 100;
    private const int resolution = 2;
    private Mesh mesh;

    // private void OnValidate()
    // {
    //     CreateSpline();
    //     spline.DrawSpline(Color.white);
    //     spline.DrawNormals(Width * 0.5f, Color.red);
    //     spline.DrawNormals(Width * -0.5f, Color.red);
    // }

    public Spline GetSpline()
    {
        return spline == null ? CreateSpline() : spline;
    }

    private void UpdateMesh()
    {
        CreateSpline();

        mesh = GetComponent<MeshFilter>().sharedMesh;
        if (mesh == null)
        {
            mesh = new Mesh();
            mesh.MarkDynamic();
        }
        else if (cloneMesh)
        {
            mesh = Instantiate(GetComponent<MeshFilter>().sharedMesh);
        }
        
        int n = spline.Count;
        Vector3[] vertices = new Vector3[n * 2];
        int[] triangles = new int[n * 6];
        for (int i = 0, v = 0, t = 0; i < n; i++)
        {
            bool last = i == n - 1;
            triangles[t++] = v;
            triangles[t++] = last ? 0 : v + 2;
            triangles[t++] = last ? 1 : v + 3;
            triangles[t++] = v;
            triangles[t++] = last ? 1 : v + 3;
            triangles[t++] = v + 1;
            Spline.Point sp = spline.GetPoint(i);
            vertices[v++] = sp.localPos + sp.normal * Width;
            vertices[v++] = sp.localPos + sp.normal * -Width;
        }

        mesh.Clear();
        mesh.vertices = vertices;
        mesh.normals = Enumerable.Repeat(Vector3.up, n * 2).ToArray();
        mesh.triangles = triangles;
        GetComponent<MeshFilter>().sharedMesh = mesh;
        GetComponent<MeshCollider>().sharedMesh = mesh;
    }

    private Spline CreateSpline()
    {
        spline = new Spline(CalcControlPoints(), resolution, transform.parent.position);
        return spline;
    }

    private Vector3[] CalcControlPoints()
    {
        Vector3[] points = new Vector3[360];
        for (int angle = 0; angle < 360; angle++)
        {
            float r = radius;
            for (int w = 0; w < curveModifiers.Length; w++)
            {
                r += curveModifiers[w].GetOffset(angle);
            }
            float height = 0;
            for (int h = 0; h < heightModifiers.Length; h++)
            {
                height += heightModifiers[h].GetOffset(angle);
            }
            points[angle] = new Vector3(
                Mathf.Cos(Mathf.Deg2Rad * angle) * r,
                height,
                Mathf.Sin(Mathf.Deg2Rad * angle) * r
            );
        }
        return points;
    }
}