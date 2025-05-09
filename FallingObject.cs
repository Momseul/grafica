using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(SphereCollider))]
public class FallingObject : MonoBehaviour
{
    [Header("Falling & Impact")]
    public float fallAcceleration = 9.81f;
    public float sphereRadius = 0.5f; 
    public float impactVelocityThreshold = 0.1f;

    [Header("Floating Properties")]
    [Tooltip("0.0=bottom on surface, 0.5=center at surface (half submerged), 1.0=top on surface (fully submerged).")]
    public float restingSubmergence = 0.5f; 
    public float buoyancySpringStrength = 40f; 
    public float waterLinearDrag = 1.5f;     
    public float settlingVelocityThreshold = 0.05f; 
    public float settlingPositionThreshold = 0.02f; 

    private Rigidbody rb;
    private bool isInWater = false;
    private WaveController waveController;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null) {
            Debug.LogError("FallingObject on " + gameObject.name + " requires a Rigidbody component!", this);
            enabled = false;
            return;
        }
        rb.useGravity = false; 
    }

    void Start()
    {
        waveController = FindObjectOfType<WaveController>();
        if (waveController == null) {
            Debug.LogError($"FallingObject ({gameObject.name}): WaveController not found!", this);
            enabled = false;
            return;
        }
        
        SphereCollider sCollider = GetComponent<SphereCollider>();
        if (sCollider != null) {
            float scale = Mathf.Max(transform.localScale.x, Mathf.Max(transform.localScale.y, transform.localScale.z));
            if (scale <= 0f) scale = 1f; 
            sphereRadius = sCollider.radius * scale;
        }
        if (sphereRadius <= 0.001f) { 
            Debug.LogWarning($"Sphere radius on {gameObject.name} is very small ({sphereRadius}). Using 0.5f.");
            sphereRadius = 0.5f;
        }
    }

    void FixedUpdate() 
    {
        if (waveController == null || rb == null) return;

        if (!isInWater)
        {
            // --- FALLING IN AIR ---
            Vector3 currentVelocity = rb.linearVelocity;
            currentVelocity.y -= fallAcceleration * Time.fixedDeltaTime;
            rb.linearVelocity = currentVelocity;

            float waterSurfaceBaseWorldY = waveController.transform.position.y; 
            float sphereBottomWorldY = rb.position.y - sphereRadius;

            if (sphereBottomWorldY <= waterSurfaceBaseWorldY)
            {
                // --- INITIAL IMPACT ---
                float impactSpeed = Mathf.Abs(rb.linearVelocity.y);
                Vector3 impactVelocity = rb.linearVelocity;       

                if (impactSpeed >= impactVelocityThreshold)
                {
                    Vector3 worldImpactPoint = new Vector3(rb.position.x, waterSurfaceBaseWorldY, rb.position.z);
                    Vector3 localImpactPoint = waveController.transform.InverseTransformPoint(worldImpactPoint);
                    Vector2 waveOrigin = new Vector2(localImpactPoint.x, localImpactPoint.z);
                    float newAmplitude = waveController.defaultAmplitude * Mathf.Clamp(impactSpeed / 3f, 0.2f, 2.5f); // Or whatever your values were
                    waveController.AddWave(waveOrigin, newAmplitude, waveController.defaultFrequency, waveController.defaultSpeed, waveController.defaultLifetime);
                }
                
                isInWater = true;
                Vector3 newVelocityInWater = impactVelocity;
                newVelocityInWater.y *= -0.2f; 
                rb.linearVelocity = newVelocityInWater;
                

                Vector3 correctedPosition = rb.position;
                float initialTargetY = waterSurfaceBaseWorldY + (sphereRadius * (1.0f - 2.0f * restingSubmergence));
                correctedPosition.y = initialTargetY;
                rb.MovePosition(correctedPosition);
            }
        }
        else 
        {
            // --- IN WATER (BOBBING/FLOATING) ---
            float actualWaterHeightAtSphere;
            bool foundWaterHeight = waveController.GetWaterHeightAtWorldXZ_Barycentric(
                                        new Vector2(rb.position.x, rb.position.z), 
                                        out actualWaterHeightAtSphere
                                    );

            Vector3 forces = Vector3.zero;

            // 1. Gravity
            forces.y -= fallAcceleration * rb.mass;

            // 2. Buoyancy Spring Force
            if (foundWaterHeight)
            {
                float targetYPosition = actualWaterHeightAtSphere + (sphereRadius * (1.0f - 2.0f * restingSubmergence));
                float currentYPosition = rb.position.y;
                float displacementY = targetYPosition - currentYPosition;
                
                forces.y += displacementY * buoyancySpringStrength;


                if (Mathf.Abs(displacementY) < settlingPositionThreshold && Mathf.Abs(rb.linearVelocity.y) < settlingVelocityThreshold)
                {
                    rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z); // Stop Y velocity
                    rb.MovePosition(new Vector3(rb.position.x, targetYPosition, rb.position.z)); // Snap to target Y
                    // No further forces needed this frame if we snapped
                    return; // Exit FixedUpdate early after snapping
                }
            }
            else
            {

            }
        
            Vector3 currentVel = rb.linearVelocity;
            forces.x -= currentVel.x * waterLinearDrag * 0.5f; // Less horizontal drag
            forces.y -= currentVel.y * waterLinearDrag;       // Full vertical drag
            forces.z -= currentVel.z * waterLinearDrag * 0.5f; // Less horizontal drag

            // Apply accumulated forces
            rb.AddForce(forces, ForceMode.Force);
        }
    }
}