using UnityEngine;
using System.Collections;

public class History
{
    // The manager index
    public int Index;

    // An array of all animated transforms (belonging to recorded sprite)
    private Transform[] bones;
    // An array of all animated transforms (belonging to replayed sprite)
    private Transform[] bones2;

    // An array of all stored positions
    private Vector3[][] positions;
    // An array of all stored rotations
    private Quaternion[][] rotations;
    // An array of all stored scales
    private Vector3[][] scales;

    // Equals true if this is a time traveler
    public bool IsTimeTraveler
    {
        get
        {
            if(this.bones[0].name == this.bones2[0].name) { return false; }
            else { return true; }
        }
    }



    // Simple constructor
    public History(Transform[] bones) : this(bones, bones) {}

    // Detailed constructor
    public History(Transform[] bones, Transform[] bones2)
    {
        // Initialize variables
        this.Index = -1;
        this.bones = bones;
        this.bones2 = bones2;

        // Get bone count
        int boneCount = this.bones.Length;

        // Initialize arrays (one slot per bone)
        this.positions = new Vector3[boneCount][];
        this.rotations = new Quaternion[boneCount][];
        this.scales = new Vector3[boneCount][];

        // Temporary hardcode
        int timeSlots = 3000;

        // Initialize nested arrays (one slot per time point)
        for (int i = 0; i < boneCount; i++)
        {
            this.positions[i] = new Vector3[timeSlots];
            this.rotations[i] = new Quaternion[timeSlots];
            this.scales[i] = new Vector3[timeSlots];
        }
    }



    // Store current state of all bones
    public void StoreBones(int t)
    {
        // Loop through bones
        for(int i = 0; i < this.bones.Length; i++)
        {
            // Store values
            this.positions[i][t] = this.bones[i].localPosition;
            this.rotations[i][t] = this.bones[i].localRotation;
            this.scales[i][t] = this.bones[i].localScale;
        }

        /* // Debug
        if (this.bones2[0].name == "timeCube0")
        {
            string s = "";
            for (int i = 0; i < 100; i++)
            {
                s += string.Format("{0}: this.positions[0][{1}] = {2}\n", this.bones2[0].name, i, this.positions[0][i]); 
            }
            Debug.Log(s);
        } */
    }
    
    // Restore previous state of all bones
    public void RestoreBones(int t)
    {
        // Loop through bones
        for (int i = 0; i < this.bones2.Length; i++)
        {
            // Restore values
            this.bones2[i].localPosition = this.positions[i][t];
            this.bones2[i].localRotation = this.rotations[i][t];
            this.bones2[i].localScale = this.scales[i][t];
        }
    }

    // Restore mixed state of all bones
    public void RestoreBones(float t)
    {
        // Note:  This function assumes t is already clamped.

        // Get first index
        int t1 = Mathf.FloorToInt(t);
        // Get second index
        int t2 = Mathf.CeilToInt(t);
        // Get weight of first index
        float w1 = Mathf.Ceil(t) - t;
        // Get weight of second index
        float w2 = 1f - w1;

        // Loop through bones
        for (int i = 0; i < this.bones2.Length; i++)
        {
            this.bones2[i].localPosition = w1 * this.positions[i][t1] + w2 * this.positions[i][t2];
            this.bones2[i].localRotation = Quaternion.Slerp(this.rotations[i][t1], this.rotations[i][t2], w2);

            // Prevent bug when isFacingRight property is changed.
            // scale.x should either be 1 or -1.  Don't allow any mixed point.
            float x;
            float y = w1 * this.scales[i][t1].y + w2 * this.scales[i][t2].y;
            float z = w1 * this.scales[i][t1].z + w2 * this.scales[i][t2].z;
            if (this.scales[i][t1].x == 1 && this.scales[i][t2].x == -1)
            {
                if (w1 > w2) { x = 1; } else { x = -1; }
            }
            else if (this.scales[i][t1].x == -1 && this.scales[i][t2].x == 1)
            {
                if (w1 > w2) { x = -1; } else { x = 1; }
            }
            else 
            {
                x = w1 * this.scales[i][t1].x + w2 * this.scales[i][t2].x;
            }
            this.bones2[i].localScale = new Vector3(x, y, z);
        }

        /* // Debug:  Check weighted average calculation
        if (bones2[0].gameObject.name == "playerClone1")
        {
            Debug.Log(string.Format("fT = {0}, t1 = {1}, t2 = {2}, w1 = {3}, w2 = {4}, z1 = {5}, z2 = {6}, z = {7}", Time.fixedTime, t1, t2, w1, w2, this.scales[0][t1].z, this.scales[0][t2].z, bones2[0].localScale.z));
        } */

        /* // Debug:  Show all time slots
        if (bones2[0].gameObject.name == "playerClone1")
        {
            string s = "";
            for (int i = 0; i < this.positions[0].Length; i++)
            {
                s += "this.positions[0][" + i + "].x = " + this.positions[0][i].x + "\n";
            }
            Debug.Log(s);
        } */
    }
}
