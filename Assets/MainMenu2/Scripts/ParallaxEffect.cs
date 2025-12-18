using UnityEngine;

public class ParallaxEffect : MonoBehaviour
{
    public Transform mainCamera;
    public float[] parallaxScales;
    public Transform[] backgrounds;

    private Vector3 previousPlayerPosition;

    float playerMovement;
    float parallaxMovement;
    Vector3 newPosition;


    void Start()
    {
        mainCamera = MainMenu.MainMenuManager.instance.mainCamera.transform;
        previousPlayerPosition = mainCamera.position;
    }

    void Update()
    {
        playerMovement = mainCamera.position.x - previousPlayerPosition.x;

        for (int i = 0; i < backgrounds.Length; i++)
        {
            parallaxMovement = playerMovement * parallaxScales[i];
            newPosition = backgrounds[i].position + new Vector3(parallaxMovement, 0, 0);
            backgrounds[i].position = newPosition;
        }

        previousPlayerPosition = mainCamera.position;
    }
}
