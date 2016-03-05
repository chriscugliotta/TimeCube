using UnityEngine;
using System.Collections;

public class PlayerClimbCheckListener : MonoBehaviour
{
    // The parent script
    PlayerController script;

    // Initialization
    void Start()
    {
        script = transform.parent.GetComponent<PlayerController>();
    }

    // Fires on collision
    void OnTriggerEnter2D(Collider2D other)
    {
        // Debug
        // Debug.Log("OnTriggerEnter2D! fT = " + Time.fixedTime + ", name = " + gameObject.name + ", other = " + other.collider2D.gameObject.name);

        // Call parent script
        script.OnTriggerEnterClimbCheck(other);
    }
}
