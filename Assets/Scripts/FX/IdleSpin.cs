// Assets/Scripts/FX/IdleSpin.cs
using UnityEngine;

public class IdleSpin : MonoBehaviour
{
    public enum SpinDirection { Clockwise, CounterClockwise }

    [Header("Cible visuelle à tourner (laisse vide -> ce GO)")]
    public Transform target;

    [Header("Paramètres")]
    public SpinDirection direction = SpinDirection.Clockwise;
    [Tooltip("Degrés/seconde (moyen ≈ 90, lent ≈ 30)")]
    public float speed = 90f;

#if UNITY_EDITOR
    [Tooltip("Prévisualiser la rotation en mode Éditeur (hors Play)")]
    public bool previewInEditor = false;   // <-- remplace l'ancien runInEditMode
#endif

    void Reset()
    {
        var art = transform.Find("Art");
        target = art ? art : transform;
    }

    void Update()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying && !previewInEditor) return;
#endif
        if (!target) target = transform;

        // En 2D, Z+ = anti-horaire. Horaire = valeur négative.
        float signedSpeed = (direction == SpinDirection.Clockwise ? -speed : speed);
        target.Rotate(0f, 0f, signedSpeed * Time.deltaTime, Space.Self);
    }
}

