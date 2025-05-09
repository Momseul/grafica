using UnityEngine;
using System.Collections.Generic;

public struct Wave // Keep this struct definition
{
    public Vector2 origin;
    public float startTime;
    public float initialAmplitude;
    public float frequency;
    public float speed;
    public float lifetime;

    public Wave(Vector2 origin, float startTime, float initialAmplitude, float frequency, float speed, float lifetime)
    {
        this.origin = origin;
        this.startTime = startTime;
        this.initialAmplitude = initialAmplitude;
        this.frequency = frequency;
        this.speed = speed;
        this.lifetime = lifetime;
    }
}

[RequireComponent(typeof(MeshFilter))]

public class WaveController : MonoBehaviour
{
    public float defaultAmplitude = 0.1f;
    public float defaultFrequency = 1.0f;
    public float defaultSpeed = 1.0f;
    public float defaultLifetime = 5.0f;
    public float dampingFactor = 0.5f;

    private MeshFilter meshFilter;
    private Mesh mesh;
    private Vector3[] baseVertices;
    private Vector3[] workingVertices;

    private List<Wave> activeWaves = new List<Wave>();

void Start()
{
    meshFilter = GetComponent<MeshFilter>();
    if (meshFilter.sharedMesh == null) 
    {
        enabled = false;
        return;
    }

    mesh = Instantiate(meshFilter.sharedMesh); 
    mesh.name = meshFilter.sharedMesh.name + "_WaveControllerInstance"; 
    meshFilter.mesh = mesh; 

    baseVertices = mesh.vertices;
    if (baseVertices == null || baseVertices.Length == 0) {
        enabled = false;
        return;
    }

    workingVertices = new Vector3[baseVertices.Length];
    System.Array.Copy(baseVertices, workingVertices, baseVertices.Length);

    if (baseVertices.Length > 0 && baseVertices.Length < 5) {
        for(int k=0; k < baseVertices.Length; ++k) Debug.Log($"  BaseVertex[{k}]: {baseVertices[k]}");
    } else if (baseVertices.Length >= 5) {
        for(int k=0; k < 5; ++k) Debug.Log($"  BaseVertex[{k}]: {baseVertices[k]}");
    }
}

void Update()
{
        if (Time.frameCount < 5 || Time.frameCount % 300 == 0) { // Log infrequently
    }
    if (workingVertices == null || workingVertices.Length == 0 || baseVertices == null || baseVertices.Length == 0 || mesh == null)
    {
        if (Time.frameCount < 5 || Time.frameCount % 300 == 0) { 
        }
        return; 
    }
    if (Time.frameCount < 5 || Time.frameCount % 300 == 0) { // Infrequent log
    }

    // VITAL: Reset vertex heights
    for (int i = 0; i < workingVertices.Length; i++)
    {
        workingVertices[i].y = baseVertices[i].y; 
    }

    float currentTime = Time.time; 

    for (int w = activeWaves.Count - 1; w >= 0; w--)
    {
        Wave wave = activeWaves[w];
        float timeSinceStart = currentTime - wave.startTime;

        if (timeSinceStart > wave.lifetime)
        {
            Debug.LogWarning($"Wave {w} EXPIRED & REMOVED. TimeSinceStart ({timeSinceStart:F2}) > WaveLifetime ({wave.lifetime:F2}).");
            activeWaves.RemoveAt(w);
            continue;
        }

        // Damping Factor is 0 
        float currentAmplitude = wave.initialAmplitude * Mathf.Pow(1f - dampingFactor, timeSinceStart);
        currentAmplitude = Mathf.Max(0, currentAmplitude); 

        if (w == activeWaves.Count - 1 && Time.frameCount % 60 == 0) 
        {
        }

        if (currentAmplitude < 0.001f)
        {
            if (Time.frameCount % 60 == 0) Debug.LogWarning($"Wave {w} amplitude too small ({currentAmplitude:F4}). Removing.");
            activeWaves.RemoveAt(w);
            continue;
        }

        bool anyVertexMovedThisWave = false;
        float maxDisplacementThisFrameForThisWave = 0f;

        for (int i = 0; i < workingVertices.Length; i++)
        {
            Vector3 vertex = baseVertices[i];
            float distance = Vector2.Distance(new Vector2(vertex.x, vertex.z), wave.origin);
            float displacement = 0f;

            if (timeSinceStart * wave.speed >= distance) // Wavefront check
            {
                float k = 2 * Mathf.PI * wave.frequency;
                float omega = k * wave.speed;
                float sineArgument = k * distance - omega * timeSinceStart;
                float sineValue = Mathf.Sin(sineArgument);
                displacement = currentAmplitude * sineValue;
                if (timeSinceStart > wave.lifetime)
                {
                    activeWaves.RemoveAt(w);
                    continue;
                }    
                workingVertices[i].y += displacement;
                
                if (Mathf.Abs(displacement) > 0.001f)
                {
                    anyVertexMovedThisWave = true;
                }
                if (Mathf.Abs(displacement) > maxDisplacementThisFrameForThisWave)
                {
                    maxDisplacementThisFrameForThisWave = Mathf.Abs(displacement);
                }
            }
        }

        if (w == activeWaves.Count - 1 && Time.frameCount % 60 == 0) 
        {
            if (anyVertexMovedThisWave) {
                 Debug.Log($"Wave {w}: MaxDisplacementThisFrame={maxDisplacementThisFrameForThisWave:F5}");
            } else if (currentAmplitude > 0.001f) { // Only log warning if amp was high enough
                 Debug.LogWarning($"Wave {w} (CurrentAmp {currentAmplitude:F3}, TimeSinceStart {timeSinceStart:F2}) did not move ANY vertices this frame. MaxDisp recorded: {maxDisplacementThisFrameForThisWave:F5}");
            }
        }
    }

    mesh.vertices = workingVertices;
    mesh.RecalculateNormals();
    mesh.RecalculateBounds();

    MeshCollider mc = GetComponent<MeshCollider>();
    if (mc != null) {
        mc.sharedMesh = null; // Force update
        mc.sharedMesh = mesh;
    }
}



public void AddWave(Vector2 origin, float amplitude, float frequency, float speed, float lifetime)
{

    if (amplitude <= 0.001f) // Check if amplitude is too small
    {
        Debug.LogWarning($"WaveController ({gameObject.name}): Attempted to add wave with negligible or zero amplitude ({amplitude:F3}). Wave not added.", this);
        return;
    }
    float waveStartTime = Time.time; 
    activeWaves.Add(new Wave(origin, waveStartTime, amplitude, frequency, speed, lifetime));
}


