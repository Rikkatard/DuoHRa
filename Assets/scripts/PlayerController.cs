using UnityEngine;
using UnityEngine.InputSystem;

// Koordinátor: čte vstup, vlastní stavový automat a rozhoduje, KDY se co spustí.

[RequireComponent(typeof(SwingAbility), typeof(DodgeAbility), typeof(PlayerVisuals))]
public class PlayerController : MonoBehaviour
{
    // Normal = chůze a skákání | Swinging = houpe se | AirMomentum = setrvačnost po swingu/úhybu | Dodging = úhyb
    private enum PlayerState { Normal, Swinging, AirMomentum, Dodging }

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 3f;
    [SerializeField] private float FallMultiplier = 2.5f;
    [SerializeField] private float LowJumpMultiplier = 5f;

    [Header("Ground")]
    [SerializeField] private Transform GroundCheck;
    [SerializeField] private float GroundCheckRadius = 0.2f;
    [SerializeField] private LayerMask GroundLayer;

    [Header("After swing")]
    [SerializeField] private float momentumTime = 1.5f;
    [SerializeField] private float airControl = 25f;
    [SerializeField] private float airDrag = 6f;

    [Header("Dodge")]
    [SerializeField] private bool allowDodgeWhileSwinging = true;

    private PlayerInputs playerInputs;
    private Rigidbody2D rb;
    private SwingAbility swing;
    private DodgeAbility dodge;
    private PlayerVisuals visuals;

    private bool isGrounded;
    private int groundedcycle;
    private Vector2 speedx;
    private Vector2 movement;
    private float facing = 1f;          // poslední směr, kam hráč mířil (+1 / -1)
    private float dodgeDirection = 1f;

    private PlayerState state = PlayerState.Normal;
    private float stateEnterTime;

    public bool IsInvulnerable => state == PlayerState.Dodging;

    // ---------- SETUP ----------

    private void Awake()
    {
        playerInputs = new PlayerInputs();

        rb = GetComponent<Rigidbody2D>();
        swing = GetComponent<SwingAbility>();
        dodge = GetComponent<DodgeAbility>();
        visuals = GetComponent<PlayerVisuals>();

        playerInputs.Movement.Jump.performed += OnJump;
        playerInputs.Movement.Swing.started += OnSwingStarted;
        playerInputs.Movement.Swing.canceled += OnSwingCanceled;
        playerInputs.Movement.Dodge.started += OnDodge;
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
            moveSpeed += trait.SpeedBonus;
        }
    }

    private void OnEnable() => playerInputs.Enable();
    private void OnDisable() => playerInputs.Disable();

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
        if (s == PlayerState.Dodging)
        {
            dodge.Begin(dodgeDirection, isGrounded);
            visuals.SetDodgeTint(true);
        }
    }

    private void ExitState(PlayerState s)
    {
        switch (s)
        {
            case PlayerState.Swinging:
                swing.Detach();
                break;

            case PlayerState.Dodging:
                dodge.End();
                visuals.SetDodgeTint(false);
                break;
        }
    }

    // ---------- UPDATE ----------

    private void Update()
    {
        PlayerInput();
        visuals.SetFacing(facing);
        CheckisGrounded();
        swing.UpdateTarget(movement.x);
        visuals.UpdateAnimator(speedx.x, isGrounded, rb.linearVelocity.y, state == PlayerState.Swinging, state == PlayerState.Dodging);
        Debug.Log("rychlost: " + moveSpeed);
    }

    private void PlayerInput()
    {
        movement = playerInputs.Movement.Move.ReadValue<Vector2>();
        if (Mathf.Abs(movement.x) > 0.01f) facing = Mathf.Sign(movement.x);
    }

    private void CheckisGrounded()
    {
        isGrounded = Physics2D.OverlapCircle(GroundCheck.position, GroundCheckRadius, GroundLayer);
        if (isGrounded)
        {
            groundedcycle = 0;
            if (state != PlayerState.Dodging) dodge.RefreshAirDodge();
        }
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
                bool landed = isGrounded && Time.time - stateEnterTime > 0.2f;

                if (!swing.IsAttached || swing.IsRopeBlocked() || landed)
                {
                    SetState(PlayerState.AirMomentum);
                    break;
                }

                swing.Tick(movement.x);
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

            case PlayerState.Dodging:
                if (dodge.IsFinished)
                {
                    SetState(isGrounded ? PlayerState.Normal : PlayerState.AirMomentum);
                    break;
                }

                dodge.Tick();
                break;
        }
    }

    // ---------- POHYB ----------

    private void Move()
    {
        speedx = new Vector2(movement.x * moveSpeed, rb.linearVelocity.y);
        rb.linearVelocity = speedx;
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

    // ---------- VSTUPY ----------

    private void OnSwingStarted(InputAction.CallbackContext context)
    {
        if (state == PlayerState.Swinging || state == PlayerState.Dodging) return;

        if (swing.TryAttach())
        {
            dodge.RefreshAirDodge();
            SetState(PlayerState.Swinging);
        }
    }

    private void OnSwingCanceled(InputAction.CallbackContext context)
    {
        if (state == PlayerState.Swinging) SetState(PlayerState.AirMomentum);
    }

    private void OnDodge(InputAction.CallbackContext context)
    {
        if (state == PlayerState.Dodging) return;
        if (state == PlayerState.Swinging && !allowDodgeWhileSwinging) return;
        if (!dodge.CanStart(isGrounded)) return;

        dodgeDirection = Mathf.Abs(movement.x) > 0.01f ? Mathf.Sign(movement.x) : facing;
        SetState(PlayerState.Dodging);
    }

    private void OnJump(InputAction.CallbackContext context)
    {
        if (state == PlayerState.Dodging) return;

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
}