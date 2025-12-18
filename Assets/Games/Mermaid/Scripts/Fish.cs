using System;
using System.Collections;
using UnityEngine;
using DG.Tweening;
using Random = UnityEngine.Random;

public enum PowerupType
{
    None,
    Bomb,
    FullScreenBomb,
    CoralReef,
    CannonCard
}



public class Fish : MonoBehaviour
{

    public static Fish CurrentLockedFish { get; private set; }
    public static event Action<Fish> OnFishKilled;
    public static event Action<Fish> OnFishRRemoved;
    public FishData fishData;

    [Header("Fish Settings")]
    public int maxHealth = 1;
    private float currentHealth;

    [Header("Prize")]
    public float prizeAmount = 0f;

    [Header("Spawn Settings")]
    //public float spawnInterval = 2f;
    public float spawnOffset = 0.5f;
    public int batchSize = 1;
    public float batchSpacing = 0.5f;

    [Header("Movement Settings")]
    public float minSpeed = 1f;
    public float maxSpeed = 3f;

    [Header("Powerup Settings")]
    public PowerupType powerupType = PowerupType.None;

    [Header("Powerup Settings (Bomb only)")]
    [Range(0f, 1f)] public float bombSpawnChance = 0.05f; 
    [SerializeField] private string bombChildName = "Bomb";

    private GameObject bombVisual;
    private bool bombArmed;


    [Header("Hit Feedback")]
    [SerializeField] private Color hitColor = Color.red;
    [SerializeField] private GameObject[] explosionPrefab;
    [SerializeField] private float flashDuration = 0.1f;
    [SerializeField] private Color lockedColor = Color.white;

    [HideInInspector] public float speed;
    [HideInInspector] public Vector3 destination;
    [HideInInspector] public object lastAttacker;

    public SpriteRenderer sr;
    private SpriteRenderer[] allRenderers;
    private Color[] originalColors;
    private string bulletName;
    public LayerMask originalLayer;

    public bool isRotatable;
    public float rotationSpeed = 0.5f;

    private GameObject explosionInstance;
    bool isFishKilled;
    public float currentBetamount;
    public bool isInScreen = true;

    private void Awake()
    {
        allRenderers = GetComponentsInChildren<SpriteRenderer>(true);

        if (allRenderers != null && allRenderers.Length > 0)
        {
            // choose a primary (first) for places where you used 'sr'
            sr = allRenderers[0];

            originalColors = new Color[allRenderers.Length];
            for (int i = 0; i < allRenderers.Length; i++)
                originalColors[i] = allRenderers[i].color;
        }
        currentHealth = maxHealth;
        if (prizeAmount <= 0f) prizeAmount = maxHealth;

        float zOffset = UnityEngine.Random.Range(-0.001f, 0.001f);
        transform.position = new Vector3(transform.position.x, transform.position.y, zOffset);
    }

    private void OnEnable()
    {
        // cache the bomb child once per activation if needed
        if (bombVisual == null)
            bombVisual = FindDeepChild(transform, bombChildName);

        // Only Bomb uses probability. Other powerups untouched.
        bombArmed = (powerupType == PowerupType.Bomb) && (Random.value < bombSpawnChance);

        // Visual on only if armed
        if (bombVisual) bombVisual.SetActive(bombArmed);

        isFishKilled = false;
        Manager.onHealthMultiplier += IncreaseHealthMultiplier;
    }
    void IncreaseHealthMultiplier()
    {
        currentHealth = currentHealth * 2;
    
    }

    private void OnDisable()
    {
        bombArmed = false;                  // reset armed state
        if (bombVisual) bombVisual.SetActive(false); // hide on return to pool

        // keep your existing resets
        ResetAllColors();
        if (LockManager.GetLockedFish() == this) LockManager.ClearLockedFish();
        Manager.onHealthMultiplier -= IncreaseHealthMultiplier;
    }

    // utility: breadth-first search by name
    private GameObject FindDeepChild(Transform root, string childName)
    {
        if (string.IsNullOrEmpty(childName)) return null;
        var q = new System.Collections.Generic.Queue<Transform>();
        q.Enqueue(root);
        while (q.Count > 0)
        {
            var t = q.Dequeue();
            if (t.name == childName) return t.gameObject;
            for (int i = 0; i < t.childCount; i++) q.Enqueue(t.GetChild(i));
        }
        return null;
    }

    public void ApplyData(FishData data)
    {
        //Debug.Log($"Nasir : ApplyData 1");
        //sr = transform.GetComponent<SpriteRenderer>();

        fishData = data;
        maxHealth = data.maxHealth;
        currentHealth = maxHealth;
        prizeAmount = data.prizeAmount;
        minSpeed = data.minSpeed;
        maxSpeed = data.maxSpeed;
        spawnOffset = data.spawnOffset;
        batchSize = data.batchSize;
        batchSpacing = data.batchSpacing;
        isRotatable = data.isRotatable;
        //sr.sprite = data.sprite;

        rotationSpeed = data.rotationSpeed;

        //Debug.Log($"Nasir : ApplyData 2");

        //AdjustColliderToSprite();
        //Debug.Log($"Nasir : ApplyData 3");

        //// Assign animator controller
        var animator = GetComponentInChildren<Animator>();
        if (animator != null)
        {
            animator.SetBool(gameObject.name, true);
        }
    }

    public void PlayAnimation() {
        var animator = GetComponentInChildren<Animator>();
        if (animator != null)
        {
            animator.SetBool(gameObject.name, true);
        }
    }

