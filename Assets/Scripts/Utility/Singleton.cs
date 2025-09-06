using UnityEngine;
namespace Project.Utility
{
    /// <summary>
    /// A generic singleton class for MonoBehaviour-derived classes.
    /// </summary>
    /// <typeparam name="T">The type of the singleton class.</typeparam>
    public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T instance;
        private static readonly object _lock = new object();

        public static T Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (_lock)
                    {
                        if (instance == null)
                        {
                            instance = FindObjectOfType<T>();
                            if (instance == null)
                            {
                                GameObject singletonObj = new GameObject(typeof(T).Name);
                                instance = singletonObj.AddComponent<T>();
                                DontDestroyOnLoad(singletonObj);
                            }
                        }
                    }
                }
                return instance;
            }
        }

        protected virtual void Awake()
        {
            if (instance == null)
            {
                instance = this as T;
                DontDestroyOnLoad(gameObject);
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }
    }
}
