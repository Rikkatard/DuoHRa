using UnityEngine;

// Všechno kolem houpání: hledání bodu, zvýraznění, lano (joint + čára) a síly při swingu.
// O tom, KDY se swing spustí nebo ukončí, rozhoduje PlayerController.
[RequireComponent(typeof(Rigidbody2D), typeof(DistanceJoint2D), typeof(LineRenderer))]
public class SwingAbility : MonoBehaviour
{
    [Header("Detekce")]
    [SerializeField] private float swingRange = 10f;          // jak daleko hráč dosáhne na bod
    [SerializeField] private LayerMask swingPointLayer;       // layer, na kterém jsou body (SwingPoint)
    [SerializeField] private LayerMask obstacleLayer;         // sem dej Ground (zeď mezi hráčem a bodem)
    [SerializeField] private float directionBonus = 1.5f;     // bonus bodům ve směru, kam hráč tlačí

    [Header("Houpání")]
    [SerializeField] private float swingForce = 25f;
    [SerializeField] private float maxSwingSpeed = 15f;
    [SerializeField] private float swingGravityScale = 1.5f;

    private Rigidbody2D rb;
    private DistanceJoint2D joint;
    private LineRenderer rope;
    private float defaultGravityScale;

    private Transform currentTarget;    // zvýrazněný bod (možný cíl)
    private Transform attachedPoint;    // bod, na kterém visíme

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

    // Voláme každý snímek: najde nejlepší bod a zvýrazní ho
    public void UpdateTarget(float inputX)
    {
        Transform newTarget = IsAttached ? attachedPoint : FindBestPoint(inputX);

        if (newTarget == currentTarget) return;

        SetHighlight(currentTarget, false);
        currentTarget = newTarget;
        SetHighlight(currentTarget, true);
    }

    // Zavěsí se na zvýrazněný bod. Vrací false, když žádný není.
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
        return true;
    }

    // Pustí lano a vrátí gravitaci
    public void Detach()
    {
        joint.enabled = false;
        rope.enabled = false;
        rb.gravityScale = defaultGravityScale;
        attachedPoint = null;
    }

    // Je mezi hráčem a bodem zeď?
    public bool IsRopeBlocked()
    {
        return IsAttached &&
               Physics2D.Linecast(transform.position, attachedPoint.position, obstacleLayer).collider != null;
    }

    // Rozhoupávání: síla do stran + strop rychlosti (voláme z FixedUpdate)
    public void Tick(float inputX)
    {
        rb.AddForce(new Vector2(inputX * swingForce, 0f));
        rb.linearVelocity = Vector2.ClampMagnitude(rb.linearVelocity, maxSwingSpeed);
    }

    // Lano se kreslí po všech pohybech, ať se čára neopožďuje za hráčem
    private void LateUpdate()
    {
        if (!IsAttached) return;

        rope.SetPosition(0, transform.position);
        rope.SetPosition(1, attachedPoint.position);
    }

    private Transform FindBestPoint(float inputX)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, swingRange, swingPointLayer);

        Transform best = null;
        float bestScore = float.MaxValue;   // nižší skóre = lepší bod

        foreach (Collider2D hit in hits)
        {
            Vector2 pos = hit.transform.position;

            if (pos.y <= transform.position.y) continue;                                                // musí být nad hráčem
            if (Physics2D.Linecast(transform.position, pos, obstacleLayer).collider != null) continue;  // ne za zdí

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
      
        Gizmos.DrawWireSphere(transform.position, swingRange);
    }
}