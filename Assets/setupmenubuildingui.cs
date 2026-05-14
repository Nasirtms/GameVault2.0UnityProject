using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;


public class setupmenubuildingui : MonoBehaviour
{
    [ContextMenu("Do")]
    public void Do()
    {
#if UNITY_EDITOR
        UnityEditor.Undo.RecordObject(gameObject, "setupmenubuildingui");
#endif

        foreach (Transform t in transform)
        {
            if (t == transform)
                continue;

            RectTransform rt = t.GetComponent<RectTransform>();
            if (rt == null)
                rt = t.AddComponent<RectTransform>();

            SpriteRenderer sr = rt.GetComponent<SpriteRenderer>();
            if (sr)
            {
                Image image = rt.GetComponent<Image>();

                if (image == null)
                    image = rt.AddComponent<Image>();

                image.sprite = sr.sprite;

                //await Task.Delay((int)(Time.deltaTime * 1000));

                image.SetNativeSize();

                rt.anchoredPosition3D = new Vector3(rt.anchoredPosition.x, rt.anchoredPosition.y, 0) * 100;

                SpriteFillController sfc = rt.GetComponent<SpriteFillController>();
                if (sfc != null)
                {
                    image.type = Image.Type.Filled;
                    image.fillAmount = sfc.fillAmount;
                    image.fillMethod = (Image.FillMethod)(int)sfc.fillMethod;
                    image.fillOrigin = sfc.fillOrigin;
                    image.fillClockwise = sfc.clockwise;
                }

                sr.enabled = false;
            }
        }
    }
}
