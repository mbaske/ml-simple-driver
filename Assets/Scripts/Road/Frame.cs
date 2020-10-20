using UnityEngine;

namespace MBaske
{
    // Spatial reference frame.
    public class Frame
    {
        public int Index { get; set; }
        public Matrix4x4 Matrix { get; private set; }

        public Frame() : this(Vector3.zero, Quaternion.identity) { }

        public Frame(Transform transform) : this(transform.position, transform.rotation) { }

        public Frame(Vector3 position, Quaternion rotation, int index = 0)
        {
            Matrix = Matrix4x4.TRS(position, rotation, Vector3.one);
            Index = index;
        }

        public Frame Copy()
        {
            return new Frame(Position, Rotation, Index);
        }

        public void Advance(Quaternion rotation, float stepLength = 1)
        {
            Index++;
            Matrix *= Matrix4x4.Rotate(rotation);
            Matrix *= Matrix4x4.Translate(Vector3.forward * stepLength);
        }

        public void Draw(float extrude = 1, float duration = 0)
        {
            Debug.DrawRay(Position, Right * extrude, Color.red, duration);
            Debug.DrawRay(Position, Up * extrude, Color.green, duration);
            Debug.DrawRay(Position, Forward * extrude, Color.blue, duration);
        }

        public Vector3 LocalDirection(Vector3 worldDirection)
        {
            return Matrix.inverse.MultiplyVector(worldDirection);
        }

        public Vector3 LocalPosition(Vector3 worldPositon)
        {
            return Matrix.inverse.MultiplyPoint3x4(worldPositon);
        }

        public Vector3 WorldDirection(Vector3 localDirection)
        {
            return Matrix.MultiplyVector(localDirection);
        }

        public Vector3 WorldPosition(Vector3 localPositon)
        {
            return Matrix.MultiplyPoint3x4(localPositon);
        }

        public Vector3 Position => new Vector3(Matrix[0, 3], Matrix[1, 3], Matrix[2, 3]);

        public Quaternion Rotation => Quaternion.LookRotation(Matrix.GetColumn(2), Matrix.GetColumn(1));

        public Vector3 Right => new Vector3(Matrix[0, 0], Matrix[1, 0], Matrix[2, 0]).normalized;

        public Vector3 Up => new Vector3(Matrix[0, 1], Matrix[1, 1], Matrix[2, 1]).normalized;

        public Vector3 Forward => new Vector3(Matrix[0, 2], Matrix[1, 2], Matrix[2, 2]).normalized;
    }
}