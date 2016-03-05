using UnityEngine;
using System.Collections;

public class CameraController : MonoBehaviour
{
    // The target's transform
    public Transform target;
    // The offset from the target
    public Vector2 offset = Vector2.zero;
    // The smooth damp time
    public float smoothDampTime;
    // The smooth damp velocity
    private Vector3 smoothDampVelocity = Vector3.zero;



    // Called once per rendered frame
    void LateUpdate()
    {
        // Get effective offset (this ensures z = -10)
        Vector3 effectiveOffset = new Vector3(offset.x, offset.y, 10f);

        // Debug
        // Debug.Log(string.Format("self = {0}, target = {1}, target_offset = {2}", transform.position, target.position, target.position - offset));

        // Move camera
        transform.position = Vector3.SmoothDamp( transform.position, target.position - effectiveOffset, ref smoothDampVelocity, smoothDampTime );
    }
}
