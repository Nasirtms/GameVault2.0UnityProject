using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.EventSystems;
using Random = UnityEngine.Random;

public enum PowerupType
{
    None,
    Bomb,
    FullScreenBomb,
    CoralReef,
    CannonCard
}



public class Fish : MonoBehaviour, IPointerDownHandler
{
    [SerializeField] public string fishGuid;
    [SerializeField] public float hitByPlayerCount = 0;

    public static Fish CurrentLockedFish { get; private set; }
    public static event Action<Fish, FishWSNetworkMessages.FishHit_Response, bool> OnFishKilledByPlayer;
    public static event Action<Fish, FishWSNetworkMessages.FishHit_Response, bool> OnFishKilledByBot;
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
    [SerializeField] private Color lockedColor = Color.green;
    private bool isLocked = false;

    [HideInInspector] public float speed;
    [HideInInspector] public Vector3 destination;
    [HideInInspector] public object lastAttacker;

    [Header("Animation Settings")]
    public bool useCustomAnimBool = false;
    public string animationParameterName = "";

    [Space]
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
    public bool isInScreen = false;

    public bool isDead = false;
    public bool despawnCallSentToBackend = false;

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
        isDead = false;
        isInScreen = false;
        despawnCallSentToBackend = false;

        // cache the bomb child once per activation if needed
        if (bombVisual == null)
            bombVisual = FindDeepChild(transform, bombChildName);

        // Only Bomb uses probability. Other powerups untouched.
        bombArmed = (powerupType == PowerupType.Bomb) && (Random.value < bombSpawnChance);

        // Visual on only if armed
        if (bombVisual) bombVisual.SetActive(bombArmed);

        isFishKilled = false;
        Manager.onHealthMultiplier += IncreaseHealthMultiplier;

        hitByPlayerCount = 0;

        //CompositeCollider2D cc2d = gameObject.GetComponent<CompositeCollider2D>();
        //if (cc2d != null)
        //{
        //    cc2d.GenerateGeometry();
        //}

