using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// To minimize parameters, we will encode all states in the Start and Stop
// attributes.  A cube must always be in one of the following states:
//
//   1.  Off
//       (Start = -1, Stop = -1)
//
//   2.  Recording, end is unknown
//       (Start > -1, Stop = -1, WorldTime - Start <= BatteryLife)
//
//   3.  Recording, end is predestined...  this IS replaying
//       (Start > -1, Stop > -1, WorldTime <= Stop)
// 
public class TimeCube : MonoBehaviour
{
    // A list of recorded intervals
    public List<TimeInterval> Intervals;
    // The active interval, i.e. the one that exists at world time
    public TimeInterval ActiveInterval;

    // Equals true if (next step) cube will start recording
    public bool WillRecord;
    // Equals true if (next step) cube will start rewinding
    public bool WillRewind;
    // Equals true if (next step) cube will start replaying
    public bool WillReplay;
    
    // Equals true if cube is recording
    public bool IsRecording;
    // Equals true if cube is rewinding
    public bool IsRewinding;
    // Equals true if cube is replaying
    public bool IsReplaying;

    // The maximum length of a recording
    public int BatteryLife;
    // A number representing how far back the player has rewinded
    public float RewindPosition;
    // The clone instance
    public Clone Clone;
    // The manager index
    public int Index;

    // Globals
    private Globals globals;
    // Transform history
    private History history;
    // The sprite renderer
    private SpriteRenderer spriteRenderer;
    // The player
    private PlayerController player;


    // Battery life remaining
    public int BatteryLeft
    {
        get
        {
            // If we're not recording, all life remains
            if (this.ActiveInterval == null) { return this.BatteryLife; }

            // Get current time
            int t = this.globals.WorldTime;
            // Get recording start time
            int t0 = this.ActiveInterval.Start;
            // After update, we have recorded t - t0 + 1 steps
            return this.BatteryLife - (t - t0 + 1);
        }
    }
    // Description
    public override string ToString()
    {
        return string.Format("[{0}: t1 = {1}, t2 = {2}, IC = {3}, Rc = {4}, Rw = {5}, Rp = {6}, wR = {7}, Ps = {8}, bL = {9}]",
                             this.name,
                             this.ActiveInterval == null ? -1 : this.ActiveInterval.Start,
                             this.ActiveInterval == null ? -1 : this.ActiveInterval.End,
                             this.Intervals.Count,
                             this.IsRecording ? 1 : 0,
                             this.IsRewinding ? 1 : 0,
                             this.IsReplaying ? 1 : 0,
                             this.WillRewind ? 1 : 0,
                             this.RewindPosition,
                             this.BatteryLeft);
    }


 // Initialization
    void Start()
    {
        // Initialize variables
        this.Intervals = new List<TimeInterval>();
        this.WillRecord = false;
        this.WillRewind = false;
        this.WillReplay = false;
        this.IsRecording = false;
        this.IsRewinding = false;
        this.IsReplaying = false;
        this.RewindPosition = 0;
        this.globals = GameObject.Find("globals").GetComponent<Globals>();
        this.history = new History(new Transform[1] { transform });
        this.spriteRenderer = gameObject.GetComponent<SpriteRenderer>();
        this.player = GameObject.Find("player").GetComponent<PlayerController>();

        // Add self to globals
        this.globals.DynamicObjects.Add(this.gameObject);
        this.globals.TCM.AddCube(this);
        this.globals.TCM.AddHistory(this.history);
    }



    // Called once per physics step
    void FixedUpdate()
    {
        // Get world time
        int t = this.globals.WorldTime;

        // Check for a request to begin recording
        if (this.WillRecord) { this.startRecording(); }

        // Check if battery died
        if (this.IsRecording && t - this.ActiveInterval.Start >= this.BatteryLife) { this.ActiveInterval.End = t - 1; }

        // Check for a request to begin replaying
        if (this.WillReplay) { this.startReplaying(); }

        // Update active interval and flags
        this.updateState();

        // Check if done replay
        if (this.ActiveInterval == null) { this.stopReplaying(); }

        // Debug:  Set color
        if (this.ActiveInterval == null) { this.spriteRenderer.color = Color.white; }
        if (this.IsRecording) { this.spriteRenderer.color = new Color32(231, 76, 60, 255); }
        if (this.IsReplaying) { this.spriteRenderer.color = new Color32(46, 204, 113, 255); }

        // These flags should only be raised for a single step
        this.WillRecord = false;
        this.WillRewind = false;
        this.WillReplay = false;
    }



