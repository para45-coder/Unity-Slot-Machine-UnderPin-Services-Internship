using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;

public class SlotMachineGame : MonoBehaviour
{
    public static SlotMachineGame Instance;

    [Header("Reel Settings")]
    public int reelCount = 3;
    public Transform reelsParent;
    public GameObject reelPrefab;

    [Header("UI")]
    public TextMeshProUGUI balanceText;
    public TextMeshProUGUI betText;
    public TextMeshProUGUI winText;

    public Button spinButton;
    public Button increaseBet;
    public Button decreaseBet;

    [Header("Game Settings")]
    public int startingBalance = 1000;
    public int minBet = 10;
    public int maxBet = 100;

    [Header("RNG Settings")]
    [Range(0f, 1f)]
    public float winChance = 0.25f;

    private List<ReelManager> reels =
        new List<ReelManager>();

    private List<SymbolData> allSymbols =
        new List<SymbolData>();

    private int currentBalance;
    private int currentBet = 10;

    private bool isSpinning = false;

    private int spinCount = 0;
    private int winCount = 0;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        currentBalance = startingBalance;

        LoadSymbols();

        if (allSymbols.Count == 0)
        {
            Debug.LogError(
                "No SymbolData found. " +
                "Put your SymbolData assets inside a Resources folder."
            );

            return;
        }

        SetupReels();
        SetupUI();
        UpdateUI();

