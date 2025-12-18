using UnityEngine;

public class LightSelfTrigger : MonoBehaviour
{
    private Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();

        // Safely check for null
        if (animator == null)
        {
            //Debug.LogError($"{gameObject.name} has no Animator component!");
            return;
        }

        // Lowercase name matching the trigger name
        string triggerName = gameObject.name.ToLower();

        // Ensure it's one of the expected ones
        if (triggerName == "light2" || triggerName == "light3" || triggerName == "light4")
        {
            //Debug.Log($"[{gameObject.name}] Triggering animation: {triggerName}");
            animator.SetTrigger(triggerName);
        }
        else
        {
            Debug.LogWarning($"[{gameObject.name}] Name does not match a valid trigger.");
        }
    }
}
