using UnityEngine;

public class TriangleElement : MonoBehaviour
{
    [Tooltip("Décalage si ton sprite n'est pas aligné (0..3 = *90°).")]
    public int rotationOffsetSteps = 0;

    // --- Hypoténuse : tourne de 90° ---
    public void OnHypotenuseHit(BallRunner ball)
    {
        // 1) Gate activable : activer SANS agir, puis ignorer le triangle un court instant
        var act = GetComponent<Activable>();
        if (act != null && !act.AllowInteraction())
        {
            ball.AddCooldownForHierarchy(transform, 0.20f); // laisse la balle traverser
            return;
        }

        // 2) Calcul de direction ("/" ou "\")
        int steps = (Mathf.RoundToInt(transform.eulerAngles.z / 90f) + rotationOffsetSteps) & 3;
        bool isBackslash = (steps % 2 == 0); // 0,2 = "\"
        Vector2 d = ball.dir;
        Vector2 nd;

        if (isBackslash) // "\"
        {
            if      (d == Vector2.right) nd = Vector2.down;
            else if (d == Vector2.down)  nd = Vector2.right;
            else if (d == Vector2.left)  nd = Vector2.up;
            else                         nd = Vector2.left; // up
        }
        else // "/"
        {
            if      (d == Vector2.right) nd = Vector2.up;
            else if (d == Vector2.up)    nd = Vector2.right;
            else if (d == Vector2.left)  nd = Vector2.down;
            else                         nd = Vector2.left; // down
        }

        // 3) Appliquer + cassable éventuel
        ball.SetDirection(nd);
        
        AudioManager.Instance?.PlayHit();
        
        FXManager.Instance?.PlayHit(ball.transform.position, ball.dir);
        
        GetComponent<BreakableOnce>()?.Consume();

        // 4) Petit cooldown pour éviter d'enchaîner sur l'autre collider du triangle
        ball.AddCooldownForHierarchy(transform, 0.12f);
    }

    // --- Faces pleines : demi-tour ---
    public void OnBlockFaceHit(BallRunner ball)
    {
        // 1) Gate activable
        var act = GetComponent<Activable>();
        if (act != null && !act.AllowInteraction())
        {
            ball.AddCooldownForHierarchy(transform, 0.20f); // modifier valeur de desactivation 
            return;
        }

        // 2) Demi-tour
        ball.SetDirection(-ball.dir);
        
        AudioManager.Instance?.PlayHit();
        
        FXManager.Instance?.PlayHit(ball.transform.position, ball.dir);
        
        GetComponent<BreakableOnce>()?.Consume();

        // 3) Antiretrigger local
        ball.AddCooldownForHierarchy(transform, 0.12f); // modifier valeur de desactivation 
    }
}