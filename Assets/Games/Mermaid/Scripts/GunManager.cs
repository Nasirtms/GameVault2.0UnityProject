using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;



public class GunManager : MonoBehaviour
{
    public static GunManager Instance { get; private set; }

    public UIBlocker uIBlocker;
    public GunDatabase gunDatabase;
    [HideInInspector] public string GunName;

    [Header("Bullet & Gun Settings")]
    public List<GunSpawnPoint> gunSpawnPoints;
    [SerializeField] private float bulletSpeed = 5f;
    public GameObject bulletContainer;
    private GameObject bulletPrefab;
    [SerializeField] private int playerPoolSize = 20;
    private GameObject fireImaage;
    private Transform firingPoint;
    [HideInInspector] public Transform gunTransform;
    public float bulletDamage_base = 1;
    public AnimationCurve bulletDamageCurve;
    public float bulletDamage_final;
    public float totalBulletsDamage;
    private int randomPos;
    private Quaternion targetRotation;
    private bool isRotating = false, isAutoOn = false, isFastOn = false;

    [Header("Powerup Durations")]
    [SerializeField] private float fastDuration = 5f;

    [Header("UI REFERENCES")]
    [SerializeField] private Button autoOnButton;
    [SerializeField] public Button lockButton;
    [SerializeField] private Button fastOnButton;

    [HideInInspector] public Image autoButtonImage, lockButtonImage;

    [Header("Cannon Card")]
    [SerializeField] private Color cannonCardColor = new Color(0.5f, 0f, 0.5f, 1f); // purple
    private bool nextShotCannonCard = false;
    [SerializeField]float spacing = 0.25f;

