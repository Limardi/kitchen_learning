using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Policies;

public class Player : Agent, IKitchenObjectParent {


    public static Player Instance { get; private set; }



    public event EventHandler OnPickedSomething;
    public event EventHandler<OnSelectedCounterChangedEventArgs> OnSelectedCounterChanged;

    public class OnSelectedCounterChangedEventArgs : EventArgs {
        public BaseCounter selectedCounter;
    }


    [SerializeField] private float moveSpeed = 7f;
    [SerializeField] private GameInput gameInput;
    [SerializeField] private LayerMask countersLayerMask;
    [SerializeField] private Transform kitchenObjectHoldPoint;


    public bool isWalking;
    private Vector3 lastInteractDir;
    public BaseCounter selectedCounter;
    public KitchenObject kitchenObject;
    private CuttingCounter cabbageCuttingCounter;
    private CuttingCounter tomatoCuttingCounter;
    [SerializeField] private ContainerCounter tomatoContainerCounter;
    [SerializeField] private ContainerCounter cabbageContainerCounter;
    [SerializeField] private PlatesCounter plateContainerCounter;
    [SerializeField] private CuttingCounter cuttingCounter;
    [SerializeField] private KitchenArea kitchenArea;
    [SerializeField] private KitchenGameManager kitchenGameManager;

    private BaseCounter cabbageSlicesCounter;
    private BaseCounter tomatoSlicesCounter;
    float previousDistanceToTarget = 0;

    //new
    public Vector3 initialPosition;

    public enum RecipeStep
    {
        None,
        PickCabbage,
        PlaceCabbageOnCounter,
        //PickCabbageFromCounter,
        SliceCabbage,
        PickCabbageSlices,
        PlaceCabbageSlicesOnCounter,
        PickTomato,
        PlaceTomatoOnCounter,
        //PickTomatoFromCounter,
        SliceTomato,
        PickTomatoSlices,
        PlaceTomatoSlicesOnCounter,
        PickPlate,
        AddTomatoSlicesToPlate,
        AddCabbageSlicesToPlate,
        SaladAssembled // Final step
    }


    public RecipeStep currentStep = RecipeStep.PickCabbage;
    //KitchenObject kitchenObject_temp;


    public override void Initialize()
    {
        //if (Instance != null)
        //{
        //    Debug.LogError("There is more than one Player instance");
        //}
        Instance = this;
    }

    public override void OnEpisodeBegin()
    {
        Debug.Log("Episode is beginning...");
        kitchenGameManager.gamePlayingTimer = 300f;
        lastInteractDir = Vector3.forward;
        // Reset the player's position and any task-specific parameters
        transform.position = Vector3.zero; // Reset position or set to a start location
        selectedCounter = null;
        kitchenObject = null;
        currentStep = RecipeStep.PickCabbage;
        //kitchenObject_temp = null;
        kitchenArea.ResetArea();

    }

