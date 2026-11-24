using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 19f;
    [SerializeField] private float FallMultiplier = 2.5f;
    [SerializeField] private float LowJumpMultiplier = 5f;
    [Header("Ground")]
    [SerializeField] private Transform GroundCheck;
    [SerializeField] private float GroundCheckRadius = 0.2f;
    [SerializeField] private LayerMask GroundLayer;

    private PlayerInputs playerInputs;
    private Animator animator;
    private Rigidbody2D rb;
    private bool isGrounded;
    private int groundedcycle;
    private Vector2 movement;

    private void Awake()
    {
        playerInputs = new PlayerInputs();
        rb = GetComponent<Rigidbody2D>();
        playerInputs.Movement.Jump.performed += OnJump;

        animator = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        playerInputs.Enable();
       
        
    }

    private void OnDisable()
    {
        
        
        playerInputs.Disable();
    }

    private void Update()
    {
       
       PlayerInput();
        CheckisGrounded();
        HandleVariableJump();
        UpdateAnimatorValues();
    }
    private void PlayerInput()
    {
        movement = playerInputs.Movement.Move.ReadValue<Vector2>();
    }
    private void CheckisGrounded()
    {
        
       isGrounded = Physics2D.OverlapCircle(GroundCheck.position, GroundCheckRadius, GroundLayer);
   if (isGrounded)
        {
            groundedcycle = 0;
        }
   
    }
    private void FixedUpdate()
    {
        Move();
        Fall();
    }

    private void Move()
    {
        rb.linearVelocity = new Vector2(movement.x * moveSpeed, rb.linearVelocity.y);
    }

    private void OnJump(InputAction.CallbackContext context)
    {
        if (isGrounded||groundedcycle < 1)
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

      

    // Přidali jsme třetí a čtvrtý parametr (dampTime a deltaTime), 
    // který zajistí, že hodnota Speed neklesne na nulu okamžitě při puštění klávesy.
    animator.SetFloat("Speed", Mathf.Abs(movement.x), 0.05f, Time.deltaTime);

    animator.SetBool("IsGrounded", isGrounded);

    animator.SetFloat("YVelocity", rb.linearVelocity.y);

    }
}

