using System;
using UnityEngine;

public class Group : MonoBehaviour
{
    public System.Guid id = System.Guid.NewGuid();
    
    [SerializeField]
    [Tooltip("Wie viele Karten dieser Gruppe müssen gefunden werden, um die Gruppe zu vervollständigen")]
    [Min(2)]
    public int requiredForMatch = 2;

    [SerializeField]
    [Tooltip("Name der Gruppe (z.B. 'Tiere', 'Früchte')")]
    public string groupName = "Unnamed Group";

    private void OnValidate()
    {
        // Stelle sicher, dass requiredForMatch mindestens 2 ist
        if (requiredForMatch < 2)
        {
            requiredForMatch = 2;
        }
    }
}