        if (winText != null)
            winText.text = "🎰 Press SPIN to play!";
    }

    private void LoadSymbols()
    {
        SymbolData[] symbols =
            Resources.LoadAll<SymbolData>("");

        allSymbols.Clear();

        allSymbols.AddRange(symbols);

        Debug.Log(
            "Loaded " + allSymbols.Count +
            " symbols."
        );
    }

    private void SetupReels()
    {
        if (reelPrefab == null)
        {
            Debug.LogError("Reel Prefab is not assigned.");
            return;
        }

        if (reelsParent == null)
        {
            Debug.LogError("Reels Parent is not assigned.");
            return;
        }

        // Clear existing reels if any
        foreach (Transform child in reelsParent)
        {
            Destroy(child.gameObject);
        }

        reels.Clear();

        for (int i = 0; i < reelCount; i++)
        {
            GameObject reelObject =
                Instantiate(reelPrefab, reelsParent);

            ReelManager reel =
                reelObject.GetComponent<ReelManager>();

            if (reel != null)
            {
                reel.Initialize(allSymbols);

                reels.Add(reel);
            }

            // Position reels horizontally
            float xPosition =
                (i - (reelCount - 1) / 2f) * 2.5f;

            reelObject.transform.localPosition =
                new Vector3(
                    xPosition,
                    0f,
                    0f
                );
        }

        Debug.Log(
            "Created " + reels.Count + " reels."
        );
    }

    private void SetupUI()
    {
        if (spinButton != null)
        {
            spinButton.onClick.RemoveAllListeners();
            spinButton.onClick.AddListener(OnSpinClick);
        }

        if (increaseBet != null)
        {
            increaseBet.onClick.RemoveAllListeners();

            increaseBet.onClick.AddListener(
                () => ChangeBet(10)
            );
        }

        if (decreaseBet != null)
        {
            decreaseBet.onClick.RemoveAllListeners();

            decreaseBet.onClick.AddListener(
                () => ChangeBet(-10)
            );
        }
    }

    public void OnSpinClick()
    {
        if (isSpinning)
            return;

        if (allSymbols.Count == 0)
            return;

        if (currentBalance < currentBet)
        {
            if (winText != null)
                winText.text = "❌ Not enough money!";

            return;
        }

        StartSpin();
    }

    private void StartSpin()
    {
        isSpinning = true;

        spinCount++;

        // Pay the bet
        currentBalance -= currentBet;

        if (winText != null)
            winText.text = "🎰 Spinning...";

        UpdateUI();

        /*
         * Generate ONE result set before spinning.
         *
         * This is important:
         * The reel animation is separate from the RNG.
         */

        List<Sprite> results =
            GenerateSpinResults();

        for (int i = 0; i < reels.Count; i++)
        {
            if (reels[i] != null)
            {
                reels[i].StartSpin(results[i]);
            }
        }

        StartCoroutine(WaitForReels());
    }

    private List<Sprite> GenerateSpinResults()
    {
        List<Sprite> results =
            new List<Sprite>();

        // Random number between 0 and 1
        float randomValue = Random.value;

        /*
         * Example:
         * winChance = 0.25
         *
         * Approximately 25% of spins become wins.
         * Approximately 75% become losses.
         */

        bool shouldWin =
            randomValue <= winChance;

        if (shouldWin)
        {
            // Select ONE random symbol
            int winningIndex =
                Random.Range(0, allSymbols.Count);

            Sprite winningSprite =
                allSymbols[winningIndex].symbolSprite;

            // Every reel gets the same symbol
            for (int i = 0; i < reelCount; i++)
            {
                results.Add(winningSprite);
            }

            Debug.Log(
                "RNG RESULT: WIN - " +
                winningSprite.name
            );
        }
        else
        {
            /*
             * Generate a losing combination.
             *
             * We make sure all reels are NOT identical,
             * so the player definitely loses.
             */

            int firstIndex =
                Random.Range(0, allSymbols.Count);

            Sprite firstSprite =
                allSymbols[firstIndex].symbolSprite;

            results.Add(firstSprite);

            for (int i = 1; i < reelCount; i++)
            {
                int randomIndex =
                    Random.Range(0, allSymbols.Count);

                Sprite selectedSprite =
                    allSymbols[randomIndex].symbolSprite;

                // Make sure the second/third symbol
                // differs from the first one.
                if (allSymbols.Count > 1)
                {
                    int safetyCounter = 0;

                    while (
                        selectedSprite == firstSprite &&
                        safetyCounter < 20
                    )
                    {
                        randomIndex =
                            Random.Range(
                                0,
                                allSymbols.Count
                            );

                        selectedSprite =
                            allSymbols[randomIndex].symbolSprite;

                        safetyCounter++;
                    }
                }

                results.Add(selectedSprite);
            }

            Debug.Log("RNG RESULT: LOSE");
        }

        return results;
    }

    private IEnumerator WaitForReels()
    {
        // Wait until every reel has stopped
        bool reelsStillSpinning = true;

        while (reelsStillSpinning)
        {
            reelsStillSpinning = false;

            foreach (ReelManager reel in reels)
            {
                if (reel != null && reel.IsSpinning)
                {
                    reelsStillSpinning = true;
                    break;
                }
            }

            yield return null;
        }

        // Now ALL reels have stopped.
        CheckWin();
    }

    public void CheckWin()
    {
        if (reels.Count != reelCount)
        {
            Debug.LogError("Incorrect reel count.");
            isSpinning = false;
            UpdateUI();
            return;
        }

        List<Sprite> results =
            new List<Sprite>();

        foreach (ReelManager reel in reels)
        {
            if (reel != null)
            {
                results.Add(
                    reel.GetCurrentSymbol()
                );
            }
        }

        if (results.Count != reelCount)
        {
            Debug.LogError(
                "Could not retrieve reel results."
            );

            isSpinning = false;
            UpdateUI();
            return;
        }

        // Check that all symbols are identical
        bool allMatch = true;

        for (int i = 1; i < results.Count; i++)
        {
            if (
                results[i] == null ||
                results[i] != results[0]
            )
            {
                allMatch = false;
                break;
            }
        }

        if (allMatch)
        {
            HandleWin(results[0]);
        }
        else
        {
            HandleLoss();
        }

        isSpinning = false;

        UpdateUI();
    }

    private void HandleWin(Sprite winningSprite)
    {
        winCount++;

        int multiplier = 1;

        // Find payout multiplier
        foreach (SymbolData data in allSymbols)
        {
            if (
                data != null &&
                data.symbolSprite == winningSprite
            )
            {
                multiplier =
                    Mathf.Max(1, data.multiplier);

                break;
            }
        }

        int winAmount =
            currentBet * multiplier;

        currentBalance += winAmount;

        if (winText != null)
        {
            winText.text =
                "🎉 WIN! +" +
                winAmount +
                " 🎉";
        }

        float winRate =
            (float)winCount /
            spinCount *
            100f;

        Debug.Log(
            "🎉 WIN! " +
            winningSprite.name +
            " | Payout: $" +
            winAmount +
            " | Multiplier: x" +
            multiplier +
            " | Win Rate: " +
            winRate.ToString("F1") +
            "%"
        );
    }

    private void HandleLoss()
    {
        if (winText != null)
            winText.text = "😔 Try Again!";

        float winRate = 0f;

        if (spinCount > 0)
        {
            winRate =
                (float)winCount /
                spinCount *
                100f;
        }

        Debug.Log(
            "❌ LOSS | Spin #" +
            spinCount +
            " | Win Rate: " +
            winRate.ToString("F1") +
            "%"
        );
    }

    private void ChangeBet(int amount)
    {
        currentBet += amount;

        currentBet =
            Mathf.Clamp(
                currentBet,
                minBet,
                maxBet
            );

        UpdateUI();
    }

    private void UpdateUI()
    {
        if (balanceText != null)
        {
            balanceText.text =
                "$" + currentBalance;
        }

        if (betText != null)
        {
            betText.text =
                "$" + currentBet;
        }

        if (spinButton != null)
        {
            spinButton.interactable =
                !isSpinning &&
                currentBalance >= currentBet;
        }

        if (increaseBet != null)
        {
            increaseBet.interactable =
                currentBet < maxBet;
        }

        if (decreaseBet != null)
        {
            decreaseBet.interactable =
                currentBet > minBet;
        }
    }
}