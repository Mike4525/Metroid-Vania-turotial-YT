using UnityEngine;

public class RollingEnemy : Enemy
{
    [Header("Rolling Settings")]
    [SerializeField] private float rollSpeed = 15f;
    [SerializeField] private float attackDuration = 2.5f;
    [SerializeField] private float attackRange = 5f;
    [SerializeField] private float coolDown = 3f;

    private float attackTime = 0f;
    private float cDTimer = 0f;
    private bool attacking = false;
    private Animator anim;

    protected override void Start()
    {
        base.Start();

        if (rb != null)
        {
            rb.gravityScale = 3f;
        }
        speed = rollSpeed;
        anim = GetComponent<Animator>();
        cDTimer = coolDown;
    }

    protected override void Update()
    {
        base.Update();

        if (!attacking && !isRecoiling && health > 0)
        {
            if (cDTimer < coolDown)
            {
                cDTimer += Time.deltaTime;
            }
            else
            {
                DetectPlayer();
            }
        }

        if (attacking)
        {
            if (health <= 0)
            {
                EndAttack(true);
                return;
            }

            attackTime += Time.deltaTime;
            if (attackTime > attackDuration)
            {
                EndAttack(false);
            }
        }
    }

    private void DetectPlayer()
    {
        Vector2 rayDirection = facingRight ? Vector2.right : Vector2.left;
        Vector2 rayOrigin = wallCheck != null ? wallCheck.position : transform.position;
        RaycastHit2D hit = Physics2D.Raycast(rayOrigin, rayDirection, attackRange);

        Debug.DrawRay(rayOrigin, rayDirection * attackRange, Color.red);

        if (hit.collider != null && hit.collider.CompareTag("Player"))
        {
            StartAttack();
        }
    }

    private void StartAttack()
    {
        attacking = true;
        attackTime = 0f;
        if (anim != null)
        {
            anim.SetBool("Attacking", true);
        }
    }

    private void EndAttack(bool isDead)
    {
        attacking = false;
        attackTime = 0f;

        if (isDead)
        {
            cDTimer = coolDown;
        }
        else
        {
            cDTimer = 0f;
        }

        if (anim != null)
        {
            anim.SetBool("Attacking", false);
        }
    }

    protected override void FixedUpdate()
    {
        if (health <= 0) return;

        // While recoiling, completely yield to the base class knockback impulse
        if (isRecoiling)
        {
            return;
        }

        if (attacking)
        {
            float moveDirection = facingRight ? 1f : -1f;
            float targetVelocityX = moveDirection * speed;

            // Smoothly blend velocity out of knockback instead of hard-snapping.
            // This prevents the abrupt awkward jerk when a hit ends.
            rb.linearVelocity = new Vector2(
                Mathf.MoveTowards(rb.linearVelocity.x, targetVelocityX, 35f * Time.fixedDeltaTime),
                rb.linearVelocity.y
            );
        }
        else
        {
            rb.linearVelocity = new Vector2(
                Mathf.MoveTowards(rb.linearVelocity.x, 0f, 15f * Time.fixedDeltaTime),
                rb.linearVelocity.y
            );
        }
    }

    protected override void PatrolCheck()
    {
        if (wallCheck != null && whatIsGround.value != 0)
        {
            float direction = facingRight ? 1f : -1f;
            RaycastHit2D hitWall = Physics2D.Raycast(wallCheck.position, new Vector2(direction, 0), checkDistance, whatIsGround);

            if (hitWall.collider != null && !hitWall.collider.transform.IsChildOf(transform))
            {
                Flip();
            }
        }
    }

    protected override void Flip()
    {
        facingRight = !facingRight;

        if (rb != null)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            float stepOutDirection = facingRight ? 0.1f : -0.1f;
            rb.position = new Vector2(rb.position.x + stepOutDirection, rb.position.y);
            transform.localScale = new Vector2(-transform.localScale.x, transform.localScale.y);
        }
    }
}