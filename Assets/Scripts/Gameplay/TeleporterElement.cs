using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Collider2D))]
public class TeleporterElement : MonoBehaviour, IBallInteractor
{
    [HideInInspector] public string pairId;
    [HideInInspector] public TeleporterElement paired;

    private SpriteRenderer[] _srs;
    private Color[] _baseColors;

    void Awake()
    {
        _srs = GetComponentsInChildren<SpriteRenderer>(true);
        _baseColors = new Color[_srs.Length];
        for (int i = 0; i < _srs.Length; i++) _baseColors[i] = _srs[i].color;

        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    public void SetTint(Color c)
    {
        if (_srs == null) return;
        for (int i = 0; i < _srs.Length; i++) _srs[i].color = c;
    }

    public void ResetTint()
    {
        if (_srs == null) return;
        for (int i = 0; i < _srs.Length; i++) _srs[i].color = _baseColors[i];
    }

    public void OnBallHit(BallRunner ball)
    {
        var act = GetComponent<Activable>();
        if (act != null && !act.AllowInteraction())
        {
            ball.AddCooldownForHierarchy(transform, 0.20f);
            return;
        }

        if (paired == null) return;
        
        FXManager.Instance?.PlayTeleport(ball.transform.position, ball.dir);
        AudioManager.Instance?.PlayTeleport(); 
        
        Vector3 exit = paired.transform.position + (Vector3)(ball.dir * 0.20f);
        
        ball.TeleportTo(exit);
        
        StartCoroutine(PlayExitFxAndSfxNextFrame(ball.dir));
        
        ball.AddCooldownForHierarchy(transform, 0.15f);
        ball.AddCooldownForHierarchy(paired.transform, 0.15f);
    }

    private IEnumerator PlayExitFxAndSfxNextFrame(Vector2 dir)
    {
        yield return null; 
        FXManager.Instance?.PlayTeleport(paired.transform.position, dir);
        AudioManager.Instance?.PlayTeleport();    
    }
}