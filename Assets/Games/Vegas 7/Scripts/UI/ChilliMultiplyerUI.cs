using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.GraphicsBuffer;

public class ChilliMultiplyerUI : MonoBehaviour
{
    [SerializeField] RectTransform chilliContent;
    [SerializeField] List<Sprite> multiplyerSprites;
    [SerializeField] Image multiplyerImage;
    bool isVisible;
    float localY;

    private void Awake()
    {
        localY = chilliContent.localPosition.y;
    }


    public void SetData(int count)
    {

        if (multiplyerSprites[count] != null)
        {

            //multiplyerImage.sprite = multiplyerSprites[count];
            if (isVisible)
            {
                StartCoroutine(PlayAnim(count));
            }
            else
            {
                PlayTween();
            }
        }

    }

    IEnumerator PlayAnim(int count) {
        PlayReverse();
        yield return new WaitUntil(() => isVisible == false);
        multiplyerImage.sprite = multiplyerSprites[count];
        PlayTween();

    }
    public void PlayTween() {
        chilliContent.DOLocalMoveY(0, 1).SetEase(Ease.InOutBack).OnComplete(() => { isVisible = true; });
    }
    public void PlayReverse()
    {
        chilliContent.DOLocalMoveY(localY, 1).SetEase(Ease.InOutBack).OnComplete(() => { isVisible = false; }); 
    }
    public void EndAnim() {

        chilliContent.DOLocalMoveY(localY, 1).SetEase(Ease.InOutBack).OnComplete(() => { isVisible = false; gameObject.SetActive(false); });
    }

    [ContextMenu("Test")]
    public void Test()
    {
        SetData(0);
    }
  
}
