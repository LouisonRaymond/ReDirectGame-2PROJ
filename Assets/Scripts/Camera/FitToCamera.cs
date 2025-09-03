using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class FitToCamera : MonoBehaviour
{
    public Camera cam;
    public SpriteRenderer sr;

#if UNITY_EDITOR
    public bool previewInEditor = true;
#endif

    void Reset() { cam = Camera.main; sr = GetComponent<SpriteRenderer>(); }

    void LateUpdate()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying && !previewInEditor) return;
#endif
        if (!cam) cam = Camera.main;
        if (!cam || !sr || !sr.sprite) return;

        float worldH = cam.orthographicSize * 2f;
        float worldW = worldH * cam.aspect;

        Vector2 s = sr.sprite.bounds.size;
        transform.localScale = new Vector3(worldW / s.x, worldH / s.y, 1f);
        transform.position = new Vector3(cam.transform.position.x, cam.transform.position.y, transform.position.z);
    }
}
