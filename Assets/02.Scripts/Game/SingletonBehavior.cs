using UnityEngine;

// 씬에 하나만 존재해야 하는 MonoBehaviour의 베이스 클래스.
// 상속한 클래스에서 Awake가 필요하면 override 후 base.Awake()를 먼저 호출한다.
public class SingletonBehavior<T> : MonoBehaviour where T : SingletonBehavior<T>
{
    private static T _instance;
    public static T Instance => _instance;

    protected virtual void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = (T)this;
    }

    protected virtual void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }
}
