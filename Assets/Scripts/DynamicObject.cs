using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DynamicObject : MonoBehaviour
{
    // Globals
    private Globals globals;
    // Transform history
    private History history;



    // Initialization
    void Start()
    {
        // Initialize variables
        this.globals = GameObject.Find("globals").GetComponent<Globals>();
        this.history = new History(new Transform[1] { transform });
        this.globals.TCM.AddHistory(this.history);

        // Add self to globals
        this.globals.DynamicObjects.Add(this.gameObject);
        this.globals.TCM.AddHistory(this.history);

        // Test
        this.rigidbody2D.AddForce(100f * Vector3.left);
        this.rigidbody2D.AddTorque(10f);
    }



    // Called once per physics step
    void FixedUpdate()
    {
        // Record, rewind, and replay
        // this.book.FixedUpdate();
    }



    // Called once per rendered frame
    void Update()
    {
        // Record, rewind, replay
        // this.book.Update();
    }
}
