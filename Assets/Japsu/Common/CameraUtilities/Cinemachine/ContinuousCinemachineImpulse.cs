using Cinemachine;
using UnityEngine;

namespace Japsu.Common.CameraUtilities.Cinemachine
{
    public class ContinuousCinemachineImpulse : MonoBehaviour
    {
        public bool Active;
 
        [CinemachineImpulseDefinitionProperty]
        public CinemachineImpulseDefinition ImpulseDefinition = new();

        private float lastEventTime;

        private void Update()
        {
            float now = Time.time;
            float eventLength = ImpulseDefinition.m_TimeEnvelope.m_AttackTime + ImpulseDefinition.m_TimeEnvelope.m_SustainTime;
            if (Active && now - lastEventTime > eventLength)
            {
                ImpulseDefinition.CreateEvent(transform.position, Vector3.down);
                lastEventTime = now;
            }
        }
    }
}