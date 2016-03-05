using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TimeCubeManager : MonoBehaviour
{
    // A list of all time cubes available to player
    public List<TimeCube> Cubes;
    // A list of all active histories (one per 'dynamic' GameObject)
    public List<History> Histories;
    // The clone type
    public GameObject Prefab;

    // Globals
    private Globals globals;

    // Equals true if at least one cube will rewind
    public bool WillRewind
    {
        get
        {
            // Loop through cubes
            for (int i = 0; i < this.Cubes.Count; i++)
            {
                // Get element
                TimeCube cube = this.Cubes[i];

                // Check if will rewind
                if (cube.WillRewind) { return true; }
            }

            // Otherwise, return false
            return false;
        }
    }
    // Equals true if at least one cube is recording
    public bool IsRecording
    {
        get
        {
            // Loop through cubes
            for (int i = 0; i < this.Cubes.Count; i++)
            {
                // Get element
                TimeCube cube = this.Cubes[i];
                
                // Check if rewinding
                if (cube.IsRecording) { return true; }
            }
            
            // Otherwise, return false
            return false;
        }
    }
    // Equals true if at least one cube is rewinding
    public bool IsRewinding
    {
        get
        {
            // Loop through cubes
            for (int i = 0; i < this.Cubes.Count; i++)
            {
                // Get element
                TimeCube cube = this.Cubes[i];
                
                // Check if rewinding
                if (cube.IsRewinding) { return true; }
            }
            
            // Otherwise, return false
            return false;
        }
    }
    // The minimum recording start time among all cubes
    public int MinStart
    {
        get
        {
            // Initialize result as positive infinity
            int result = int.MaxValue;

            // Loop through cubes
            for (int i = 0; i < this.Cubes.Count; i++)
            {
                // Get element
                TimeCube cube = this.Cubes[i];

                // Loop through cubes
                for (int j = 0; j < cube.Intervals.Count; j++)
                {
                    // Get element
                    TimeInterval interval = cube.Intervals[j];

                    // Check if smaller
                    if (interval.Start < result) { result = interval.Start; }
                }
            }

            // Return result
            return result;
        }
    }
    // The maximum recording end time among all cubes
    public int MaxEnd
    {
        get
        {
            // Initialize result as negative infinity
            int result = int.MinValue;
            
            // Loop through cubes
            for (int i = 0; i < this.Cubes.Count; i++)
            {
                // Get element
                TimeCube cube = this.Cubes[i];
                
                // Loop through cubes
                for (int j = 0; j < cube.Intervals.Count; j++)
                {
                    // Get element
                    TimeInterval interval = cube.Intervals[j];

                    // If one cube is open, the whole pack is open
                    if (interval.Start != -1 && interval.End == -1) { return int.MaxValue; }

                    // Check if bigger
                    if (interval.End > result) { result = interval.End; }
                }
            }
            
            // Return result
            return result;
        }
    }
    // A cube that is rewinding (or will rewind), otherwise null
    public TimeCube RewindingCube
    {
        get
        {
            // Initialize result as null
            TimeCube result = null;

            // Loop through cubes
            for (int i = 0; i < this.Cubes.Count; i++)
            {
                // Get element
                TimeCube cube = this.Cubes[i];

                // Check if rewinding (or will rewind)
                if (cube.IsRewinding || cube.WillRewind) { return cube; }
            }

            // Return result
            return result;
        }
    }
    // Description
    public override string ToString()
    {
        string s = string.Format("[TCM: Cubes = {0}, Histories = {1}]\n", this.Cubes.Count, this.Histories.Count);
        for (int i = 0; i < this.Cubes.Count; i++) { s += string.Format("Cubes[{0}] = {1}\n", i, this.Cubes[i]); }
        // for (int i = 0; i < this.Histories.Count; i++) { s += string.Format("Histories[{0}] = {1}\n", i, this.Histories[i]); }
        return s;
    }


    // Pre-initialization
    void Awake()
    {
        // Initialize variables
        this.Cubes = new List<TimeCube>();
        this.Histories = new List<History>();
        this.globals = GameObject.Find("globals").GetComponent<Globals>();
    }



    // Called once per physics step
    void FixedUpdate()
    {
        // Get world time
        int t = this.globals.WorldTime;
        // Check if in past
        bool inPast = t < this.globals.FurthestTime ? true : false;
        // Check if at least one cube is recording
        bool isRecording = this.IsRecording;
        // Get minimum recording start time
        int minStart = this.MinStart;



        // Loop over non-time-traveler histories
        for (int i = 0; i < this.Histories.Count; i++)
        {
            // Get element
            History history = this.Histories[i];

            // For non-time-travelers, the rules are simpler...
            if (!history.IsTimeTraveler)
            {
                // CASE 1 OF 2:  NOT A TIME TRAVELER

                // If (in present and recording) or (in past and altered), store
                if (!inPast && isRecording) { history.StoreBones(t - minStart); }

                // If (in past and unaltered), restore
                if (inPast) { history.RestoreBones(t - minStart); }
            }
        }

        // Loop over time traveler histories
        for (int i = 0; i < this.Cubes.Count; i++)
        {
            // Get element
            TimeCube cube = this.Cubes[i];

            // Get clone history
            History history = cube.Clone.History;

            // If cube is recording, store
            if (cube.IsRecording) { history.StoreBones(t - minStart); }

            // If cube is rewinding or replaying, restore
            if (cube.IsRewinding || cube.IsReplaying) { history.RestoreBones(t - minStart); }
        }



        // If all cubes are off, delete all intervals
        if(this.MaxEnd < t)
        {
            // Loop through cubes
            for (int i = 0; i < this.Cubes.Count; i++)
            {
                // Get element
                TimeCube cube = this.Cubes[i];

                // Delete intervals
                if (cube.Intervals.Count > 0)
                {
                    cube.Intervals.Clear();

                    // Debug:  History clear message
                    Debug.Log(cube.name + " history has been cleared!");
                }
            }
        }
    }



    // Add cube
    public void AddCube(TimeCube cube)
    {
        // Create a clone
        GameObject clone = (GameObject)Instantiate(this.Prefab);
        clone.name = "playerClone" + this.Cubes.Count;
        cube.Clone = clone.GetComponent<Clone>();

        // A cube should always remember its index
        cube.Index = this.Cubes.Count;

        // Add cube to list
        this.Cubes.Add(cube);
    }

    // Remove cube
    public void RemoveCube(int index)
    {
        // Destroy corresponding clone
        Destroy(this.Cubes[index]);

        // Remove cube from list
        this.Cubes.RemoveAt(index);

        // Some cubes will be shifted back in list.  Update their indices.
        for (int i = 0; i < this.Cubes.Count; i++) { this.Cubes[i].Index = i; }
    }

    // Add history
    public void AddHistory(History history)
    {
        // Add to list
        this.Histories.Add(history);

        // A history should always remember its index
        history.Index = this.Histories.Count - 1;
    }

    // Remove history 
    public void RemoveBookAt(int index)
    {
        // Remove from list
        this.Histories.RemoveAt(index);

        // Some histories will be shifted back in list.  Update their indices.
        for (int i = 0; i < this.Histories.Count; i++) { this.Histories[i].Index = i; }
    }
}
