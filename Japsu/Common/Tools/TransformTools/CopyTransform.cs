using UnityEngine;

namespace Japsu.Common.TransformTools
{
    public enum UpdateMethod
    {
        Update,
        FixedUpdate,
        LateUpdate
    }
    
    public class CopyTransform : MonoBehaviour
    {
        [SerializeField]
        private UpdateMethod updateType;

        [SerializeField]
        private Transform target;

        private void Update()
        {
            if (updateType != UpdateMethod.Update) return;
            
            if(target == null) return;
            
            transform.SetPositionAndRotation(target.position, target.rotation);
        }

        private void FixedUpdate()
        {
            if (updateType != UpdateMethod.FixedUpdate) return;
            
            if(target == null) return;
            
            transform.SetPositionAndRotation(target.position, target.rotation);
        }

        private void LateUpdate()
        {
            if (updateType != UpdateMethod.LateUpdate) return;
            
            if(target == null) return;
            
            transform.SetPositionAndRotation(target.position, target.rotation);
        }
    }
}
