using UnityEngine;

[CreateAssetMenu(fileName = "NewObstacleData", menuName = "Obstacles/Obstacle Data")]
public class ObstacleData : ScriptableObject
{
    public ObstacleType obstacleType;
    public ObstacleColor obstacleColor;
    // You might also move destructionParticleSystem here if it's data-driven
    public GameObject destructionParticlePrefab;
    // Other properties like health, specific behaviors, etc.
}