using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class PlayerControllerAI : MonoBehaviour
{
    public PlayerController playerController;
    public List<Task> taskQueue = new List<Task>(); // Queue of tasks for the AI to perform
    public Task currentTask; // The current task being executed
    public PlayerObstacleController playerObstacleController;
    public PlayerAttack playerAttack;
    private bool isTaskActive = false; // Flag to indicate if a task is currently being executed
    private bool isExecuting = false; // Flag to check if a task is currently being executed

    // Enum to define different types of tasks
    public enum TaskType
    {
        MoveToPosition,
        MoveToTile,
        PullObstacle,
        PushObstacle,
        Jump,
        Rotate,
        HitObstacle,
        None
        // Add more task types as needed
    }

    // Class to represent a task
    [System.Serializable]
    public class Task
    {
        public TaskType type;
        public Vector3 targetPosition;
        public float targetRotation;
        public float jumpDuration;
        public Tile targetTile;
        public float jumpDelay;
        public Obstacle obstacleToPull; // Reference to the obstacle to pull (if applicable)
        public Obstacle obstacleToPush; // Reference to the obstacle to push (if applicable)
        public float pullDuration;
        public float pullDelay;

        public Task(
            TaskType _type,
            Vector3 _targetPosition,
            float _targetRotation,
            Obstacle _obstacleToPull = null,
            Obstacle _obstacleToPush = null,
            float _jumpDuration = 0,
            float _jumpDelay = 0,
            float _pullDuration = 0,
            float _pullDelay = 0

        )
        {
            type = _type;
            targetPosition = _targetPosition;
            targetRotation = _targetRotation;
            obstacleToPull = _obstacleToPull;
            obstacleToPush = _obstacleToPush;
            jumpDuration = _jumpDuration;
            jumpDelay = _jumpDelay;
            pullDuration = _pullDuration;
            pullDelay = _pullDelay;
        }
    }
    public bool delayedAI;
    public float aiDelay;
    public IEnumerator DelayAI(float delay)
    {
        yield return new WaitForSeconds(delay);
        delayedAI = true;
        ExecuteNextTask();
    }
    private void Start()
    {
        // Get reference to the PlayerController component
        playerController = GetComponent<PlayerController>();
        playerObstacleController = GetComponent<PlayerObstacleController>();
        playerAttack = GetComponent<PlayerAttack>();

        StartCoroutine(DelayAI(aiDelay));
        // Start executing tasks

    }

    private void FixedUpdate()
    {
        if (!delayedAI) return;
        // Execute the current task
        if (playerObstacleController.pushObstacle != null)
        {

            playerObstacleController.HandlePush();
            if (!playerController.isPushing && playerObstacleController.previousPushObstacle != null)
            {
                playerObstacleController.previousPushObstacle.ResetObstacle();
                playerObstacleController.previousPushObstacle = null;
            }
        }
        if (isTaskActive && !isExecuting)
        {
            ExecuteTask(currentTask);
        }
    }

    // Method to add a task to the task queue
    public void AddTask(Task task)
    {
        // Debug.Log("Adding task: " + task.type + " with target position: " + task.targetPosition);
        taskQueue.Add(task);
        // If no task is currently being executed, start executing the new task
        if (!isTaskActive)
        {
            ExecuteNextTask();
        }
    }

    // Method to execute the next task in the task queue
    private void ExecuteNextTask()
    {
        if (taskQueue.Count > 0)
        {
            currentTask = taskQueue[0];
            taskQueue.RemoveAt(0);
            isTaskActive = true;
            // Debug.Log("Executing next task: " + currentTask.type + " with target position: " + currentTask.targetPosition);
        }
        else
        {
            isTaskActive = false; // No more tasks to execute
            Debug.Log("No more tasks to execute.");
        }
    }

    // Method to execute a specific task
    private void ExecuteTask(Task task)
    {
        if (isExecuting) return;

        isExecuting = true;
        switch (task.type)
        {
            case TaskType.MoveToPosition:
                StartCoroutine(MoveToPosition(task.targetPosition));
                break;
            case TaskType.MoveToTile:
                StartCoroutine(MoveToTile(task.targetTile));
                break;
            case TaskType.PullObstacle:
                StartCoroutine(HandlePull(task.obstacleToPull, task.pullDuration, task.pullDelay));
                break;
            case TaskType.Jump:
                StartCoroutine(HandleJumpAndMove(task.jumpDuration, task.jumpDelay));
                break;
            case TaskType.HitObstacle:
                StartCoroutine(Hit());
                break;
            case TaskType.None:
                playerController._dir = Vector3.zero;
                Debug.Log("Doing nothing");
                isExecuting = false;
                ExecuteNextTask();
                break;
            case TaskType.Rotate:
                StartCoroutine(Rotate(task.targetRotation));
                break;
                // Add cases for other task types as needed
        }
    }

    public bool movingAlongX = false; // Flag to track if the AI is currently moving along the X axis
    public Vector3 targetPosition; // Target position for the AI to move to
    public bool isXDone, isZDone;

    private IEnumerator Hit()
    {
        playerAttack.Hit();
        yield return new WaitForSeconds(0.3f);
        isExecuting = false;
        ExecuteNextTask();
    }

    private IEnumerator MoveToPosition(Vector3 targetPosition)
    {
        isXDone = false;
        isZDone = false;
        movingAlongX = true;

        while (!isXDone || !isZDone)
        {
            Vector3 direction = Vector3.zero;
            float distanceToTarget = 0;

            if (movingAlongX)
            {
                distanceToTarget = Mathf.Abs(targetPosition.x - transform.position.x);
                if (distanceToTarget <= 0.03f)
                {
                    // Debug.Log("Finished moving along X");
                    isXDone = true;
                    movingAlongX = false;
                }
                else
                {
                    direction = new Vector3(Mathf.Sign(targetPosition.x - transform.position.x), 0, 0);
                }
            }

            if (!movingAlongX)
            {
                distanceToTarget = Mathf.Abs(targetPosition.z - transform.position.z);
                if (distanceToTarget <= 0.03f)
                {
                    // Debug.Log("Finished moving along Z");
                    isZDone = true;
                }
                else
                {
                    direction = new Vector3(0, 0, Mathf.Sign(targetPosition.z - transform.position.z));
                }
            }

            playerController._dir = direction;
            playerController.HandleWalking();
            yield return null;
        }

        playerController._dir = Vector3.zero;
        // Debug.Log("Movement done");
        isExecuting = false;
        ExecuteNextTask();
    }

    private IEnumerator MoveToTile(Tile targetTile)
    {
        Vector3 targetPosition = new Vector3(targetTile.transform.position.x, transform.position.y, targetTile.transform.position.z);
        yield return StartCoroutine(MoveToPosition(targetPosition));
    }

    private IEnumerator HandleJumpAndMove(float moveDur, float delay)
    {
        // Trigger the jump
        yield return new WaitForSeconds(delay);
        playerController.HandleJump();

        // Wait for the jump animation or physics to take effect
        yield return new WaitForSeconds(0.1f); // Adjust the duration as needed

        // Set the direction to move forward
        Vector3 forwardMove = transform.forward;
        forwardMove.y = 0; // Ensure the movement is horizontal
        forwardMove.Normalize();
        playerController._dir = forwardMove;

        // Move forward for a short duration to simulate a jump forward by 1 unit
        float moveDuration = moveDur; // Duration to move forward
        float elapsedTime = 0;

        while (elapsedTime < moveDuration)
        {
            playerController.HandleWalking();
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Stop moving forward
        playerController._dir = Vector3.zero;

        // Proceed to the next task
        // Debug.Log("Jump completed");
        yield return new WaitForSeconds(0.05f);
        isExecuting = false;
        ExecuteNextTask();
    }

    private IEnumerator Rotate(float degrees)
    {
        transform.DORotate(new Vector3(0, degrees, 0), 0.1f).Play();
        Debug.Log("Rotating");
        yield return new WaitForSeconds(0.1f);
        isExecuting = false;
        ExecuteNextTask();
    }

    private IEnumerator HandlePull(Obstacle obstacleToPull, float pullDuration, float pullDelay)
    {
        if (obstacleToPull == null)
        {
            // Debug.Log("No obstacle to pull.");
            isExecuting = false;
            ExecuteNextTask();
            yield break;
        }

        // Stop the player movement
        // playerController._dir = Vector3.zero;
        yield return new WaitForSeconds(pullDelay);
        // Start pulling the obstacle
        playerController._pullButtonHeld = true;
        playerController._pullButtonReleased = false;
        playerObstacleController.HandlePull();

        // Ensure the player moves backward while pulling
        // Vector3 pullDirection = -transform.forward;

        // Simulate the pulling process

        float elapsedTime = 0;

        while (elapsedTime < pullDuration)
        {
            // playerController._dir = pullDirection;

            playerObstacleController.HandlePull();
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Stop pulling the obstacle
        playerController._pullButtonHeld = false;
        playerController._pullButtonReleased = true;
        playerController._dir = Vector3.zero;

        isExecuting = false;
        ExecuteNextTask();
    }
}
