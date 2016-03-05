using UnityEngine;
using System.Collections;

public static class GeometryHelper
{
    // Rotate a vector by an angle about an axis
    public static Vector3 Rotate(Vector3 vector, float angle, Vector3 axis)
    {
        // Convert to radians
        angle = angle * Mathf.PI / 180f;

        // Translate into axis-centered coordinate system
        float x1 = vector.x - axis.x;
        float y1 = vector.y - axis.y;
        
        // Rotate by angle about new origin
        float x2 = x1 * Mathf.Cos(angle) - y1 * Mathf.Sin(angle);
        float y2 = x1 * Mathf.Sin(angle) + y1 * Mathf.Cos(angle);
        
        // Translate back into original coordinate system
        float x = x2 + axis.x;
        float y = y2 + axis.y;
        
        // Return result
        return new Vector3(x, y, 0f);
    }

    // Find 'topmost' edge of a rectangle.  'Topmost' is evaluated in world
    // space, but the result is returned in local space.  If two edges are tied
    // for the 'topmost' position, such as in a square rotated by 45 degrees, we
    // will pick the 'leftmost' or 'rightmost' edge, depending on the pickLeft
    // parameter.  Also, we will structure the resultant array such that the
    // first element is the 'leftmost' or 'rightmost' endpoint, depending on the
    // pickLeft parameter.
    public static Vector3[] FindTopEdge(Transform transform, BoxCollider2D box, bool pickLeft)
    {
        // Initialize result
        Vector3[] result = new Vector3[2];

        // Get array of vertices in local space
        float w = box.size.x;
        float h = box.size.y;
        Vector3[] vertices = new Vector3[4];
        vertices[0] = new Vector3(0, 0, 0);
        vertices[1] = new Vector3(0, h, 0);
        vertices[2] = new Vector3(w, h, 0);
        vertices[3] = new Vector3(w, 0, 0);

        // Convert to world space
        for (int i = 0; i < vertices.Length; i++) { vertices[i] = transform.TransformPoint(vertices[i]); }



        // The maximum y-coordinates, and corresponding vertices
        float y1 = float.MinValue;
        float y2 = float.MinValue;
        float y3 = float.MinValue;
        Vector3 v1 = Vector3.zero;
        Vector3 v2 = Vector3.zero;
        Vector3 v3 = Vector3.zero;

        // Loop through vertices
        foreach(Vector3 vertex in vertices)
        {
            // Get current y-position
            float y = vertex.y;
            
            // Check if greatest
            if(y > y1)
            {
                // If so, demote the former champions
                y3 = y2;
                v3 = v2;

                y2 = y1;
                v2 = v1;
                
                // Proclaim the new champion
                y1 = y;
                v1 = vertex;
            }
            
            // Otherwise, check if second greatest
            else if (y > y2)
            {
                // If so, demote former champion
                y3 = y2;
                v3 = v2;

                // Proclaim new champion
                y2 = y;
                v2 = vertex;
            }

            // Otherwise, check if third greatest
            else if (y > y3)
            {
                // Proclaim
                y3 = y;
                v3 = vertex;
            }
        }

        // Debug
        // Debug.Log(string.Format("v1 = {0}\nv2 = {1}\nv3 = {2}", v1, v2, v3));
        // DrawPoint(v1, Color.green, 0.1f);
        // DrawPoint(v2, Color.yellow, 0.1f);
        // DrawPoint(v3, Color.red, 0.1f);



        // Check for a tie
        if (Mathf.RoundToInt(100f * y2) == Mathf.RoundToInt(100f * y3))
        {
            // If tied, we proceed according to the pickLeft parameter
            if (pickLeft)
            {
                result[0] = v2.x < v3.x ? v2 : v3;
            }
            else
            {
                result[0] = v2.x > v3.x ? v2 : v3;
            }

            // Add topmost vertex
            result[1] = v1;
        }
        else
        {
            // Otherwise, we return the 'close' and 'far' points, respectively
            if (pickLeft)
            {
                result[0] = v1.x < v2.x ? v1 : v2;
                result[1] = v1.x < v2.x ? v2 : v1;
            }
            else
            {
                result[0] = v1.x > v2.x ? v1 : v2;
                result[1] = v1.x > v2.x ? v2 : v1;
            }
        }

        // Debug
        // DrawPoint(result[0], Color.green, 0.1f);
        // DrawPoint(result[1], Color.yellow, 0.1f);

        // Convert back to local space
        result[0] = transform.InverseTransformPoint(result[0]);
        result[1] = transform.InverseTransformPoint(result[1]);

        // Return result
        return result;
    }

    // Draw a point
    public static void DrawPoint(Vector2 point, Color color, float radius)
    {
        // Top edge
        Debug.DrawLine(point + new Vector2(-1,  1) * radius, point + new Vector2( 1,  1) * radius, color);
        // Right edge
        Debug.DrawLine(point + new Vector2( 1,  1) * radius, point + new Vector2( 1, -1) * radius, color);
        // Bottom edge
        Debug.DrawLine(point + new Vector2( 1, -1) * radius, point + new Vector2(-1, -1) * radius, color);
        // Left edge
        Debug.DrawLine(point + new Vector2(-1, -1) * radius, point + new Vector2(-1,  1) * radius, color);
    }
}


