using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(DistanceJoint2D), typeof(LineRenderer))]
public class SwingAbility : MonoBehaviour
{
    [Header("Swing vizuál")]
    [SerializeField] private GameObject swingVisuals;
    [SerializeField] private SpriteRenderer bodySpriteRenderer;
    [SerializeField] private Transform armPivot;
    [SerializeField] private SpriteRenderer armSprite;
    [SerializeField] private Transform chainOrigin;
    [SerializeField] private SpriteRenderer weaponSprite;

    [Header("Detekce")]
    [SerializeField] private float swingRange = 10f;
    [SerializeField] private LayerMask swingPointLayer;
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private float directionBonus = 1.5f;

    [Header("Houpání")]
    [SerializeField] private float swingForce = 25f;
    [SerializeField] private float maxSwingSpeed = 15f;
    [SerializeField] private float swingGravityScale = 1.5f;

    private Rigidbody2D rb;
    private DistanceJoint2D joint;
    private LineRenderer rope;
    private float defaultGravityScale;

    private Transform currentTarget;
    private Transform attachedPoint;

    public bool IsAttached => attachedPoint != null;
    public bool HasTarget => currentTarget != null;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        joint = GetComponent<DistanceJoint2D>();
        rope = GetComponent<LineRenderer>();

        defaultGravityScale = rb.gravityScale;

        joint.enabled = false;
        rope.enabled = false;
        rope.positionCount = 2;

        SyncSwingVisuals();
    }

    private void Start()
    {
        ApplyTraitStats();
    }

    private void ApplyTraitStats()
    {
        if (TraitRoller.ActiveTraits == null) return;
        foreach (var trait in TraitRoller.ActiveTraits)
        {
            swingForce += trait.SwingBonus;
        }
    }

    public void UpdateTarget(float inputX)
    {
        Transform newTarget = IsAttached ? attachedPoint : FindBestPoint(inputX);

        if (newTarget == currentTarget) return;

        SetHighlight(currentTarget, false);
        currentTarget = newTarget;
        SetHighlight(currentTarget, true);
    }

    public bool TryAttach()
    {
        if (currentTarget == null) return false;

        attachedPoint = currentTarget;

        joint.connectedBody = null;
        joint.connectedAnchor = attachedPoint.position;
        joint.distance = Vector2.Distance(transform.position, attachedPoint.position);
        joint.enabled = true;

        rope.enabled = true;
        rb.gravityScale = swingGravityScale;

        SyncSwingVisuals();
        return true;
    }

    public void Detach()
    {
        joint.enabled = false;
        rope.enabled = false;
        rb.gravityScale = defaultGravityScale;
        attachedPoint = null;

        SyncSwingVisuals();
    }

    public bool IsRopeBlocked()
    {
        return IsAttached &&
               Physics2D.Linecast(transform.position, attachedPoint.position, obstacleLayer).collider != null;
    }

    public void Tick(float inputX)
    {
        rb.AddForce(new Vector2(inputX * swingForce, 0f));
        rb.linearVelocity = Vector2.ClampMagnitude(rb.linearVelocity, maxSwingSpeed);
    }

    private void LateUpdate()
    {
        SyncSwingVisuals();

        if (!IsAttached) return;

        if (armPivot != null)
        {
            Vector2 dir = (Vector2)attachedPoint.position - (Vector2)armPivot.position;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            armPivot.rotation = Quaternion.Euler(0f, 0f, angle);

            bool aimingLeft = dir.x < 0f;
            if (armSprite != null) armSprite.flipY = aimingLeft;
            if (weaponSprite != null) weaponSprite.flipY = aimingLeft;
        }

        Vector3 startPos = chainOrigin != null ? chainOrigin.position : transform.position;
        rope.SetPosition(0, startPos);
        rope.SetPosition(1, attachedPoint.position);
    }

    private void SyncSwingVisuals()
    {
        bool swinging = IsAttached;
        if (swingVisuals != null && swingVisuals.activeSelf != swinging)
            swingVisuals.SetActive(swinging);

        if (bodySpriteRenderer != null && bodySpriteRenderer.enabled == swinging)
            bodySpriteRenderer.enabled = !swinging;
    }

    private Transform FindBestPoint(float inputX)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, swingRange, swingPointLayer);

        Transform best = null;
        float bestScore = float.MaxValue;

        foreach (Collider2D hit in hits)
        {
            Vector2 pos = hit.transform.position;

            if (pos.y <= transform.position.y) continue;
            if (Physics2D.Linecast(transform.position, pos, obstacleLayer).collider != null) continue;

            float score = Vector2.Distance(transform.position, pos);

            float dx = pos.x - transform.position.x;
            if (Mathf.Abs(inputX) > 0.01f && Mathf.Sign(dx) == Mathf.Sign(inputX))
                score -= directionBonus;

            if (score < bestScore)
            {
                bestScore = score;
                best = hit.transform;
            }
        }

        return best;
    }

    private void SetHighlight(Transform t, bool on)
    {
        if (t == null) return;
        if (t.TryGetComponent(out SwingPoint sp)) sp.SetHighlighted(on);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, swingRange);
    }
}