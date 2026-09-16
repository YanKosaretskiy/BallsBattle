using UnityEngine;
using System.Collections;

public class HitStop : MonoBehaviour
{
    public static HitStop Instance { get; private set; }

    private Coroutine activeHitStop;

    private void Awake()
    {
        Instance = this;
    }

    public void Trigger(float duration)
    {
        if (activeHitStop != null)
            StopCoroutine(activeHitStop);

        activeHitStop = StartCoroutine(DoHitStop(duration));
    }

    private IEnumerator DoHitStop(float duration)
    {
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = 1f;
        activeHitStop = null;
    }
}