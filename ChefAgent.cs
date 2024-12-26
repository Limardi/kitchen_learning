//using System;
//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using Unity.MLAgents;
//using Unity.MLAgents.Actuators;
//using Unity.MLAgents.Sensors;
//using Unity.MLAgents.Policies;

//public class Player : Agent, IKitchenObjectParent
//{


//    public static Player Instance { get; private set; }



//    public event EventHandler OnPickedSomething;
//    public event EventHandler<OnSelectedCounterChangedEventArgs> OnSelectedCounterChanged;

//    public class OnSelectedCounterChangedEventArgs : EventArgs
//    {
//        public BaseCounter selectedCounter;
//    }


//    [SerializeField] private float moveSpeed = 7f;
//    [SerializeField] private GameInput gameInput;
//    [SerializeField] private LayerMask countersLayerMask;
//    [SerializeField] private Transform kitchenObjectHoldPoint;


//    private bool isWalking;
//    private Vector3 lastInteractDir;
//    private BaseCounter selectedCounter;
//    private KitchenObject kitchenObject;

//    //new


//    public override void Initialize()
//    {
//        if (Instance != null)
//        {
//            Debug.LogError("There is more than one Player instance");
//        }
//        Instance = this;
//    }

//    public override void OnEpisodeBegin()
//    {
//        isWalking = false;
//        lastInteractDir = Vector3.forward;
//        // Reset the player's position and any task-specific parameters
//        transform.position = Vector3.zero; // Reset position or set to a start location
//        selectedCounter = null;
//        kitchenObject = null;
//    }

//    public override void OnActionReceived(ActionBuffers actionBuffers)
//    {
//        // Actions: [0] = forward/backward movement, [1] = turn left/right, [2] = interact
//        float moveAmount = actionBuffers.DiscreteActions[0]; // 0: no move, 1: move forward
//        float turnAmount = 0f;
//        if (actionBuffers.DiscreteActions[1] == 1) turnAmount = -1f; // Turn left
//        else if (actionBuffers.DiscreteActions[1] == 2) turnAmount = 1f; // Turn right
//        bool interactAction = actionBuffers.DiscreteActions[2] == 1;

//        // Movement
//        Vector3 moveDir = transform.forward * moveAmount * moveSpeed * Time.deltaTime;
//        transform.Rotate(0, turnAmount * moveSpeed * Time.deltaTime, 0);

//        // Interaction
//        if (interactAction)
//        {
//            if (selectedCounter != null)
//            {
//                selectedCounter.Interact(this);
//                AddReward(0.1f); // Reward for interacting
//            }
//            else
//            {
//                AddReward(-0.1f); // Penalize if interaction is unsuccessful
//            }
//        }

//        // Tiny step penalty to encourage efficiency
//        AddReward(-0.01f);
//        //Debug.Log($"Reward added. Current cumulative reward: {GetCumulativeReward()}");
//    }

//    private void Start()
//    {
//        var behaviorType = GetComponent<BehaviorParameters>().BehaviorType;

//        if (behaviorType == BehaviorType.HeuristicOnly || behaviorType == BehaviorType.Default)
//        {
//            gameInput.OnInteractAction += GameInput_OnInteractAction;
//            gameInput.OnInteractAlternateAction += GameInput_OnInteractAlternateAction;
//        }
//    }

//    public override void Heuristic(in ActionBuffers actionsOut)
//    {
//        var discreteActions = actionsOut.DiscreteActions;

//        Vector2 inputVector = gameInput.GetMovementVectorNormalized();
//        discreteActions[0] = inputVector.y > 0 ? 1 : 0; // Forward movement
//        discreteActions[1] = inputVector.x < 0 ? 1 : (inputVector.x > 0 ? 2 : 0); // Turn left or right
//        discreteActions[2] = Input.GetKey(KeyCode.Space) ? 1 : 0; // Interact
//    }

//    public override void CollectObservations(VectorSensor sensor)
//    {
//        // Add position of agent and selected counter
//        sensor.AddObservation(transform.localPosition); // Agent position (3 values)
//        sensor.AddObservation(selectedCounter != null ? 1 : 0); // If a counter is selected (1 value)

