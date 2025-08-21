using System.Collections;
using UnityEngine;

public class CameraShake2D : MonoBehaviour
{
    [Header("Default")]
    public float defaultDuration = 0.25f;
    public float defaultMagnitude = 0.25f;
    public float frequency = 28f; // lissage type perlin

    Vector3 _basePos;
    Coroutine _co;

    void Awake() { _basePos = transform.localPosition; }

    public void Shake(float duration, float magnitude)
    {
        if (_co != null) StopCoroutine(_co);
        _co = StartCoroutine(CoShake(duration <= 0 ? defaultDuration : duration,
            magnitude <= 0 ? defaultMagnitude : magnitude));
    }

    IEnumerator CoShake(float dur, float mag)
    {
        float t = 0f;
        _basePos = transform.localPosition;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float n1 = Mathf.PerlinNoise(Time.time * frequency, 0.37f) * 2f - 1f;
            float n2 = Mathf.PerlinNoise(0.93f, Time.time * frequency) * 2f - 1f;
            transform.localPosition = _basePos + new Vector3(n1, n2, 0f) * mag;
            yield return null;
        }
        transform.localPosition = _basePos;
        _co = null;
    }
}
