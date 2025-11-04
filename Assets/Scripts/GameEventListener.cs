using UnityEngine;
using UnityEngine.Events;

public class GameEventListener : MonoBehaviour
{
    [SerializeField,Header("検知する対象のイベント")] private GameEvent gameEvent;
    [SerializeField,Header("イベント発生時の挙動")] private UnityEvent response;

    private void OnEnable()
    {
        if (gameEvent != null)
        {
            gameEvent.RegisterListener(this);
        }
    }

    private void OnDisable()
    {
        if (gameEvent != null)
        {
            gameEvent.UnregisterListener(this);
        }
    }

    public void OnEventRaised()
    {
        response?.Invoke();
    }
}