//        // Add normalized direction to the last interactable counter
//        if (selectedCounter != null)
//        {
//            Vector3 directionToCounter = (selectedCounter.transform.position - transform.position).normalized;
//            sensor.AddObservation(directionToCounter); // Direction to counter (3 values)
//        }
//        else
//        {
//            sensor.AddObservation(Vector3.zero); // No direction
//        }

//        // Add whether the agent is holding an object
//        sensor.AddObservation(HasKitchenObject() ? 1 : 0); // Is holding an object (1 value)

//        // Total: 3 + 1 + 3 + 1 = 8 values
//    }

//    private void Update()
//    {
//        var behaviorType = GetComponent<BehaviorParameters>().BehaviorType;

//        if (behaviorType == BehaviorType.HeuristicOnly || behaviorType == BehaviorType.Default)
//        {
//            Vector2 inputVector = gameInput.GetMovementVectorNormalized();
//            Vector3 moveDir = new Vector3(inputVector.x, 0f, inputVector.y);
//            float moveDistance = moveSpeed * Time.deltaTime;

//            HandleMovement(moveDir, moveDistance);
//            HandleInteractions();
//        }

//    }

//    private void HandleMovement(Vector3 moveDir, float moveDistance)
//    {
//        float playerRadius = .7f;
//        float playerHeight = 2f;
//        bool canMove = !Physics.CapsuleCast(transform.position, transform.position + Vector3.up * playerHeight, playerRadius, moveDir, moveDistance);

//        if (!canMove)
//        {
//            Vector3 moveDirX = new Vector3(moveDir.x, 0, 0).normalized;
//            canMove = (moveDir.x < -.5f || moveDir.x > +.5f) && !Physics.CapsuleCast(transform.position, transform.position + Vector3.up * playerHeight, playerRadius, moveDirX, moveDistance);

//            if (canMove) moveDir = moveDirX;
//            else
//            {
//                Vector3 moveDirZ = new Vector3(0, 0, moveDir.z).normalized;
//                canMove = (moveDir.z < -.5f || moveDir.z > +.5f) && !Physics.CapsuleCast(transform.position, transform.position + Vector3.up * playerHeight, playerRadius, moveDirZ, moveDistance);
//                if (canMove) moveDir = moveDirZ;
//            }
//        }

//        if (canMove)
//        {
//            transform.position += moveDir * moveDistance;
//        }

//        isWalking = moveDir != Vector3.zero;

//        float rotateSpeed = 10f;
//        transform.forward = Vector3.Slerp(transform.forward, moveDir, Time.deltaTime * rotateSpeed);
//    }

//    //end new

//    private void GameInput_OnInteractAlternateAction(object sender, EventArgs e)  // interact button pressed (F)
//    {
//        if (!KitchenGameManager.Instance.IsGamePlaying()) return;

//        if (selectedCounter != null)
//        {
//            selectedCounter.InteractAlternate(this);
//        }
//    }

//    private void GameInput_OnInteractAction(object sender, System.EventArgs e) // interact button pressed (E)
//    {
//        if (!KitchenGameManager.Instance.IsGamePlaying()) return;

//        if (selectedCounter != null) // interact with what counter
//        {
//            //print(this.selectedCounter.GetType().Name);
//            //if (selectedCounter is ContainerCounter containerCounter) // Safe cast to ContainerCounter
//            //{
//            //    // Access and print the kitchen object name
//            //    Debug.Log($"Kitchen Object Name: {containerCounter.kitchenObjectSO.name}"); // print the object for container counter
//            //}
//            selectedCounter.Interact(this);
//        }
//    }


//    /* old start*/
//    //private void Start()
//    //{
//    //    gameInput.OnInteractAction += GameInput_OnInteractAction;
//    //    gameInput.OnInteractAlternateAction += GameInput_OnInteractAlternateAction;
//    //}

//    // old update
//    //private void Update() {
//    //    HandleMovement();
//    //    HandleInteractions();
//    //}

//    public bool IsWalking()
//    {
//        return isWalking;
//    }