    private float GetDistanceToCurrentTarget()
    {
        Vector3 targetPosition = Vector3.zero;

        switch (currentStep)
        {
            case RecipeStep.PickCabbage:
                targetPosition = (cabbageContainerCounter.transform.position).normalized;
                break;
            case RecipeStep.PlaceCabbageOnCounter:
                targetPosition = (cuttingCounter.transform.position).normalized;
                break;
            case RecipeStep.PickTomato:
                targetPosition = (tomatoContainerCounter.transform.position).normalized;
                break;
            case RecipeStep.PlaceTomatoOnCounter:
                targetPosition = (cuttingCounter.transform.position).normalized;
                break;
            case RecipeStep.PickPlate:
                targetPosition = (plateContainerCounter.transform.position).normalized;
                break;
            
                // Add other cases as needed
        }

        return Vector3.Distance(transform.position, targetPosition);
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        // Parse movement actions
        int moveXAction = actionBuffers.DiscreteActions[0]; // 0: left, 1: no move, 2: right
        int moveZAction = actionBuffers.DiscreteActions[1]; // 0: back, 1: no move, 2: forward
        bool interactAction = actionBuffers.DiscreteActions[2] == 1; // Interact
        bool interactAltAction = actionBuffers.DiscreteActions[3] == 1; // Alternate Interact

        // Map actions to movement values
        float moveX = 0f;
        if (moveXAction == 0)
        {
            moveX = -1f; // Left
        }
        else if (moveXAction == 2)
        {
            moveX = 1f; // Right
        }

        float moveZ = 0f;
        if (moveZAction == 0)
        {
            moveZ = -1f; // Backward
        }
        else if (moveZAction == 2) {
            moveZ = 1f;
        } // Forward

        // Determine movement direction
        Vector3 moveDir = new Vector3(moveX, 0f, moveZ).normalized;


        //if (currentStep == RecipeStep.PickTomato && tomatoContainerCounter != null)
        //{
        //    Vector3 directionToTomato = (tomatoContainerCounter.transform.position - transform.position).normalized;

        //    // Optionally adjust moveDir to favor movement towards the tomato container
        //    moveDir = Vector3.Lerp(moveDir, directionToTomato, 0.5f).normalized;
        //}

        //if (currentStep == RecipeStep.PickCabbage && cabbageContainerCounter != null)
        //{
        //    Vector3 directionToCabbage = (cabbageContainerCounter.transform.position - transform.position).normalized;

        //    // Optionally adjust moveDir to favor movement towards the tomato container
        //    moveDir = Vector3.Lerp(moveDir, directionToCabbage, 0.5f).normalized;
        //}

        //if (currentStep == RecipeStep.PickPlate && plateContainerCounter != null)
        //{
        //    Vector3 directionToPlate = (plateContainerCounter.transform.position - transform.position).normalized;

        //    // Optionally adjust moveDir to favor movement towards the tomato container
        //    moveDir = Vector3.Lerp(moveDir, directionToPlate, 0.5f).normalized;
        //}


        //float distanceToTarget = GetDistanceToCurrentTarget();
        

        // Provide small reward for reducing distance
        //if (distanceToTarget < previousDistanceToTarget)
        //{
        //    //AddReward(0.0001f); // Small positive reward
        //}
        //else
        //{
        //    AddReward(-0.001f); // Small penalty if moving away
        //}

        //previousDistanceToTarget = distanceToTarget;


        // Handle movement with collision detection
        float moveDistance = moveSpeed * Time.deltaTime;
        HandleMovement(moveDir, moveDistance);

        // Update last interact direction
        if (moveDir != Vector3.zero)
        {
            lastInteractDir = moveDir;
        }

        // Handle interactions
        HandleInteractions();

        // Interaction logic
        if (interactAction)
        {
            if (selectedCounter != null)
            {
                selectedCounter.Interact(this);
            }

        }

        if (interactAltAction)
        {
            if (selectedCounter != null)
            {
                selectedCounter.InteractAlternate(this);
            }

        }
        AddReward(-0.001f);

    }


