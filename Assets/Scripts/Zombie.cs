using UnityEngine;

public class Zombie : Enemy
{
    protected override void Start()
    {
        base.Start();
        if (rb != null)
        {
            rb.gravityScale = 12f;
        }
        recoilLength = 0.05f; // Custom short recoil length specifically for Zombies
    }

    protected override void Update()
    {
        base.Update();

        // Orient facing direction toward player when not recoiling
        if (!isRecoiling && PlayerController.Instance != null)
        {
            float playerX = PlayerController.Instance.transform.position.x;

            // If player is to the right and we are facing left -> Flip
            if (playerX > transform.position.x && !facingRight)
            {
                Flip();
            }
            // If player is to the left and we are facing right -> Flip
            else if (playerX < transform.position.x && facingRight)
            {
                Flip();
            }
        }
    }

    protected override void PatrolCheck()
    {
        // Optionally keep wall checks so they don't get stuck hugging walls, 
        // but remove pitCheck entirely so they chase the player off ledges.
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

    protected override void FixedUpdate()
    {
        // Chase player using physics instead of transform manipulation
        if (!isRecoiling && health > 0 && PlayerController.Instance != null)
        {
            // Set velocity toward player X position
            float direction = facingRight ? 1f : -1f;
            rb.linearVelocity = new Vector2(direction * speed, rb.linearVelocityY);
        }
    }

    public override void EnemyHit(float _damageDone, Vector2 _hitDirection, float _hitForce)
    {
        base.EnemyHit(_damageDone, _hitDirection, _hitForce);
    }
}