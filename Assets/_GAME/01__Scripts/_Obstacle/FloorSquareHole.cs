using UnityEngine;
using DG.Tweening;

public class FloorSquareHole : MonoBehaviour
{
    [Header("Hole Properties")]
    public ObstacleColor holeColor = ObstacleColor.Default;
    
    [Header("Movement")]
    public float moveDuration = 0.5f;
    public float riseHeight = 0.5f;
    public float riseDuration = 0.5f;
    public float fallDepth = -5f;
    public float rotationDuration = 1f;

    [Header("Scaling")]
    public Vector3 punchScale = new Vector3(1.1f, 1.1f, 1.1f);
    public float punchDuration = 0.5f;
    public int punchVibrato = 5;
    public float targetScale = 0.1f;

    [Header("Respawn")]
    public float respawnScaleDuration = 0.5f;
    public Ease respawnEase = Ease.OutBack;
    
    public Collider holeTrigger;
    
    private Vector3 startScale;
    private GameObject currentObject;
    private PlayerObstacleController playerObstacleController;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Obstacle"))
        {
            Obstacle obstacleComponent = other.GetComponent<Obstacle>();
            
            if (IsCorrectObstacle(obstacleComponent))
            {
                HandleCorrectObstacle(other);
            }
            else
            {
                HandleWrongObstacle(other);
            }
        }
    }

    private bool IsCorrectObstacle(Obstacle obstacle)
    {
        if (obstacle == null) return false;
        return obstacle.obstacleColor == holeColor || obstacle.obstacleColor == ObstacleColor.Universal;
    }

    private void HandleCorrectObstacle(Collider other)
    {
        // Disable collider and clean up
        other.GetComponent<Collider>().enabled = false;
        
        if (other.GetComponent<Obstacle>().playerController != null)
        {
            other.GetComponent<Obstacle>().playerController.isPushing = false;
            other.GetComponent<Obstacle>().playerController.transform.GetComponent<PlayerMovement>().IsPushing = false;
            other.GetComponent<Obstacle>().playerController.transform.GetComponent<PlayerMovement>().CanMove = false;
            playerObstacleController = other.GetComponent<Obstacle>().playerController.transform.GetComponent<PlayerObstacleController>();
            other.GetComponent<Obstacle>().enabled = false;
        }
        
        currentObject = other.gameObject;
        AnimateObstacleFallingIn(true); // Destroy after animation
    }

    private void HandleWrongObstacle(Collider other)
    {
        currentObject = other.gameObject;
        Obstacle obstacleComp = currentObject.GetComponent<Obstacle>();
        
        if (obstacleComp != null)
        {
            // Use the initialPosition stored on the obstacle
            Vector3 initialPos = obstacleComp.initialPosition;
            AnimateObstacleFallingIn(false, initialPos);
            
            Debug.Log($"❌ Wrong obstacle {currentObject.name} (Color: {obstacleComp.obstacleColor}) rejected by {gameObject.name} (Color: {holeColor})");
        }
        else
        {
            Debug.LogWarning($"No Obstacle component found on {currentObject.name}. Destroying it instead.");
            Destroy(currentObject);
        }
    }

    private void AnimateObstacleFallingIn(bool destroyAfterAnimation, Vector3? respawnPosition = null)
    {
        Vector3 holeCenter = holeTrigger.bounds.center;
        startScale = currentObject.transform.localScale;
        
        Sequence sequence = DOTween.Sequence();
        
        GameObject obstacleRef = currentObject;
        Obstacle obstacleComponent = obstacleRef.GetComponent<Obstacle>();
        
        // 1. Move towards the hole center
        sequence.Append(obstacleRef.transform.DOMove(holeCenter, moveDuration));
        
        // 2. Stop pulling animation
        sequence.AppendCallback(() =>
        {
            if (playerObstacleController != null)
            {
                playerObstacleController.StopPull();
                playerObstacleController = null;
            }
        });
        
        // 3. Knock up slightly
        Vector3 raisedPosition = holeCenter + Vector3.up * riseHeight;
        sequence.Append(obstacleRef.transform.DOMove(raisedPosition, riseDuration));
        
        // 4. Punch Scale
        sequence.Append(obstacleRef.transform.DOPunchScale(punchScale, punchDuration, punchVibrato));
        
        if (!destroyAfterAnimation && respawnPosition.HasValue)
        {
            // WRONG OBSTACLE: Fall, shrink, then respawn
            Vector3 targetPosition = new Vector3(holeCenter.x, fallDepth, holeCenter.z);
            
            // Fall and shrink
            sequence.Append(obstacleRef.transform.DOMove(targetPosition, rotationDuration).SetEase(Ease.Linear));
            sequence.Join(obstacleRef.transform.DOScale(Vector3.one * targetScale, rotationDuration).SetEase(Ease.Linear));
            
            // Respawn sequence
            sequence.AppendCallback(() =>
            {
                // Hide and teleport to initial position
                obstacleRef.SetActive(false);
                obstacleRef.transform.position = respawnPosition.Value;
                obstacleRef.transform.localScale = Vector3.one * targetScale;
                obstacleRef.SetActive(true);
                
                // Reset physics
                Rigidbody rb = obstacleRef.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
                
                // Re-enable components
                Collider col = obstacleRef.GetComponent<Collider>();
                if (col != null) col.enabled = true;
                
                if (obstacleComponent != null) obstacleComponent.enabled = true;
            });
            
            // Scale up with bounce effect
            sequence.Append(obstacleRef.transform.DOScale(startScale, respawnScaleDuration).SetEase(respawnEase));
            sequence.Append(obstacleRef.transform.DOPunchScale(new Vector3(0.05f, 0.05f, 0.05f), 0.2f, 1, 0.5f));
            
            Debug.Log($"🔄 Wrong obstacle {obstacleRef.name} respawned at {respawnPosition.Value}");
        }
        else
        {
            // CORRECT OBSTACLE: Fall, shrink, and destroy
            Vector3 targetPosition = new Vector3(holeCenter.x, fallDepth, holeCenter.z);
            sequence.Append(obstacleRef.transform.DOMove(targetPosition, rotationDuration).SetEase(Ease.Linear));
            sequence.Join(obstacleRef.transform.DOScale(Vector3.one * targetScale, rotationDuration).SetEase(Ease.Linear));
            
            sequence.AppendCallback(() =>
            {
                if (obstacleComponent != null)
                {
                    Debug.Log($"✅ Correct obstacle {obstacleRef.name} (Color: {obstacleComponent.obstacleColor}) destroyed by {gameObject.name} (Color: {holeColor})");
                    obstacleComponent.ParticleDestroy();
                }
                else
                {
                    Destroy(obstacleRef);
                }
            });
        }
        
        sequence.Play();
    }
}