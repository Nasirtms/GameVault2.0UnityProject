using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum BonusFishMovementType
{
    Forward,
    ClockWiseRotate,
    AntiClockWiseRotate
}

public class BonusFishController : MonoBehaviour
{
    public BonusFishMovementType movementType = BonusFishMovementType.Forward;

    [Tooltip("Assign big fish GameObject (root), and auto-apply data to all children")]
    public GameObject bigFishRoot;

    [Tooltip("Root List of GameObjects whose children will receive this pattern data")]
    public List<GameObject> rootObject;

    [HideInInspector] public Vector3 destination;
    [HideInInspector] public float speed = 2f;

    private bool isActive = false;

    public void StartMovement(Vector3 target, float moveSpeed)
    {
        destination = target;
        speed = moveSpeed;
        isActive = true;
    }

    private void Update()
    {
        if (!isActive) return;
        float rotationSpeed = 25f;
        switch (movementType)
        {
            case BonusFishMovementType.Forward:
                HandleForwardMovement();
                break;
            case BonusFishMovementType.ClockWiseRotate:
                transform.Rotate(Vector3.forward, -rotationSpeed * Time.deltaTime);
                break;

            case BonusFishMovementType.AntiClockWiseRotate:
                transform.Rotate(Vector3.forward, rotationSpeed * Time.deltaTime);
                break;
        }

        if (movementType == BonusFishMovementType.Forward &&
            Vector3.Distance(transform.position, destination) < 0.1f)
        {
            Destroy(gameObject);
        }
    }

    private void HandleForwardMovement()
    {
        Vector3 direction = (destination - transform.position).normalized;
        float distance = speed * Time.deltaTime;
        transform.position += direction * distance;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            Quaternion.Euler(0, 0, angle),
            180f * Time.deltaTime
        );
    }
}
