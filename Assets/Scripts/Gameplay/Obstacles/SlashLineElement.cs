using UnityEngine;

public class SlashLineElement : MonoBehaviour, IBallInteractor
{
    [Tooltip("Décalage de 90° (0..3) si ton sprite n'est pas aligné avec le monde.")]
    public int rotationOffsetSteps = 0;

    [Tooltip("Inverse la direction résultante (180°). Utile si gauche/droite sont inversées visuellement.")]
    public bool invert180 = false;

    public void OnBallHit(BallRunner ball)
    {
        int steps = (Mathf.RoundToInt(transform.eulerAngles.z / 90f) + rotationOffsetSteps) & 3;
        bool isSlash = (steps % 2 == 0); // 0,2 = "/", 1,3 = "\"
        Vector2 d = ball.dir;
        Vector2 nd;
        
        var act = GetComponentInParent<Activable>();
        if (act != null && !act.AllowInteraction()) return;
        

        if (isSlash) // "/"
        {
            if (d == Vector2.left) nd = Vector2.up;
            else if (d == Vector2.right) nd = Vector2.down;
            else if (d == Vector2.up) nd = Vector2.left;
            else /*down*/ nd = Vector2.right;
        }
        else // "\"
        {
            if (d == Vector2.up) nd = Vector2.right;
            else if (d == Vector2.down) nd = Vector2.left;
            else if (d == Vector2.left) nd = Vector2.down;
            else /*right*/ nd = Vector2.up;
        }
        
        if (invert180) nd = -nd;
        ball.SetDirection(nd);
        
        AudioManager.Instance?.PlayHit();
        
        FXManager.Instance?.PlayHit(ball.transform.position, nd);
        
        GetComponent<BreakableOnce>()?.Consume();
    }
    
    
}