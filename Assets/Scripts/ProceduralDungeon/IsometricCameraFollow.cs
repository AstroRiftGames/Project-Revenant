using UnityEngine;

namespace ProceduralDungeon
{
    public class IsometricCameraFollow : MonoBehaviour
    {
        public Transform Target;
        public float SmoothTime = 0.3f;
        public Vector3 Offset = new Vector3(0f, 0f, -10f);

        private Vector3 _velocity = Vector3.zero;

        private void LateUpdate()
        {
            if (Target == null) return;

            Vector3 targetPosition = Target.position + Offset;

            transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref _velocity, SmoothTime);
        }
    }
}
