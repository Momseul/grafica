using UnityEngine;
using UnityEngine.InputSystem;

public class SphereSpawner : MonoBehaviour
{
    public GameObject spherePrefab;
    public float spawnAreaRadius = 5.0f; 

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.gKey.wasPressedThisFrame)
        {
            SpawnSphere();
        }
    }

    void SpawnSphere()
    {
        if (spherePrefab == null)
        {
            Debug.LogWarning("SpherePrefab is not assigned in SphereSpawner! Cannot spawn sphere.");
            return;
        }

        Vector2 randomCircleOffset = Random.insideUnitCircle * spawnAreaRadius;

        // Spawn position uses the spawner's transform directly for X, Y, Z
        // and then applies the random XZ offset.
        Vector3 spawnPosition = new Vector3(
            transform.position.x + randomCircleOffset.x,
            transform.position.y, // Use the spawner's Y position directly
            transform.position.z + randomCircleOffset.y
        );

        Instantiate(spherePrefab, spawnPosition, Quaternion.identity);
        Debug.Log($"Sphere spawned at {spawnPosition} (Spawner is at {transform.position}, random offset applied in XZ within radius {spawnAreaRadius})");
    }
}