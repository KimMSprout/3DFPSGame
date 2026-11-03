using UnityEngine;

public class SingletonBehavior<T> : MonoBehaviour where T : class
{
    private static T _instance;
    public static T Instance;

    private void Awake()
    {
        if (_instance != null)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this as T;
    }
}