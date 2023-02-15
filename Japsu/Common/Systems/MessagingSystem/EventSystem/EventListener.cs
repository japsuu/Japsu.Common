using UnityEngine;
using UnityEngine.Events;

namespace Japsu.Common.MessagingSystem.EventSystem
{
    [System.Serializable]
    public class EventResponse : UnityEvent<object> {}

    public class EventListener : MonoBehaviour
    {
        [Tooltip("Channel to listen to.")]
        public EventChannel listenedChannel;

        [Tooltip("Response to invoke when an event is raised.")]
        public EventResponse response;

        private void OnEnable()
        {
            listenedChannel.RegisterListener(this);
        }

        private void OnDisable()
        {
            listenedChannel.UnregisterListener(this);
        }

        public void OnEventRaised(object data)
        {
            response.Invoke(data);
        }
    }
}