//    private void HandleInteractions()
//    {
//        Vector2 inputVector = gameInput.GetMovementVectorNormalized();

//        Vector3 moveDir = new Vector3(inputVector.x, 0f, inputVector.y);

//        if (moveDir != Vector3.zero)
//        {
//            lastInteractDir = moveDir;
//        }

//        float interactDistance = 2f;
//        if (Physics.Raycast(transform.position, lastInteractDir, out RaycastHit raycastHit, interactDistance, countersLayerMask))
//        { //detect counter
//            if (raycastHit.transform.TryGetComponent(out BaseCounter baseCounter))
//            {
//                // Has ClearCounter
//                if (baseCounter != selectedCounter)
//                {
//                    SetSelectedCounter(baseCounter); // change counter
//                }
//            }
//            else
//            {
//                SetSelectedCounter(null);

//            }
//        }
//        else
//        {
//            SetSelectedCounter(null);
//        }
//    }

//    // old handle movement
//    //private void HandleMovement() {
//    //    Vector2 inputVector = gameInput.GetMovementVectorNormalized();

//    //    Vector3 moveDir = new Vector3(inputVector.x, 0f, inputVector.y);

//    //    float moveDistance = moveSpeed * Time.deltaTime;
//    //    float playerRadius = .7f;
//    //    float playerHeight = 2f;
//    //    bool canMove = !Physics.CapsuleCast(transform.position, transform.position + Vector3.up * playerHeight, playerRadius, moveDir, moveDistance);

//    //    if (!canMove) {
//    //        // Cannot move towards moveDir

//    //        // Attempt only X movement
//    //        Vector3 moveDirX = new Vector3(moveDir.x, 0, 0).normalized;
//    //        canMove = (moveDir.x < -.5f || moveDir.x > +.5f) && !Physics.CapsuleCast(transform.position, transform.position + Vector3.up * playerHeight, playerRadius, moveDirX, moveDistance);

//    //        if (canMove) {
//    //            // Can move only on the X
//    //            moveDir = moveDirX;
//    //        } else {
//    //            // Cannot move only on the X

//    //            // Attempt only Z movement
//    //            Vector3 moveDirZ = new Vector3(0, 0, moveDir.z).normalized;
//    //            canMove = (moveDir.z < -.5f || moveDir.z > +.5f) && !Physics.CapsuleCast(transform.position, transform.position + Vector3.up * playerHeight, playerRadius, moveDirZ, moveDistance);

//    //            if (canMove) {
//    //                // Can move only on the Z
//    //                moveDir = moveDirZ;
//    //            } else {
//    //                // Cannot move in any direction
//    //            }
//    //        }
//    //    }

//    //    if (canMove) {
//    //        transform.position += moveDir * moveDistance;
//    //    }

//    //    isWalking = moveDir != Vector3.zero;

//    //    float rotateSpeed = 10f;
//    //    transform.forward = Vector3.Slerp(transform.forward, moveDir, Time.deltaTime * rotateSpeed);
//    //}

//    private void SetSelectedCounter(BaseCounter selectedCounter)
//    {
//        this.selectedCounter = selectedCounter;

//        OnSelectedCounterChanged?.Invoke(this, new OnSelectedCounterChangedEventArgs
//        {
//            selectedCounter = selectedCounter
//        });
//    }

//    public Transform GetKitchenObjectFollowTransform()
//    { //transform player when input
//        return kitchenObjectHoldPoint;
//    }

//    public void SetKitchenObject(KitchenObject kitchenObject)
//    { // pickup object (pickup something) //check what object to pickup
//        this.kitchenObject = kitchenObject;

//        print(this.kitchenObject.kitchenObjectSO.objectName);

//        if (this.kitchenObject.kitchenObjectSO.objectName == "Plate")
//        {
//            int j = 1;
//            this.kitchenObject.TryGetPlate(out PlateKitchenObject plateKitchenObject);
//            foreach (var i in plateKitchenObject.kitchenObjectSOList)
//            {
//                print($"{j}. {i.objectName}");
//                j++;
//            }
//        }

