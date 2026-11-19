using UnityEngine;

public class SwingPoint : MonoBehaviour
{
    [SerializeField] private Color highlightColor = Color.yellow;
    [SerializeField] private float highlightScale = 1.3f;

    private SpriteRenderer sr;
    private Color baseColor;
    private Vector3 baseScale;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        baseScale = transform.localScale;
        if (sr != null) baseColor = sr.color;
    }

    public void SetHighlighted(bool on)
    {
        if (sr != null) sr.color = on ? highlightColor : baseColor;
        transform.localScale = on ? baseScale * highlightScale : baseScale;
    }
}