    // Update active interval and flags
    private void updateState()
    {
        // Get world time
        int t = this.globals.WorldTime;

        // By default, assume cube is off
        this.ActiveInterval = null;
        this.IsRecording = false;
        this.IsReplaying = false;
        
        // Loop through intervals
        for (int i = 0; i < this.Intervals.Count; i++)
        {
            // Get element
            TimeInterval interval = this.Intervals[i];
            
            // Check if exists at time t
            if(interval.Start <= t && (interval.End == -1 || interval.End >= t))
            {
                this.ActiveInterval = interval;
                break;
            }
        }

        // If an active interval exists, we must be either recording or replaying...
        if (this.ActiveInterval != null)
        {
            // If the end is unknown, we haven't gone back in time yet.  We're recording.
            if(this.ActiveInterval.End == -1 || this.ActiveInterval.End - this.ActiveInterval.Start + 1 == this.BatteryLife)
            {
                this.IsRecording = true;
            }
            // If the end is known, we've used this cube already.  We're in the past, replaying.
            // NOT TRUE!  What about if the battery died?
            else
            {
                this.IsReplaying = true;
            }
        }
    }

    // Start a new recording
    private void startRecording()
    {
        // Create interval
        TimeInterval interval = new TimeInterval(this.globals.WorldTime);

        // Add to list
        this.Intervals.Add(interval);
    }

    // Stop an existing recording
    private void stopRecording()
    {
        // Set recording stop time
        this.ActiveInterval.End = this.globals.WorldTime;
    }

    // Starts the rewinding process
    public void startRewinding()
    {
        // Note:  This method is called by Globals, because it affects the
        // WorldTime update, which must happen at the very beginning of the
        // step.

        // We can only rewind if we're still recording.  So, stop recording.
        this.stopRecording();

        // Raise rewind flag
        this.IsRewinding = true;

        // Reset rewind position
        this.RewindPosition = this.ActiveInterval.End - this.ActiveInterval.Start;

        // Pause scene
        foreach (GameObject gameObject in globals.DynamicObjects)
        {
            // Skip player
            if (gameObject.name == "player") { continue; }

            gameObject.rigidbody2D.isKinematic = true;
            Animator anim = gameObject.GetComponent<Animator>();
            if (anim != null) { anim.enabled = false; }
        }

        // Spaghetti code:  Use 'rewind' controls
        this.player.controls = "Rewind";
    }

    // Stops the rewinding process
    private void stopRewinding()
    {
        // Lower rewind flag
        this.IsRewinding = false;

        // Unpause scene
        foreach (GameObject gameObject in globals.DynamicObjects)
        {
            // Skip player
            if (gameObject.name == "player") { continue; }

            gameObject.rigidbody2D.isKinematic = false;
            Animator anim = gameObject.GetComponent<Animator>();
            if (anim != null) { anim.enabled = true; }
        }

        // Spaghetti code:  Player needs to be kinematic for 'exiting' animation
        // this.player.rigidbody2D.isKinematic = true;
    }

    // Starts the replay process
    private void startReplaying()
    {
        // We can only replay if we're still rewinding.  So, stop rewinding.
        this.stopRewinding();
    }

    // Stops the playback process
    private void stopReplaying()
    {
        // 'Destroy' (but actually hide) clone
        this.Clone.transform.localPosition = new Vector3(0f, 10000f, 0f);
    }
}





// A recorded time interval
public class TimeInterval
{
    // The first recorded step
    public int Start;
    // The last recorded step, if predestined.  Otherwise -1.
    public int End;
    
    // Constructor
    public TimeInterval(int start)
    {
        this.Start = start;
        this.End = -1;
    }
}