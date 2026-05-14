using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class AddressablesUpdater : MonoBehaviour
{
    IEnumerator Start()
    {
        var initHandle = Addressables.InitializeAsync(false);
        yield return initHandle;

        if (initHandle.Status != AsyncOperationStatus.Succeeded)
        {
            Debug.LogError("Addressables init failed.");
            Addressables.Release(initHandle);
            yield break;
        }

        var checkHandle = Addressables.CheckForCatalogUpdates(false);
        yield return checkHandle;

        if (checkHandle.Status != AsyncOperationStatus.Succeeded)
        {
            Debug.LogError("CheckForCatalogUpdates failed.");
            Addressables.Release(checkHandle);
            Addressables.Release(initHandle);
            yield break;
        }

        List<string> catalogsToUpdate = checkHandle.Result;

        if (catalogsToUpdate != null && catalogsToUpdate.Count > 0)
        {
            Debug.Log("Catalog updates found. Updating...");

            var updateHandle = Addressables.UpdateCatalogs(catalogsToUpdate, false);
            yield return updateHandle;

            if (updateHandle.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogError("UpdateCatalogs failed.");
                Addressables.Release(updateHandle);
                Addressables.Release(checkHandle);
                Addressables.Release(initHandle);
                yield break;
            }

            Debug.Log("Catalogs updated.");
            Addressables.Release(updateHandle);
        }
        else
        {
            Debug.Log("No catalog updates found.");
        }

        Addressables.Release(checkHandle);

        // Keep initHandle alive while you continue using Addressables.
        // Do NOT release it here if your app will keep using Addressables.
    }
}