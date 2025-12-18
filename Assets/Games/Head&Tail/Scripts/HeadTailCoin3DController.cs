// Assets/Scripts/Coin/Coin3DController.cs
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using DG.Tweening;
using Unity.VisualScripting;

namespace HeadTailGame
{
    public class HeadTailCoin3DController : MonoBehaviour
    {
        [Header("Script Refs")]
        [SerializeField] private HeadTailBetManager betManager;
        [SerializeField] private HeadTailGameManager headTailGameManager;


        [Header("Model")]
        [SerializeField] private Transform coinModel;


        [Header("Landing Poses (local)")]
        [SerializeField] private Transform headsPose;
        [SerializeField] private Transform tailsPose;

        [Header("Spin")]
        [SerializeField, Min(0.2f)] private float spinDuration = 2f;
        [SerializeField, Min(1)] private int totalRevolutions = 2;
        [SerializeField] private Vector3 localSpinAxis = Vector3.up;
        [SerializeField] private AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);
        public float newBalance;



        //[Header("Force Landing")]
        //[Tooltip("If true coin always lands on Heads, if false it lands on Tails.")]
        //public bool forceHeads = true;

        Coroutine _spinRoutine;

        void Reset()
        {
            coinModel = transform;
            localSpinAxis = Vector3.up;
        }

        public bool IsSpinning => _spinRoutine != null;
        public void SetImmediateFace(int faceId)
        {
            if (!Validate(out var targetRot)) return;
            targetRot = (faceId == HeadTailCoinFaces.Heads ? headsPose.localRotation : tailsPose.localRotation);

            if (_spinRoutine != null) { StopCoroutine(_spinRoutine); _spinRoutine = null; }
            coinModel.localRotation = targetRot;
        }
        //public void Spin(Action<int> onComplete = null)
        //{
        //    // use the single bool
        //    int faceId = forceHeads ? HeadTailCoinFaces.Heads : HeadTailCoinFaces.Tails;

        //    if (_spinRoutine != null) StopCoroutine(_spinRoutine);
        //    _spinRoutine = StartCoroutine(SpinRoutine(faceId, onComplete));
        //}
        public void SpinTo(int faceId, Action<int> onComplete = null)
        {
            if (_spinRoutine != null) StopCoroutine(_spinRoutine);
            _spinRoutine = StartCoroutine(SpinRoutine(faceId, onComplete));
        }
        bool Validate(out Quaternion dummy)
        {
            dummy = Quaternion.identity;
            if (coinModel == null) { Debug.LogError("Coin3DController: coinModel not set."); return false; }
            if (headsPose == null || tailsPose == null)
            {
                Debug.LogError("Coin3DController: Assign headsPose and tailsPose (empty child transforms).");
                return false;
            }
            return true;
        }


        private Tween _spinTween; // track active DOTween

        public bool LastIsWin { get; private set; }     // <-- store backend outcome
        public string LastResultRaw { get; private set; } // optional (e.g., "Head"/"Tail")
        public int LastFaceByServer { get; private set; } // optional parsed 0/1 if backend sends face
        private static int ParseFaceFromServer(string s)
        {
            if (string.IsNullOrEmpty(s)) return HeadTailCoinFaces.Heads;
            s = s.Trim().ToLowerInvariant();
            if (s.StartsWith("h")) return HeadTailCoinFaces.Heads;
            if (s.StartsWith("t")) return HeadTailCoinFaces.Tails;
            return HeadTailCoinFaces.Heads;
        }

        private Tween _spinLoopTween;
        private Coroutine _stopSpinCoroutine;
        public void StartLoopSpin()
        {
            if (!Validate(out _)) return;

            if (_spinLoopTween != null && _spinLoopTween.IsActive())
            {
                _spinLoopTween.Kill();
            }

            float angle = 0f;

            _spinLoopTween = DOTween.To(() => angle, x =>
            {
                angle = x;
                coinModel.localRotation = Quaternion.AngleAxis(x, localSpinAxis);
            }, 360f, 0.6f)
            .SetEase(Ease.Linear)
            .SetLoops(-1, LoopType.Incremental);
        }

        public IEnumerator StopSpinToResultAfterMinimum(int faceId, float minDuration = 2f, int minRevolutions = 2, Action<int> onComplete = null)
        {
            float elapsed = 0f;

            // Wait for minDuration seconds using a simple yield return
            yield return new WaitForSeconds(minDuration);

            // Kill the continuous spin tween if active
            if (_spinLoopTween != null && _spinLoopTween.IsActive())
            {
                _spinLoopTween.Kill();
                _spinLoopTween = null;
            }

            if (!Validate(out _)) yield break;

            Quaternion targetRot = (faceId == HeadTailCoinFaces.Heads)
                ? headsPose.localRotation
                : tailsPose.localRotation;
            HeadTailSoundManager.Instance.StopSpinMusic("Spin");
            // Tween rotation to final pose over 0.8 seconds
            Tween finalRotationTween = coinModel.DOLocalRotateQuaternion(targetRot, 0.8f)
                .SetEase(Ease.OutCubic);

            bool done = false;
            finalRotationTween.OnComplete(() =>
            {
                done = true;
                onComplete?.Invoke(faceId);
            });

            // Wait for tween to complete (this yields without while loop)
            yield return finalRotationTween.WaitForCompletion();
        }
        IEnumerator SpinRoutine(int faceId, Action<int> onComplete)
        {
            if (!Validate(out _)) yield break;

            // Kill any previous tween cleanly
            if (_spinTween != null && _spinTween.IsActive()) _spinTween.Kill();

            Quaternion startRot = coinModel.localRotation;
            Quaternion targetRot = (faceId == HeadTailCoinFaces.Heads)
                ? headsPose.localRotation
                : tailsPose.localRotation;

            //StartCoroutine(CallApiAndWaitForResult());
            float driver = 0f; // 0..1

            _spinTween = DOTween.To(() => driver, v =>
            {
                driver = v;
                float k = ease.Evaluate(driver);

                float angle = 360f * totalRevolutions * k;
                Quaternion spin = Quaternion.AngleAxis(angle, localSpinAxis);

                Quaternion blended = Quaternion.Slerp(startRot, targetRot, k);

                coinModel.localRotation = spin * blended;

            }, 1f, spinDuration)
            .SetEase(Ease.Linear) 
            .OnComplete(() =>
            {
                coinModel.localRotation = targetRot; 
                _spinTween = null;
                _spinRoutine = null;
                onComplete?.Invoke(faceId);
            });

            yield return _spinTween.WaitForCompletion();
        }


