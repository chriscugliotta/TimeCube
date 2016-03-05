using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

class AnimationConverter : EditorWindow
{
    // Add item to the Window menu
    [MenuItem ("Window/Animation Converter")]    
    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(AnimationConverter));
    }



    // Called on UI rendering
    void OnGUI()
    {
        // Convert
        if(GUILayout.Button("Convert")) { prepare(); }
    }



    // Prepare to convert
    private void prepare()
    {
        // There are four inputs to the conversion process:
        //
        //   1.)  A list of animation clips (to be converted)
        //
        //   2.)  For each clip, a list of time points (for keyframes)
        //
        //   3.)  The root path containing all animated properties
        //
        //   4.)  The prefix of the output .anim files

        // Test
        // Get animation clips
        string[] paths = new string[2];
        paths[0] = "Assets/Animations/Clips/Test.anim";
        paths[1] = "Assets/Animations/Clips/Test2.anim";

        // Get time points
        int[][] times = new int[2][];
        times[0] = new int[3] { 0, 30, 60 };
        times[1] = new int[3] { 0, 45, 60 };

        // Get root
        string root = "player";

        // Get output prefix
        string prefix = "legacy_";

        /* // Template
        // Get animation clips and time points
        string[] paths = new string[10];
        int[][] times = new int[10][];

        paths[0] = "Assets/Animations/Clips/Climb.anim";
        times[0] = new int[6] { 0, 15, 30, 45, 75, 90 };

        paths[1] = "Assets/Animations/Clips/Enter_Cube.anim";
        times[1] = new int[8] { 0, 10, 20, 30, 60, 75, 90, 105 };

        paths[2] = "Assets/Animations/Clips/Exit_Cube.anim";
        times[2] = new int[9] { 0, 15, 30, 40, 45, 55, 60, 70, 75 };

        paths[3] = "Assets/Animations/Clips/Hang.anim";
        times[3] = new int[5] { 0, 15, 30, 45, 60 };

        paths[4] = "Assets/Animations/Clips/Idle.anim";
        times[4] = new int[2] { 0, 60 };

        paths[5] = "Assets/Animations/Clips/Jump_InAir2.anim";
        times[5] = new int[2] { 0, 60 };

        paths[6] = "Assets/Animations/Clips/Jump_Launch.anim";
        times[6] = new int[5] { 0, 8, 15, 23, 30 };

        paths[7] = "Assets/Animations/Clips/Jump_Land.anim";
        times[7] = new int[5] { 0, 8, 15, 23, 30 };

        paths[8] = "Assets/Animations/Clips/Push_Button.anim";
        times[8] = new int[4] { 0, 15, 30, 45 };

        paths[9] = "Assets/Animations/Clips/Run.anim";
        times[9] = new int[13] { 0, 8, 15, 30, 45, 53, 60, 68, 75, 90, 105, 113, 120 };

        // Get root
        string root = "player";
        
        // Get output prefix
        string prefix = "legacy_"; */



        // Start timer
        float startTime = Time.realtimeSinceStartup;

        // Call conversion process
        convert(paths, times, root, prefix);

        // Stop timer
        float stopTime = Time.realtimeSinceStartup;

        // Debug
        Debug.Log(string.Format("Conversion completed after {0} seconds", stopTime - startTime));
    }

    // Convert
    private void convert(string[] paths, int[][] times, string root, string prefix)
    {
        // We want all animated properties to appear in each animation.
        // Otherwise, some properties will remain in a previous clip's state,
        // even after playing a subsequent animation.  For instance, if a
        // property exists in clip A (but not B), and we are transitioning from
        // A to B, then this property will not reset to its default,
        // non-animated value (as it would in Mecanim).  Instead, it will be
        // 'stuck' in last frame of A.  To fix this, we must ensure that each
        // clip contains an identical set of properties.
        //
        // So, first we will get the union of all animated properties across
        // all clips.  Then, we will convert each Mecanim clip to legacy via the
        // following algorithm:
        //
        //   1.  Create a curve for each property.
        //
        //   2.  For each property, create a keyframe on each specified time
        //       points.
        //   
        //   3.  If this property exists in the source animation, obtain the
        //       keyframe value via AnimationCurve.Evaluate(time).  Otherwise,
        //       use the default, non-animated value.

        // Get animation clips from paths
        AnimationClip[] clips = getClips(paths);

        // Get the union of all animated properties
        EditorCurveBinding[] properties = getProperties(clips);

        // Loop through source clips and process (and eventually store or print directly)
        for (int i = 0; i < clips.Length; i++) { process(clips[i], properties, times[i], root, prefix); }
    }

    // Get animation clips from paths
    private AnimationClip[] getClips(string[] paths)
    {
        // Get array size
        int n = paths.Length;

        // Initialize array
        AnimationClip[] clips = new AnimationClip[n];

        // Loop over clips
        for (int i = 0; i < n; i++)
        {
            // Load clip
            clips[i] = AssetDatabase.LoadAssetAtPath(paths[i], typeof(AnimationClip)) as AnimationClip;

            // Debug
            // Debug.Log(string.Format("clips[{0}] = {1}", i, clips[i].name));
        }

        // Return
        return clips;
    }

    // Get the union of all animated properties
    private EditorCurveBinding[] getProperties(AnimationClip[] clips)
    {
        // Initialize set of path-propertyName combinations
        HashSet<EditorCurveBinding> properties = new HashSet<EditorCurveBinding>();

        // Loop through clips
        foreach(AnimationClip clip in clips)
        {
            // Loop through bindings
            foreach(EditorCurveBinding binding in AnimationUtility.GetCurveBindings(clip))
            {
                // Add to set
                properties.Add(binding);
            }
        }

        // Copy to array
        EditorCurveBinding[] result = new EditorCurveBinding[properties.Count];
        properties.CopyTo(result);

        /* // Debug
        string s = "";
        for (int i = 0; i < result.Length; i++)
        {
            s += string.Format("result[{0}] = {1}\n", i, result[i].path + ", " + result[i].propertyName);
        }
        Debug.Log(s); */

        // Return
        return result;
    }

    // Process an individual clip
    private void process(AnimationClip clip, EditorCurveBinding[] properties, int[] times, string root, string prefix)
    {
        // Initialize
        AnimationClip newClip = new AnimationClip();

        // Loop over properties
        foreach (EditorCurveBinding property in properties)
        {
            // Format property string (change m_LocalPosition.x to localPosition.x)
            string p = "l" + property.propertyName.Substring(3);

            // Initialize array of keyframes
            Keyframe[] keys = new Keyframe[times.Length];

            // Check if property is in source animation
            bool inSource = containsProperty(clip, property);

            // Proceed accordingly
            if (inSource)
            {
                // Case 1:  Property is in source animation
                //          Get keyframe values from interpolation

                // Get old curve
                AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, property);

                // Loop through time points
                for (int i = 0; i < times.Length; i++)
                {
                    // Get time
                    float t = times[i] / 60f;
                    // Get value
                    float v = curve.Evaluate(t);
                    // Create keyframe
                    keys[i] = new Keyframe(t, v);
                }
            }
            else
            {
                // Case 2:  Property is not in source animation
                //          Get keyframe values from non-animated pose

                // Get non-animated transform
                Transform transform = GameObject.Find(root + "/" + property.path).transform;
                // Debug.Log(string.Format("clip = {0}, property = {1}, transform = {2}", clip.name, property.path + property.propertyName, transform.localPosition.x));

                // Get value
                float v = 0f;
                if (p == "localRotation.w" ) { v = transform.localRotation.w; }
                else if (p == "localRotation.z" ) { v = transform.localRotation.z; }
                else if (p == "localRotation.y" ) { v = transform.localRotation.y; }
                else if (p == "localRotation.x" ) { v = transform.localRotation.x; }
                else if (p == "localPosition.x") { v = transform.localPosition.x; }
                else if (p == "localPosition.y" ) { v = transform.localPosition.y; }
                else if (p == "localPosition.z" ) { v = transform.localPosition.z; }
                else if (p == "localScale.x" ) { v = transform.localScale.x; }
                else if (p == "localScale.y" ) { v = transform.localScale.y; }
                else if (p == "localScale.z" ) { v = transform.localScale.z; }

                // Loop through time points
                for (int i = 0; i < times.Length; i++)
                {
                    // Get time
                    float t = times[i] / 60f;
                    // Create keyframe
                    keys[i] = new Keyframe(t, v);
                }
            }

            // Create new curve
            AnimationCurve newCurve = new AnimationCurve(keys);
            
            // Add curve to clip
            newClip.SetCurve(property.path, typeof(Transform), p, newCurve);
        }

        // Save as a new asset
        AssetDatabase.CreateAsset(newClip, "Assets/" + prefix + clip.name + ".anim");
    }

    // Check if clip contains property
    private bool containsProperty(AnimationClip clip, EditorCurveBinding property)
    {
        // Initialize result as false
        bool result = false;

        // Get clip properties
        EditorCurveBinding[] properties = AnimationUtility.GetCurveBindings(clip);

        // Loop through clip properties
        foreach (EditorCurveBinding p in properties)
        {
            // If equal to property, stop
            if (p == property) { result = true; break; }
        }

        // Return result
        return result;
    }
}
