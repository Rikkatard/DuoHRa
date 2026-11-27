using UnityEngine;


[RequireComponent(typeof(Animator))]
public class PlayerVisuals : MonoBehaviour
{
    [SerializeField] private bool spriteFacesRight = true;
    [SerializeField] private float dodgeAlpha = 0.5f;   // průhlednost sprite během úhybu

    private Animator animator;
    private SpriteRenderer sprite;
    private Color baseColor = Color.white;
    private bool hasSwingingParam;
    private bool hasDodgingParam;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        sprite = GetComponentInChildren<SpriteRenderer>();   // sprite může být na potomkovi
        if (sprite != null) baseColor = sprite.color;

        hasSwingingParam = HasParam("IsSwinging");
        hasDodgingParam = HasParam("IsDodging");
    }

    public void SetFacing(float facing)
    {
        if (sprite == null) return;

        bool faceRight = facing > 0f;
        sprite.flipX = spriteFacesRight ? faceRight : !faceRight;
    }

    public void SetDodgeTint(bool on)
    {
        if (sprite == null) return;

        Color c = baseColor;
        if (on) c.a = dodgeAlpha;
        sprite.color = c;
    }

    public void UpdateAnimator(float moveX, bool grounded, float yVelocity, bool swinging, bool dodging)
    {
        animator.SetFloat("Speedx", Mathf.Abs(moveX), 0.05f, Time.deltaTime);
        animator.SetBool("IsGrounded", grounded);
        animator.SetFloat("YVelocity", yVelocity);
        if (hasSwingingParam) animator.SetBool("IsSwinging", swinging);
        if (hasDodgingParam) animator.SetBool("IsDodging", dodging);
    }

    private bool HasParam(string paramName)
    {
        foreach (AnimatorControllerParameter p in animator.parameters)
        {
            if (p.name == paramName) return true;
        }
        return false;
    }
}