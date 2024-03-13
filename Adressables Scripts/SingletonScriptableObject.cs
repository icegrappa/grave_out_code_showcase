using System.Linq;
using UnityEngine;

    public abstract class SingletonScriptableObject<T> : ScriptableObject
        where T : SingletonScriptableObject<T>
    {
        protected static T instance;
        public static string InstancePath { protected get; set; } = string.Empty;

        public static T Instance
        {
            get
            {
                if (instance == null)
                {
                    var type = typeof(T);
                    var instances = Resources.LoadAll<T>(InstancePath);
                    instance = instances.FirstOrDefault();
                    if (instance == null)
                    {
                        Debug.LogErrorFormat("[ScriptableSingleton] No instance of {0} found!", type);
                    }
                    else if (instances.Length > 1)
                    {
                        Debug.LogErrorFormat("[ScriptableSingleton] Multiple instances of {0} found!", type);
                    }
                }

                return instance;
            }
        }
    }
