using System.Collections.Generic;
using UnityEngine;
using TMPro;


public class KitchenArea : MonoBehaviour
{
    public Player chefAgent;

    public TextMeshPro cumulativeRewardText;

    private List<RecipeSO> recipeList;

    public float spawnRangeX = 2f; // Half the width of the spawn area
    public float spawnRangeZ = 1f; // Half the height of the spawn area

    private void Start()
    {
        ResetArea();
    }


    public void ResetArea()
    {
        float randomX = Random.Range(-spawnRangeX, spawnRangeX);
        float randomZ = Random.Range(-spawnRangeZ, spawnRangeZ);
        Vector3 spawnPosition = new Vector3(
            chefAgent.initialPosition.x + randomX,
            chefAgent.initialPosition.y, // Maintain the original Y position
            chefAgent.initialPosition.z + randomZ
        );
        chefAgent.transform.position = spawnPosition;
        chefAgent.ClearHoldPoint(chefAgent.GetKitchenObjectFollowTransform());
        if (chefAgent.HasKitchenObject())
        {
            chefAgent.kitchenObject.DestroySelf();
        }
        ResetCounters();
        // Clear the kitchen object the player might be holding
        chefAgent.ClearKitchenObject();
        //chefAgent.kitchenObject = null;

        PlatesCounter[] platesCounters = FindObjectsByType<PlatesCounter>(FindObjectsSortMode.None);
        foreach (PlatesCounter platesCounter in platesCounters)
        {
            if (platesCounter.platesSpawnedAmount > 0)
                platesCounter.ResetPlatesCounter(platesCounter.counterTopPoint);
        }

        DeliveryManager.Instance.ResetWaitingRecipes();

        // Reset the selected counter (no counter selected after reset)
        chefAgent.selectedCounter = null;
        chefAgent.isWalking = false;

        ProgressBarUI[] progressBars = FindObjectsByType<ProgressBarUI>(FindObjectsSortMode.None);
        foreach (ProgressBarUI progressBar in progressBars)
        {
            progressBar.Hide();
        }

        StoveBurnWarningUI[] burnUI = FindObjectsByType<StoveBurnWarningUI>(FindObjectsSortMode.None);
        foreach (StoveBurnWarningUI burn in burnUI)
        {
            burn.Hide();
        }

    }


    private void ResetCounters()
    {
        // Find all objects of type BaseCounter in the scene
        BaseCounter[] allCounters = FindObjectsByType<BaseCounter>(FindObjectsSortMode.None);

        // Loop through each counter and call ClearKitchenObject on it
        foreach (BaseCounter counter in allCounters)
        {
            if (counter.HasKitchenObject())
            {
                counter.GetKitchenObject().DestroySelf();
            }
        }
    }

    private void Update()
    {
        // Update the cumulative reward text
        cumulativeRewardText.text = chefAgent.GetCumulativeReward().ToString("0.00");
        //print(cumulativeRewardText.text);
        print(chefAgent.currentStep);
    }
}
