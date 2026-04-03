using UnityEngine;

public class Timer : MonoBehaviour
{
    public float timer;

    void Update()
    {
        timer += Time.deltaTime; 

    }
}
