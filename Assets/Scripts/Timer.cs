using UnityEngine;

public class Timer : MonoBehaviour
{
    public float timer;
    private bool isRunning = false;

    public void StartTimer()
    {
        isRunning = true;
    }

    public void StopTimer()
    {
        isRunning = false;
    }

    void Update()
    {
        if (isRunning)
        {
            timer += Time.deltaTime;
        }
    }
}