//        AddReward(1.0f);
//        if (kitchenObject != null)
//        {
//            OnPickedSomething?.Invoke(this, EventArgs.Empty);
//        }
//    }

//    public KitchenObject GetKitchenObject()
//    {
//        return kitchenObject;
//    }

//    public void ClearKitchenObject()
//    {
//        kitchenObject = null;
//    }

//    public bool HasKitchenObject()
//    {
//        return kitchenObject != null;
//    }

//}



//lio

//using System;
//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using Unity.MLAgents;
//using Unity.MLAgents.Actuators;
//using Unity.MLAgents.Sensors;

//public class Player : Agent, IKitchenObjectParent
//{
//    public event EventHandler OnPickedSomething;
//    public event EventHandler<OnSelectedCounterChangedEventArgs> OnSelectedCounterChanged;
//    public class OnSelectedCounterChangedEventArgs : EventArgs
//    {
//        public BaseCounter selectedCounter;
//    }
//    public static Player Instance { get; private set; }


//    [SerializeField] private float moveSpeed = 7f;
//    [SerializeField] private LayerMask countersLayerMask;
//    [SerializeField] private GameInput gameInput;
//    [SerializeField] private Transform kitchenObjectHoldPoint;

//    private bool isWalking;

//    private Vector3 lastInteractDir;
//    private BaseCounter selectedCounter;
//    private KitchenObject kitchenObject;
//    private Vector3 initialPosition;

//    // RL-Specific Variables
//    private float reward;

//    private void Awake()
//    {
//        Instance = this;
//        Debug.Log("Player.Instance initialized in Awake.");
//    }
//    public override void OnEpisodeBegin()
//    {
//        ResetEnvironment();
//    }
//    // public override void CollectObservations(VectorSensor sensor)
//    // {
//    //             // Example: Observing the positions of specific counters
//    //     Vector3 cheeseCounterPosition = cheeseCounter.transform.position;
//    //     Vector3 cabbageCounterPosition = cabbageCounter.transform.position;
//    //     Vector3 plateCounterPosition = plateCounter.transform.position;

//    //     // Add positions relative to the agent's position
//    //     sensor.AddObservation(cheeseCounterPosition - transform.position); // 3 floats
//    //     sensor.AddObservation(cabbageCounterPosition - transform.position); // 3 floats
//    //     sensor.AddObservation(plateCounterPosition - transform.position); // 3 floats

//    //     // Example: Adding states (e.g., how many items are on each counter)
//    //     sensor.AddObservation(cheeseCounter.GetItemCount()); // 1 float
//    //     sensor.AddObservation(cabbageCounter.GetItemCount()); // 1 float
//    //     sensor.AddObservation(plateCounter.GetItemCount()); // 1 float

//    //     // Example: Adding the agent's own state
//    //     sensor.AddObservation(transform.position); // Agent's position (3 floats)
//    //     sensor.AddObservation(transform.forward); // Agent's forward direction (3 floats)
//    // }
//    private void Update()
//    {
//        // Step the RL Environment
//        gameInput.OnInteractAction += GameInput_OnInteractAction;
//        gameInput.OnInteractAlternateAction += GameInput_OnInteractAlternateAction;
//        RLStep();
//    }
//    private void RLStep()
//    {
//        // Observe state
//        Vector3 currentPosition = transform.position;
//        Vector3 targetDirection = lastInteractDir;

//        // Choose an action (replace this with RL policy)
//        int action = GetRandomAction();
//        if (KitchenGameManager.Instance.IsGamePlaying())
//        {
//            //Debug.Log("dkwhufjwhdlwjdliwjdilawjdlkwfnlkwnfklwnflakhdwalk;fhwlkfhalw;kfhawfihawofihwaildawhdiwa");
//        }

//        // Perform the action
//        PerformAction(action);


//        // Calculate reward
//        CalculateReward();


//        // Reset environment if necessary
//        if (ResetCondition())
//        {
//            ResetEnvironment();
//        }
//    }

//    private int GetRandomAction()
//    {
//        // Replace this with an RL-trained policy
//        return UnityEngine.Random.Range(0, 6); // Example: 6 discrete actions
//    }

