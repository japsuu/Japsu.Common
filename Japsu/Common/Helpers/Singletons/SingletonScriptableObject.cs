using UnityEngine;

namespace Japsu.Common.Singletons
{
    public class SingletonScriptableObject<T> : ScriptableObject where T : SingletonScriptableObject<T>
    {
        private static T instance;

        public static T Instance
        {
            get
            {
                if (instance != null) return instance;
                
                T[] assets = Resources.LoadAll<T>("");
                if (assets == null || assets.Length < 1)
                {
                    throw new System.Exception("No scriptableObject instances found in resources!");
                }

                if (assets.Length > 1)
                {
                    UnityEngine.Debug.LogWarning("Multiple instances of ScriptableObject found in resources!");
                }

                instance = assets[0];

                return instance;
            }
        }
    }
}