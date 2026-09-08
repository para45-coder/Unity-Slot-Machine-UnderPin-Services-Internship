using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ReelManager : MonoBehaviour
{
    [Header("Reel Settings")]
    public float spinSpeed = 10f;
    public float spinDuration = 2.5f;
    public int visibleSymbolCount = 3;
    public float symbolSpacing = 2f;

    [Header("References")]
    public Transform symbolContainer;
    public GameObject symbolPrefab;

    private List<GameObject> symbols = new List<GameObject>();
    private List<SymbolData> symbolDataList = new List<SymbolData>();

    private bool isSpinning = false;
    private Sprite currentSymbol;

    public bool IsSpinning
    {
        get { return isSpinning; }
    }

    public void Initialize(List<SymbolData> data)
    {
        symbolDataList = data;

        if (symbolDataList == null || symbolDataList.Count == 0)
        {
            Debug.LogError("ReelManager: No SymbolData found.");
            return;
        }

        CreateSymbols();
    }

    private void CreateSymbols()
    {
        // Remove old symbols
        foreach (GameObject symbol in symbols)
        {
            if (symbol != null)
                Destroy(symbol);
        }

        symbols.Clear();

        // Create extra symbols so the reel can scroll smoothly
        int totalSymbols = visibleSymbolCount + 5;

        for (int i = 0; i < totalSymbols; i++)
        {
            GameObject newSymbol = Instantiate(symbolPrefab, symbolContainer);

            SpriteRenderer renderer = newSymbol.GetComponent<SpriteRenderer>();

            if (renderer != null)
            {
                int randomIndex = Random.Range(0, symbolDataList.Count);
                renderer.sprite = symbolDataList[randomIndex].symbolSprite;
                renderer.sortingOrder = 1;
            }

            newSymbol.transform.localPosition =
                new Vector3(0f, i * symbolSpacing, 0f);

            symbols.Add(newSymbol);
        }

        // Put the first visible symbol in the center position
        currentSymbol = GetMiddleSymbolSprite();
    }

    public void StartSpin(Sprite targetSprite)
    {
        if (isSpinning)
            return;

        if (targetSprite == null)
        {
            Debug.LogError("ReelManager: Target sprite is null.");
            return;
        }

        StartCoroutine(SpinCoroutine(targetSprite));
    }

    private IEnumerator SpinCoroutine(Sprite targetSprite)
    {
        isSpinning = true;

        float elapsed = 0f;

        // Spin the reel
        while (elapsed < spinDuration)
        {
            float deltaMovement = spinSpeed * Time.deltaTime;

            symbolContainer.localPosition += Vector3.down * deltaMovement;

            // Move symbols back to the top when they leave the reel
            foreach (GameObject symbol in symbols)
            {
                if (symbol == null)
                    continue;

                if (symbol.transform.localPosition.y <
                    -symbolSpacing * 2f)
                {
                    float highestY = GetHighestSymbolY();

                    symbol.transform.localPosition =
                        new Vector3(
                            0f,
                            highestY + symbolSpacing,
                            0f
                        );

                    // Give recycled symbol a new random sprite
                    SpriteRenderer renderer =
                        symbol.GetComponent<SpriteRenderer>();

                    if (renderer != null)
                    {
                        int randomIndex =
                            Random.Range(0, symbolDataList.Count);

                        renderer.sprite =
                            symbolDataList[randomIndex].symbolSprite;
                    }
                }
            }

            elapsed += Time.deltaTime;

            yield return null;
        }

        // Slow down slightly before stopping
        float slowdownTime = 0.4f;
        float slowdownElapsed = 0f;

        while (slowdownElapsed < slowdownTime)
        {
            float t = slowdownElapsed / slowdownTime;

            float currentSpeed =
                Mathf.Lerp(spinSpeed, 0f, t);

            symbolContainer.localPosition +=
                Vector3.down * currentSpeed * Time.deltaTime;

            slowdownElapsed += Time.deltaTime;

            yield return null;
        }

        // Force the final visible result
        SetFinalSymbol(targetSprite);

        isSpinning = false;
    }

    private void SetFinalSymbol(Sprite targetSprite)
    {
        /*
         * Instead of trying to calculate which randomly recycled
         * symbol happens to be in the middle, we explicitly place
         * the requested result in the middle position.
         *
         * This makes the RNG/result system reliable.
         */

        symbolContainer.localPosition = Vector3.zero;

        // Put symbols at fixed positions.
        for (int i = 0; i < symbols.Count; i++)
        {
            if (symbols[i] == null)
                continue;

            float yPosition = (i - 2) * symbolSpacing;

            symbols[i].transform.localPosition =
                new Vector3(0f, yPosition, 0f);
        }

        // Middle symbol = final result
        if (symbols.Count >= 3)
        {
            SpriteRenderer middleRenderer =
                symbols[2].GetComponent<SpriteRenderer>();

            if (middleRenderer != null)
            {
                middleRenderer.sprite = targetSprite;
                currentSymbol = targetSprite;
            }
        }

        // Randomize symbols above and below the result
        for (int i = 0; i < symbols.Count; i++)
        {
            if (i == 2 || symbols[i] == null)
                continue;

            SpriteRenderer renderer =
                symbols[i].GetComponent<SpriteRenderer>();

            if (renderer != null)
            {
                int randomIndex =
                    Random.Range(0, symbolDataList.Count);

                renderer.sprite =
                    symbolDataList[randomIndex].symbolSprite;
            }
        }
    }

    private float GetHighestSymbolY()
    {
        float highest = float.MinValue;

        foreach (GameObject symbol in symbols)
        {
            if (symbol == null)
                continue;

            float y = symbol.transform.localPosition.y;

            if (y > highest)
                highest = y;
        }

        return highest;
    }

    private Sprite GetMiddleSymbolSprite()
    {
        if (symbols.Count < 3)
            return null;

        SpriteRenderer renderer =
            symbols[2].GetComponent<SpriteRenderer>();

        if (renderer != null)
            return renderer.sprite;

        return null;
    }

    public Sprite GetCurrentSymbol()
    {
        return currentSymbol;
    }
}