//    private void PerformAction(int action)
//    {
//        Vector2 inputVector = Vector2.zero;

//        // Map actions to movement inputs or interactions
//        switch (action)
//        {
//            case 0:
//                inputVector = new Vector2(0, 1); // Forward
//                                                 //Debug.Log("Move Forward");
//                break;
//            case 1:
//                inputVector = new Vector2(0, -1); // Backward
//                                                  //Debug.Log("Move Backward");
//                break;
//            case 2:
//                inputVector = new Vector2(-1, 0); // Left
//                                                  //Debug.Log("Move Left");
//                break;
//            case 3:
//                inputVector = new Vector2(1, 0); // Right
//                                                 //Debug.Log("Move Right");
//                break;
//            case 4:
//                Interact();
//                //gameInput.OnInteractAction += GameInput_OnInteractAction;
//                //Debug.Log("Interact");
//                break;
//            case 5:
//                AlternateInteract();
//                // gameInput.OnInteractAtlernateAction += GameInput_OnInteractAlternateAction;
//                //Debug.Log("Alternate Interact");
//                break;
//        }

//        // Call HandleMovement with the derived input vector
//        HandleMovement(inputVector);
//        // Handle interactions based on current position and direction
//        HandleInteractions(inputVector);
//    }
//    private void HandleInteractions(Vector2 inputVector)
//    {
//        Vector3 moveDir = new Vector3(inputVector.x, 0f, inputVector.y);

//        if (moveDir != Vector3.zero)
//        {
//            lastInteractDir = moveDir; // Update the last interaction direction
//        }

//        float interactDistance = 2f;
//        if (Physics.Raycast(transform.position, lastInteractDir, out RaycastHit raycastHit, interactDistance, countersLayerMask))
//        {
//            if (raycastHit.transform.TryGetComponent(out BaseCounter baseCounter))
//            {
//                // Has a ClearCounter
//                if (baseCounter != selectedCounter)
//                {

//                    SetSelectedCounter(baseCounter);
//                }
//            }
//            else
//            {
//                SetSelectedCounter(null);
//            }
//        }
//        else
//        {
//            SetSelectedCounter(null);
//        }
//    }

//    private void HandleMovement(Vector2 inputVector)
//    {
//        // Convert inputVector to 3D direction
//        Vector3 moveDir = new Vector3(inputVector.x, 0f, inputVector.y);

//        float moveDistance = moveSpeed * Time.deltaTime;
//        float playerRadius = 0.7f;
//        float playerHeight = 2f;

//        bool canMove = !Physics.CapsuleCast(transform.position, transform.position + Vector3.up * playerHeight, playerRadius, moveDir, moveDistance);

//        if (!canMove)
//        {
//            // Handle restricted movement in X and Z separately
//            Vector3 moveDirX = new Vector3(moveDir.x, 0, 0).normalized;
//            canMove = (Mathf.Abs(moveDir.x) > 0.5f) && !Physics.CapsuleCast(transform.position, transform.position + Vector3.up * playerHeight, playerRadius, moveDirX, moveDistance);

//            if (canMove)
//            {
//                moveDir = moveDirX;
//            }
//            else
//            {
//                Vector3 moveDirZ = new Vector3(0, 0, moveDir.z).normalized;
//                canMove = (Mathf.Abs(moveDir.z) > 0.5f) && !Physics.CapsuleCast(transform.position, transform.position + Vector3.up * playerHeight, playerRadius, moveDirZ, moveDistance);

//                if (canMove)
//                {
//                    moveDir = moveDirZ;
//                }
//            }
//        }

//        if (canMove)
//        {
//            transform.position += moveDir * moveDistance;
//        }

//        isWalking = moveDir != Vector3.zero;

//        // Rotate player to face the direction of movement
//        float rotateSpeed = 10f;
//        if (isWalking)
//        {
//            transform.forward = Vector3.Slerp(transform.forward, moveDir, Time.deltaTime * rotateSpeed);
//        }
//    }


//    private void Interact()
//    {
//        if (selectedCounter != null)
//        {
//            selectedCounter.Interact(this);
//        }
//    }

