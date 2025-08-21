using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class TriHypTrigger : MonoBehaviour, IBallInteractor
{
    private TriangleElement _parent;

    void Awake()
    {
        _parent = GetComponentInParent<TriangleElement>();
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    public void OnBallHit(BallRunner ball)
    {
        if (_parent != null) _parent.OnHypotenuseHit(ball);
    }
}
