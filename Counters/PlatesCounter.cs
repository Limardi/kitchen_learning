using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlatesCounter : BaseCounter {


    public event EventHandler OnPlateSpawned;
    public event EventHandler OnPlateRemoved;


    [SerializeField] private KitchenObjectSO plateKitchenObjectSO;
    [SerializeField] private PlatesCounterVisual platesCounterVisual;


    private float spawnPlateTimer;
    private float spawnPlateTimerMax = 4f;
    public int platesSpawnedAmount;
    private int platesSpawnedAmountMax = 4;


    private void Update() {
        spawnPlateTimer += Time.deltaTime;
        if (spawnPlateTimer > spawnPlateTimerMax) {
            spawnPlateTimer = 0f;

            if (KitchenGameManager.Instance.IsGamePlaying() && platesSpawnedAmount < platesSpawnedAmountMax) {
                platesSpawnedAmount++;

                OnPlateSpawned?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public override void Interact(Player player) {
        if (!player.HasKitchenObject()) {
            // Player is empty handed
            if (platesSpawnedAmount > 0) {
                // There's at least one plate here
                platesSpawnedAmount--;

                KitchenObject.SpawnKitchenObject(plateKitchenObjectSO, player);

                OnPlateRemoved?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public void ResetPlatesCounter(Transform holdPoint)
    {
        spawnPlateTimer = 0f; // Reset the timer
        platesSpawnedAmount = 0; // Reset the number of plates spawned
        if (platesCounterVisual != null)
        {
            platesCounterVisual.ResetVisuals();
        }

        foreach (Transform child in holdPoint)
        {
            Destroy(child.gameObject);
        }
        Debug.Log("PlatesCounter has been reset.");
    }


}