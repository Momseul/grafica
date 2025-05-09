using UnityEngine;
using System.IO; // Needed for Path operations

#if UNITY_EDITOR // Only compile this part in the Unity Editor
using UnityEditor;
#endif

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class PlaneGenerator : MonoBehaviour
{
    public int xSegments = 50; // Number of segments along X
    public int zSegments = 50; // Number of segments along Z
    public float width = 10f;  // Total width
    public float length = 10f; // Total length

    private bool meshGeneratedAndSaved = false;

    void Start()
    {
        MeshFilter mf = GetComponent<MeshFilter>();
        if (mf.sharedMesh != null && mf.sharedMesh.name.StartsWith("GeneratedPlane_"))
        {
           Debug.Log("Mesh already generated and assigned: " + mf.sharedMesh.name);
           meshGeneratedAndSaved = true; 

           AssignDefaultMaterialIfNeeded();
           return; // Don't regenerate
        }

        if (!meshGeneratedAndSaved)
        {
             GenerateAndSavePlane();
        }
         else
         {
            // If already generated, just ensure material is set
            AssignDefaultMaterialIfNeeded();
         }
    }

    void GenerateAndSavePlane()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        Mesh mesh = new Mesh();

        meshFilter.mesh = mesh;

        int xVertices = xSegments + 1;
        int zVertices = zSegments + 1;
        int numVertices = xVertices * zVertices;
        int numTriangles = xSegments * zSegments * 6;

        Vector3[] vertices = new Vector3[numVertices];
        Vector2[] uv = new Vector2[numVertices];
        int[] triangles = new int[numTriangles];

        float xStep = width / xSegments;
        float zStep = length / zSegments;
        float uvXStep = 1.0f / xSegments;
        float uvZStep = 1.0f / zSegments;

        for (int z = 0; z < zVertices; z++)
        {
            for (int x = 0; x < xVertices; x++)
            {
                int index = z * xVertices + x;
                vertices[index] = new Vector3(x * xStep - width / 2f, 0, z * zStep - length / 2f);
                uv[index] = new Vector2(x * uvXStep, z * uvZStep);
            }
        }

        int triangleIndex = 0;
        for (int z = 0; z < zSegments; z++)
        {
            for (int x = 0; x < xSegments; x++)
            {
                int bottomLeft = z * xVertices + x;
                int bottomRight = bottomLeft + 1;
                int topLeft = (z + 1) * xVertices + x;
                int topRight = topLeft + 1;

                triangles[triangleIndex++] = bottomLeft;
                triangles[triangleIndex++] = topLeft;
                triangles[triangleIndex++] = bottomRight;

                triangles[triangleIndex++] = bottomRight;
                triangles[triangleIndex++] = topLeft;
                triangles[triangleIndex++] = topRight;
            }
        }

        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        string meshName = $"GeneratedPlane_{xSegments}x{zSegments}_{width}x{length}";
        mesh.name = meshName;

        AssignDefaultMaterialIfNeeded();

#if UNITY_EDITOR // --- Save Asset Logic (Editor Only) ---
        string directoryPath = "Assets/GeneratedMeshes";
        string filePath = $"{directoryPath}/{meshName}.asset";

        // Ensure the directory exists
        if (!Directory.Exists(Path.Combine(Application.dataPath, "GeneratedMeshes"))) // Check using full path
        {
            Debug.Log($"Creating directory: {directoryPath}");
            AssetDatabase.CreateFolder("Assets", "GeneratedMeshes");
        }

        if (AssetDatabase.LoadAssetAtPath<Mesh>(filePath) == null)
        {
            Debug.Log($"Saving new mesh asset to: {filePath}");
            AssetDatabase.CreateAsset(mesh, filePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(); 
            meshGeneratedAndSaved = true;
            Debug.Log("Mesh saved successfully!");
        }
        else
        {
             Debug.LogWarning($"Mesh asset already exists at {filePath}. Not overwriting. Using existing mesh.");
             meshFilter.mesh = AssetDatabase.LoadAssetAtPath<Mesh>(filePath);
             meshGeneratedAndSaved = true;
        }

#else 
        Debug.Log("Running in build mode or outside editor. Mesh generated in memory.");
        meshGeneratedAndSaved = true; // Mark as generated for this session
#endif
    }


    void AssignDefaultMaterialIfNeeded()
    {
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer.sharedMaterial == null)
        {

            Material defaultLit = AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.render-pipelines.universal/Runtime/Materials/Lit.mat"); // Path might change!

            if (defaultLit != null)
            {
                 meshRenderer.sharedMaterial = defaultLit;

            }
            else
            {
                Debug.LogWarning("Could not find default URP Lit material at expected path. Creating simple Unlit material.");
                 Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit");
                 if (unlitShader != null) {
                    meshRenderer.sharedMaterial = new Material(unlitShader);
                 } else {
                    Debug.LogError("Cannot find URP Unlit shader!");
                 }
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position + transform.TransformVector(Vector3.zero), new Vector3(width, 0.01f, length)); // Use transform.position
    }
}