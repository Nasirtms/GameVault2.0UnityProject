using UnityEngine.UI;
using UnityEngine;

public class FavoriteAnimationSpawner : MonoBehaviour
{
    public GameObject FavPrefab;
    public GameObject targetObject;

    public void TriggerEffect(Transform toggleTransform)
    {
        Debug.Log("[FavoriteAnimationSpawner] TriggerEffect called.");
        if (FavPrefab == null || targetObject == null || toggleTransform == null)
        {
            Debug.LogWarning("[FavoriteAnimationSpawner] Missing prefab, targetObject, or toggleTransform reference.");
            return;
        }

        SpawnEffectAtTarget(toggleTransform, "ToggleOn");
    }

    private void SpawnEffectAtTarget(Transform target, string inputType)
    {
        Debug.Log("[FavoriteAnimationSpawner] Spawning effect at toggle position.");

        Vector3 screenPos = Camera.main.WorldToScreenPoint(target.position);
        screenPos.z = Mathf.Abs(targetObject.transform.position.z - Camera.main.transform.position.z);

        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(screenPos);

        GameObject effect = Instantiate(FavPrefab, worldPosition, Quaternion.identity);
        Debug.Log($"[FavoriteAnimationSpawner] Instantiated effect at {worldPosition} for input: {inputType}");

        EffectMover mover = effect.GetComponent<EffectMover>();
        if (mover == null)
        {
            mover = effect.AddComponent<EffectMover>();
            Debug.Log("[FavoriteAnimationSpawner] EffectMover component added dynamically.");
        }

        mover.Initialize(targetObject.transform);
    }
}

