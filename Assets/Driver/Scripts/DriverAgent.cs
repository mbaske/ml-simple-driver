using UnityEngine;
using UnityStandardAssets.Vehicles.Car;
using MLAgents;

public class DriverAgent : Agent
{
    [SerializeField] private Road road;
    [SerializeField] private CarController carCtrl;
    [SerializeField] private CollisionDetection collisionDetection;
    [SerializeField] private RoadDetection[] roadDetection;
    [SerializeField] private RoadObserver roadObserver;
    [SerializeField] private Camera agentCam;
    [SerializeField] private bool useCam;
    [SerializeField] private float speedRewardMultiplier = 0.0025f;

    private Transform environment;
    private Resetter resetter;
    private Spline spline;
    private int startSPIndex = 5; 
    private int crntSPIndex;
    private Vector3 localCarPos;
    private int collisionCount;

    public override void InitializeAgent()
    {
        environment = transform.parent;
        spline = road.GetSpline();
        crntSPIndex = startSPIndex;
        Spline.Point sp = spline.GetPoint(crntSPIndex);
        Vector3 pos = sp.globalPos;
        pos.y += 0.5f;
        carCtrl.transform.position = pos;
        carCtrl.transform.rotation = Quaternion.LookRotation(sp.tangent);   
        resetter = new Resetter(environment);

        agentCam.gameObject.SetActive(useCam);
        roadObserver.gameObject.SetActive(!useCam);
        if (!useCam)
        {
            roadObserver.Initialize();
        }
        collisionDetection.CollisionCallback = OnObstacleCollision;
    }

    public override void CollectObservations()
    {
        Spline.Point sp = GetSplinePointAtCar(crntSPIndex);

        Vector3 forwardXZ = Vector3.ProjectOnPlane(carCtrl.transform.forward, Vector3.up);
        float orientation = Vector3.SignedAngle(forwardXZ, sp.tangentXZ, Vector3.up);
        AddVectorObs(orientation / 180f); // 1
        float offset = (sp.localPos + sp.normal * road.Width - localCarPos).magnitude;
        AddVectorObs((offset / road.Width) * 2f - 1f); // 1

        AddVectorObs(Sigmoid(carCtrl.LocalVelocity)); // 3
        AddVectorObs(Sigmoid(carCtrl.LocalAngularVelocity)); // 3
        AddVectorObs(carCtrl.Inclination); // 3
        AddVectorObs(carCtrl.NormalizedSteerAngle); // 1
        if (!useCam)
        {
            AddVectorObs(roadObserver.GetNormalizedObs(sp, carCtrl.transform)); // 50
        }

        float forwardSpeed = Vector3.Dot(sp.tangent, carCtrl.Velocity);
        AddReward(forwardSpeed * speedRewardMultiplier);
    }

    public override void AgentAction(float[] vectorAction)
    {
        carCtrl.Move(vectorAction[0], vectorAction[1]);

        if (!IsOnRoad() || carCtrl.transform.up.y < 0.5f || collisionCount > 10)
        {
            SetReward(-1f);
            resetter.Reset();
            collisionCount = 0;
            crntSPIndex = startSPIndex;
        }
    }

    private bool IsOnRoad()
    {
        for (int i = 1; i < 5; i++)
        {
            if (!roadDetection[i].IsOnRoad(out Vector3 position))
            {
                return false;
            }
        }
        return true;
    }

    private void OnObstacleCollision()
    {
        AddReward(-1f);
        collisionCount++;
    }

    private Spline.Point GetSplinePointAtCar(int startIndex = -1)
    {
        if (roadDetection[0].IsOnRoad(out Vector3 position))
        {
            localCarPos = environment.InverseTransformPoint(position);
            crntSPIndex = spline.FindClosestIndex(localCarPos, startIndex);
        }
        return spline.GetPoint(crntSPIndex);
    }

    // Normalize to -1/+1 range.
    private static float Sigmoid(float val)
    {
        return val / (1f + Mathf.Abs(val));
    }

    private static Vector3 Sigmoid(Vector3 v3)
    {
        v3.x = Sigmoid(v3.x);
        v3.y = Sigmoid(v3.y);
        v3.z = Sigmoid(v3.z);
        return v3;
    }
}
