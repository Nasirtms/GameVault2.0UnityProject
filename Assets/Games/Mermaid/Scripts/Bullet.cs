using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class Bullet : MonoBehaviour
{
    public LayerMask targetLayer;

    [HideInInspector] public float damage = 1;
    public object shooter;

    private Rigidbody2D rb;
    private Collider2D col;

    private float _lifeTime = 8f;
    private float _lifeTimer;
    public float currentBetAmount;

    private void OnEnable()
    {
        _lifeTimer = 0f;
        isCannonCard = false;

        if (rb) { rb.velocity = Vector2.zero; rb.angularVelocity = 0f; }

        if (_srs != null && _orig != null)
            for (int i = 0; i < _srs.Length; i++) _srs[i].color = _orig[i];

        //currentBetAmount = Manager.currentBetAmoun;
    }
    public void TintAll(Color c)
    {
        if (_srs == null) return;
        for (int i = 0; i < _srs.Length; i++) _srs[i].color = c;
    }

    void Update()
    {
        _lifeTimer += Time.deltaTime;
        if (_lifeTimer >= _lifeTime)
        {
            BulletPool.Instance.Release(gameObject);
        }

    }
    public bool isCannonCard = false;

    private SpriteRenderer[] _srs;
    private Color[] _orig;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        _srs = GetComponentsInChildren<SpriteRenderer>(true);

        if (_srs != null && _srs.Length > 0)
        {
            _orig = new Color[_srs.Length];
            for (int i = 0; i < _srs.Length; i++) _orig[i] = _srs[i].color;
        }

        if (col.sharedMaterial == null)
        {
            var mat = new PhysicsMaterial2D("Bullet2DMat")
            {
                bounciness = 1f,
                friction = 0f
            };
            col.sharedMaterial = mat;
        }
        else
        {
            col.sharedMaterial.bounciness = 1f;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (LockManager.GetLockedFish() == null)
        {
            gameObject.layer = LayerMask.NameToLayer("Bullet");
            targetLayer = LayerMask.GetMask("Fish");
        }

        GameObject other = collision.gameObject;
        if ((targetLayer.value & (1 << other.layer)) != 0)
        {
            if (other == null)
            {
                Debug.LogWarning("Fish GameObject reference is null, skipping...");
                return;
            }

            Fish fishComponent = other.GetComponent<Fish>();
            if (fishComponent != null)
            {
                fishComponent.lastAttacker = shooter;
                float damageAmount = isCannonCard ? int.MaxValue : damage;
                Debug.Log($"bulletHit ___ damage: {damageAmount} ___ currentBetAmount: {currentBetAmount}");
                fishComponent.TakeDamage(damageAmount, gameObject.name, currentBetAmount);
                //if (isCannonCard)
                //    fishComponent.TakeDamage(int.MaxValue, gameObject.name, currentBetAmount);
                //else
                //    fishComponent.TakeDamage(damage, gameObject.name, currentBetAmount);

                BulletPool.Instance.Release(gameObject);
                return;
            }
            return;
        }
        if (collision.contactCount > 0)
        {
            //Vector2 normal = collision.GetContact(0).normal;
            //Vector2 reflected = Vector2.Reflect(rb.velocity, normal) * 1f;
            ////rb.velocity = reflected;

            //if (reflected.sqrMagnitude > 0.01f)
            //{
            //    float angle = Mathf.Atan2(reflected.y, reflected.x) * Mathf.Rad2Deg;
            //    transform.rotation = Quaternion.Euler(0, 0, angle);
            //}

            StartCoroutine(nameof(SetDirection));
        }
    }

    IEnumerator SetDirection()
    {
        yield return new WaitForEndOfFrame();

        Debug.Log($"bulletdirection __ rb.velocity: {rb.velocity} ___ atan2: {(Mathf.Atan2(rb.velocity.y, rb.velocity.x) * Mathf.Rad2Deg) - 90}");
        transform.rotation = Quaternion.Euler(0, 0, (Mathf.Atan2(rb.velocity.y, rb.velocity.x) * Mathf.Rad2Deg) - 90);
    }
}