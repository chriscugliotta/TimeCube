using UnityEngine;
using UnityEditor;
using System.Collections;

class TestWindow : EditorWindow
{
    public AnimationClip clip;
    public string text = "testAnim";

    // Add item to the Window menu
    [MenuItem ("Window/Test Window")]    
    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(TestWindow));
    }




    // Called on UI rendering
    void OnGUI()
    {
        if(GUILayout.Button("Log Selection")) { logSelection(); }

        GUILayout.BeginHorizontal();
        GUILayout.Label("Clip name");
        text = GUILayout.TextField(text, 25);
        if(GUILayout.Button("Test")) { test(); }
        GUILayout.EndHorizontal();

        clip = EditorGUILayout.ObjectField("Clip", clip, typeof(AnimationClip), false) as AnimationClip;
    }



    // Log selected values
    private void logSelection()
    {
        // Initialize string
        string s = "";

        // Loop through selected objects
        for(int i = 0; i < Selection.objects.Length; i++)
        {
            // Update string
            s += string.Format("objects[{0}] = {1}, instanceID = {2}\n", i, Selection.objects[i], Selection.instanceIDs[i]);
        }

        // Display string
        Debug.Log(s);
    }

    // Miscellaneous experiments
    private void test()
    {
        /* // Obsolete:  Get all curves in clip
        AnimationClipCurveData[] curves = AnimationUtility.GetAllCurves(clip);
        // Debug.Log("curves.Length = " + curves.Length);

        // Initialize an individual curve
        AnimationClipCurveData curve = null;

        // Loop through curves
        for (int i = 0; i < curves.Length; i++)
        {
            // Get current curve
            AnimationClipCurveData c = curves[i];

            // Log curve
            // Debug.Log(string.Format("curve = {0}, path = {1}, propertyName = {2}, type = {3}", c.curve, c.path, c.propertyName, c.type));

            // Remember shoulders positionX
            if (c.path == "Template_Waist/Template_RightLeg/Template_RightShin/Template_RightFoot" && c.propertyName == "m_LocalRotation.z")
            {
                Debug.Log("found it!");
                curve = c;
            }
        }

        // Show key count
        Debug.Log("keys = " + curve.curve.keys.Length); */

        // New:  Get all bindings in clip
        EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(clip);
        // Debug.Log("bindings.Length = " + bindings.Length);

        // Initialize an individual binding
        EditorCurveBinding binding = new EditorCurveBinding();

        // Loop through bindings
        for (int i = 0; i < bindings.Length; i++)
        {
            // Get current binding
            EditorCurveBinding b = bindings[i];

            // Log binding
            // Debug.Log(string.Format("path = {0}, propertyName = {1}", b.path, b.propertyName));

            // Remember shoulders positionX
            if (b.path == "Template_Waist/Template_RightLeg/Template_RightShin/Template_RightFoot" && b.propertyName == "m_LocalRotation.z")
            {
                Debug.Log("found it!");
                binding = b;
            }
        }

        // Get corresponding curve
        AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, binding);

        // Show key count (still fucked)
        // UPDATE:  This depends on animationCurve's interpolation setting.
        // In inspector, you can pick Interpolation --> Euler or Quaternion.
        // Quaternion keyFrame count makes sense, Euler is overstated and f'd.
        Debug.Log("keys = " + curve.keys.Length);



        /* // Get all keyframes in curve
        Keyframe[] keys = curve.curve.keys;

        for (int i = 0 ;i < keys.Length; i++)
        {
            // Get current key
            Keyframe k = keys[i];

            // Log key
            // Debug.Log(string.Format("inTangent = {0}, outTangent = {1}, time = {2}, value = {3}", k.inTangent, k.outTangent, k.time, k.value));
        } */

        /* // Create a new clip
        AnimationClip clip = new AnimationClip();
        AnimationCurve curve;

        // Create a new curve (shoulder position.x)
        curve = new AnimationCurve();
        curve.AddKey(new Keyframe(0.00f, 0.00f));
        curve.AddKey(new Keyframe(0.75f, 2.00f));
        curve.AddKey(new Keyframe(1.00f, 1.00f));
        clip.SetCurve("Template_Waist/Template_Back/Template_Shoulders", typeof(Transform), "localPosition.x", curve);

        // Create a new curve (rightLeg rotation.z)
        curve = new AnimationCurve();
        curve.AddKey(new Keyframe(0.00f, 0.00f));
        curve.AddKey(new Keyframe(1.00f, 0.7071067f));
        clip.SetCurve("Template_Waist/Template_RightLeg", typeof(Transform), "localRotation.z", curve);

        // Create a new curve (rightLeg rotation.w)
        curve = new AnimationCurve();
        curve.AddKey(new Keyframe(0.00f, 1.00f));
        curve.AddKey(new Keyframe(1.00f, 0.7071067f));
        clip.SetCurve("Template_Waist/Template_RightLeg", typeof(Transform), "localRotation.w", curve);

        // Save as a new asset
        AssetDatabase.CreateAsset(clip, "Assets/" + text + ".anim"); */

        /* // Test evaluate

        // Get clip
        AnimationClip clip = AssetDatabase.LoadAssetAtPath("Assets/Animations/Clips/Test.anim", typeof(AnimationClip)) as AnimationClip;
        Debug.Log("clip = " + clip);

        // Get curve
        AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, "Template_Waist/Template_RightLeg", typeof(Transform), "m_LocalRotation.z");
        Debug.Log("curve = " + curve.keys.Length);

        // Evaluate curve at time
        string s = "";
        float t = 0;
        while (t <= 1.05f)
        {
            s += string.Format("t = {0}, v = {1}\n", t, curve.Evaluate(t));
            t += 0.1f;
        }
        Debug.Log(s); */
    }


}