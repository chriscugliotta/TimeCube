using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Globals : MonoBehaviour
{
    // The player's point in time
    public int WorldTime;
    // The furthest observed point in time
    public int FurthestTime;
    // A list of all dynamic objects
    public List<GameObject> DynamicObjects;

    // The player
    public PlayerController Player;
    // The time cube manager
    public TimeCubeManager TCM;
    // The monitor
    public Monitor Monitor;

    // Description
    public override string ToString()
    {
        return string.Format("[Globals: wT = {0}, fT = {1}]", this.WorldTime, this.FurthestTime);
    }



    // Pre-initialization
    void Awake()
    {
        // Initialize variables
        this.WorldTime = 0;
        this.FurthestTime = 0;
        this.DynamicObjects = new List<GameObject>();
    }



    // Initialization
    void Start()
    {
        // Initialize variables
        this.Player = GameObject.Find("player").GetComponent<PlayerController>();
        this.TCM = GameObject.Find("timeCubeManager").GetComponent<TimeCubeManager>();

        // If exists, get monitor
        GameObject m = GameObject.Find("globals/monitor");
        if (m != null) { this.Monitor = m.GetComponent<Monitor>(); }
    }



    // Called once per physics step
    void FixedUpdate()
    {
        // If exists, get a rewinding cube
        TimeCube rewindingCube = this.TCM.RewindingCube;

        // Check if rewinding (or will rewind)
        if (rewindingCube == null)
        {
            // If not, increment world time as usual
            this.WorldTime++;

            // If we've never seen this moment before, update furthest time
            if (this.WorldTime == this.FurthestTime + 1) { this.FurthestTime++; }

            // Debug:  Show this step's WorldTime
            // Debug.Log("Just finished Globals.FixedUpdate!  wT = " + this.WorldTime);
        }
        else
        {
            // Spaghetti code:  Necessary because it affects WorldTime update,
            // which must happen first.
            if (rewindingCube.WillRewind) { rewindingCube.startRewinding(); }

            // If rewinding, let player adjust position
            this.Player.FixedControlRewind();

            // Now that rewind has occurred, update world time
            this.WorldTime = rewindingCube.ActiveInterval.Start + Mathf.RoundToInt(rewindingCube.RewindPosition);
        }



        // Exit
        if (Input.GetKeyDown(KeyCode.Escape)) { Application.Quit(); }
    }
}
