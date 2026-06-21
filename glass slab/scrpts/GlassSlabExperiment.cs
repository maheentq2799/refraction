using UnityEngine;
using UnityEngine.InputSystem; // Pure New Input System use karne ke liye

[RequireComponent(typeof(LineRenderer))]
public class GlassSlabExperiment : MonoBehaviour
{
    [Header("Refraction Settings")]
    public float glassIOR = 1.5f;   
    public float airIOR = 1.0f;     
    public float rayLength = 30f;   

    [Header("Pins Setup")]
    public GameObject pinPrefab;    
    private float pinHeight = 0.18f; 

    private LineRenderer lineRenderer;
    private GameObject pinP1, pinP2, pinP3, pinP4;
    private Camera mainCam;

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        mainCam = Camera.main;

        if (pinPrefab != null)
        {
            pinP1 = Instantiate(pinPrefab); pinP1.name = "Pin_P1";
            pinP2 = Instantiate(pinPrefab); pinP2.name = "Pin_P2";
            pinP3 = Instantiate(pinPrefab); pinP3.name = "Pin_P3";
            pinP4 = Instantiate(pinPrefab); pinP4.name = "Pin_P4";
            
            pinP1.transform.position = new Vector3(-2.17f, 0.18f, -2.13f);
            pinP1.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
            pinP1.transform.localScale = new Vector3(0.02f, 0.2f, 0.02f);

            Vector3 customScale = new Vector3(0.02f, 0.2f, 0.02f);
            pinP2.transform.localScale = customScale; pinP2.transform.rotation = Quaternion.identity;
            pinP3.transform.localScale = customScale; pinP3.transform.rotation = Quaternion.identity;
            pinP4.transform.localScale = customScale; pinP4.transform.rotation = Quaternion.identity;

            DisableCollider(pinP2);
            DisableCollider(pinP3);
            DisableCollider(pinP4);
            
            BoxCollider boxCol = pinP1.GetComponent<BoxCollider>();
            if(boxCol == null) boxCol = pinP1.AddComponent<BoxCollider>();
            boxCol.size = new Vector3(3f, 1f, 3f); 

            // Naye input system wala fully compatible dragger lagana
            NewInputPinDragger dragger = pinP1.AddComponent<NewInputPinDragger>();
            dragger.mainCam = mainCam;
            dragger.pinHeight = pinHeight; 
        }
    }

    void DisableCollider(GameObject obj)
    {
        if (obj == null) return;
        Collider col = obj.GetComponent<Collider>();
        if (col != null) col.enabled = false;
    }

    void Update()
    {
        CalculateRefractionFromPin1();
    }

    void CalculateRefractionFromPin1()
    {
        if (pinP1 == null) return;

        Vector3[] rayPoints = new Vector3[4];
        rayPoints[0] = pinP1.transform.position; 

        Vector3 targetPoint = new Vector3(0f, 0.18f, 0f); 
        Vector3 laserDirection = (targetPoint - rayPoints[0]).normalized;

        RaycastHit hitEnter;

        if (Physics.Raycast(rayPoints[0], laserDirection, out hitEnter, rayLength))
        {
            rayPoints[1] = hitEnter.point; 

            if (pinP2)
            {
                Vector3 p2Pos = Vector3.Lerp(rayPoints[0], rayPoints[1], 0.6f);
                pinP2.transform.position = new Vector3(p2Pos.x, pinHeight, p2Pos.z);
            }

            Vector3 normal1 = hitEnter.normal;
            Vector3 refractedDir = RefractRay(laserDirection, normal1, airIOR, glassIOR);

            RaycastHit hitExit;
            Vector3 insideSlabStartPoint = hitEnter.point + refractedDir * 0.05f; 

            if (Physics.Raycast(insideSlabStartPoint, refractedDir, out hitExit, rayLength))
            {
                rayPoints[2] = hitExit.point; 

                Vector3 normal2 = -hitExit.normal; 
                Vector3 emergentDir = RefractRay(refractedDir, normal2, glassIOR, airIOR);

                rayPoints[3] = hitExit.point + emergentDir * rayLength;
                lineRenderer.positionCount = 4;

                if (pinP3 && pinP4)
                {
                    Vector3 p3Pos = Vector3.Lerp(rayPoints[2], rayPoints[3], 0.3f);
                    pinP3.transform.position = new Vector3(p3Pos.x, pinHeight, p3Pos.z);

                    Vector3 p4Pos = Vector3.Lerp(rayPoints[2], rayPoints[3], 0.7f);
                    pinP4.transform.position = new Vector3(p4Pos.x, pinHeight, p4Pos.z);
                }
            }
            else
            {
                rayPoints[2] = hitEnter.point + refractedDir * rayLength;
                lineRenderer.positionCount = 3;
                HidePins(pinP3, pinP4);
            }
        }
        else
        {
            rayPoints[1] = rayPoints[0] + laserDirection * rayLength;
            lineRenderer.positionCount = 2;
            HidePins(pinP2, pinP3, pinP4);
        }

        lineRenderer.SetPositions(rayPoints);
    }

    void HidePins(params GameObject[] pins)
    {
        foreach (var p in pins)
        {
            if (p) p.transform.position = Vector3.down * 100f;
        }
    }

    Vector3 RefractRay(Vector3 incident, Vector3 normal, float n1, float n2)
    {
        float eta = n1 / n2;
        float cosTheta1 = Mathf.Clamp(Vector3.Dot(-incident, normal), -1f, 1f);
        float k = 1f - eta * eta * (1f - cosTheta1 * cosTheta1);
        return k < 0f ? Vector3.Reflect(incident, normal) : eta * incident + (eta * cosTheta1 - Mathf.Sqrt(k)) * normal;
    }
}

// DRAG COMPONENT FOR UNITY 6 INPUT SYSTEM (Zero Old Input Code)
public class NewInputPinDragger : MonoBehaviour
{
    public Camera mainCam;
    public float pinHeight;
    private bool isDragging = false;

    void Update()
    {
        if (mainCam == null || Mouse.current == null) return;

        Vector2 mousePos = Mouse.current.position.ReadValue();
        bool isClicking = Mouse.current.leftButton.isPressed;

        if (isClicking)
        {
            Ray ray = mainCam.ScreenPointToRay(mousePos);
            RaycastHit hit;

            if (!isDragging)
            {
                if (Physics.Raycast(ray, out hit))
                {
                    if (hit.transform == transform)
                    {
                        isDragging = true;
                    }
                }
            }

            if (isDragging)
            {
                Plane groundPlane = new Plane(Vector3.up, new Vector3(0, pinHeight, 0));
                float rayDistance;

                if (groundPlane.Raycast(ray, out rayDistance))
                {
                    Vector3 targetPos = ray.GetPoint(rayDistance);
                    transform.position = new Vector3(targetPos.x, pinHeight, targetPos.z);
                }
            }
        }
        else
        {
            isDragging = false;
        }
    }
}