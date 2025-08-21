using UnityEngine;

public class ArrowElement : MonoBehaviour, IBallInteractor
{
    [Tooltip("Décalage en pas de 90° si ton sprite ne pointe pas naturellement vers le haut.")]
    public int rotationOffsetSteps = 0;

    public void OnBallHit(BallRunner ball)
    {
        int steps = (Mathf.RoundToInt(transform.eulerAngles.z / 90f) + rotationOffsetSteps) & 3;
        Vector2[] dirs = { Vector2.right, Vector2.up, Vector2.left, Vector2.down };
        ball.SetDirection(dirs[steps]);
        
        AudioManager.Instance?.PlayHit();
        
        FXManager.Instance?.PlayHit(ball.transform.position, dirs[steps]);
    }
}
