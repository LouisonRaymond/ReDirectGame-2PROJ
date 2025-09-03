using UnityEngine;

public class TriangleElement : MonoBehaviour
{
    [Tooltip("Rotation Offset")]
    public int rotationOffsetSteps = 0;
    
    public void OnHypotenuseHit(BallRunner ball)
    {
        var act = GetComponent<Activable>();
        if (act != null && !act.AllowInteraction())
        {
            ball.AddCooldownForHierarchy(transform, 0.20f); 
            return;
        }
        
        int steps = (Mathf.RoundToInt(transform.eulerAngles.z / 90f) + rotationOffsetSteps) & 3;
        bool isBackslash = (steps % 2 == 0); 
        Vector2 d = ball.dir;
        Vector2 nd;

        if (isBackslash) // "\"
        {
            if      (d == Vector2.right) nd = Vector2.down;
            else if (d == Vector2.down)  nd = Vector2.right;
            else if (d == Vector2.left)  nd = Vector2.up;
            else                         nd = Vector2.left; 
        }
        else // "/"
        {
            if      (d == Vector2.right) nd = Vector2.up;
            else if (d == Vector2.up)    nd = Vector2.right;
            else if (d == Vector2.left)  nd = Vector2.down;
            else                         nd = Vector2.left; 
        }
        
        ball.SetDirection(nd);
        
        AudioManager.Instance?.PlayHit();
        
        FXManager.Instance?.PlayHit(ball.transform.position, ball.dir);
        
        GetComponent<BreakableOnce>()?.Consume();
        
        ball.AddCooldownForHierarchy(transform, 0.12f);
    }
    
    public void OnBlockFaceHit(BallRunner ball)
    {
        var act = GetComponent<Activable>();
        if (act != null && !act.AllowInteraction())
        {
            ball.AddCooldownForHierarchy(transform, 0.20f); 
            return;
        }
        
        ball.SetDirection(-ball.dir);
        
        AudioManager.Instance?.PlayHit();
        
        FXManager.Instance?.PlayHit(ball.transform.position, ball.dir);
        
        GetComponent<BreakableOnce>()?.Consume();
        
        ball.AddCooldownForHierarchy(transform, 0.12f); 
    }
}