    private void Start() {
        var behaviorType = GetComponent<BehaviorParameters>().BehaviorType;

        if (behaviorType == BehaviorType.HeuristicOnly || behaviorType == BehaviorType.Default)
        {
            gameInput.OnInteractAction += GameInput_OnInteractAction;
            gameInput.OnInteractAlternateAction += GameInput_OnInteractAlternateAction;
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActions = actionsOut.DiscreteActions;

        // Get input from the player or set to "no movement"
        Vector2 inputVector = Vector2.zero;
        if (gameInput != null)
        {
            inputVector = gameInput.GetMovementVectorNormalized();
        }

        // Map the input to discrete actions
        discreteActions[0] = inputVector.x < -0.5f ? 0 : (inputVector.x > 0.5f ? 2 : 1); // Left, No Move, Right
        discreteActions[1] = inputVector.y < -0.5f ? 0 : (inputVector.y > 0.5f ? 2 : 1); // Backward, No Move, Forward

        // Interact actions
        discreteActions[2] = IsInteractPressed() ? 1 : 0;       // Interact
        discreteActions[3] = IsInteractAltPressed() ? 1 : 0;    // Alternate Interact
    }

    public bool IsInteractPressed()
    {
        // Replace KeyCode.E with the key you use for interaction
        return Input.GetKeyDown(KeyCode.E);
    }

    public bool IsInteractAltPressed()
    {
        // Replace KeyCode.F with the key you use for alternate interaction
        return Input.GetKeyDown(KeyCode.F);
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // Add position of agent and selected counter
        sensor.AddObservation(transform.localPosition); // Agent position (3 values)
        sensor.AddObservation(selectedCounter != null ? 1 : 0); // If a counter is selected (1 value)

        // Add normalized direction to the last interactable counter
        if (selectedCounter != null)
        {
            Vector3 directionToCounter = (selectedCounter.transform.position - transform.position).normalized;
            sensor.AddObservation(directionToCounter); // Direction to counter (3 values)
        }
        else
        {
            sensor.AddObservation(Vector3.zero); // No direction
        }

        // Add whether the agent is holding an object
        sensor.AddObservation(HasKitchenObject() ? 1 : 0); // Is holding an object (1 value)

        sensor.AddObservation((int)currentStep);
        sensor.AddObservation(selectedCounter is CuttingCounter ? 1 : 0);

        if (tomatoContainerCounter != null)
        {
            Vector3 relativePosition = tomatoContainerCounter.transform.position - transform.position;
            sensor.AddObservation(relativePosition.x);
            sensor.AddObservation(relativePosition.z);
        }
        else
        {
            sensor.AddObservation(0f);
            sensor.AddObservation(0f);
        }

        if (cabbageContainerCounter != null)
        {
            Vector3 relativePosition = cabbageContainerCounter.transform.position - transform.position;
            sensor.AddObservation(relativePosition.x);
            sensor.AddObservation(relativePosition.z);
        }
        else
        {
            sensor.AddObservation(0f);
            sensor.AddObservation(0f);
        }

        if (plateContainerCounter != null)
        {
            Vector3 relativePosition = cabbageContainerCounter.transform.position - transform.position;
            sensor.AddObservation(relativePosition.x);
            sensor.AddObservation(relativePosition.z);
        }
        else
        {
            sensor.AddObservation(0f);
            sensor.AddObservation(0f);
        }

        if (selectedCounter is CuttingCounter cuttingCounter)
        {
            sensor.AddObservation(cuttingCounter.IsCuttingComplete() ? 1 : 0);
        }
        else
        {
            sensor.AddObservation(0);
        }

        if (cabbageSlicesCounter != null)
        {
            Vector3 relativePosition = cabbageSlicesCounter.transform.position - transform.position;
            sensor.AddObservation(relativePosition.x / 10f); // Normalize as necessary
            sensor.AddObservation(relativePosition.z / 10f);
        }
        else
        {
            sensor.AddObservation(0f);
            sensor.AddObservation(0f);
        }

        // Relative position to tomato slices counter
        if (tomatoSlicesCounter != null)
        {
            Vector3 relativePosition = tomatoSlicesCounter.transform.position - transform.position;
            sensor.AddObservation(relativePosition.x / 10f);
            sensor.AddObservation(relativePosition.z / 10f);
        }
        else
        {
            sensor.AddObservation(0f);
            sensor.AddObservation(0f);
        }

    }

    private void Update()
    {
        var behaviorType = GetComponent<BehaviorParameters>().BehaviorType;
      

        if (behaviorType == BehaviorType.HeuristicOnly || behaviorType == BehaviorType.Default)
        {
            Vector2 inputVector = gameInput.GetMovementVectorNormalized();
            Vector3 moveDir = new Vector3(inputVector.x, 0f, inputVector.y);
            float moveDistance = moveSpeed * Time.deltaTime;

            HandleMovement(moveDir, moveDistance);
            HandleInteractions();
            //PrintSelectedCounterInfo();
        }
        HandleRecipeSteps();
        //print(kitchenGameManager.gamePlayingTimer);

        //if (kitchenGameManager.gamePlayingTimer <= 0f)
        //{
        //    EndEpisode();
        //}


    }

    private void HandleRecipeSteps()
    {
        // Print current step for debugging
        Debug.Log("Current Step: " + currentStep);

        switch (currentStep)
        {
            case RecipeStep.PickCabbage:
                {
                    if (HasKitchenObject() && kitchenObject.kitchenObjectSO.objectName == "Cabbage")
                    {
                        AddReward(0.1f); // Reward for picking cabbage
                        currentStep = RecipeStep.PlaceCabbageOnCounter;
                    }
                    else
                    {
                        AddReward(-0.01f);
                    }
                    break;
                }

            case RecipeStep.PlaceCabbageOnCounter:
                {
                    if (!HasKitchenObject() && selectedCounter is CuttingCounter cuttingCounter && cuttingCounter.HasKitchenObject())
                    {
                        KitchenObjectSO counterObject = cuttingCounter.GetKitchenObject().kitchenObjectSO;
                        if (counterObject.objectName == "Cabbage")
                        {
                            AddReward(0.2f); // Reward for placing cabbage on counter
                            cabbageCuttingCounter = cuttingCounter; // Save the counter where cabbage is placed
                            currentStep = RecipeStep.SliceCabbage;
                        }
                        else
                        {
                            AddReward(-0.01f);
                        }

                    }
                    //else if (!HasKitchenObject())
                    //{
                    //    currentStep = RecipeStep.PickCabbageFromCounter;
                    //}
                    else
                    {
                        AddReward(-0.01f);
                    }
                    break;
                }

            //case RecipeStep.PickCabbageFromCounter:
            //    {
            //        if (HasKitchenObject() && selectedCounter is ClearCounter clearCounter && clearCounter.HasKitchenObject())
            //        {
            //            KitchenObjectSO counterObject = clearCounter.GetKitchenObject().kitchenObjectSO;
            //            if (kitchenObject.kitchenObjectSO.objectName == "Cabbage")
            //            {
            //                AddReward(0.01f); // Reward for placing cabbage on counter
            //                currentStep = RecipeStep.SliceCabbage;
            //            }
            //            else
            //            {
            //                AddReward(-0.01f);
            //            }
            //        }
            //        else
            //        {
            //            AddReward(-0.01f);
            //        }
            //        break;
            //    }
            case RecipeStep.SliceCabbage:
                {
                    if (cabbageCuttingCounter != null)
                    {
                        if (cabbageCuttingCounter.IsCuttingComplete())
                        {
                            AddReward(0.3f); // Reward for slicing cabbage
                            currentStep = RecipeStep.PickCabbageSlices;
                        }
                        else
                        {
                            // Encourage the agent to perform the alternate interaction
                            AddReward(-0.01f); // Small penalty for not slicing
                        }
                    }
                    else
                    {
                        AddReward(-0.01f); // Penalty for losing track of the counter
                    }
                    break;
                }

            case RecipeStep.PickCabbageSlices:
                {
                    if (HasKitchenObject() && kitchenObject.kitchenObjectSO.objectName == "Cabbage Slices")
                    {

                        AddReward(0.1f); // Reward for picking cabbage slices
                        currentStep = RecipeStep.PlaceCabbageSlicesOnCounter;
                    }
                    else
                    {
                        AddReward(-0.01f);
                    }
                    break;
                }

            case RecipeStep.PlaceCabbageSlicesOnCounter://ERROR
                {
                    if (!HasKitchenObject() && selectedCounter != null)
                    {
                        // The agent has placed the cabbage slices on an empty counter
                        AddReward(0.2f); // Reward for placing slices on counter
                        cabbageSlicesCounter = selectedCounter;
                        currentStep = RecipeStep.PickTomato;
                    }
                    else if (selectedCounter is TrashCounter)
                    {
                        // The agent has placed the slices into the trash can
                        AddReward(-0.5f); // Penalty for discarding slices
                        currentStep = RecipeStep.PickCabbage; // Reset to picking cabbage
                    }
                    else
                    {
                        AddReward(-0.001f);
                    }
                    break;
                }

            case RecipeStep.PickTomato:
                {
                    if (HasKitchenObject() && kitchenObject.kitchenObjectSO.objectName == "Tomato")
                    {
                        AddReward(0.1f); // Reward for picking tomato
                        currentStep = RecipeStep.PlaceTomatoOnCounter;
                    }
                    else
                    {
                        AddReward(-0.01f);
                    }
                    break;
                }

            case RecipeStep.PlaceTomatoOnCounter:
                {
                    if (!HasKitchenObject() && selectedCounter is CuttingCounter cuttingCounter && cuttingCounter.HasKitchenObject())
                    {
                        KitchenObjectSO counterObject = cuttingCounter.GetKitchenObject().kitchenObjectSO;
                        if (kitchenObject.kitchenObjectSO.objectName == "Tomato")
                        {
                            AddReward(0.2f); // Reward for placing tomato on counter
                            tomatoCuttingCounter = cuttingCounter; // Save the counter where tomato is placed
                            currentStep = RecipeStep.SliceTomato;
                        }
                        else
                        {
                            AddReward(-0.01f);
                        }
                    }
                    else
                    {
                        //currentStep = RecipeStep.PickTomatoFromCounter;
                        AddReward(-0.01f);
                    }
                    break;
                }
            //case RecipeStep.PickTomatoFromCounter:
            //    {
            //        if (HasKitchenObject() && selectedCounter is ClearCounter clearCounter && clearCounter.HasKitchenObject())
            //        {
            //            KitchenObjectSO counterObject = clearCounter.GetKitchenObject().kitchenObjectSO;
            //            if (counterObject.objectName == "Tomato")
            //            {
            //                AddReward(0.01f); // Reward for placing cabbage on counter
            //                currentStep = RecipeStep.SliceTomato;
            //            }
            //            else
            //            {
            //                AddReward(-0.01f);
            //            }
            //        }
            //        else
            //        {
            //            AddReward(-0.01f);
            //        }
            //        break;
            //    }
            case RecipeStep.SliceTomato:
                {
                    if (tomatoCuttingCounter != null)
                    {
                        if (tomatoCuttingCounter.IsCuttingComplete())
                        {
                            AddReward(0.3f); // Reward for slicing tomato
                            currentStep = RecipeStep.PickTomatoSlices;
                        }
                        else
                        {
                            // Encourage the agent to perform the alternate interaction
                            AddReward(-0.01f); // Small penalty for not slicing
                        }
                    }
                    else
                    {
                        AddReward(-0.01f); // Penalty for losing track of the counter
                    }
                    break;
                }

            case RecipeStep.PickTomatoSlices: 
                {
                    if (HasKitchenObject() && kitchenObject.kitchenObjectSO.objectName == "Tomato Slices")
                    {
                        AddReward(0.1f); // Reward for picking tomato slices
                        currentStep = RecipeStep.PlaceTomatoSlicesOnCounter; // Transition to new state
                    }
                    else
                    {
                        AddReward(-0.01f);
                    }
                    break;
                }

            case RecipeStep.PlaceTomatoSlicesOnCounter: //ERROR
                {
                    if (!HasKitchenObject() && selectedCounter != null)
                    {
                        // The agent has placed the tomato slices on an empty counter
                        AddReward(0.2f); // Reward for placing slices on counter
                        currentStep = RecipeStep.PickPlate;
                        tomatoSlicesCounter = selectedCounter;
                    }
                    else if (selectedCounter is TrashCounter)
                    {
                        // The agent has placed the slices into the trash can
                        AddReward(-0.5f); // Penalty for discarding slices
                        currentStep = RecipeStep.PickTomato; // Reset to picking tomato
                    }
                    else
                    {
                        AddReward(-0.01f);
                    }
                    break;
                }

            case RecipeStep.PickPlate: //ERROR
                {
                    if (HasKitchenObject() && kitchenObject.kitchenObjectSO.objectName == "Plate")
                    {
                        AddReward(0.1f); // Reward for picking plate
                        currentStep = RecipeStep.AddTomatoSlicesToPlate;
                    }
                    else
                    {
                        AddReward(-0.01f);
                    }
                    break;
                }
            case RecipeStep.AddTomatoSlicesToPlate: //ERROR
                {
                    if (HasKitchenObject() && kitchenObject is PlateKitchenObject plateKitchenObject)
                    {
                        // Check if the plate already has tomato slices
                        List<KitchenObjectSO> plateIngredients = plateKitchenObject.GetKitchenObjectSOList();
                        bool hasTomatoSlices = plateIngredients.Exists(ingredient => ingredient.objectName == "Tomato Slices");
                        bool hasCabbageSlices = plateIngredients.Exists(ingredient => ingredient.objectName == "Cabbage Slices");
                        if (!hasTomatoSlices)
                        {
                            if (tomatoSlicesCounter != null && tomatoSlicesCounter.HasKitchenObject())
                            {
                                // Agent needs to add tomato slices from the counter to the plate
                                if (selectedCounter == tomatoSlicesCounter)
                                {
                                    // Get the tomato slices from the counter
                                    KitchenObject tomatoSlicesObject = tomatoSlicesCounter.GetKitchenObject();
                                    if (tomatoSlicesObject != null && tomatoSlicesObject.kitchenObjectSO.objectName == "Tomato Slices")
                                    {
                                        // Try to add tomato slices to the plate
                                        bool added = plateKitchenObject.TryAddIngredient(tomatoSlicesObject.kitchenObjectSO);
                                        if (added)
                                        {
                                            // Remove tomato slices from the counter
                                            tomatoSlicesObject.DestroySelf();
                                            tomatoSlicesCounter.ClearKitchenObject();
                                            AddReward(0.2f); // Reward for adding tomato slices to plate
                                            currentStep = RecipeStep.AddCabbageSlicesToPlate;
                                        }
                                        else
                                        {
                                            AddReward(-0.01f); // Could not add ingredient to plate
                                        }
                                    }
                                    else
                                    {
                                        AddReward(-0.01f); // No tomato slices on counter
                                    }
                                }
                                else
                                {
                                    AddReward(-0.01f); // Encourage agent to move to tomato slices counter
                                }
                            }
                            else
                            {
                                AddReward(-0.1f); // Penalty for missing tomato slices
                                currentStep = RecipeStep.PickTomato; // Reset to pick tomato
                            }
                        }
                        else
                        {
                            // Plate already has tomato slices, proceed to next step
                            currentStep = RecipeStep.AddCabbageSlicesToPlate;
                        }
                    }
                    else
                    {
                        AddReward(-0.01f);
                        // If plate is placed on counter, pick it up again
                        if (selectedCounter != null && selectedCounter.HasKitchenObject())
                        {
                            KitchenObjectSO counterObjectSO = selectedCounter.GetKitchenObject().kitchenObjectSO;
                            if (counterObjectSO.objectName == "Plate")
                            {
                                selectedCounter.Interact(this); // Pick up the plate
                                AddReward(0.005f); // Small reward for picking up plate again
                            }
                        }
                        else if (selectedCounter is TrashCounter)
                        {
                            AddReward(-0.05f); // Penalty for trashing the plate
                            currentStep = RecipeStep.PickPlate; // Reset to pick plate
                        }
                    }
                    break;
                }

            case RecipeStep.AddCabbageSlicesToPlate: //ERROR
                {
                    if (HasKitchenObject() && kitchenObject is PlateKitchenObject plateKitchenObject)
                    {
                        // Check if the plate already has cabbage slices
                        List<KitchenObjectSO> plateIngredients = plateKitchenObject.GetKitchenObjectSOList();
                        bool hasTomatoSlices = plateIngredients.Exists(ingredient => ingredient.objectName == "Tomato Slices");
                        bool hasCabbageSlices = plateIngredients.Exists(ingredient => ingredient.objectName == "Cabbage Slices");
                        if (!hasCabbageSlices)
                        {
                            if (cabbageSlicesCounter != null && cabbageSlicesCounter.HasKitchenObject())
                            {
                                // Agent needs to add cabbage slices from the counter to the plate
                                if (selectedCounter == cabbageSlicesCounter)
                                {
                                    // Get the cabbage slices from the counter
                                    KitchenObject cabbageSlicesObject = cabbageSlicesCounter.GetKitchenObject();
                                    if (cabbageSlicesObject != null && cabbageSlicesObject.kitchenObjectSO.objectName == "Cabbage Slices")
                                    {
                                        // Try to add cabbage slices to the plate
                                        bool added = plateKitchenObject.TryAddIngredient(cabbageSlicesObject.kitchenObjectSO);
                                        if (added)
                                        {
                                            // Remove cabbage slices from the counter
                                            cabbageSlicesObject.DestroySelf();
                                            cabbageSlicesCounter.ClearKitchenObject();
                                            AddReward(0.2f); // Reward for adding cabbage slices to plate
                                            currentStep = RecipeStep.SaladAssembled;
                                        }
                                        else
                                        {
                                            AddReward(-0.01f); // Could not add ingredient to plate
                                        }
                                    }
                                    else
                                    {
                                        AddReward(-0.01f); // No cabbage slices on counter
                                    }
                                }
                                else
                                {
                                    AddReward(-0.01f); // Encourage agent to move to cabbage slices counter
                                }
                            }
                            else
                            {
                                AddReward(-0.025f); // Penalty for missing cabbage slices
                                currentStep = RecipeStep.PickCabbage; // Reset to pick cabbage
                            }
                        }
                        else
                        {
                            // Plate already has cabbage slices, proceed to salad assembled
                            currentStep = RecipeStep.SaladAssembled;
                        }
                    }
                    else
                    {
                        AddReward(-0.1f);
                        // If plate is placed on counter, pick it up again
                        if (selectedCounter != null && selectedCounter.HasKitchenObject())
                        {
                            KitchenObjectSO counterObjectSO = selectedCounter.GetKitchenObject().kitchenObjectSO;
                            if (counterObjectSO.objectName == "Plate")
                            {
                                selectedCounter.Interact(this); // Pick up the plate
                                AddReward(0.01f); // Small reward for picking up plate again
                            }
                        }
                        else if (selectedCounter is TrashCounter)
                        {
                            AddReward(-0.05f); // Penalty for trashing the plate
                            currentStep = RecipeStep.PickPlate; // Reset to pick plate
                        }
                    }
                    break;
                }

            case RecipeStep.SaladAssembled: //ERROR
                {
                    if (HasKitchenObject() && kitchenObject is PlateKitchenObject plateKitchenObject)
                    {
                        List<KitchenObjectSO> plateIngredients = plateKitchenObject.GetKitchenObjectSOList();
                        bool hasTomatoSlices = plateIngredients.Exists(ingredient => ingredient.objectName == "Tomato Slices");
                        bool hasCabbageSlices = plateIngredients.Exists(ingredient => ingredient.objectName == "Cabbage Slices");

                        if (hasTomatoSlices && hasCabbageSlices)

                        {
                            AddReward(50.0f); // Big reward for completing the salad
                            EndEpisode();
                        }
                        else
                        {
                            AddReward(-0.1f); // Penalize for incomplete salad
                        }
                    }
                    else
                    {
                        AddReward(-0.1f);
                        if (selectedCounter != null && selectedCounter.HasKitchenObject())
                        {
                            KitchenObjectSO counterObjectSO = selectedCounter.GetKitchenObject().kitchenObjectSO;
                            if (counterObjectSO.objectName == "Plate")
                            {
                                selectedCounter.Interact(this); // Pick up the plate
                                AddReward(0.3f); // Small reward for picking up plate again
                            }
                        }
                        else if (selectedCounter is TrashCounter)
                        {
                            AddReward(-0.2f); // Penalty for trashing the plate
                            currentStep = RecipeStep.PickPlate; // Reset to pick plate
                        }
                    }
                    break;
                }

            default:
                {
                    AddReward(-0.001f); // Small penalty for unrecognized actions
                    break;
                }
        }
    }


    private void PrintSelectedCounterInfo()
    {
        if (selectedCounter != null)
        {
            Debug.Log("Selected Counter Type: " + selectedCounter.GetType().Name);
            Debug.Log("Selected Counter Instance Name: " + selectedCounter.name);
        }
        else
        {
            Debug.Log("No counter is currently selected.");
        }
    }

    private void HandleMovement(Vector3 moveDir, float moveDistance)
    {
        float playerRadius = .7f;
        float playerHeight = 2f;
        bool canMove = !Physics.CapsuleCast(transform.position, transform.position + Vector3.up * playerHeight, playerRadius, moveDir, moveDistance);

        if (!canMove)
        {
            Vector3 moveDirX = new Vector3(moveDir.x, 0, 0).normalized;
            canMove = (moveDir.x < -.5f || moveDir.x > +.5f) && !Physics.CapsuleCast(transform.position, transform.position + Vector3.up * playerHeight, playerRadius, moveDirX, moveDistance);

            if (canMove) moveDir = moveDirX;
            else
            {
                Vector3 moveDirZ = new Vector3(0, 0, moveDir.z).normalized;
                canMove = (moveDir.z < -.5f || moveDir.z > +.5f) && !Physics.CapsuleCast(transform.position, transform.position + Vector3.up * playerHeight, playerRadius, moveDirZ, moveDistance);
                if (canMove) moveDir = moveDirZ;
            }
        }

        if (canMove)
        {
            transform.position += moveDir * moveDistance;
        }

        isWalking = moveDir != Vector3.zero;

        float rotateSpeed = 10f;
        transform.forward = Vector3.Slerp(transform.forward, moveDir, Time.deltaTime * rotateSpeed);
    }

    //end new

    private void GameInput_OnInteractAlternateAction(object sender, EventArgs e)  // interact button pressed (F)
    {
        if (!KitchenGameManager.Instance.IsGamePlaying()) return;

        if (selectedCounter != null)
        {
            selectedCounter.InteractAlternate(this);
        }
    }

    private void GameInput_OnInteractAction(object sender, System.EventArgs e) // interact button pressed (E)
    {
        if (!KitchenGameManager.Instance.IsGamePlaying()) return;

        if (selectedCounter != null) // interact with what counter
        {
            //print(this.selectedCounter.GetType().Name);
            //if (selectedCounter is ContainerCounter containerCounter) // Safe cast to ContainerCounter
            //{
            //    // Access and print the kitchen object name
            //    Debug.Log($"Kitchen Object Name: {containerCounter.kitchenObjectSO.name}"); // print the object for container counter
            //}
            selectedCounter.Interact(this);
        }
    }


    /* old start*/
    //private void Start()
    //{
    //    gameInput.OnInteractAction += GameInput_OnInteractAction;
    //    gameInput.OnInteractAlternateAction += GameInput_OnInteractAlternateAction;
    //}
    
    // old update
    //private void Update() {
    //    HandleMovement();
    //    HandleInteractions();
    //}

    public bool IsWalking() {
        return isWalking;
    }

    private void HandleInteractions() {
        Vector2 inputVector = gameInput.GetMovementVectorNormalized();

        Vector3 moveDir = new Vector3(inputVector.x, 0f, inputVector.y);

        if (moveDir != Vector3.zero) {
            lastInteractDir = moveDir;
        }

        float interactDistance = 2f;
        if (Physics.Raycast(transform.position, lastInteractDir, out RaycastHit raycastHit, interactDistance, countersLayerMask)) { //detect counter
            if (raycastHit.transform.TryGetComponent(out BaseCounter baseCounter)) {
                // Has ClearCounter
                if (baseCounter != selectedCounter) {
                    SetSelectedCounter(baseCounter); // change counter
                }
            } else {
                SetSelectedCounter(null);

            }
        } else {
            SetSelectedCounter(null);
        }
    }

    private void HandleInteractionsVec2(Vector2 inputVector)
    {
        Vector3 moveDir = new Vector3(inputVector.x, 0f, inputVector.y);

        if (moveDir != Vector3.zero)
        {
            lastInteractDir = moveDir;
        }

        float interactDistance = 2f;
        if (Physics.Raycast(transform.position, lastInteractDir, out RaycastHit raycastHit, interactDistance, countersLayerMask))
        { //detect counter
            if (raycastHit.transform.TryGetComponent(out BaseCounter baseCounter))
            {
                // Has ClearCounter
                if (baseCounter != selectedCounter)
                {
                    SetSelectedCounter(baseCounter); // change counter
                }
            }
            else
            {
                SetSelectedCounter(null);

            }
        }
        else
        {
            SetSelectedCounter(null);
        }
    }

    // old handle movement
    //private void HandleMovement() {
    //    Vector2 inputVector = gameInput.GetMovementVectorNormalized();

    //    Vector3 moveDir = new Vector3(inputVector.x, 0f, inputVector.y);

    //    float moveDistance = moveSpeed * Time.deltaTime;
    //    float playerRadius = .7f;
    //    float playerHeight = 2f;
    //    bool canMove = !Physics.CapsuleCast(transform.position, transform.position + Vector3.up * playerHeight, playerRadius, moveDir, moveDistance);

    //    if (!canMove) {
    //        // Cannot move towards moveDir

    //        // Attempt only X movement
    //        Vector3 moveDirX = new Vector3(moveDir.x, 0, 0).normalized;
    //        canMove = (moveDir.x < -.5f || moveDir.x > +.5f) && !Physics.CapsuleCast(transform.position, transform.position + Vector3.up * playerHeight, playerRadius, moveDirX, moveDistance);

    //        if (canMove) {
    //            // Can move only on the X
    //            moveDir = moveDirX;
    //        } else {
    //            // Cannot move only on the X

    //            // Attempt only Z movement
    //            Vector3 moveDirZ = new Vector3(0, 0, moveDir.z).normalized;
    //            canMove = (moveDir.z < -.5f || moveDir.z > +.5f) && !Physics.CapsuleCast(transform.position, transform.position + Vector3.up * playerHeight, playerRadius, moveDirZ, moveDistance);

    //            if (canMove) {
    //                // Can move only on the Z
    //                moveDir = moveDirZ;
    //            } else {
    //                // Cannot move in any direction
    //            }
    //        }
    //    }

    //    if (canMove) {
    //        transform.position += moveDir * moveDistance;
    //    }

    //    isWalking = moveDir != Vector3.zero;

    //    float rotateSpeed = 10f;
    //    transform.forward = Vector3.Slerp(transform.forward, moveDir, Time.deltaTime * rotateSpeed);
    //}

    private void SetSelectedCounter(BaseCounter selectedCounter) {
        this.selectedCounter = selectedCounter;

        OnSelectedCounterChanged?.Invoke(this, new OnSelectedCounterChangedEventArgs {
            selectedCounter = selectedCounter
        });
    }

    public Transform GetKitchenObjectFollowTransform() { //transform player when input
        return kitchenObjectHoldPoint;
    }



    public void SetKitchenObject(KitchenObject kitchenObject)
    {
        this.kitchenObject = kitchenObject;
        //print(this.kitchenObject.kitchenObjectSO.objectName);
        if (this.kitchenObject != null)
        {
            OnPickedSomething?.Invoke(this, EventArgs.Empty);
        }
    }



    public KitchenObject GetKitchenObject() {
        return kitchenObject;
    }

    public void ClearKitchenObject() {
        kitchenObject = null;
    }

    public bool HasKitchenObject() {
        return kitchenObject != null;
    }

    public void ClearHoldPoint(Transform holdPoint)
    {
        foreach (Transform child in holdPoint)
        {
            Destroy(child.gameObject);
        }
    }


}