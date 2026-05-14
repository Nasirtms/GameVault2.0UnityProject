using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class FeedbackStar : MonoBehaviour
{
    //public EventTrigger eventTrigger;
    public Button button;
    public Image fillImage;

    //[HideInInspector] public FeedbackPanel feedbackPanel;

    //private void Start()
    //{

    //}

    public void SetStarFill(bool state)
    {
        fillImage.gameObject.SetActive(state);
    }

    //public void StarSelected(int number)
    //{
    //    feedbackPanel.StarsGiven = number;
    //}
}