    public float GetDeformedWaterHeightAtLocalXZ(Vector2 localXZ)
    {
        float dynamicHeight = 0f;
        float currentTime = Time.time;

        foreach (Wave wave in activeWaves)
        {
            float timeSinceWaveStart = currentTime - wave.startTime;
            if (timeSinceWaveStart > wave.lifetime || timeSinceWaveStart < 0) continue;

            float currentAmplitude = wave.initialAmplitude * Mathf.Pow(1f - dampingFactor, timeSinceWaveStart);
            currentAmplitude = Mathf.Max(0, currentAmplitude);
            if (currentAmplitude < 0.001f) continue;

            float distance = Vector2.Distance(localXZ, wave.origin);
            if (timeSinceWaveStart * wave.speed >= distance)
            {
                float k = 2 * Mathf.PI * wave.frequency;
                float omega = k * wave.speed;
                dynamicHeight += currentAmplitude * Mathf.Sin(k * distance - omega * timeSinceWaveStart);
            }
        }
        return baseVertices.Length > 0 ? baseVertices[0].y + dynamicHeight : dynamicHeight;
    }

    public float GetWaterHeightAtLocalPoint(Vector2 localXZ)
{
    if (mesh == null || workingVertices == null || baseVertices == null || workingVertices.Length == 0)
    {
        return transform.position.y;
    }

    float minSqDist = float.MaxValue;
    int closestVertexIndex = -1;

    for (int i = 0; i < workingVertices.Length; i++)
    {
        float sqDist = (new Vector2(workingVertices[i].x, workingVertices[i].z) - localXZ).sqrMagnitude;
        if (sqDist < minSqDist)
        {
            minSqDist = sqDist;
            closestVertexIndex = i;
        }
    }

    if (closestVertexIndex != -1)
    {

        return workingVertices[closestVertexIndex].y; 
    }

    return 0f; 
}
public int GetActiveWaveCount()
{
    return activeWaves.Count;
}

public bool GetWaterHeightAtWorldXZ_Barycentric(Vector2 worldXZ, out float worldY)
{
    worldY = transform.position.y; 

    if (mesh == null || workingVertices == null || workingVertices.Length == 0) return false;

    Ray ray = new Ray(new Vector3(worldXZ.x, transform.position.y + 10f, worldXZ.y), Vector3.down); 
    RaycastHit hit;


    MeshCollider mc = GetComponent<MeshCollider>();
    if (mc == null) {
        Debug.LogError("WaveController needs a MeshCollider to raycast for precise water height.", this);
        return false;
    }



    if (mc.Raycast(ray, out hit, 20f)) // Raycast distance 20 units
    {
        Mesh hitMesh = mesh; 

        int[] triangles = hitMesh.triangles;
        Vector3 p0 = workingVertices[triangles[hit.triangleIndex * 3 + 0]];
        Vector3 p1 = workingVertices[triangles[hit.triangleIndex * 3 + 1]];
        Vector3 p2 = workingVertices[triangles[hit.triangleIndex * 3 + 2]];

        p0 = transform.TransformPoint(p0);
        p1 = transform.TransformPoint(p1);
        p2 = transform.TransformPoint(p2);
        
        Vector3 barycentricCoords = hit.barycentricCoordinate;

        worldY = p0.y * barycentricCoords.x +
                 p1.y * barycentricCoords.y +
                 p2.y * barycentricCoords.z;
        return true;
    }
    return false; 
}
}
