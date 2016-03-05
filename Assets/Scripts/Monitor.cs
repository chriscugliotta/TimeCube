using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class Monitor : MonoBehaviour
{
    // Text
    public Text Text;
    // Globals
    private Globals globals;
    // Player
    private PlayerController player;
    // Camera
    private Camera camera;


    // Initialization
    void Start()
    {
        // Initialize variables
        this.Text = GameObject.Find("myText").GetComponent<Text>();
        this.globals = GameObject.Find("globals").GetComponent<Globals>();
        this.player = GameObject.Find("player").GetComponent<PlayerController>();
        this.camera = GameObject.Find("camera").GetComponent<Camera>();
    }



    // Called once per rendered frame
    void Update()
    {
        string s = this.globals + "\n";
        s += this.globals.TCM + "\n";
        s += this.player + "\n";
        s += "mousePosition = " + (Vector2)this.camera.ScreenToWorldPoint(Input.mousePosition);
        this.Text.text = s;
    }
}

