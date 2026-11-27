using UnityEngine;


//  - posílá hodnoty do Animatoru, aby se přehrávaly správné animace
[RequireComponent(typeof(Animator))] 
public class PlayerVisuals : MonoBehaviour
{
    [SerializeField] private bool spriteFacesRight = true;   
    [SerializeField] private float dodgeAlpha = 0.5f;        // průhlednost sprite během úhybu (0 = neviditelný, 1 = plný)

    private Animator animator;
    private SpriteRenderer sprite;
    private Color baseColor = Color.white;   

    // Zapamatujeme si, jestli parametry v Animatoru existují.
    // Když neexistují, nebudeme se je snažit nastavovat (jinak by Unity spamovala varování).
    private bool hasSwingingParam;
    private bool hasDodgingParam;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        sprite = GetComponentInChildren<SpriteRenderer>();   // sprite může být na potomkovi
        if (sprite != null) baseColor = sprite.color;        // uložíme si původní barvu

        // Zkontrolujeme, jestli jsou v Animatoru parametry IsSwinging / IsDodging
        hasSwingingParam = HasParam("IsSwinging");
        hasDodgingParam = HasParam("IsDodging");
    }

    // Otočení postavy podle směru pohybu (volá se z PlayerController.Update)
    public void SetFacing(float facing)
    {
        if (sprite == null) return;

        bool faceRight = facing > 0f;
        // flipX zrcadlí sprite; záleží na tom, na jakou stranu je původní obrázek nakreslený
        sprite.flipX = spriteFacesRight ? faceRight : !faceRight;
    }

    // Zapne/vypne průhlednost při úhybu (volá se při vstupu/výstupu ze stavu Dodging)
    public void SetDodgeTint(bool on)
    {
        if (sprite == null) return;

        Color c = baseColor;
        if (on) c.a = dodgeAlpha; 
        sprite.color = c;
    }

    // Každý snímek pošle aktuální stav hráče do Animatoru.
    // Názvy v uvozovkách MUSÍ přesně odpovídat parametrům v Animatoru (rozlišují se velká/malá písmena).
    public void UpdateAnimator(float moveX, bool grounded, float yVelocity, bool swinging, bool dodging)
    {
        animator.SetFloat("Speedx", Mathf.Abs(moveX), 0.05f, Time.deltaTime);
        animator.SetBool("IsGrounded", grounded);
        animator.SetFloat("YVelocity", yVelocity);
        if (hasSwingingParam) animator.SetBool("IsSwinging", swinging);
        if (hasDodgingParam) animator.SetBool("IsDodging", dodging);
    }

    // Pomocná funkce: vrátí true, pokud má Animator parametr s daným jménem
    private bool HasParam(string paramName)
    {
        foreach (AnimatorControllerParameter p in animator.parameters)
        {
            if (p.name == paramName) return true;
        }
        return false;
    }
}