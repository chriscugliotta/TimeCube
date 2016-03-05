using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class PlayerController : MonoBehaviour
{
    // The maximum run speed
    public float runSpeed = 20f;
    // The maximum jump height
    public float jumpHeight = 4f;
    // Ground smoothing factor
    public float groundSmoothing = 0.4f;
    // Air smoothing factor
    public float airSmoothing = 0.1f;
    // The current velocity
    public Vector2 velocity = Vector2.zero;
    // The active control set
    public string controls = "Standard";

    // Equals true if facing right
    private bool isFacingRight = true;
    // Equals true if allowed to flip direction
    private bool canFlip = true;
    // Equals true if on ground
    private bool isGrounded = true;
    // Equals true if grounded last step
    private bool wasGrounded = true;
    // Equals true if falling
    private bool isFalling = false;
    // Equals true if falling last step
    private bool wasFalling = false;
    // The elapsed time on ground
    private float groundTime = 0f;
    // The elapsed time in air
    private float airTime = 0f;

    // Equals true if (next step) character will jump
    private bool willLaunch = false;
    // Equals true if launching
    private bool isLaunching = false;
    // Equals true if hanging
    private bool isHanging = false;
    // Equals true if climbing
    private bool isClimbing = false;

    // An array of linecast hits, checking for walls
    private RaycastHit2D[] hits;
    // A nearby time cube
    private TimeCube cube;

    // Globals
    private Globals globals;
    // The character controller component
    private CharacterController2D controller;
    // The animator component
    private Animator animator;
    // The box collider component
    private BoxCollider2D box;
    // The camera
    private CameraController camera;

    // Physics interaction prototype
    private RaycastHit2D[] controllerHits;
    private int controllerHitCount = 0;

    // Enter/exit cube prototype
    private float cubeTime;
    private bool cubeLeft;
    private bool cubeAttached;
    private Vector3[] cubeEdge;
    private Vector3 cubeLand;


    // Description
    public override string ToString()
    {
        return string.Format("[{0}: v = {1}, FR = {2}, cF = {3}, iG = {4}, iF = {5}, wL = {6}, iL = {7}, iH = {8}, iC = {9}, c = {10}]",
                             name,
                             (Vector2)velocity,
                             isFacingRight ? 1 : 0,
                             canFlip ? 1 : 0,
                             isGrounded ? 1 : 0,
                             isFalling ? 1 : 0,
                             willLaunch ? 1 : 0,
                             isLaunching ? 1 : 0,
                             isHanging ? 1 : 0,
                             isClimbing ? 1 : 0,
                             controls);
    }



    // Initialization
    void Start()
    {
        // Initialize variables
        globals = GameObject.Find("globals").GetComponent<Globals>();
        controller = GetComponent<CharacterController2D>();
        animator = GetComponent<Animator>();
        box = GetComponent<BoxCollider2D>();
        camera = GameObject.Find("camera").GetComponent<CameraController>();
        hits = new RaycastHit2D[1];
        controls = "Standard";
        controllerHits = new RaycastHit2D[controller.totalHorizontalRays + controller.totalVerticalRays];

        // Add collision listener
        controller.onControllerCollidedEvent += OnControllerCollider;

        // Add self to globals
        globals.DynamicObjects.Add(gameObject);
    }

    // Called once per physics step
    void FixedUpdate()
    {
        // Standard controls
        if (controls == "Standard") { FixedControlStandard(); }
       
        // Rewind controls
        // Note:  This is actually called by Globals, to ensure it happens
        // first.  This way, the result trickles down into WorldTime update...
        // else if (controls == "Rewind") { FixedControlRewind(); }
    }

    // Called once per rendered frame
    void Update()
    {   
        // 'Standard' controls
        if (controls == "Standard") { ControlStandard(); }

        // 'Rewind' controls
        else if (controls == "Rewind") { ControlRewind(); }

        // 'InCube' controls
        else if (controls == "InCube") { ControlInCube(); }

        // 'Entering' animation
        else if (controls == "Entering") { doEnterCube(); }

        // 'Exiting' animation
        else if (controls == "Exiting") { doExitCube(); }
    }



    // 'Standard' controls, called by FixedUpdate
    public void FixedControlStandard()
    {
        // Get horizontal input
        float h = Input.GetAxisRaw("Horizontal");

        // If grounded, clear vertical force (prevents build up)
        if (isGrounded) { velocity.y = 0f; }



        // Check flip
        if (canFlip && ((isFacingRight && h < 0) || (!isFacingRight && h > 0)))
        {
            // Switch the way the player is labelled as facing
            isFacingRight = !isFacingRight;

            // Multiply the player's x local scale by -1
            transform.localScale = new Vector3(-transform.localScale.x, transform.localScale.y, transform.localScale.z);
        }



        // Check for a request to jump
        if (willLaunch)
        {
            // Apply jump force
            velocity.y = Mathf.Sqrt(2f * jumpHeight * -Physics2D.gravity.y);

            // This flag should only be raised for a single step
            willLaunch = false;
        }



        // Apply horizontal smoothing
        velocity.x = Mathf.Lerp(velocity.x, h * runSpeed, isGrounded ? groundSmoothing : airSmoothing);

        // Apply gravity force
        velocity += Physics2D.gravity * Time.fixedDeltaTime;

        // Obey terminal velocity
        // velocity = Vector2.ClampMagnitude(velocity, 40f);

        // If hanging, clear all forces
        if (isHanging) { velocity = Vector3.zero; }



        // Move
        controllerHitCount = 0;
        if (!isHanging) { controller.move(velocity * Time.fixedDeltaTime); }
        applyForces();



        // Store previous state
        wasGrounded = isGrounded;
        wasFalling = isFalling;
        
        // Check if grounded
        isGrounded = controller.isGrounded;
        
        // Update grounded and air times
        if (isGrounded)
        {
            if (isGrounded == wasGrounded) { groundTime += Time.fixedDeltaTime; } else { groundTime = 0f; }
        }
        else
        {
            if (isGrounded == wasGrounded) { airTime += Time.fixedDeltaTime; } else { airTime = 0f; }
        }
        
        // Check if falling
        if(!isGrounded && !isHanging && airTime >= 0.1f) { isFalling = true; } else { isFalling = false; }
        
        // Update animator
        animator.SetBool("Grounded", isGrounded);
        animator.SetBool("WasFalling", wasFalling);
        animator.SetFloat("Speed", Mathf.Abs(h));
    }

    // 'Standard' controls, called by Update
    public void ControlStandard()
    {
        // If grounded and pressing jump, then launch
        if(isGrounded && Input.GetButtonDown("Jump") && !isLaunching && !isHanging) { animator.SetTrigger("Jump_Launch"); isLaunching = true; }

        // If hanging and pressing jump, then climb
        if (isHanging && Input.GetButtonDown("Jump") && !isClimbing) { animator.SetTrigger("Climb"); isClimbing = true; }
        
        
        
        // If grounded, pressing fire and near cube, activate or enter cube
        if (isGrounded && Input.GetButtonDown("Fire1"))
        {
            // TODO:  We need to streamline these linecast points.
            // We need to make sure we're not hitting terrain...
            // We should check on a single layer only, if possible.

            // Get linecast points
            float x1 = transform.position.x + (isFacingRight ? 1 : -1) * 1.2f;
            float y1 = transform.position.y + box.center.y - 0.4f * box.size.y;
            float y2 = transform.position.y + box.center.y + 0.4f * box.size.y;
            Vector2 start = new Vector2(x1, y1);
            Vector2 end = new Vector2(x1, y2);
            //Debug.Log("start = " + start + ", end = " + end);
            Debug.DrawLine(start, end, Color.green);
            
            // Perform linecast
            int result = Physics2D.LinecastNonAlloc(start, end, hits);
            // if(result > 0) { Debug.Log(string.Format("result = {0}, other = {1}", result, hits[0].collider.gameObject.tag)); }
            if(result > 0 && hits[0].collider.gameObject.tag == "TimeCube")
            {
                // Remember nearby cube
                cube = hits[0].collider.gameObject.GetComponent<TimeCube>();
                
                // If cube is off, activate
                if (!cube.IsRecording && !cube.IsRewinding) { animator.SetTrigger("PushButton"); controls = "Activating"; }
                
                // If cube is recording, enter
                else if (cube.IsRecording) { beginEnterCube(); }
            }
        }



        // Bug:  Check isLaunching
        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
        if (isLaunching && !state.IsName("Jump_Launch")) { isLaunching = false; }
    }

    // 'Rewind' controls, called by FixedUpdate
    public void FixedControlRewind()
    {
        // Get input
        float h = Input.GetAxis("Horizontal");

        // Get recording length
        int L = cube.ActiveInterval.End - cube.ActiveInterval.Start + 1;
        
        // Adjust rewind position
        cube.RewindPosition = Mathf.Clamp(cube.RewindPosition + 0.01f * L * h, 0, L - 1);

        // Debug
        // Debug.Log("rP = " + cube.RewindPosition + ", t1 = " + cube.ActiveInterval.Start + ", t2 = " + cube.ActiveInterval.End);
    }

    // 'Rewind' controls, called by Update
    public void ControlRewind()
    {
        // If pressing fire, request replay and exit cube
        if(Input.GetButtonDown("Fire1"))
        {
            // Make a request to start replaying
            cube.WillReplay = true;

            // TODO:  This is causing an animation bug.
            // Replay doesn't occur until NEXT frame, but we're snapping into
            // place below.  Should this be deferred?  It seems like sprite
            // is appearing too early, still showing last frame of Enter anim.

            // Begin the 'exit' animation process
            beginExitCube();
        }
    }

    // 'InCube' controls, called by Update
    public void ControlInCube()
    {
        // If pressing fire, exit cube
        if(Input.GetButtonDown("Fire1"))
        {
            // Begin the 'exit' animation process
            beginExitCube();
        }
    }



    // Fired on 'launch' animation
    void OnAnimationLaunch()
    {
        // Lower 'launching' flag
        isLaunching = false;

        // Make a request to jump
        if (isGrounded) { willLaunch = true; }
    }

    // Fired on 'climb' animation
    void OnAnimationClimb()
    {
        // Grapple point is (X, Y) = (0.3, 3)...
        // X travels 0.5 units, Y travels 3.0 units...

        // Snap into place
        transform.Translate(new Vector2((isFacingRight ? 1f : -1f ) * 0.5f, 3.0f));

        // Allow flip
        canFlip = true;

        // Lower 'hanging' and 'climbing' flag
        isHanging = false;
        isClimbing = false;
    }

    // Fired on 'button' animation
    void OnAnimationPushButton()
    {
        // If cube is off, make a request to start recording
        if (!cube.IsRecording && !cube.IsReplaying) { cube.WillRecord = true; }

        // Use 'standard' controls (we were frozen while 'activating')
        controls = "Standard";
    }

    // Fired on 'enter' animation
    void OnAnimationEnterCube()
    {
        // End the 'enter' animation process
        endEnterCube();
    }

    // Fired on 'exit' animation (release)
    void OnAnimationExitCube()
    {
        // Lower the 'attached' flag
        cubeAttached = false;
    }

    // Fired on 'exit' animation (end)
    void OnAnimationExitCube2()
    {
        // End the 'exit' animation process
        endExitCube();
    }

    // Fired on climb check collision
    public void OnTriggerEnterClimbCheck(Collider2D other)
    {
        // Grapple point is (X, Y) = (0.3, 3)...
        // X travels 0.5 units, Y travels 3.0 units...

        // Debug
        Debug.Log("OnTriggerEnterClimbCheck! fT = " + Time.fixedTime + ", name = " + name + ", other = " + other.collider2D.gameObject.name);

        // You can't climb another climbCheck...
        if(other.collider2D.gameObject.name == "climbCheck") { return; }



        // Get corner position
        Vector3 corner = other.collider2D.gameObject.transform.position;

        // Snap into place
        transform.position = corner - new Vector3(isFacingRight ? 0.3f : -0.3f, 3.0f);



        // Don't allow flip
        canFlip = false;

        // Raise 'hanging' flag
        isHanging = true;

        // Start 'hang' animation
        animator.SetTrigger("Hang");
    }

    // Fired on controller collision
    void OnControllerCollider(RaycastHit2D hit)
    {
        // If dynamic rigid body, apply normal force
        if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Dynamic") && hit.collider.rigidbody2D != null)
        {
            //Debug.Log(string.Format("{0}:  OnControllerCollider!  collider = {1}, point = {2}", Time.fixedTime, hit.collider.name, hit.point));
            GeometryHelper.DrawPoint(hit.point, Color.green, 0.1f);

            // Add to queue
            controllerHits[controllerHitCount] = hit;
            controllerHitCount++;
        }
    }

    // Apply collision forces on nearby dynamic objects
    void applyForces()
    {
        // TODO:  Handle multiple objects in queue

        // If no hits, skip
        if (controllerHitCount == 0) { return; }

        // Initialize point of contact
        Vector2 contact = Vector2.zero;

        // Add points
        for (int i = 0; i < controllerHitCount; i++) { contact += controllerHits[i].point; }

        // Divide by number of points
        contact /= controllerHitCount;
        GeometryHelper.DrawPoint(contact, Color.yellow, 0.08f);

        // Apply force
        float m = rigidbody2D.mass;
        Vector2 n = controllerHits[0].normal;
        float v = Vector2.Dot(velocity, n);
        controllerHits[0].collider.rigidbody2D.AddForceAtPosition(1.0f * m * v * n, contact);
    }



    // Get an array of all animated transforms
    public Transform[] GetBones(Transform root)
    {
        // Initialize result
        Transform[] bones = new Transform[18];

        // Get bones
        bones[0]  = root;
        bones[1]  = root.Find("Template_Waist");
        bones[2]  = root.Find("Template_Waist/Template_Back");
        bones[3]  = root.Find("Template_Waist/Template_Back/Template_Shoulders");
        bones[4]  = root.Find("Template_Waist/Template_Back/Template_Shoulders/Template_Neck");
        bones[5]  = root.Find("Template_Waist/Template_Back/Template_Shoulders/Template_Neck/Template_Head");
        bones[6]  = root.Find("Template_Waist/Template_Back/Template_Shoulders/Template_LeftArm");
        bones[7]  = root.Find("Template_Waist/Template_Back/Template_Shoulders/Template_LeftArm/Template_LeftForearm");
        bones[8]  = root.Find("Template_Waist/Template_Back/Template_Shoulders/Template_LeftArm/Template_LeftForearm/Template_LeftHand");
        bones[9]  = root.Find("Template_Waist/Template_Back/Template_Shoulders/Template_RightArm");
        bones[10] = root.Find("Template_Waist/Template_Back/Template_Shoulders/Template_RightArm/Template_RightForearm");
        bones[11] = root.Find("Template_Waist/Template_Back/Template_Shoulders/Template_RightArm/Template_RightForearm/Template_RightHand");
        bones[12] = root.Find("Template_Waist/Template_LeftLeg");
        bones[13] = root.Find("Template_Waist/Template_LeftLeg/Template_LeftShin");
        bones[14] = root.Find("Template_Waist/Template_LeftLeg/Template_LeftShin/Template_LeftFoot");
        bones[15] = root.Find("Template_Waist/Template_RightLeg");
        bones[16] = root.Find("Template_Waist/Template_RightLeg/Template_RightShin");
        bones[17] = root.Find("Template_Waist/Template_RightLeg/Template_RightShin/Template_RightFoot");

        // Return result
        return bones;
    }

    // Called before 'enter' animation
    private void beginEnterCube()
    {
        // Don't allow flip
        canFlip = false;

        // Initialize animation parameters
        cubeTime = 0f;
        cubeLeft = transform.position.x < cube.transform.position.x;
        cubeEdge = GeometryHelper.FindTopEdge(cube.transform, (BoxCollider2D)cube.collider2D, cubeLeft);
      
        // Start 'enter' animation
        animator.SetTrigger("Cube_Enter");

        // Use 'entering' controls
        controls = "Entering";
    }

    // Called during 'enter' animation
    private void doEnterCube()
    {
        // Convert edge points to world space
        Vector3 e1 = cube.transform.TransformPoint(cubeEdge[0]);
        Vector3 e2 = cube.transform.TransformPoint(cubeEdge[1]);
        
        // Get edge direction vector (from 'close' point to 'far' point)
        Vector3 v1 = e2 - e1;
        // Debug.DrawLine(Vector3.zero, v1, Color.green);
        
        // Rotate 90 degrees (toward ground)
        Vector3 v2 = GeometryHelper.Rotate(v1, cubeLeft ? -90f : 90f, Vector3.zero);
        // Debug.DrawLine(Vector3.zero, v2, Color.yellow);
        
        // Subtract by animation x-offset
        Vector3 v3 = v2 - 1f * Vector3.Normalize(v1);
        // Debug.DrawLine(Vector3.zero, v3, Color.red);
        
        // Add to 'close' point
        Vector3 v4 = v3 + e1;
        // Debug.DrawLine(Vector3.zero, v4, Color.cyan);
        
        
        
        // How to rotate character?
        // If entering from left, we want angle from x-axis to edge direction vector
        // If entering from right, we want angle from x-axis to negative edge direction vector
        if (!cubeLeft) { v1 *= -1f; }
        float theta = Mathf.Atan2(v1.y, v1.x) * 180f / Mathf.PI;
        
        
        
        // Lerp rotation
        theta = Mathf.LerpAngle(transform.localRotation.eulerAngles.z, theta, cubeTime);
        transform.localRotation = Quaternion.identity;
        transform.Rotate(new Vector3(0f, 0f, theta));
        
        // Lerp position
        transform.localPosition = Vector3.Lerp(transform.localPosition, v4, cubeTime);
        
        // Increment time
        cubeTime += 0.5f * Time.deltaTime;
    }
    
    // Called after 'enter' animation
    private void endEnterCube()
    {
        // Temporarily hide sprite
        transform.localPosition = new Vector3(0f, 20000f, 0f);
        transform.localRotation = Quaternion.identity;

        // Set camera target to cube
        camera.target = cube.transform;

        // Bug:  Wait for rewind controls (happens in FixedUpdate)
        // Otherwise, lerping still happens after 'hide' above
        controls = "";

        // Check if battery is still alive
        if (cube.IsRecording)
        {
            // If so, make a request to start rewinding
            cube.WillRewind = true;
        }
        else
        {
            // Otherwise, we're just sitting in a dead cube
            controls = "InCube";
        }
    }

    // Called before 'exit' animation
    private void beginExitCube()
    {
        // Disable collider
        box.enabled = false; // TODO:  instead, make Player-Dynamic layers non-collidable?

        // Initialize animation parameters
        cubeTime = 0f;
        cubeLeft = false; // TODO:  opposite of activation side?
        cubeAttached = true;
        cubeEdge = GeometryHelper.FindTopEdge(cube.transform, (BoxCollider2D)cube.collider2D, cubeLeft);

        // Snap into place
        doExitCube();

        // Set camera target to player
        camera.target = transform;

        // Start 'exit' animation
        animator.SetTrigger("Cube_Exit");

        // Use 'exiting' controls
        controls = "Exiting";
    }

    // Called during 'exit' animation
    private void doExitCube()
    {
        // Convert edge points to world space
        Vector3 e1 = cube.transform.TransformPoint(cubeEdge[0]);
        Vector3 e2 = cube.transform.TransformPoint(cubeEdge[1]);
        GeometryHelper.DrawPoint(e1, Color.green, 0.1f);
        GeometryHelper.DrawPoint(e2, Color.yellow, 0.1f);

        // Get edge direction vector (from 'close' point to 'far' point)
        Vector3 v1 = e2 - e1;
        // Debug.DrawLine(Vector3.zero, v1, Color.green);
        
        // Rotate 90 degrees (toward ground)
        Vector3 v2 = GeometryHelper.Rotate(v1, cubeLeft ? -90f : 90f, Vector3.zero);
        // Debug.DrawLine(Vector3.zero, v2, Color.yellow);
        
        // Subtract by animation x-offset
        Vector3 v3 = v2 + 1f * Vector3.Normalize(v1);
        // Debug.DrawLine(Vector3.zero, v3, Color.red);
        
        // Add to 'close' point
        Vector3 v4 = v3 + e1;
        // Debug.DrawLine(Vector3.zero, v4, Color.cyan);

        

        // Proceed according to release
        if (cubeAttached)
        {
            // Phase 1:  Still attached to cube

            // How to rotate character?
            // If entering from left, we want angle from x-axis to edge direction vector
            // If entering from right, we want angle from x-axis to negative edge direction vector
            if (!cubeLeft) { v1 *= -1f; }
            float theta = Mathf.Atan2(v1.y, v1.x) * 180f / Mathf.PI;
            // while (theta < 0) { theta += 360f; }

            // Link
            transform.localPosition = v4;
            transform.localRotation = Quaternion.identity;
            transform.Rotate(new Vector3(0f, 0f, theta));
        }
        else
        {
            // Phase 2:  No longer attached to cube

            // If just released, find landing spot
            //if (cubeTime == 0)
            //{
                Bounds bounds = cube.collider2D.bounds;
                cubeLand = bounds.center + new Vector3((isFacingRight ? 1f : -1f) * bounds.extents.x, -bounds.extents.y, 0f);
                cubeLand += -1f * (isFacingRight ? Vector3.right : Vector3.left);
                GeometryHelper.DrawPoint(cubeLand, Color.green, 0.2f);
            //}

            // Lerp position
            transform.localPosition = Vector3.Lerp(transform.localPosition, cubeLand, cubeTime);
            GeometryHelper.DrawPoint(cubeLand, Color.green, 0.2f);

            // Lerp rotation
            float theta = Mathf.LerpAngle(transform.localRotation.eulerAngles.z, 0f, cubeTime);
            transform.localRotation = Quaternion.identity;
            transform.Rotate(new Vector3(0f, 0f, theta));

            // Increment time
            Debug.Log("cubeTime = " + cubeTime);
            cubeTime += 3f * Time.deltaTime;
        }
    }
    
    // Called after 'exit' animation
    private void endExitCube()
    {
        // Grapple point is (X, Y) = (1, 2)...
        // X travels 2 units, Y travels 0 units...

        // Snap into place
        transform.Translate(new Vector2((isFacingRight ? 1f : -1f ) * 2f, 0f));
        transform.localRotation = Quaternion.identity;

        // Reset velocity
        velocity = Vector3.zero;

        // Enable collider
        box.enabled = true;

        // Allow flip
        canFlip = true;

        // Use 'standard' controls
        controls = "Standard";
    }
}
