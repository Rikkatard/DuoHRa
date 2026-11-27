using UnityEngine;


[RequireComponent(typeof(Rigidbody2D))]
public class DodgeAbility : MonoBehaviour
{
    [SerializeField] private float dodgeSpeed = 16f;
    [SerializeField] private float dodgeDuration = 0.18f;   // jak dlouho úhyb trvá (s)
    [SerializeField] private float dodgeCooldown = 0.5f;    // pauza před dalším úhybem

    private Rigidbody2D rb;
    private float defaultGravityScale;

    private float direction = 4f;
    private float startTime;
    private float readyTime;               // kdy je úhyb znovu k dispozici
    private bool airDodgeAvailable = true; // ve vzduchu jen jeden úhyb

    public bool IsFinished => Time.time - startTime >= dodgeDuration;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        defaultGravityScale = rb.gravityScale;
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
            
            dodgeSpeed += trait.DashDistanceBonus;
        }
    }

    // Obnoví úhyb ve vzduchu (po dopadu na zem nebo po chycení lana)
    public void RefreshAirDodge() => airDodgeAvailable = true;

    public bool CanStart(bool grounded)
    {
        return Time.time >= readyTime && (grounded || airDodgeAvailable);
    }

    public void Begin(float dir, bool grounded)
    {
        if (!grounded) airDodgeAvailable = false;

        direction = dir;
        startTime = Time.time;

        rb.gravityScale = 0f;   // úhyb letí rovně
        rb.linearVelocity = new Vector2(direction * dodgeSpeed, 0f);
    }

    // Každý fyzikální krok držíme rychlost, ať ji nic nezmění
    public void Tick()
    {
        rb.linearVelocity = new Vector2(direction * dodgeSpeed, 0f);
    }

    public void End()
    {
        rb.gravityScale = defaultGravityScale;
        readyTime = Time.time + dodgeCooldown;
    }
}