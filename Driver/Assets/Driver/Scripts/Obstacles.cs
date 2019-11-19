using UnityEngine;

public class Obstacles : MonoBehaviour
{
    [SerializeField] private Road road;
    [SerializeField] [Range(0, 20)] private int amount = 10;
    [SerializeField] [Range(0.25f, 1)] private float size = 0.5f;
    [SerializeField] [Range(1, 20)] private float massMultiplier = 10;
    [Space]
    [SerializeField] [InspectorButton("UpdateObstacles")] private bool update;

    private void UpdateObstacles()
    {
        if (amount > 0)
        {
            Spline spline = road.GetSpline();
            int n = spline.Count;
            int step = n / amount;
            float offW = road.Width * 0.5f;
            Vector3 scale = Vector3.one * road.Width * size * 0.5f;
            Vector3 offH = Vector3.up * scale.y * 0.5f;
            for (int i = 0; i < amount; i++)
            {
                Spline.Point sp = spline.GetPoint(i * step);
                Vector3 v = sp.localPos + sp.normal * offW * (Random.value > 0.5f ? 1f : -1f);
                Transform cube = GetCube(i);
                cube.localScale = scale;
                cube.localPosition = v + offH;
                cube.rotation = Quaternion.LookRotation(sp.tangent);
                cube.gameObject.GetComponent<Rigidbody>().mass = Mathf.Pow(scale.x, 3) * massMultiplier;
            }
        }
        DeactiveUnused(amount);
    }

    private Transform GetCube(int index)
    {
        Transform cube;
        if (index < transform.childCount)
        {
            cube = transform.GetChild(index);
            cube.gameObject.SetActive(true);
            return cube;
        }
        cube = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
        cube.gameObject.AddComponent<Rigidbody>();
        cube.gameObject.layer = gameObject.layer;
        cube.parent = transform;
        return cube;
    }

    private void DeactiveUnused(int index)
    {
        for (int i = index; i < transform.childCount; i++)
        {
            transform.GetChild(i).gameObject.SetActive(false);
        }
    }
}
