using UnityEngine;
using System.Collections.Generic;
using System.Collections;

namespace ProceduralDungeon
{
    public class IsometricCameraFollow : MonoBehaviour
    {
        public Transform Target;
        public float SmoothTime = 0.3f;
        public Vector3 Offset = new Vector3(0f, 0f, -10f);

        private Vector3 _velocity = Vector3.zero;
        private Vector3 _targetPosition = Vector3.zero;

        private void Start()
        {
            if (Target == null)
            {
                StartCoroutine(FindPlayerRoutine());
            }
        }

        private IEnumerator FindPlayerRoutine()
        {
            while (Target == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    Target = player.transform;
                    UpdateTargetPos();
                    transform.position = _targetPosition;
                }
                else
                {
                    yield return new WaitForSeconds(0.1f);
                }
            }
        }

        private void UpdateTargetPos() 
        {
            if (Target == null) return;

            _targetPosition = Target.position + Offset;
        }

        private void LateUpdate()
        {
            UpdateTargetPos();
            UpdatePos();
        }

        private void UpdatePos()
        {
            transform.position = Vector3.SmoothDamp(transform.position, _targetPosition, ref _velocity, SmoothTime);
        }
    }
}
