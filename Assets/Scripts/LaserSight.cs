using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class LaserSight : MonoBehaviour
{
    public float maxRange = 100f;
    private LineRenderer line;

    void Start()
    {
        line = GetComponent<LineRenderer>();
        // Set the start position of the line to (0,0,0) relative to this object
        line.SetPosition(0, Vector3.zero); 
    }

    void Update()
    {
        // Set the start position (it's always 0,0,0 relative to us)
        line.SetPosition(0, Vector3.zero);

        // Cast a ray forward
        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.forward, out hit, maxRange))
        {
            // We hit something. Set the end point to the hit location.
            // We use transform.InverseTransformPoint to convert the world hit-point
            // to a local point for the LineRenderer.
            line.SetPosition(1, transform.InverseTransformPoint(hit.point));
        }
        else
        {
            // We hit nothing. Set the end point to max range.
            line.SetPosition(1, new Vector3(0, 0, maxRange));
        }
    }
}