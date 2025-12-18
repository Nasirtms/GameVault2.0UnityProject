//not in use// remove this comment if using this script
using UnityEngine;

public class PlayerClickMovement : MonoBehaviour
{
    [SerializeField] private Player player;
    [SerializeField] private Camera mainCamera;

    [Header("Click Settings")]
    [SerializeField] private LayerMask floorLayer;  // assign "Floor" layer in inspector
    [SerializeField] private float moveDuration = 0.5f;

    void Awake()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            HandleClick();
        }
    }

    void HandleClick()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, floorLayer))
        {
            Vector3 target = hit.point;

            // Keep player's Y level fixed (so it doesn't sink into floor)
            target.y = player.transform.position.y;

            // Move player along X or Z axis depending on Player settings
            if (player.Axis == Player.MoveAxis.Z)
                player.MoveTo(target.z, moveDuration);
            else
                player.MoveTo(target.x, moveDuration);
        }
    }
}
