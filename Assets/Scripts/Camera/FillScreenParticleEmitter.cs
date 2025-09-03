using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class FillScreenParticleEmitter : MonoBehaviour
{
    public Camera cam;
    public float padding = 1f;

#if UNITY_EDITOR
    public bool previewInEditor = true;
#endif

    ParticleSystem ps;

    void Awake(){ ps = GetComponent<ParticleSystem>(); }

    void LateUpdate()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying && !previewInEditor) return;
#endif
        if (!cam) cam = Camera.main;
        if (!cam || !ps) return;
        
        transform.rotation = Quaternion.identity;

        float worldH = cam.orthographicSize * 2f + padding * 2f;
        float worldW = worldH * cam.aspect + padding * 2f;

        var sh = ps.shape;
        sh.shapeType = ParticleSystemShapeType.Box;
        sh.scale = new Vector3(worldW, worldH, 1f);

        transform.position = new Vector3(cam.transform.position.x, cam.transform.position.y, transform.position.z);
    }
}
