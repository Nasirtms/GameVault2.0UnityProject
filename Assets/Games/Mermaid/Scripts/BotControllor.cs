using DG.Tweening;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public enum GunPosition { Top, Bottom }

public class BotController : MonoBehaviour
{
    [HideInInspector]public GunPosition botPosition;
    [SerializeField] private float rotationSpeed = 360f;

    [Header("Bot Settings")]
    [HideInInspector] public float startingBalance = 10f;
    [HideInInspector] public float botShootInterval;

    public Transform botFiringPoint;


    [Header("References")]
    public Transform bulletContainer;
    public float bulletSpeed = 16f;
    private bool _prewarmed;

    private float balance;
    public int betIndex;

    [HideInInspector] public GunSpawnPoint spawnPoint;

    public string ObjName;


    void Start()
    {
        balance = startingBalance;
        ObjName = gameObject.name;
        UpdateUI();

    }
    private Coroutine shootingRoutine;

    public void StartShooting()
    {
        if (shootingRoutine == null)
            shootingRoutine = StartCoroutine(AutoShoot());
    }

    private bool isFiring = false;
    private IEnumerator AutoShoot()
    {
        while (true)
        {
            if (!isFiring)
            {
                Fish[] allFish = FindObjectsOfType<Fish>();
                if (allFish.Length > 0)
                {
                    if (balance >= BotManager.instance.betOptions[betIndex])
                    {
                        Fish targetFish = allFish[Random.Range(0, allFish.Length)];
                        isFiring = true;
                        yield return StartCoroutine(RotateAndFire(targetFish.transform.position));
                        isFiring = false;
                    }
                    else
                    {
                        spawnPoint.uiComponent.SetActive(false);
                        Destroy(gameObject);
                    }
                }
            }
            yield return new WaitForSeconds(botShootInterval);
        }
    }

    public void StopShooting()
    {
        if (shootingRoutine != null)
        {
            StopCoroutine(shootingRoutine);
            shootingRoutine = null;
        }
    }

    public void AddBalance(float prize)
    {
        //balance += prize;
        //Debug.Log($"[Bot {name}] Balance now: {balance:0}");
    }
    IEnumerator RotateAndFire(Vector3 worldTarget)
    {
        Vector3 dir = (worldTarget - botFiringPoint.position).normalized;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        float desiredZ = angle - 90;

        if (botPosition == GunPosition.Top)
        {
            desiredZ = (desiredZ + 360f) % 360f;
            desiredZ = Mathf.Clamp(desiredZ, 90f, 210f);
        }
        else if (botPosition == GunPosition.Bottom)
        {
            desiredZ = Mathf.Clamp(desiredZ, -90f, 90f);
        }

        //Debug.Log("Position" + this.gameObject.name + " " + botPosition);

        Quaternion aim = Quaternion.Euler(0, 0, desiredZ);

        while (Quaternion.Angle(transform.rotation, aim) > 0.5f)
        {
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                aim,
                rotationSpeed * Time.deltaTime
            );
            yield return null;
        }

        // Use actual gun facing direction
        Vector3 shootDirection = transform.up;
        SpawnBullet(shootDirection);
    }



    private void SpawnBullet(Vector3 shootDirection)
    {
        if (balance < BotManager.instance.betOptions[betIndex]) return;
        balance -= BotManager.instance.betOptions[betIndex];

        UpdateUI();

        // Get bullet prefab from GunDatabase
        var gunLevels = GunManager.Instance.gunDatabase.gunLevels;


        GameObject bulletPrefab = gunLevels[Mathf.Clamp(betIndex, 0, gunLevels.Count - 1)].bulletPrefab;

        GameObject bulletGO = BulletPool.Instance.Get(
            bulletPrefab,
            botFiringPoint.position,
            transform.rotation,
            bulletContainer
        );

        if (!_prewarmed && bulletPrefab != null)
        {
            BulletPool.Instance.Prewarm(bulletPrefab, 20, bulletContainer); // bot pool size
            _prewarmed = true;
        }

        bulletGO.transform.rotation = transform.rotation;

        var rb = bulletGO.GetComponent<Rigidbody2D>();
        var bs = bulletGO.GetComponent<Bullet>();

        if (bs != null)
        {
            //bs.damage = Mathf.CeilToInt(BotManager.instance.betOptions[betIndex]);
            //bs.damage = GunManager.Instance.bulletDamage_base * GunManager.Instance.bulletDamageCurve.Evaluate(BotManager.instance.betOptions[betIndex]) * (Mathf.Clamp(betIndex, 0, gunLevels.Count - 1) + 1);
            bs.damage = 1;
            //Debug.Log($"Bot Bullet Spawned ___ betIndex: {betIndex} ___ betamount: {BotManager.instance.betOptions[betIndex]} ___ gunlevel: {(Mathf.Clamp(betIndex, 0, gunLevels.Count - 1) + 1)} ___ bulletDamage_base: {GunManager.Instance.bulletDamage_base} ___ damageCurveValue: {GunManager.Instance.bulletDamageCurve.Evaluate(BotManager.instance.betOptions[betIndex])} ___ damage: {bs.damage}");
            bs.shooter = this;
            bs.currentBetAmount = BotManager.instance.betOptions[betIndex];
            bs.targetLayer = LayerMask.GetMask("Fish");
        }

        if (rb != null)
            rb.velocity = shootDirection.normalized * bulletSpeed;

        SimulateRecoil();
    }

    public void UpdateUI()
    {
        float truncated = Mathf.Floor(balance * 10f) / 10f;
        spawnPoint.amount.text = truncated.ToString("F1");
    }

    private void SimulateRecoil()
    {
        if (transform == null) return;

        transform.DOKill();
        transform
            .DOLocalMoveY(-0.2f, 0.05f)
            .SetRelative()
            .SetLoops(2, LoopType.Yoyo)
            .SetEase(Ease.OutQuad);

    }
}