using System.Collections.Generic;
using UnityEngine;

namespace Japsu.Common.MessagingSystem.EventSystem
{
    [CreateAssetMenu(menuName="EventChannel", fileName="EventChannel_")]
    public class EventChannel : ScriptableObject
    {
        [SerializeField] protected List<EventListener> listeners = new();

        public virtual void Raise(object data = null)
        {
            for (int i = listeners.Count -1; i >= 0; i--)
            {
                listeners[i].OnEventRaised(data);
            }
        }

        public virtual void RegisterListener(EventListener listener)
        {
            if (!listeners.Contains(listener))
                listeners.Add(listener);
        }

        public virtual void UnregisterListener(EventListener listener)
        {
            if (listeners.Contains(listener))
                listeners.Remove(listener);
        }
    }
}