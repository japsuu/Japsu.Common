using UnityEngine;

// ReSharper disable StaticMemberInGenericType
// ReSharper disable FieldCanBeMadeReadOnly.Local

namespace Japsu.Common.Singletons
{
    public class SingletonBehaviour<T> : MonoBehaviour where T : MonoBehaviour
    {
        // Check to see if we're about to be destroyed.
        private static bool shuttingDown;
        private static object lockObj = new();
        private static T singleton;

        public static T Singleton
        {
            get
            {
                if (!Application.isPlaying) return null;
            
                if (shuttingDown)
                {
                    //Debug.LogWarning("[Singleton] Instance '" + typeof(T) +
                    //    "' already destroyed. Returning null.");
                    return null;
                }

                lock (lockObj)
                {
                    if (singleton == null)
                    {
                        singleton = (T)FindObjectOfType(typeof(T));
                    }

                    return singleton;
                }
            }
        }
        
        private void OnEnable()
        {
            shuttingDown = false;
        }


        private void OnApplicationQuit()
        {
            shuttingDown = true;
        }


        private void OnDestroy()
        {
            shuttingDown = true;
        }
    }
}