    public void ActivateCannonCardOneShot()
    {
        nextShotCannonCard = true;
    }



    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

    }

    // Start is called before the first frame update
    void Start()
    {
        //Manager.Instance.balance += Manager.Instance.betOptions[Manager.Instance.betIndex];
        autoButtonImage = autoOnButton?.GetComponent<Image>();
        lockButtonImage = lockButton?.GetComponent<Image>();
        if (autoOnButton != null) autoOnButton.onClick.AddListener(OnAutoOn);
        if (lockButton != null) lockButton.onClick.AddListener(ToggleLockMode);
        if (fastOnButton != null) fastOnButton.onClick.AddListener(OnFastOn);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            //if (uIBlocker.IsPointerOverUI())
            //    return;
            //if (EventSystem.current.IsPointerOverGameObject())
            //    return;
            if (MainMenu.UIDragHandler.IsPointerOverUIObject())
                return;

            Vector3 clickWorld = GetWorldPointFromScreen(Input.mousePosition);

            Vector3 dir = (clickWorld - gunTransform.position).normalized;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            if (angle >= 0f && angle <= 180f)
            {
                StartAiming(clickWorld);
            }
        }

        if (isRotating)
        {
            gunTransform.rotation = Quaternion.RotateTowards(
                gunTransform.rotation,

                targetRotation,
                360f * Time.deltaTime
            );

            if (Quaternion.Angle(gunTransform.rotation, targetRotation) < 0.5f)
            {
                isRotating = false;
                FireSingle();
            }
        }
    }

    public IEnumerator FireImage()
    {
        if (fireImaage == null) yield break;

        fireImaage.SetActive(true);
        yield return new WaitForSeconds(0.05f);

        if (fireImaage != null)
        {
            fireImaage.SetActive(false);
        }
    }

    private IEnumerator FastRoutine()
    {
        isFastOn = true;
        yield return new WaitForSeconds(fastDuration);
        isFastOn = false;
    }

    public void OnFastOn()
    {
        if (!isFastOn)
        {
            StartCoroutine(FastRoutine());
        }
    }

    public void ToggleLockMode()
    {
        lockButtonImage.color = LockManager.IsLockModeEnabled ? Color.white : new Color32(200, 255, 0, 255);
        LockManager.ToggleLockMode();
    }

    private Vector3 GetWorldPointFromScreen(Vector3 screenPos)
    {
        float zDist = -Manager.Instance.mainCam.transform.position.z;
        return Manager.Instance.mainCam.ScreenToWorldPoint(
            new Vector3(screenPos.x, screenPos.y, zDist)
        );
    }
    int currentgunIndex;
    public void UpdateGun(float bet)
    {
        if (gunTransform != null)
        {
            Destroy(gunTransform.gameObject);
            gunTransform = null;
        }

        int index = Mathf.Clamp((int)bet, 0, gunDatabase.gunLevels.Count - 1);
        currentgunIndex = index;
        var gunLevel = gunDatabase.gunLevels[index];
        //totalBulletsDamage = bulletDamage_base * (index + 1);
        totalBulletsDamage = bulletDamage_base * bulletDamageCurve.Evaluate(Manager.Instance.betOptions[(int)bet]) * (index + 1);
        Debug.Log($"Player Update Gun ___ betIndex: {bet} ___ betamount: {Manager.Instance.betOptions[(int)bet]} ___ gunlevel: {(index+1)} ___ bulletDamage_base: {GunManager.Instance.bulletDamage_base} ___ damageCurveValue: {bulletDamageCurve.Evaluate(Manager.Instance.betOptions[(int)bet])} ___ damage: {totalBulletsDamage}");
        gunTransform = Instantiate(gunLevel.fireSystemPrefab).transform;
        firingPoint = gunTransform.Find("FiringPoint");
        bulletPrefab = gunLevel.bulletPrefab;
        GunName = gunLevel.gunName;
        fireImaage = gunTransform.Find("fire").gameObject;
        SetGunTransform();

        if (bulletContainer != null && bulletPrefab != null)
            BulletPool.Instance.Prewarm(bulletPrefab, playerPoolSize, bulletContainer.transform);
    }

    private void StartAiming(Vector3 worldPoint)
    {
        if (LockManager.IsLockModeEnabled && LockManager.GetLockedFish() != null)
            worldPoint = LockManager.GetLockedFish().transform.position;

        Vector3 dir = (worldPoint - gunTransform.position).normalized;
        targetRotation = Quaternion.LookRotation(Vector3.forward, dir);
        isRotating = true;
    }

    //private void FireSingle()
    //{
    //    float bet = Manager.Instance.betOptions[Manager.Instance.betIndex];
    //    if (Manager.Instance.balance < bet)
    //    {
    //        isAutoOn = false;
    //        return;
    //    }

    //    Manager.Instance.balance -= bet;
    //    Manager.totalBetAmountPerInterval += bet;
    //    Manager.Instance.UpdateBalanceUI();

    //    int index = currentgunIndex;
    //    var gunLevel = gunDatabase.gunLevels[index];
    //    int bulletsToSpawn = gunLevel.numberOfBullets;

    //    Debug.Log($"Spawning {bulletsToSpawn} bullets from gun {index}");

    //    float spacing = 0.25f; // horizontal distance between bullets
    //    Vector3 right = firingPoint.right; // horizontal offset direction

    //    // Center the bullets horizontally
    //    float totalWidth = (bulletsToSpawn - 1) * spacing;
    //    Vector3 startOffset = -right * (totalWidth / 2f);

    //    for (int i = 0; i < bulletsToSpawn; i++)
    //    {
    //        // spawn next to each other horizontally
    //        Vector3 spawnPos = firingPoint.position + startOffset + right * (i * spacing);

    //        GameObject bulletGO = BulletPool.Instance.Get(
    //            bulletPrefab,
    //            spawnPos,
    //            firingPoint.rotation,
    //            bulletContainer != null ? bulletContainer.transform : null
    //        );

    //        if (bulletGO == null)
    //        {
    //            Debug.LogWarning("BulletPool returned null!");
    //            continue;
    //        }

    //        var rb = bulletGO.GetComponent<Rigidbody2D>();
    //        var bs = bulletGO.GetComponent<Bullet>();

    //        if (bs != null)
    //        {
    //            bs.damage = totalBulletsDamage;
    //            bs.shooter = this;
    //            bs.currentBetAmount = Manager.currentBetAmoun;

    //            if (nextShotCannonCard)
    //            {
    //                bs.isCannonCard = true;
    //                bs.TintAll(cannonCardColor);
    //                nextShotCannonCard = false;
    //            }
    //        }

    //        Fish targetFish = LockManager.GetLockedFish();
    //        if (LockManager.IsLockModeEnabled && targetFish != null)
    //        {
    //            bulletGO.layer = LayerMask.NameToLayer("LockedFishBullet");
    //            if (bs != null) bs.targetLayer = LayerMask.GetMask("LockedFish");

    //            // aim directly at target fish
    //            Vector2 dir = (targetFish.transform.position - firingPoint.position).normalized;
    //            if (rb != null)
    //                rb.velocity = dir * (bulletSpeed / 2f) * targetFish.maxSpeed;
    //        }
    //        else
    //        {
    //            bulletGO.layer = LayerMask.NameToLayer("Bullet");
    //            if (bs != null) bs.targetLayer = LayerMask.GetMask("Fish");

    //            // straight bullet fire
    //            Vector2 dir = firingPoint.up; // fire direction (up)
    //            if (rb != null)
    //                rb.velocity = dir * bulletSpeed;
    //        }
    //    }

    //    SimulateRecoil();
    //    StartCoroutine(FireImage());
    //}

    private void FireSingle()
    {
        float bet = Manager.Instance.betOptions[Manager.Instance.betIndex];
        if (Manager.Instance.balance < bet)
        {
            isAutoOn = false;
            return;
        }

        StartCoroutine("FireSingle_Coroutine", bet);
    }

    IEnumerator FireSingle_Coroutine(float bet)
    {
        //Manager.Instance.balance -= bet;
        //Manager.Instance.UpdateBalanceUI();
        Manager.totalBetAmountPerInterval += bet;

        int index = currentgunIndex;
        var gunLevel = gunDatabase.gunLevels[index];
        int bulletsToSpawn = gunLevel.numberOfBullets;

        Debug.Log($"Spawning {bulletsToSpawn} bullets from gun {index}");

        float spacing = 0.25f; // horizontal distance between bullets
        Vector3 right = firingPoint.right; // horizontal offset direction

        // Center the bullets horizontally
        float totalWidth = (bulletsToSpawn - 1) * spacing;
        Vector3 startOffset = -right * (totalWidth / 2f);

        for (int i = 0; i < bulletsToSpawn; i++)
        {
            // spawn next to each other horizontally
            Vector3 spawnPos = firingPoint.position + startOffset + right * (i * spacing);

            GameObject bulletGO = BulletPool.Instance.Get(
                bulletPrefab,
                spawnPos,
                firingPoint.rotation,
                bulletContainer != null ? bulletContainer.transform : null
            );

            if (bulletGO == null)
            {
                Debug.LogWarning("BulletPool returned null!");
                continue;
            }

            var rb = bulletGO.GetComponent<Rigidbody2D>();
            var bs = bulletGO.GetComponent<Bullet>();

            if (bs != null)
            {
                bs.damage = totalBulletsDamage;
                bs.shooter = this;
                bs.currentBetAmount = Manager.currentBetAmoun;

                bs.bulletGuid = Guid.NewGuid().ToString();

                FishWSNetworkMessages.BulletFire_Request bf_req = new FishWSNetworkMessages.BulletFire_Request()
                {
                    requestId = Guid.NewGuid().ToString(),
                    gameId = SceneManagement.currentGameID,
                    bulletId = bs.bulletGuid,
                    bulletCost = bs.currentBetAmount.ToString("G17", CultureInfo.InvariantCulture),
                };
                FishWSNetworkManager.Instance.Send(bf_req);

                if (nextShotCannonCard)
                {
                    bs.isCannonCard = true;
                    bs.TintAll(cannonCardColor);
                    nextShotCannonCard = false;
                }
            }

            Fish targetFish = LockManager.GetLockedFish();
            if (LockManager.IsLockModeEnabled && targetFish != null)
            {
                bulletGO.layer = LayerMask.NameToLayer("LockedFishBullet");
                if (bs != null) bs.targetLayer = LayerMask.GetMask("LockedFish");

                // aim directly at target fish
                Vector2 dir = (targetFish.transform.position - firingPoint.position).normalized;
                if (rb != null)
                    rb.velocity = dir * (bulletSpeed / 2f) * targetFish.maxSpeed;
            }
            else
            {
                bulletGO.layer = LayerMask.NameToLayer("Bullet");
                if (bs != null) bs.targetLayer = LayerMask.GetMask("Fish");

                // straight bullet fire
                Vector2 dir = firingPoint.up; // fire direction (up)
                if (rb != null)
                    rb.velocity = dir * bulletSpeed;
            }
        }

        SimulateRecoil();
        StartCoroutine(FireImage());

        yield return new WaitForEndOfFrame();
    }

    public void BulletFireResponse(FishWSNetworkMessages.BulletFire_Response response)
    {
        if (response.success)
        {
            //Manager.Instance.balance = response.newBalance;
            Manager.Instance.balance -= response.bulletCost;
            Manager.Instance.UpdateBalanceUI();
        }
    }



    //private void FireSingle()
    //{
    //    float bet = Manager.Instance.betOptions[Manager.Instance.betIndex];
    //    if (Manager.Instance.balance < bet) return;
    //    Manager.Instance.balance -= bet;
    //    Manager.totalBetAmountPerInterval += bet;
    //    Manager.Instance.UpdateBalanceUI();
    //    GameObject bulletGO = BulletPool.Instance.Get(
    //        bulletPrefab,
    //        firingPoint.position,
    //        firingPoint.rotation,
    //        bulletContainer != null ? bulletContainer.transform : null
    //    );
    //    bulletGO.transform.rotation = gunTransform.rotation * Quaternion.Euler(0, 0, 0);
    //    var rb = bulletGO.GetComponent<Rigidbody2D>();
    //    var bs = bulletGO.GetComponent<Bullet>();
    //    if (bs != null)
    //    {
    //        //Debug.Log("DAMAGE" + bs.damage);
    //        //bs.damage = Mathf.CeilToInt(bet);
    //        bs.damage = totalBulletsDamage;
    //        bs.shooter = this;

    //        if (nextShotCannonCard)
    //        {
    //            bs.isCannonCard = true;
    //            bs.TintAll(cannonCardColor);
    //            nextShotCannonCard = false;
    //        }
    //    }

    //    Fish targetFish = LockManager.GetLockedFish();
    //    if (LockManager.IsLockModeEnabled && targetFish != null)
    //    {
    //        bulletGO.gameObject.layer = LayerMask.NameToLayer("LockedFishBullet");
    //        if (bs != null) bs.targetLayer = LayerMask.GetMask("LockedFish");

    //        // aim directly at the locked fish
    //        Vector2 dir = (targetFish.transform.position - firingPoint.position).normalized;
    //        //if (rb != null) rb.velocity = dir * bulletSpeed * targetFish.maxSpeed;
    //        if (rb != null) rb.velocity = dir * bulletSpeed/2 * targetFish.maxSpeed;
    //    }
    //    else
    //    {
    //        //Debug.Log("Unlocked fish");
    //        bulletGO.gameObject.layer = LayerMask.NameToLayer("Bullet");
    //        if (bs != null) bs.targetLayer = LayerMask.GetMask("Fish");

    //        // fire straight along the barrel's right axis
    //        Vector2 dir = firingPoint.right;
    //        if (rb != null) rb.velocity = dir * bulletSpeed;
    //    }

    //    //Debug.Log($"[Manager] Fired bullet with bet = {bet}");
    //    SimulateRecoil();
    //    StartCoroutine(FireImage());
    //}

    private void SimulateRecoil(float intensity = 0.2f, float duration = 0.05f)
    {
        if (gunTransform == null) return;

        gunTransform.DOKill();
        //gunTransform.localPosition = new Vector3(-6.15f, -4.33f, 10f);
        gunTransform.localPosition = gunSpawnPoints[randomPos].transform.position;
        Vector3 recoilDirection = -gunTransform.up * intensity;
        gunTransform
            .DOLocalMove(gunTransform.localPosition + recoilDirection, duration)
            .SetLoops(2, LoopType.Yoyo)
            .SetEase(Ease.OutQuad);
    }

    private IEnumerator AutoRoutine()
    {
        isAutoOn = true;

        while (isAutoOn)
        {
            // 1) If lock-mode is on and there's a locked fish, aim at it:
            Fish lockedFish = LockManager.GetLockedFish();
            if (LockManager.IsLockModeEnabled && lockedFish != null)
            {
                Vector3 dir = (lockedFish.transform.position - gunTransform.position).normalized;
                gunTransform.up = dir;
            }
            else
            {
                // 2) Otherwise do your old random‐aim
                Vector3 randV = new Vector3(
                    UnityEngine.Random.Range(0f, 1f),
                    UnityEngine.Random.Range(0f, 1f),
                    -Manager.Instance.mainCam.transform.position.z
                );
                Vector3 randW = Manager.Instance.mainCam.ViewportToWorldPoint(randV);
                Vector3 aimDir = (randW - gunTransform.position).normalized;

                // 3) Clamp direction so the gun never flips below horizon (i.e., only upward direction)
                if (aimDir.y < 0) aimDir.y = 0f;

                gunTransform.up = aimDir;
            }

            // Apply rotation and fire
            FireSingle();

            // wait (and allow click to cancel)
            float wait = isFastOn ? 0.1f : 0.3f;
            float timer = 0f;
            while (timer < wait)
            {
                timer += Time.deltaTime;
                yield return null;
            }
        }

        isAutoOn = false;
        autoButtonImage.color = Color.white;
    }

    public void OnAutoOn()
    {
        if (!isAutoOn)
        {
            autoButtonImage.color = new Color32(200, 255, 0, 255);
            StartCoroutine(AutoRoutine());
        }
        else
        {
            autoButtonImage.color = Color.white;
            isAutoOn = false;
        }
    }


    void SetGunTransform()
    {
        //gunTransform.SetParent(Manager.Instance.playerUI.transform);
        gunTransform.localPosition = gunSpawnPoints[randomPos].transform.position;
        Manager.Instance.playerUI.transform.localPosition = new Vector3(gunSpawnPoints[randomPos].uiPos, Manager.Instance.playerUI.transform.localPosition.y, Manager.Instance.playerUI.transform.localPosition.z);
        gunTransform.localEulerAngles = new Vector3(0f, 0f, 0f);
        //gunTransform.localScale = Vector3.one;
        gunSpawnPoints[randomPos].booked = true;
    }
}
