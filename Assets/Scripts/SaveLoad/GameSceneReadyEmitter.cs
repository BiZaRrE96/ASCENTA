using ASCENTA.Events;
using UnityEngine;

[DefaultExecutionOrder(100)]
public sealed class GameSceneReadyEmitter : MonoBehaviour
{
    void Start()
    {
        StartCoroutine(PublishAfterFirstFrame());
    }

    System.Collections.IEnumerator PublishAfterFirstFrame()
    {
        yield return null;
        EventBus.Publish(new GameSceneReadyEvent());
    }
}
