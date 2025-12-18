using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class PlayerMovementControllor : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Player player;

    [Header("Button Move")]
    [SerializeField] public float stepDistance = 1f;
    [SerializeField] public float stepDuration = 0.25f;
    [SerializeField] private Button Left;
    [SerializeField] private Button Right;
    [SerializeField] public Button ExitCatagory;

    private void Start()
    {
        Left.onClick.AddListener(MoveLeft);
        Right.onClick.AddListener(MoveRight);
        //ExitCatagory.onClick.AddListener(SpriteButton.Instance.OnExit);
    }

    public void MoveLeft()
    {
        if (player.switchingEnvironment) return;

        if (!player) return;
        if (player.CurrentState == Player.playerState.Walking) return;
        player.movingByButtons = true;

        if (player.clickTargetIndicator != null && player.clickTargetIndicator.parent == null)
            player.clickTargetIndicator.SetParent(player.transform);

        // Left/right = negative/positive along chosen axis
        float delta = player.Axis == Player.MoveAxis.Z ? -stepDistance : -stepDistance;
        player.MoveBy(delta, stepDuration);
    }

    public void MoveRight()
    {
        if (player.switchingEnvironment) return;

        if (!player) return;
        if (player.CurrentState == Player.playerState.Walking) return;

        player.movingByButtons = true;

        if (player.clickTargetIndicator != null && player.clickTargetIndicator.parent == null)
            player.clickTargetIndicator.SetParent(player.transform);


        float delta = player.Axis == Player.MoveAxis.Z ? stepDistance : stepDistance;
        player.MoveBy(delta, stepDuration);
    }
}