        //StopCoroutine(nameof(CheckIfOutOfScreen_Coroutine));
        //StartCoroutine(nameof(CheckIfOutOfScreen_Coroutine));
    }
    void IncreaseHealthMultiplier()
    {
        currentHealth = currentHealth * 2;
    
    }

    private void OnDisable()
    {
        //StopCoroutine(nameof(CheckIfOutOfScreen_Coroutine));
        fishGuid = "";
        isInScreen = false;
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
            if (!useCustomAnimBool)
                animator.SetBool(gameObject.name, true);
            else
                animator.SetBool(animationParameterName, true);
        }
    }

    public void PlayAnimation() {
        var animator = GetComponentInChildren<Animator>();
        if (animator != null)
        {
            if (!useCustomAnimBool)
                animator.SetBool(gameObject.name, true);
            else
                animator.SetBool(animationParameterName, true);
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

            this.isLocked = true;
            CurrentLockedFish = this;
            originalLayer = gameObject.layer;
            gameObject.layer = LayerMask.NameToLayer("LockedFish");
            SetAllColors(lockedColor);
        }
        else
        {
            if (CurrentLockedFish == this)
                CurrentLockedFish = null;

            this.isLocked = false;
            gameObject.layer = originalLayer;
            ResetAllColors();
        }
    }

    public void TakeDamageByBot(FishWSNetworkMessages.FishHit_Response response, Bullet bullet, float damageAmount, float betAmount)
    {
        if (isDead)
            return;

        if (response == null)
        {
            currentHealth -= damageAmount;
            currentBetamount = betAmount;

            if (currentHealth > 0)
            {
                StartCoroutine(HitFeedback());
            }
            else
            {
                FishWSNetworkMessages.FishHit_Request fh = new FishWSNetworkMessages.FishHit_Request()
                {
                    requestId = Guid.NewGuid().ToString(),
                    gameId = SceneManagement.currentGameID,
                    bulletId = bullet.bulletGuid,
                    fishId = fishGuid,
                    bulletCost = currentBetamount.ToString("G17", CultureInfo.InvariantCulture),
                    killedByBomb = false,
                    killedByBot = true
                };

                FishWSNetworkManager.Instance.Send(fh);
                Debug.Log($"bulletHit ___ BotManager ___ currentBetAmount: {currentBetamount}");
            }
        }
        else
        {
            if (currentHealth <= 0)
            {
                StartCoroutine(DieWithFeedback(response, false));
            }
        }

        //if (LockManager.IsLockModeEnabled && LockManager.GetLockedFish() == null)
        //    return;
    }

    public void TakeDamageByPlayer(FishWSNetworkMessages.FishHit_Response response)
    {
        if (isDead)
            return;

        hitByPlayerCount = response.hitCount;
        currentBetamount = response.bulletCost;

        //if (LockManager.IsLockModeEnabled && LockManager.GetLockedFish() == null)
        //    return;

        StartCoroutine(HitOrDieWithFeedback(response, false));
    }

    public IEnumerator HitOrDieWithFeedback(FishWSNetworkMessages.FishHit_Response response, bool fromForceKill)
    {
        if (!isDead)
        {
            //SetAllColors(hitColor);
            //SpawnExplosion(ref explosionInstance);

            if (!response.killed)
            {
                //yield return new WaitForSeconds(flashDuration);

                //if (isLocked)
                //{
                //    SetAllColors(lockedColor);
                //}
                //else
                //{
                //    ResetAllColors();
                //}

                //CleanupExplosion(ref explosionInstance);
            }
            else
            {
                isDead = true;

                if (LockManager.GetLockedFish() == this)
                {
                    Debug.Log("Fish killed while locked: " + gameObject.name);
                    LockManager.ClearLockedFish();
                }
                CallDeathEvent();
                OnFishKilledByPlayer?.Invoke(this, response, fromForceKill);
                switch (powerupType)
                {
                    case PowerupType.Bomb:
                        if (!fromForceKill)
                        {
                            if (bombArmed) FishManager.Instance.TriggerBomb(this, response);
                        }
                        break;

                    case PowerupType.FullScreenBomb:
                        if (!fromForceKill)
                        {
                            FishManager.Instance.TriggerFullScreenBomb(this, response);
                        }
                        break;

                    case PowerupType.CoralReef:
                        FishManager.Instance.TriggerCoralReef(this, response);
                        break;

                    case PowerupType.CannonCard:
                        FishManager.Instance.TriggerCannonCard(this, response);
                        break;
                }

                //yield return new WaitForSeconds(flashDuration);
                //CleanupExplosion(ref explosionInstance);

                FishManager.Instance.NotifyFishKilled(this);
                FishPool.Instance.Release(gameObject);
            }
        }

        yield return new WaitForEndOfFrame();
    }

    public void ShowDamageEffect()
    {
        StopCoroutine("ShowDamageEffect_Coroutine");
        StartCoroutine("ShowDamageEffect_Coroutine");
    }

    IEnumerator ShowDamageEffect_Coroutine()
    {
        if (!isDead)
        {
            SetAllColors(hitColor);
            SpawnExplosion(ref explosionInstance);

            yield return new WaitForSeconds(flashDuration);

            if (isLocked)
            {
                SetAllColors(lockedColor);
            }
            else
            {
                ResetAllColors();
            }

            CleanupExplosion(ref explosionInstance);
        }
    }

    private IEnumerator HitFeedback()
    {
        SetAllColors(hitColor);
        SpawnExplosion(ref explosionInstance);
        yield return new WaitForSeconds(flashDuration);
        if (isLocked)
        {
            SetAllColors(lockedColor);
        }
        else
        {
            ResetAllColors();
        }
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


    public IEnumerator DieWithFeedback(FishWSNetworkMessages.FishHit_Response response, bool fromForceKill)
    {
        if (!isDead)
        {
            isDead = true;

            SetAllColors(hitColor);
            SpawnExplosion(ref explosionInstance);
            yield return new WaitForSeconds(flashDuration);

            if (LockManager.GetLockedFish() == this)
            {
                Debug.Log("Fish killed while locked: " + gameObject.name);
                LockManager.ClearLockedFish();
            }
            CallDeathEvent();
            OnFishKilledByBot?.Invoke(this, response, fromForceKill);
            CleanupExplosion(ref explosionInstance);

            switch (powerupType)
            {
                case PowerupType.Bomb:
                    if (!fromForceKill)
                    {
                        if (bombArmed) FishManager.Instance.TriggerBomb(this, response);
                    }
                    break;

                case PowerupType.FullScreenBomb:
                    if (!fromForceKill)
                    {
                        FishManager.Instance.TriggerFullScreenBomb(this, response);
                    }
                    break;

                case PowerupType.CoralReef:
                    FishManager.Instance.TriggerCoralReef(this, response);
                    break;

                case PowerupType.CannonCard:
                    FishManager.Instance.TriggerCannonCard(this, response);
                    break;
            }

            FishManager.Instance.NotifyFishKilled(this);
            FishPool.Instance.Release(gameObject);
        }
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

    //private void OnMouseDown()
    //{
    //    Debug.LogError($"Fish Lock: {fishData.fishName} ___ OnMouseDown 111");
    //    if (LockManager.IsLockModeEnabled)
    //    {
    //        Debug.LogError($"Fish Lock: {fishData.fishName} ___ OnMouseDown 222");
    //        //Debug.Log("Fish Selected: " + gameObject.name);
    //        LockManager.SetLockedFish(this);
    //    }
    //}

    public void OnPointerDown(PointerEventData eventData)
    {
        if (LockManager.IsLockModeEnabled)
        {
            //Debug.Log("Fish Selected: " + gameObject.name);
            LockManager.SetLockedFish(this);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("InScreenTrigger"))
        {
            isInScreen = true;
            Debug.Log($"Fish entered screen (InScreenTrigger): Fish: {fishData.fishName}");
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

            FishExitingScreen();
        }
    }

    public void ForceReachDestination()
    {
        isInScreen = false;
        Debug.Log($"Fish forced to reach destination: Fish: {fishData.fishName}");
        transform.position = destination;
        try
        {
            FishManager.Instance.FishReachedDestination(this);
        }
        catch (System.Exception ex)
        {
            Debug.LogError("FishReachedDestination error: " + gameObject.name + " ___ " + ex.Message);
        }
    }

    IEnumerator CheckIfOutOfScreen_Coroutine()
    {
        yield return new WaitForSeconds(15);

        yield return new WaitUntil(() => IsOutOfCamera());

        //while (gameObject.activeInHierarchy)
        //{
            ForceReachDestination();
        //}
    }

    public bool IsOutOfCamera()
    {
        return !sr.isVisible;
    }

    void FishExitingScreen()
    {
        // fish-hidden code here
    }

    public float GetWinAmountByFormula(float bulletCost)
    {
        return fishData.fishMultiplyer * bulletCost;
    }

    private void UpdateBotOneScore() { }
    private void UpdateBotTwoScore() { }
    private void UpdateBotThreeScore() { }
    private void UpdateBotFourScore() { }

    //    //[ContextMenu("SetupNewFishPrefab")]
    //    [Button]
    //    public void SetupNewFishPrefab()
    //    {
    //#if UNITY_EDITOR
    //        UnityEditor.Undo.RecordObject(gameObject, "SetupNewFishPrefab");

    //        if (UnityEditor.PrefabUtility.IsAnyPrefabInstanceRoot(gameObject))
    //            UnityEditor.PrefabUtility.UnpackPrefabInstance(gameObject, UnityEditor.PrefabUnpackMode.OutermostRoot, UnityEditor.InteractionMode.AutomatedAction);
    //#endif

    //        GameObject fishObject = transform.GetChild(0).gameObject;

    //        SpriteRenderer[] renderers = fishObject.GetComponentsInChildren<SpriteRenderer>(true);

    //        foreach (SpriteRenderer r in renderers)
    //        {
    //            PolygonCollider2D pc2d = r.GetComponent<PolygonCollider2D>();
    //            if (pc2d == null)
    //                pc2d = r.gameObject.AddComponent<PolygonCollider2D>();

    //            pc2d.usedByComposite = true;
    //        }

    //        if (!gameObject.GetComponent<CompositeCollider2D>())
    //            gameObject.AddComponent<CompositeCollider2D>();
    //        else
    //            Debug.Log("SetupNewFishPrefab: Composite Collider already added.");

    //        if (gameObject.GetComponent<BoxCollider2D>())
    //            gameObject.GetComponent<BoxCollider2D>().enabled = false;

    //        gameObject.name = fishObject.name.Replace(" ", "-");

    //        fishObject.transform.localPosition = Vector3.zero;
    //        fishObject.transform.localEulerAngles = new Vector3(0, 0, 270);

    //        Transform bomb = transform.Find("Bomb");

    //        if (bomb != null)
    //        {
    //            bomb.localPosition = new Vector3(0.25f, 0, 0);
    //            bomb.localScale = fishObject.transform.localScale + new Vector3(0.1f, 0.1f, 0.1f);
    //        }
    //        else
    //            Debug.Log("SetupNewFishPrefab: No bomb found.");

    //        if (renderers.Length > 0)
    //            sr = renderers[0];
    //        else
    //            Debug.Log("SetupNewFishPrefab: No renderers found.");

    //#if UNITY_EDITOR
    //        bool prefabCreated;
    //        UnityEditor.PrefabUtility.SaveAsPrefabAssetAndConnect(gameObject, "Assets/Games/OceanKing/Prefabs/Fish/" + gameObject.name + ".prefab", UnityEditor.InteractionMode.AutomatedAction, out prefabCreated);
    //#endif

    //        Debug.Log($"SetupNewFishPrefab: Prefab{(!prefabCreated ? " NOT" : "")} created.");
    //    }

    //[Button]
    //public void SetCollidersTag()
    //{

    //#if UNITY_EDITOR
    //        UnityEditor.Undo.RecordObject(gameObject, "SetCollidersTag");
    //#endif

    //    GameObject fishObject = transform.GetChild(0).gameObject;

    //    SpriteRenderer[] renderers = fishObject.GetComponentsInChildren<SpriteRenderer>(true);

    //    foreach (SpriteRenderer r in renderers)
    //    {
    //        PolygonCollider2D pc2d = r.GetComponent<PolygonCollider2D>();
    //        if (pc2d != null)
    //            pc2d.gameObject.tag = "Fish";
    //    }
    //}

    //    [Button]
    //    public void ChangePolygonColliderSystemToEdgeColliderSystem()
    //    {

    //#if UNITY_EDITOR
    //        UnityEditor.Undo.RecordObject(gameObject, "ChangePolygonColliderSystemToEdgeColliderSystem");
    //#endif

    //        GameObject fishObject = transform.GetChild(0).gameObject;

    //        SpriteRenderer[] renderers = fishObject.GetComponentsInChildren<SpriteRenderer>(true);

    //        foreach (SpriteRenderer r in renderers)
    //        {
    //            PolygonCollider2D pc2d = r.GetComponent<PolygonCollider2D>();
    //            if (pc2d != null)
    //            {
    //                EdgeCollider2D ec2d = r.GetComponent<EdgeCollider2D>();
    //                if (ec2d == null)
    //                    ec2d = r.gameObject.AddComponent<EdgeCollider2D>();

    //                if (pc2d.pathCount > 0)
    //                {
    //                    List<Vector2> pathPoints = new List<Vector2>(pc2d.GetPath(0));
    //                    if (pathPoints.Count > 0)
    //                    {
    //                        pathPoints.Add(new Vector2(pathPoints[0].x, pathPoints[0].y));
    //                        ec2d.points = pathPoints.ToArray();
    //                    }
    //                }

    //                DestroyImmediate(pc2d);
    //            }
    //        }

    //        if (gameObject.GetComponent<CompositeCollider2D>())
    //            DestroyImmediate(gameObject.GetComponent<CompositeCollider2D>());
    //    }

    //    [Button]
    //    public void RemoveEdgeCollidersAndMoveToCompositeSystem()
    //    {
    //#if UNITY_EDITOR
    //        UnityEditor.Undo.RecordObject(gameObject, "RemoveEdgeCollidersAndMoveToCompositeSystem");
    //#endif

    //        GameObject fishObject = transform.GetChild(0).gameObject;

    //        SpriteRenderer[] renderers = fishObject.GetComponentsInChildren<SpriteRenderer>(true);

    //        foreach (SpriteRenderer r in renderers)
    //        {
    //            EdgeCollider2D ec2d = r.GetComponent<EdgeCollider2D>();
    //            if (ec2d != null)
    //                DestroyImmediate(ec2d);

    //            PolygonCollider2D pc2d = r.GetComponent<PolygonCollider2D>();
    //            if (pc2d == null)
    //                pc2d = r.gameObject.AddComponent<PolygonCollider2D>();

    //            pc2d.usedByComposite = true;
    //        }

    //        if (!gameObject.GetComponent<CompositeCollider2D>())
    //            gameObject.AddComponent<CompositeCollider2D>();
    //    }
}