using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Header("Health & Combat")]
    [SerializeField] protected float health;
    [SerializeField] protected float damage;

    [Header("Recoil Settings")]
    [SerializeField] protected float recoilLength = 0.08f; // Reduced from 0.2f for a shorter recoil
    [SerializeField] protected float recoilFactor = 1f;
    [SerializeField] protected bool isRecoiling = false;

    [Header("Movement & Patrol Settings")]
    [SerializeField] protected float speed = 2f;
    [SerializeField] protected Transform wallCheck;
    [SerializeField] protected Transform pitCheck;
    [SerializeField] protected float checkDistance = 0.4f;
    [SerializeField] protected LayerMask whatIsGround;
    [SerializeField] protected bool facingRight = true;

    protected float recoilTimer;
    protected Rigidbody2D rb;
    protected PlayerController player;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    protected virtual void Start()
    {
        player = PlayerController.Instance;

        if (rb != null)
        {
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        // Keep 3D rotation clean
        transform.eulerAngles = Vector3.zero;

        // Apply scale based on initial facing direction
        Vector3 scale = transform.localScale;
        scale.x = facingRight ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
        transform.localScale = scale;
    }

    protected virtual void Update()
    {
        if (health <= 0)
        {
            Destroy(gameObject);
            return;
        }

        HandleRecoil();

        if (!isRecoiling)
        {
            PatrolCheck();
        }
    }

    protected virtual void FixedUpdate()
    {
        if (!isRecoiling && health > 0)
        {
            Move();
        }
    }

    protected virtual void Move()
    {
        // Explicitly set world direction: true = Right (+X), false = Left (-X)
        float moveDirection = facingRight ? 1f : -1f;
        rb.linearVelocity = new Vector2(moveDirection * speed, rb.linearVelocityY);
    }

    protected virtual void PatrolCheck()
    {
        // Explicit world vector matching current movement direction
        Vector2 moveVector = facingRight ? Vector2.right : Vector2.left;

        // 1. Check for Walls directly ahead
        if (wallCheck != null)
        {
            RaycastHit2D hitWall = Physics2D.Raycast(wallCheck.position, moveVector, checkDistance, whatIsGround);

            if (hitWall.collider != null && !hitWall.collider.transform.IsChildOf(transform))
            {
                Flip();
                return;
            }
        }

        // 2. Check for Ledges/Pits directly below
        if (pitCheck != null)
        {
            RaycastHit2D hitGround = Physics2D.Raycast(pitCheck.position, Vector2.down, checkDistance, whatIsGround);

            if (hitGround.collider == null)
            {
                Flip();
            }
        }
    }

    protected virtual void Flip()
    {
        facingRight = !facingRight;

        // Mirror scale to flip artwork & child transforms
        Vector3 scale = transform.localScale;
        scale.x *= -1f;
        transform.localScale = scale;

        if (rb != null)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocityY);

            // Nudge out of walls in the NEW direction
            float stepOutDirection = facingRight ? 0.05f : -0.05f;
            rb.position = new Vector2(rb.position.x + stepOutDirection, rb.position.y);
        }
    }

    protected virtual void HandleRecoil()
    {
        if (isRecoiling)
        {
            if (recoilTimer < recoilLength)
            {
                recoilTimer += Time.deltaTime;
            }
            else
            {
                // End recoil state and reset timer cleanly
                isRecoiling = false;
                recoilTimer = 0f;
            }
        }
    }

    public virtual void EnemyHit(float _damageDone, Vector2 _hitDirection, float _hitForce)
    {
        health -= _damageDone;

        if (isRecoiling) return;

        isRecoiling = true;
        recoilTimer = 0f;

        // Set velocity directly: instant and independent of Rigidbody mass.
        // Direction keeps the same sign convention as your current callers (player, fireball, spell).
        rb.linearVelocity = -_hitDirection.normalized * (_hitForce * recoilFactor);
    }

    protected void OnCollisionStay2D(Collision2D _other)
    {
        if (_other.gameObject.CompareTag("Player") && player != null && !player.pState.invincible)
        {
            Attack();
            player.HitStopTime(0.05f, 5, .2f);
        }
    }

    protected virtual void Attack()
    {
        if (player != null)
        {
            player.TakeDamage(damage);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector2 moveVector = facingRight ? Vector2.right : Vector2.left;

        if (wallCheck != null)
        {
            Gizmos.DrawRay(wallCheck.position, moveVector * checkDistance);
        }

        if (pitCheck != null)
        {
            Gizmos.DrawRay(pitCheck.position, Vector2.down * checkDistance);
        }
    }
}