//    private void AlternateInteract()
//    {
//        if (selectedCounter != null)
//        {
//            selectedCounter.InteractAlternate(this);
//        }
//    }
//    public bool IsWalking()
//    {
//        return isWalking;
//    }


//    private void CalculateReward()
//    {
//        // Example reward system:
//        if (HasKitchenObject())
//        {
//        }
//        if (selectedCounter != null)
//        {
//        }
//    }

//    private float resetTimer = 0f; // Timer to track the time since last reset
//    private float resetCooldown = 5f; // Time before resetting position to (0, 0, 0)

//    private bool ResetCondition()
//    {
//        // Increment the resetTimer by the time passed since the last frame
//        resetTimer += Time.deltaTime;

//        // If 5 seconds have passed, reset position to (0, 0, 0)
//        if (resetTimer >= resetCooldown)
//        {
//            Debug.Log("Cumulative Reward: " + GetCumulativeReward());
//            EndEpisode();
//            resetTimer = 0f;
//            return true; // Reset condition met
//        }

//        return false; // Reset condition not met yet
//    }


//    private void ResetEnvironment()
//    {
//        transform.position = initialPosition;
//        if (this.HasKitchenObject())
//        {
//            this.kitchenObject.DestroySelf();
//        }
//        ResetCounters();

//        // Clear the kitchen object the player might be holding
//        ClearKitchenObject();

//        PlatesCounter[] platesCounters = FindObjectsOfType<PlatesCounter>();
//        foreach (PlatesCounter platesCounter in platesCounters)
//        {
//            if (platesCounter.platesSpawnedAmount > 0)
//                platesCounter.ResetPlatesCounter();
//        }


//        DeliveryManager.Instance.ResetWaitingRecipes();

//        // Reset the selected counter (no counter selected after reset)
//        selectedCounter = null;

//        // Reset reward system
//        SetReward(0f);
//        reward = 0f;

//        // Optionally, reset the resetTimer if necessary (optional, depends on the logic you want)
//        resetTimer = 0f;
//    }

//    // IKitchenObjectParent Interface Implementation
//    public Transform GetKitchenObjectFollowTransform()
//    {
//        return kitchenObjectHoldPoint;
//    }

//    public void SetKitchenObject(KitchenObject kitchenObject)
//    {
//        this.kitchenObject = kitchenObject;

//        if (kitchenObject != null)
//        {
//            AddReward(10f); // Reward for holding a kitchen object
//            OnPickedSomething?.Invoke(this, EventArgs.Empty);
//        }
//    }

//    public KitchenObject GetKitchenObject()
//    {
//        return kitchenObject;
//    }

//    public void ClearKitchenObject()
//    {
//        kitchenObject = null;
//    }

//    public bool HasKitchenObject()
//    {
//        return kitchenObject != null;
//    }

//    private void SetSelectedCounter(BaseCounter baseCounter)
//    {
//        selectedCounter = baseCounter;

//        OnSelectedCounterChanged?.Invoke(this, new OnSelectedCounterChangedEventArgs
//        {
//            selectedCounter = selectedCounter
//        });
//    }
//    private void GameInput_OnInteractAlternateAction(object sender, EventArgs e)
//    {
//        if (!KitchenGameManager.Instance.IsGamePlaying()) return;

//        if (selectedCounter != null)
//        {

//            selectedCounter.InteractAlternate(this);
//        }
//    }

//    private void GameInput_OnInteractAction(object sender, System.EventArgs e)
//    {
//        if (!KitchenGameManager.Instance.IsGamePlaying()) return;

//        if (selectedCounter != null)
//        {

//            selectedCounter.Interact(this);
//        }
//    }
//    private void ResetCounters()
//    {
//        // Find all objects of type BaseCounter in the scene
//        BaseCounter[] allCounters = FindObjectsOfType<BaseCounter>();

//        // Loop through each counter and call ClearKitchenObject on it
//        foreach (BaseCounter counter in allCounters)
//        {
//            if (counter.HasKitchenObject())
//            {
//                counter.GetKitchenObject().DestroySelf();
//            }
//        }
//    }

//}

