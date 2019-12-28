using System;
using UnityEngine;

public class CollisionDetection : MonoBehaviour
{
    public Action CollisionCallback;

    private void OnCollisionEnter(Collision other) 
    {
        CollisionCallback?.Invoke();
    }
}
