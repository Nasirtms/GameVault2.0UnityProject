using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SpriteManager : MonoBehaviour
{
    public static SpriteManager Instance;

    public Image backgroundImage;

    public Sprite sprite1; // blinking sprite A
    public Sprite sprite2; // blinking sprite B
    public Sprite sprite3; // Play sprite
    public Sprite sprite4; // Winner sprite

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        backgroundImage.enabled = false;
        StartBlinking();
    }

    private void StartBlinking()
    {
        backgroundImage.enabled = true;
        InvokeRepeating(nameof(SwapSprite), 0f, 9f);
    }

    private void StopBlinking()
    {
        CancelInvoke(nameof(SwapSprite));
    }

    private void SwapSprite()
    {
        backgroundImage.sprite = backgroundImage.sprite == sprite1 ? sprite2 : sprite1;
    }



    public void OnPlayButtonClicked()
    {
        StartCoroutine(Play());
    }

    private IEnumerator Play()
    {
        StopBlinking();
        backgroundImage.sprite = sprite3;

        yield return new WaitForSeconds(1.6f);

        StartBlinking();
    }

    public void ShowWinner()
    {
        StartCoroutine(Winner());
    }

    private IEnumerator Winner()
    {
        StopBlinking();

        backgroundImage.sprite = sprite4;

        yield return new WaitForSeconds(8f);

        StartBlinking();
    }
}

