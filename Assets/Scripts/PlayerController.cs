using System.Collections;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
    [Header("Horizontal Movement Settings")]
    [SerializeField] private float walkSpeed = 1;
    [Space(5)]

    [Header("Virtical Movement Settings")]
    [SerializeField] private float jumpForce = 45;
    private float jumpBufferCounter = 0;
    [SerializeField] private float jumpBufferTime;
    private float coyoteTimeCounter = 0;
    [SerializeField] private float coyoteTime;
    private int airJumpCounter = 0;
    [SerializeField] private int maxAirJumps;
    [Space(5)]

    [Header("Ground Check Settings")]
    [SerializeField] private Transform groundCheckPoint;
    [SerializeField] private float groundCheckY = 0.2f;
    [SerializeField] private float groundCheckX = 0.5f;
    [SerializeField] private LayerMask whatIsGround;
    [Space(5)]

    [Header("Dash Settings")]
    [SerializeField] private float dashSpeed;
    [SerializeField] private float dashTime;
    [SerializeField] private float dashCooldown;
    [SerializeField] private GameObject dashEffect;
    private bool canDash = true;
    private bool dashed = false; // Default to false so dash works on game launch
    [Space(5)]

    [Header("Attack Settings")]
    [SerializeField] float damage;
    [SerializeField] Transform SideAttackTransform, UpAttackTransform, DownAttackTransform;
    [SerializeField] Vector2 SideAttackArea, UpAttackArea, DownAttackArea;
    bool attack = false;
    float timeBetweenAttack = 0.35f, timeSinceAttack = 0;
    [SerializeField] private LayerMask attackableLayer;
    [SerializeField] GameObject slashEffect;
    bool restoreTime;
    float restoreTimeSpeed;
    [Space(5)]

    [Header("Recoil Settings")]
    [SerializeField] float recoilXTimer = 0;
    [SerializeField] float recoilYTimer = 0;
    [SerializeField] float recoilXSpeed = 30;
    [SerializeField] float recoilYSpeed = 40;
    [Space(5)]

    [Header("Health Settings")]
    public int health;
    public int maxHealth;
    [SerializeField] GameObject bloodSpurt;
    [SerializeField] float hitFlashSpeed;
    public delegate void OnHealthChangedDelegate();
    [HideInInspector] public OnHealthChangedDelegate onHealthChangedCallback;
    private Coroutine hitStopCoroutine;
    float healTimer;
    [SerializeField] float timeToHeal;
    [Space(5)]

    [Header("Mana Settings")]
    [SerializeField] float mana = 0f;
    [SerializeField] float manaGain;
    bool enoughManaToHeal = false;
    [SerializeField] float manaHealCost = 0.3f;
    public delegate void OnManaChangedDelegate();
    [HideInInspector] public OnManaChangedDelegate onManaChangedCallback;
    private bool blockCastOnRelease = false;
    private float castLockTimer = 0f;
    [Space(5)]

    [Header("Spell Settings")]
    [SerializeField] float manaSpellCost = 0.3f;
    [SerializeField] float timeBetweenCast = 0.5f;
    [SerializeField] float spellDamage;
    [SerializeField] float downSpellForce;

    [SerializeField] GameObject sideSpellFireball;
    [SerializeField] GameObject upSpellExplosion;
    [SerializeField] GameObject downSpellFireball;
    float timeSinceCast = 0.3f;
    float castOrHealTimer;
    [Space(5)]

    [HideInInspector] public PlayerStateList pState;
    private Animator anim;
    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private float xAxis, yAxis;
    private float gravity;

    public static PlayerController Instance;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();

        if (rb != null)
        {
            gravity = rb.gravityScale;
        }

        if (health <= 0)
        {
            health = maxHealth;
        }
    }

    private void OnEnable()
    {
        if (Instance != this) return;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public void ResetDashState()
    {
        StopAllCoroutines();
        dashed = false;
        canDash = true;

        if (rb != null)
        {
            rb.gravityScale = gravity <= 0 ? 9.5f : gravity;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (Instance != this) return;
        ResetPlayerState();
    }

    public void ResetPlayerState()
    {
        Time.timeScale = 1f;
        restoreTime = false;
        hitStopCoroutine = null;
        StopAllCoroutines();

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.simulated = true;

            if (gravity <= 0) gravity = 9.5f;
            rb.gravityScale = gravity;
        }

        if (pState != null)
        {
            pState.cutscene = false;
            pState.invincible = false;
            pState.dashing = false;
            pState.casting = false;
            pState.recoilingX = false;
            pState.recoilingY = false;
            pState.healing = false;
            pState.jumping = false;
        }

        if (anim != null)
        {
            anim.SetBool("Casting", false);
            anim.SetBool("Healing", false);
            anim.SetBool("Walking", false);
            anim.SetBool("Jumping", false);

            anim.Rebind();
            anim.Update(0f);
        }        

        if (onHealthChangedCallback != null)
        {
            onHealthChangedCallback.Invoke();
        }

        onManaChangedCallback?.Invoke();

        if (sr != null) sr.material.color = Color.white;

        // Force Dash Reset
        canDash = true;
        dashed = false;
        airJumpCounter = 0;

        if (downSpellFireball != null) downSpellFireball.SetActive(false);
    }

    void Start()
    {
        if (Instance != this) return;
        pState = GetComponent<PlayerStateList>();

        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        if (anim == null) anim = GetComponent<Animator>();

        if (rb != null) gravity = rb.gravityScale;

        ResetPlayerState();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        if (SideAttackTransform != null) Gizmos.DrawWireCube(SideAttackTransform.position, SideAttackArea);
        if (UpAttackTransform != null) Gizmos.DrawWireCube(UpAttackTransform.position, UpAttackArea);
        if (DownAttackTransform != null) Gizmos.DrawWireCube(DownAttackTransform.position, DownAttackArea);
    }

    void Update()
    {
        if (pState.cutscene) return;

        RestoreTimeScale();
        FlashWhileInvincible();

        if (Time.timeScale <= 0.1f) return;

        // ADD THIS HERE:
        if (castLockTimer > 0)
        {
            castLockTimer -= Time.deltaTime;
        }

        GetInputs();
        UpdateJumpVariables();
        if (pState.dashing) return;
        Move();
        Heal();
        CastSpell();
        if (pState.healing) return;
        Flip();
        Jump();
        StartDash();
        Attack();
    }

    private void OnTriggerEnter2D(Collider2D _other)
    {
        if (_other.GetComponent<Enemy>() != null && pState.casting)
        {
            _other.GetComponent<Enemy>().EnemyHit(spellDamage, (_other.transform.position - transform.position).normalized, -recoilYSpeed);
        }
    }

    private void FixedUpdate()
    {
        if (pState.cutscene) return;

        if (!pState.dashing && !pState.recoilingY && !pState.casting)
        {
            rb.gravityScale = gravity;
        }

        if (pState.dashing) return;
        Recoil();
    }

    void Flip()
    {
        if (xAxis < 0)
        {
            transform.localScale = new Vector2(-1, transform.localScale.y);
            pState.lookingRight = false;
        }
        else if (xAxis > 0)
        {
            transform.localScale = new Vector2(1, transform.localScale.y);
            pState.lookingRight = true;
        }
    }

    void GetInputs()
    {
        xAxis = Input.GetAxisRaw("Horizontal");
        yAxis = Input.GetAxisRaw("Vertical");
        attack = Input.GetButtonDown("Attack");

        // Only accumulate time here. Do NOT reset variables on button-up here, 
        // otherwise we clear them before CastSpell() can check them!
        if (Input.GetButton("Cast/Heal"))
        {
            castOrHealTimer += Time.deltaTime;
        }
    }

    private void Move()
    {
        if (pState.recoilingX || pState.casting) return;

        rb.linearVelocity = new Vector2(walkSpeed * xAxis, rb.linearVelocityY);
        anim.SetBool("Walking", rb.linearVelocityX != 0 && Grounded());
    }

    void StartDash()
    {
        if (Grounded())
        {
            dashed = false;
        }

        if (Input.GetButtonDown("Dash") && canDash && !dashed)
        {
            StartCoroutine(Dash());
            dashed = true;
        }
    }

    IEnumerator Dash()
    {
        canDash = false;
        pState.dashing = true;
        anim.SetTrigger("Dashing");
        rb.gravityScale = 0;

        int _dir = pState.lookingRight ? 1 : -1;
        rb.linearVelocity = new Vector2(_dir * dashSpeed, 0);

        if (Grounded() && dashEffect != null)
        {
            Instantiate(dashEffect, transform);
        }

        try
        {
            yield return new WaitForSecondsRealtime(dashTime);
            rb.gravityScale = gravity;
            pState.dashing = false;
            yield return new WaitForSecondsRealtime(dashCooldown);
        }
        finally
        {
            rb.gravityScale = gravity;
            pState.dashing = false;
            canDash = true;

            // Only clear 'dashed' if on the ground, otherwise ground check will clear it when landing
            if (Grounded())
            {
                dashed = false;
            }
        }
    }

    public IEnumerator WalkIntoNewScene(Vector2 _exitDir, float _delay)
    {
        pState.cutscene = true;

        // Reset velocity on spawn
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        float timer = 0f;

        while (timer < _delay)
        {
            timer += Time.deltaTime;

            // Force walk direction
            if (_exitDir.x != 0)
            {
                xAxis = _exitDir.x > 0 ? 1 : -1;
                Move();
            }

            // If the player walks off a ledge during the entrance cutscene, 
            // cancel the forced walk so they don't get launched horizontally in mid-air
            if (!Grounded())
            {
                break;
            }

            yield return null;
        }

        // Stop horizontal force immediately
        xAxis = 0;
        if (rb != null)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocityY);
        }

        // Restore player control
        pState.cutscene = false;
        canDash = true;
        dashed = false;
    }

    void Attack()
    {
        timeSinceAttack += Time.deltaTime;
        if (attack && timeSinceAttack >= timeBetweenAttack)
        {
            timeSinceAttack = 0;
            anim.SetTrigger("Attacking");
            int dir_attack = pState.lookingRight ? 1 : -1;

            if (yAxis == 0 || (yAxis < 0 && Grounded()))
            {
                Instantiate(slashEffect, SideAttackTransform);
                Hit(SideAttackTransform, SideAttackArea, ref pState.recoilingX, recoilXSpeed);
            }
            else if (yAxis > 0)
            {
                SlashEffectAngle(slashEffect, 80, UpAttackTransform);
                Hit(UpAttackTransform, UpAttackArea, ref pState.recoilingY, recoilYSpeed);
            }
            else if (yAxis < 0 && !Grounded())
            {
                SlashEffectAngle(slashEffect, -90, DownAttackTransform);
                Hit(DownAttackTransform, DownAttackArea, ref pState.recoilingY, recoilYSpeed);
            }
        }
    }

    private void Hit(Transform _attackTranform, Vector2 _attackArea, ref bool _recoilDir, float _recoilStrength)
    {
        Collider2D[] objectsToHit = Physics2D.OverlapBoxAll(_attackTranform.position, _attackArea, 0, attackableLayer);

        if (objectsToHit.Length > 0)
        {
            _recoilDir = true;
        }

        for (int i = 0; i < objectsToHit.Length; i++)
        {
            if (objectsToHit[i].GetComponent<Enemy>() != null)
            {
                objectsToHit[i].GetComponent<Enemy>().EnemyHit(damage, (transform.position - objectsToHit[i].transform.position).normalized, _recoilStrength);

                if (objectsToHit[i].CompareTag("Enemy"))
                {
                    Mana += manaGain;
                }
            }
        }
    }

    void SlashEffectAngle(GameObject _slashEffect, int _effectAngle, Transform _attackTransform)
    {
        _slashEffect = Instantiate(_slashEffect, _attackTransform);
        _slashEffect.transform.eulerAngles = new Vector3(0, 0, _effectAngle);
        _slashEffect.transform.localScale = new Vector2(transform.localScale.x, transform.localScale.y);
    }

    void Recoil()
    {
        if (pState.recoilingX)
        {
            if (pState.lookingRight)
            {
                rb.linearVelocity = new Vector2(-recoilXSpeed, 0);
            }
            else
            {
                rb.linearVelocity = new Vector2(recoilXSpeed, 0);
            }
        }

        if (pState.recoilingY)
        {
            rb.gravityScale = 0;
            if (yAxis < 0)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocityX, recoilYSpeed);
            }
            else
            {
                rb.linearVelocity = new Vector2(rb.linearVelocityX, -recoilYSpeed);
            }
            airJumpCounter = 0;
        }
        else if (!pState.casting && !pState.dashing)
        {
            rb.gravityScale = gravity;
        }

        if (pState.recoilingX)
        {
            recoilXTimer += Time.deltaTime;
            if (recoilXTimer >= 0.05f)
            {
                StopRecoilX();
            }
        }

        if (pState.recoilingY)
        {
            recoilYTimer += Time.deltaTime;
            if (recoilYTimer >= 0.05f)
            {
                StopRecoilY();
            }

            if (Grounded())
            {
                StopRecoilY();
            }
        }
    }

    void StopRecoilX()
    {
        recoilXTimer = 0;
        pState.recoilingX = false;
    }

    void StopRecoilY()
    {
        recoilYTimer = 0;
        pState.recoilingY = false;
        rb.gravityScale = gravity;
    }

    public void TakeDamage(float _damage)
    {
        pState.healing = false;
        anim.SetBool("Healing", false);
        healTimer = 0;

        Health -= Mathf.RoundToInt(_damage);
        StartCoroutine(StopTakingDamage());
    }

    IEnumerator StopTakingDamage()
    {
        pState.invincible = true;
        Vector3 spawnPos = new Vector3(transform.position.x, transform.position.y, transform.position.z - 1f);

        if (bloodSpurt != null)
        {
            GameObject _bloodSpurtParticles = Instantiate(bloodSpurt, spawnPos, Quaternion.identity);
            Destroy(_bloodSpurtParticles, .2f);
        }

        if (anim != null) anim.SetTrigger("TakeDamage");

        try
        {
            yield return new WaitForSeconds(1f);
        }
        finally
        {
            if (pState != null) pState.invincible = false;
        }
    }

    void FlashWhileInvincible()
    {
        sr.material.color = pState.invincible ? Color.Lerp(Color.white, Color.black, Mathf.PingPong(Time.time * hitFlashSpeed, 1f))
            : Color.white;
    }

    void RestoreTimeScale()
    {
        if (restoreTime)
        {
            if (Time.timeScale < 1)
            {
                Time.timeScale += Time.unscaledDeltaTime * restoreTimeSpeed;
            }
            else
            {
                Time.timeScale = 1;
                restoreTime = false;
            }
        }
    }

    public void HitStopTime(float _newTimeScale, int _restoreSpeed, float _delay)
    {
        restoreTimeSpeed = _restoreSpeed;
        Time.timeScale = _newTimeScale;
        if (_delay > 0)
        {
            if (hitStopCoroutine != null) StopCoroutine(hitStopCoroutine);
            restoreTime = false;
            hitStopCoroutine = StartCoroutine(StartTimeAgain(_delay));
        }
        else
        {
            restoreTime = true;
        }
    }

    IEnumerator StartTimeAgain(float _delay)
    {
        yield return new WaitForSecondsRealtime(_delay);
        restoreTime = true;
    }

    public int Health
    {
        get { return health; }
        set
        {
            if (health != value)
            {
                health = Mathf.Clamp(value, 0, maxHealth);

                if (onHealthChangedCallback != null)
                {
                    onHealthChangedCallback.Invoke();
                }
            }
        }
    }

    void Heal()
    {
        // small tolerance so float rounding never blocks a heart you can afford
        if (Mana >= manaHealCost - 0.001f) enoughManaToHeal = true;

        bool holding = Input.GetButton("Cast/Heal") && castOrHealTimer > 0.15f;
        bool canHeal = Health < maxHealth && enoughManaToHeal && Mana > 0f
                       && !pState.jumping && !pState.dashing && Grounded()
                       && !pState.invincible && !pState.recoilingX;

        if (holding && canHeal)
        {
            rb.linearVelocity = Vector2.zero;
            anim.SetBool("Walking", false);
            anim.SetBool("Jumping", false);
            pState.healing = true;
            anim.SetBool("Healing", true);
            blockCastOnRelease = true;

            // Never overshoot the end of a heart, so each one costs EXACTLY manaHealCost
            float step = Mathf.Min(Time.deltaTime, timeToHeal - healTimer);
            healTimer += step;
            Mana -= step * (manaHealCost / timeToHeal);

            if (healTimer >= timeToHeal - 0.0001f)
            {
                Health++;
                healTimer = 0;
                enoughManaToHeal = false;
            }
        }
        else
        {
            anim.SetBool("Healing", false);
            pState.healing = false;
            healTimer = 0;
            enoughManaToHeal = false;
        }
    }

    public float Mana
    {
        get { return mana; }
        set
        {
            mana = Mathf.Clamp(value, 0, 1);

            // Invoke the callback so the UI knows to update
            if (onManaChangedCallback != null)
            {
                onManaChangedCallback.Invoke();
            }
        }
    }

    void CastSpell()
    {
        // Evaluate button release here, while blockCastOnRelease and castOrHealTimer are still intact
        if (Input.GetButtonUp("Cast/Heal"))
        {
            if (!blockCastOnRelease && castOrHealTimer <= 0.15f && timeSinceCast >= timeBetweenCast && Mana >= manaSpellCost)
            {
                pState.casting = true;
                timeSinceCast = 0;
                StartCoroutine(CastCoroutine());
            }

            // NOW we reset the timer and block because the release has been fully handled
            castOrHealTimer = 0f;
            blockCastOnRelease = false;
        }
        else
        {
            timeSinceCast += Time.deltaTime;

            // If the button isn't held at all and we aren't healing, ensure timer stays 0
            if (!Input.GetButton("Cast/Heal") && !pState.healing)
            {
                castOrHealTimer = 0f;
            }
        }

        if (Grounded())
        {
            if (downSpellFireball != null) downSpellFireball.SetActive(false);
        }

        if (downSpellFireball != null && downSpellFireball.activeInHierarchy)
        {
            rb.linearVelocity += downSpellForce * Vector2.down;
        }
    }

    IEnumerator CastCoroutine()
    {
        pState.casting = true;
        anim.SetBool("Casting", true);
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = 0;

        try
        {
            yield return new WaitForSeconds(0.3f);

            if (yAxis == 0 || (yAxis < 0 && Grounded()))
            {
                GameObject _fireBall = Instantiate(sideSpellFireball, SideAttackTransform.position, Quaternion.identity);

                if (pState.lookingRight)
                {
                    _fireBall.transform.eulerAngles = Vector3.zero;
                }
                else
                {
                    _fireBall.transform.eulerAngles = new Vector2(_fireBall.transform.eulerAngles.x, 180);
                }
                pState.recoilingX = true;
            }
            else if (yAxis > 0)
            {
                Instantiate(upSpellExplosion, transform);
            }
            else if (yAxis < 0 && !Grounded())
            {
                if (downSpellFireball != null) downSpellFireball.SetActive(true);
            }

            Mana -= manaSpellCost;
            yield return new WaitForSeconds(0.3f);
        }
        finally
        {
            rb.gravityScale = gravity;
            if (anim != null) anim.SetBool("Casting", false);
            pState.casting = false;
        }
    }

    public bool Grounded()
    {
        if (Physics2D.Raycast(groundCheckPoint.position, Vector2.down, groundCheckY, whatIsGround)
            || Physics2D.Raycast(groundCheckPoint.position + new Vector3(groundCheckX, 0, 0), Vector2.down, groundCheckY, whatIsGround)
            || Physics2D.Raycast(groundCheckPoint.position + new Vector3(-groundCheckX, 0, 0), Vector2.down, groundCheckY, whatIsGround))
        {
            return true;
        }
        return false;
    }

    void Jump()
    {
        if (Input.GetKeyUp(KeyCode.Space) && rb.linearVelocityY > 0)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocityX, rb.linearVelocityY * 0.35f);
            jumpBufferCounter = 0;
            pState.jumping = false;
        }

        if (!pState.jumping)
        {
            if (jumpBufferCounter > 0 && coyoteTimeCounter > 0)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocityX, jumpForce);
                pState.jumping = true;
                jumpBufferCounter = 0;
            }
            else if (!Grounded() && airJumpCounter < maxAirJumps && Input.GetKeyDown(KeyCode.Space))
            {
                pState.jumping = true;
                airJumpCounter++;
                rb.linearVelocity = new Vector2(rb.linearVelocityX, jumpForce);
                jumpBufferCounter = 0;
            }
        }

        anim.SetBool("Jumping", !Grounded());
    }

    void UpdateJumpVariables()
    {
        if (Grounded())
        {
            pState.jumping = false;
            coyoteTimeCounter = coyoteTime;
            airJumpCounter = 0;
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            jumpBufferCounter = jumpBufferTime;
        }
        else
        {
            jumpBufferCounter -= Time.deltaTime;
        }
    }
}