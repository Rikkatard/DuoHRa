using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    private enum PlayerState { Normal, Swinging, AirMomentum, Dodging }

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 19f;
    [SerializeField] private float FallMultiplier = 2.5f;
    [SerializeField] private float LowJumpMultiplier = 5f;
    [Header("Visual")]
    [SerializeField] private bool spriteFacesRight = true;  // kam se sprite dívá, když je nepřevrácený

    [Header("Ground")]
    [SerializeField] private Transform GroundCheck;
    [SerializeField] private float GroundCheckRadius = 0.2f;
    [SerializeField] private LayerMask GroundLayer;

    [Header("Swing")]
    [SerializeField] private float swingRange = 6f;
    [SerializeField] private LayerMask swingPointLayer;
    [SerializeField] private float swingForce = 15f;
    [SerializeField] private float maxSwingSpeed = 10f;
    [SerializeField] private float swingGravityScale = 1.5f;

    [Header("Swing detection")]
    [SerializeField] private float directionBonus = 1.5f;
    [SerializeField] private float swingBufferTime = 0.15f;

    [Header("After swing")]
    [SerializeField] private float momentumTime = 1.5f;
    [SerializeField] private float airControl = 25f;
    [SerializeField] private float airDrag = 6f;

    // DODGE
    [Header("Dodge")]
    [SerializeField] private float dodgeSpeed = 16f;
    [SerializeField] private float dodgeDuration = 0.18f;
    [SerializeField] private float dodgeCooldown = 0.5f;
    [SerializeField] private bool allowDodgeWhileSwinging = true;
    [SerializeField] private float dodgeAlpha = 0.5f;   // průhlednost sprite během úhybu

    private PlayerInputs playerInputs;
    private Animator animator;
    private Rigidbody2D rb;
    private DistanceJoint2D joint;
    private LineRenderer rope;
    private SpriteRenderer sprite;

    private bool isGrounded;
    private int groundedcycle;
    private Vector2 movement;

    private PlayerState state = PlayerState.Normal;
    private float stateEnterTime;

    private Transform currentTarget;
    private Transform currentSwingPoint;
    private float swingBufferTimer;
    private float defaultGravityScale;
    private bool animHasSwinging;

    // DODGE
    private bool animHasDodging;
    private float facing = 1f;           // poslední směr, kam hráč mířil (+1 / -1)
    private float dodgeDirection = 1f;
    private float dodgeReadyTime;        // kdy je úhyb znovu k dispozici
    private bool airDodgeAvailable = true;
    private Color spriteBaseColor = Color.white;

    // Nepřátelé/pasti se můžou ptát: if (player.IsInvulnerable) return;
    public bool IsInvulnerable => state == PlayerState.Dodging;

    // ---------- SETUP ----------

    private void Awake()
    {
        playerInputs = new PlayerInputs();
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        joint = GetComponent<DistanceJoint2D>();
        rope = GetComponent<LineRenderer>();
        sprite = GetComponentInChildren<SpriteRenderer>();
        if (sprite != null) spriteBaseColor = sprite.color;

        defaultGravityScale = rb.gravityScale;

        joint.enabled = false;
        rope.enabled = false;
        rope.positionCount = 2;

        animHasSwinging = HasAnimatorParam("IsSwinging");
        animHasDodging = HasAnimatorParam("IsDodging");

        playerInputs.Movement.Jump.performed += OnJump;
        playerInputs.Movement.Swing.started += OnSwingStarted;
        playerInputs.Movement.Swing.canceled += OnSwingCanceled;
        playerInputs.Movement.Dodge.started += OnDodge;   // DODGE
    }

    private void OnEnable() => playerInputs.Enable();
    private void OnDisable() => playerInputs.Disable();

    private bool HasAnimatorParam(string paramName)
    {
        foreach (AnimatorControllerParameter p in animator.parameters)
        {
            if (p.name == paramName) return true;
        }
        return false;
    }

    // ---------- STATE MACHINE ----------

    private void SetState(PlayerState newState)
    {
        if (state == newState) return;

        ExitState(state);
        state = newState;
        stateEnterTime = Time.time;
        EnterState(newState);
    }

    private void EnterState(PlayerState s)
    {
        switch (s)
        {
            case PlayerState.Swinging:
                rb.gravityScale = swingGravityScale;
                rope.enabled = true;
                joint.enabled = true;
                break;

            case PlayerState.Dodging:   // DODGE
                rb.gravityScale = 0f;   // úhyb letí rovně, bez pádu
                rb.linearVelocity = new Vector2(dodgeDirection * dodgeSpeed, 0f);
                if (sprite != null)
                {
                    Color c = spriteBaseColor;
                    c.a = dodgeAlpha;
                    sprite.color = c;
                }
                break;
        }
    }

    private void ExitState(PlayerState s)
    {
        switch (s)
        {
            case PlayerState.Swinging:
                joint.enabled = false;
                rope.enabled = false;
                rb.gravityScale = defaultGravityScale;
                currentSwingPoint = null;
                break;

            case PlayerState.Dodging:   // DODGE
                rb.gravityScale = defaultGravityScale;
                if (sprite != null) sprite.color = spriteBaseColor;
                dodgeReadyTime = Time.time + dodgeCooldown;
                break;
        }
    }

    // ---------- UPDATE ----------

    private void Update()
    {
        PlayerInput();
        UpdateFacing();
        CheckisGrounded();
        UpdateSwingTarget();
        HandleSwingBuffer();
        UpdateAnimatorValues();
    }

    private void LateUpdate()
    {
        if (state == PlayerState.Swinging && currentSwingPoint != null)
        {
            rope.SetPosition(0, transform.position);
            rope.SetPosition(1, currentSwingPoint.position);
        }
    }

    private void PlayerInput()
    {
        movement = playerInputs.Movement.Move.ReadValue<Vector2>();

        // DODGE: pamatuj si, kam hráč naposledy mířil
        if (Mathf.Abs(movement.x) > 0.01f) facing = Mathf.Sign(movement.x);
    }

    private void CheckisGrounded()
    {
        isGrounded = Physics2D.OverlapCircle(GroundCheck.position, GroundCheckRadius, GroundLayer);
        if (isGrounded)
        {
            groundedcycle = 0;
            if (state != PlayerState.Dodging) airDodgeAvailable = true;   // DODGE
        }
    }
    private void UpdateFacing()
    {
        if (sprite == null) return;

        bool faceRight = facing > 0f;
        sprite.flipX = spriteFacesRight ? faceRight : !faceRight;
        
    }
    private void FixedUpdate()
    {
        switch (state)
        {
            case PlayerState.Normal:
                Move();
                Fall();
                HandleVariableJump();
                break;

            case PlayerState.Swinging:
                if (currentSwingPoint == null) { SetState(PlayerState.AirMomentum); break; }

                bool blocked = Physics2D.Linecast(transform.position, currentSwingPoint.position, GroundLayer).collider != null;
                bool landed = isGrounded && Time.time - stateEnterTime > 0.2f;

                if (blocked || landed)
                {
                    SetState(PlayerState.AirMomentum);
                    break;
                }

                SwingMove();
                break;

            case PlayerState.AirMomentum:
                float elapsed = Time.time - stateEnterTime;
                if (elapsed >= momentumTime || (isGrounded && elapsed > 0.1f))
                {
                    SetState(PlayerState.Normal);
                    break;
                }

                AirMomentumMove();
                Fall();
                HandleVariableJump();
                break;

            case PlayerState.Dodging:   // DODGE
                if (Time.time - stateEnterTime >= dodgeDuration)
                {
                    // na zemi zpět do chůze, ve vzduchu plynulý doběh
                    SetState(isGrounded ? PlayerState.Normal : PlayerState.AirMomentum);
                    break;
                }

                // držíme rychlost každý krok, ať ji nic nezmění
                rb.linearVelocity = new Vector2(dodgeDirection * dodgeSpeed, 0f);
                break;
        }
    }

    // ---------- POHYB ----------

    private void Move()
    {
        rb.linearVelocity = new Vector2(movement.x * moveSpeed, rb.linearVelocity.y);
    }

    private void SwingMove()
    {
        rb.AddForce(new Vector2(movement.x * swingForce, 0f));
        rb.linearVelocity = Vector2.ClampMagnitude(rb.linearVelocity, maxSwingSpeed);
    }

    private void AirMomentumMove()
    {
        Vector2 v = rb.linearVelocity;

        if (Mathf.Abs(movement.x) > 0.01f)
            v.x = Mathf.MoveTowards(v.x, movement.x * moveSpeed, airControl * Time.fixedDeltaTime);
        else
            v.x = Mathf.MoveTowards(v.x, 0f, airDrag * Time.fixedDeltaTime);

        rb.linearVelocity = v;
    }

    // ---------- DODGE ----------

    private void OnDodge(InputAction.CallbackContext context)
    {
        TryDodge();
    }

    private void TryDodge()
    {
        if (state == PlayerState.Dodging) return;
        if (Time.time < dodgeReadyTime) return;                              // cooldown
        if (state == PlayerState.Swinging && !allowDodgeWhileSwinging) return;
        if (!isGrounded && !airDodgeAvailable) return;                       // ve vzduchu jen jednou

        if (!isGrounded) airDodgeAvailable = false;

        // směr: kam drží hráč, jinak kam naposledy mířil
        dodgeDirection = Mathf.Abs(movement.x) > 0.01f ? Mathf.Sign(movement.x) : facing;

        swingBufferTimer = 0f;   // zapomeň případný čekající swing
        SetState(PlayerState.Dodging);
    }

    // ---------- SWING: DETEKCE ----------

    private Transform FindBestSwingPoint()

    {

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, swingRange, swingPointLayer);
        Debug.Log($"Nalezeno bodu v dosahu: {hits.Length}");
        Transform best = null;
        float bestScore = float.MaxValue;

        foreach (Collider2D hit in hits)
        {
            Vector2 pos = hit.transform.position;

            if (pos.y <= transform.position.y) continue;
            if (Physics2D.Linecast(transform.position, pos, GroundLayer).collider != null) continue;

            float score = Vector2.Distance(transform.position, pos);

            float dx = pos.x - transform.position.x;
            if (Mathf.Abs(movement.x) > 0.01f && Mathf.Sign(dx) == Mathf.Sign(movement.x))
                score -= directionBonus;

            if (score < bestScore)
            {
                bestScore = score;
                best = hit.transform;
            }
        }

        return best;
    }

    private void UpdateSwingTarget()
    {
        Transform newTarget = state == PlayerState.Swinging ? currentSwingPoint : FindBestSwingPoint();

        if (newTarget == currentTarget) return;

        SetHighlight(currentTarget, false);
        currentTarget = newTarget;
        SetHighlight(currentTarget, true);
    }

    private void SetHighlight(Transform t, bool on)
    {
        if (t == null) return;
        if (t.TryGetComponent(out SwingPoint sp)) sp.SetHighlighted(on);
    }

    private void HandleSwingBuffer()
    {
        if (swingBufferTimer <= 0f) return;

        swingBufferTimer -= Time.deltaTime;

        // DODGE: během úhybu se swing nespouští
        if (state != PlayerState.Swinging && state != PlayerState.Dodging && currentTarget != null)
        {
            StartSwing(currentTarget);
            swingBufferTimer = 0f;
        }
    }

    // ---------- SWING: VSTUP ----------

    private void OnSwingStarted(InputAction.CallbackContext context)
    {
        swingBufferTimer = swingBufferTime;
    }

    private void OnSwingCanceled(InputAction.CallbackContext context)
    {
        swingBufferTimer = 0f;
        if (state == PlayerState.Swinging) SetState(PlayerState.AirMomentum);
    }

    private void StartSwing(Transform point)
    {
        currentSwingPoint = point;

        joint.connectedBody = null;
        joint.connectedAnchor = point.position;
        joint.distance = Vector2.Distance(transform.position, point.position);

        airDodgeAvailable = true;   // DODGE: chycení lana obnoví úhyb ve vzduchu

        SetState(PlayerState.Swinging);
    }

    // ---------- SKOK ----------

    private void OnJump(InputAction.CallbackContext context)
    {
        if (state == PlayerState.Dodging) return;   // DODGE: během úhybu se neskáče

        if (state == PlayerState.Swinging)
        {
            SetState(PlayerState.AirMomentum);
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            return;
        }

        if (isGrounded || groundedcycle < 1)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            groundedcycle++;
        }
    }

    private void Fall()
    {
        if (rb.linearVelocity.y < 0)
        {
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * FallMultiplier * Time.fixedDeltaTime;
        }
    }

    private void HandleVariableJump()
    {
        bool releasedJumpEarly = rb.linearVelocity.y > 0 && !playerInputs.Movement.Jump.IsPressed();
        if (releasedJumpEarly)
        {
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * LowJumpMultiplier * Time.fixedDeltaTime;
        }
    }

    private void UpdateAnimatorValues()
    {
        animator.SetFloat("Speed", Mathf.Abs(movement.x), 0.05f, Time.deltaTime);
    
        animator.SetBool("IsGrounded", isGrounded);
        animator.SetFloat("YVelocity", rb.linearVelocity.y);
        if (animHasSwinging) animator.SetBool("IsSwinging", state == PlayerState.Swinging);
        if (animHasDodging) animator.SetBool("IsDodging", state == PlayerState.Dodging);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, swingRange);
    }
}