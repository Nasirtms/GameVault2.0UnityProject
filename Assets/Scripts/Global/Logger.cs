using UnityEngine;

public class Logger : MonoBehaviour
{
    public class Logs
    {
        public Logs(bool isActive)
        {
            logsActive = isActive;
        }

        private bool logsActive = false;

        public void Log(string message)
        {
            if (!logsActive)
                return;

            Debug.Log(message);
        }
    }

    public static Logs testLogger = new Logs(true);
    public static Logs liveLogger = new Logs(true);
}