        public void SetMeshRenderersEnabled(bool enabled)
        {
            if (coinModel == null) return;

            var meshRenderers = coinModel.GetComponentsInChildren<MeshRenderer>(true);
            foreach (var mr in meshRenderers)
            {
                mr.enabled = enabled;
            }
        }


        #region Call API Code
        //IEnumerator CallApiAndWaitForResult()
        //{
        //    var requestData = new HeadNTailsRequestBody
        //    {
        //        requestId = Guid.NewGuid().ToString(),
        //        gameId = SceneManagement.currentGameID,
        //        betAmount = betManager.CurrentBet,
        //        choice = headTailGameManager.currentPick.ToString(),
        //    };

        //    string json = JsonUtility.ToJson(requestData);
        //    Debug.Log("📦 Sending payload: " + json);

        //    UnityWebRequest request = new UnityWebRequest(ApiEndpoints.HeadsNTails, "POST");
        //    byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        //    request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        //    request.downloadHandler = new DownloadHandlerBuffer();
        //    request.SetRequestHeader("Content-Type", "application/json");

        //    foreach (var header in ApiEndpoints.GetAuthHeaders())
        //        request.SetRequestHeader(header.Key, header.Value);

        //    yield return request.SendWebRequest();

        //    if (request.result == UnityWebRequest.Result.Success)
        //    {
        //        string responseText = request.downloadHandler.text;
        //        Debug.Log("✅ Received response: " + responseText);

        //        // Parse the response
        //        var response = JsonUtility.FromJson<HeadNTailsResponse>(responseText);
        //        Debug.Log($"Result: {response.result}, Win: {response.isWin}, Payout: {response.payout}");

        //    }
        //    else
        //    {
        //        Debug.LogError("❌ API Request failed: " + request.error);
        //    }
        //}
        IEnumerator CallApiAndWaitForResult()
        {

            var requestData = new HeadNTailsRequestBody
            {
                requestId = Guid.NewGuid().ToString(),
                gameId = SceneManagement.currentGameID,
                betAmount = betManager.CurrentBet,
                choice = headTailGameManager.currentPick.ToString(),
            };

            string json = JsonUtility.ToJson(requestData);
            UnityWebRequest request = new UnityWebRequest(ApiEndpoints.HeadsNTails, "POST");
            request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(json));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            foreach (var header in ApiEndpoints.GetAuthHeaders())
                request.SetRequestHeader(header.Key, header.Value);

            yield return request.SendWebRequest();
            
            if (request.responseCode == 401)
            {
                yield return ApiEndpoints.CheckApiResponse(request, ApiEndpoints.HeadsNTails, json, "POST", () => CallApiAndWaitForResult());
                yield break;
            }

            if (request.result == UnityWebRequest.Result.Success)
            {
                string responseText = request.downloadHandler.text;
                //Debug.Log("✅ Received response: " + responseText);

                // Parse the response
                var response = JsonUtility.FromJson<HeadNTailsResponse>(responseText);
                Debug.Log($"Result: {response.result} ____ Win: {response.isWin} ____ Payout: {response.payout} ____ NewBalance: {response.newBalance}");

                LastIsWin = response.isWin;
                newBalance = response.newBalance;
                LastResultRaw = response.result;                  // optional
                LastFaceByServer = ParseFaceFromServer(response.result); // optional

            }
            else
            {
                Debug.LogError("❌ API Request failed: " + request.error);
                // choose a safe default if API fails
                LastIsWin = false;
                LastResultRaw = null;
                LastFaceByServer = HeadTailCoinFaces.Heads;
            }
        }

        public IEnumerator FetchOutcome()
        {
            yield return CallApiAndWaitForResult();
        }

        #endregion


#if UNITY_EDITOR
        [ContextMenu("Capture HeadsPose From Current")]
        void CaptureHeadsPose()
        {
            if (coinModel && headsPose) headsPose.localRotation = coinModel.localRotation;
        }

        [ContextMenu("Capture TailsPose From Current")]
        void CaptureTailsPose()
        {
            if (coinModel && tailsPose) tailsPose.localRotation = coinModel.localRotation;
        }
#endif
    }
}



[Serializable]
public class HeadNTailsRequestBody
{
    public string requestId;
    public string gameId;
    public string choice;
    public double betAmount;
}

[Serializable]
public class HeadNTailsResponse
{
    public string result;
    public bool isWin;
    public int payout;
    public float newBalance;
}