    private void AdjustColliderToSprite()
    {
        if (transform.childCount > 0)
        {
            SpriteRenderer sr = transform.GetChild(0).GetComponent<SpriteRenderer>();
            if (sr == null || sr.sprite == null) return;

            Bounds spriteBounds = sr.sprite.bounds;

            // Resize BoxCollider2D
            BoxCollider2D box = GetComponent<BoxCollider2D>();
            if (box != null)
            {
                Debug.Log($"Changing Collider Bounds: fish: {gameObject.name} ___ bounds Before: {box.offset} , {box.size}");
                box.size = spriteBounds.size;
                box.offset = spriteBounds.center;
                Debug.Log($"Changing Collider Bounds: fish: {gameObject.name} ___ bounds After: {box.offset} , {box.size}");
            }
        }
    }

    public void InitializeMovement(Vector3 dest)
    {
        destination = dest;
    }


    public void SetLocked(bool isLocked)
    {
        if (isLocked)
        {
            if (CurrentLockedFish != null && CurrentLockedFish != this)
                CurrentLockedFish.SetLocked(false);

            CurrentLockedFish = this;
            originalLayer = gameObject.layer;
            gameObject.layer = LayerMask.NameToLayer("LockedFish");
            SetAllColors(Color.green);
        }
        else
        {
            if (CurrentLockedFish == this)
                CurrentLockedFish = null;

            gameObject.layer = originalLayer;
            ResetAllColors();
        }
    }

    public void TakeDamage(float amount, string name, float bulletBetMultiplyer)
    {
        bulletName = name;
        currentHealth -= amount;

        //if (LockManager.IsLockModeEnabled && LockManager.GetLockedFish() == null)
        //    return;

        if (currentHealth > 0)
            StartCoroutine(HitFeedback());
        else
            StartCoroutine(DieWithFeedback());

        currentBetamount = bulletBetMultiplyer;
    }

    private IEnumerator HitFeedback()
    {
        SetAllColors(hitColor);
        SpawnExplosion(ref explosionInstance);
        yield return new WaitForSeconds(flashDuration);
        ResetAllColors();
        CleanupExplosion(ref explosionInstance);
    }

    private void OnDestroy()
    {
        CleanupExplosion(ref explosionInstance);
    }

    public void CallDeathEvent() {
        if (!isFishKilled)
        {
            OnFishRRemoved?.Invoke(this);
            isFishKilled = true;

        }
    }


    public IEnumerator DieWithFeedback()
    {
        SetAllColors(hitColor);
        SpawnExplosion(ref explosionInstance);
        yield return new WaitForSeconds(flashDuration);

        if (LockManager.GetLockedFish() == this)
        {
            Debug.Log("Fish killed while locked: " + gameObject.name);
            LockManager.ClearLockedFish();
        }
        CallDeathEvent();
        OnFishKilled?.Invoke(this);
        CleanupExplosion(ref explosionInstance);
        switch (powerupType)
        {
            case PowerupType.Bomb:
                if (bombArmed) FishManager.Instance.TriggerBomb(this);
                break;

            case PowerupType.FullScreenBomb:
                FishManager.Instance.TriggerFullScreenBomb(this);
                break;

            case PowerupType.CoralReef:
                FishManager.Instance.TriggerCoralReef(this);
                break;

            case PowerupType.CannonCard:
                FishManager.Instance.TriggerCannonCard(this);
                break;
        }



        FishManager.Instance.NotifyFishKilled(this);
        FishPool.Instance.Release(gameObject);
    }

    private void SpawnExplosion(ref GameObject instance)
    {
        int index = GetGunIndex();

        if (index < 0 || index >= explosionPrefab.Length || explosionPrefab[index] == null)
            return;

        if (instance != null) Destroy(instance);

        instance = Instantiate(explosionPrefab[index], transform.position, Quaternion.identity);
        instance.transform.localScale = Vector3.zero;
        instance.transform.DOScale(new Vector3(0.35f, 0.35f, 1f), 0.17f).SetEase(Ease.OutBack);
    }

    private void CleanupExplosion(ref GameObject instance)
    {
        if (instance != null)
        {
            DOTween.Kill(instance.transform);
            Destroy(instance);
            instance = null;
        }
    }

    private int GetGunIndex()
    {
        string key = !string.IsNullOrEmpty(bulletName) ? bulletName : GunManager.Instance.GunName;
        //Debug.Log("Gun Key: " + key);
        if (key.Contains("Bullet1")) { UpdateBotOneScore(); return 0; }
        if (key.Contains("Bullet2")) { UpdateBotTwoScore(); return 1; }
        if (key.Contains("Bullet3")) { UpdateBotThreeScore(); return 2; }
        if (key.Contains("Bullet4")) { UpdateBotFourScore(); return 3; }
        return -1;
    }
    private void SetAllColors(Color c)
    {
        if (allRenderers == null) return;
        for (int i = 0; i < allRenderers.Length; i++)
            if (allRenderers[i] != null) allRenderers[i].color = c;
    }

    private void ResetAllColors()
    {
        if (allRenderers == null || originalColors == null) return;
        for (int i = 0; i < allRenderers.Length; i++)
            if (allRenderers[i] != null) allRenderers[i].color = originalColors[i];
    }

    private void OnMouseDown()
    {
        if (LockManager.IsLockModeEnabled)
        {
            //Debug.Log("Fish Selected: " + gameObject.name);
            LockManager.SetLockedFish(this);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("InScreenTrigger"))
        {
            isInScreen = false;
            Debug.Log($"Fish reached screen exit (InScreenTrigger): Fish: {fishData.fishName}");
            //FishManager.Instance.FishReachedDestination(this);
            transform.position = destination;
        }
    }

    private void UpdateBotOneScore() { }
    private void UpdateBotTwoScore() { }
    private void UpdateBotThreeScore() { }
    private void UpdateBotFourScore() { }
}