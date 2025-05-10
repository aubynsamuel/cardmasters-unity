using UnityEngine;
using System.Collections; // Required for Coroutines
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class CardsSetup : MonoBehaviour
{
    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip dealSound;
    public AudioClip winSound;
    public AudioClip shuffleSound;
    public AudioClip loseSound;

    [Header("Hand Positions")]
    public List<Transform> playersHandPosition;
    public List<Transform> opponentsHandPosition;

    [Header("Play Slots")]
    public Transform playSpot;
    public Transform playSpot2;

    [Header("Deal Animation Settings")]
    public float moveDuration = 0.5f;
    public float dealDelay = 0.3f;

    [HideInInspector]
    public List<List<Transform>> allPlayersHandsTransform;

    [Header("Deck Setup")]
    public Transform deckTransform;
    public GameObject cardPrefab;

    // Deck data
    private readonly string[] suits = { "diamond", "spade", "heart", "club" };
    private readonly string[] ranks = { "6", "7", "8", "9", "10", "J", "Q", "K" };
    public List<Card> deck = new();

    public List<Player> players;
    public Dictionary<Card, GameObject> cardObjectMap = new();
    public GameManager gameClass;

    [Header("Spawn Animation Settings")]
    public float spawnAnimationSpread = 3.0f;
    public float spawnAnimationHeight = 4.0f;
    public float spawnAnimationUpDuration = 0.35f;
    public float spawnAnimationDownDuration = 0.5f;
    public float spawnAnimationCardDelay = 0.03f;
    public float spawnAnimationRotationIntensity = 90f;


    IEnumerator Start()
    {
        if (playersHandPosition == null || opponentsHandPosition == null)
        {
            Debug.LogError("Player's Hand or Opponent's Hand transform list is not assigned in the Inspector!");
            yield break;
        }
        if (playersHandPosition.Count == 0 || opponentsHandPosition.Count == 0)
        {
            Debug.LogError("Player's Hand or Opponent's Hand transform list is assigned but empty!");
            yield break;
        }
        if (deckTransform == null)
        {
            Debug.LogError("Deck Transform is not assigned in the Inspector!");
            yield break;
        }
        if (cardPrefab == null)
        {
            Debug.LogError("Card Prefab is not assigned in the Inspector!");
            yield break;
        }

        allPlayersHandsTransform = new List<List<Transform>> { opponentsHandPosition, playersHandPosition };

        Player human = new("You", "1234");
        Player computer = new("Computer", "5678");

        players = new List<Player> { computer, human };

        List<Card> initialDeckData = CreateDeck();
        deck = ShuffleDeck(initialDeckData);

        SpawnCardsInScene(deckTransform, deck);
        // audioSource.PlayOneShot(shuffleSound);
        yield return StartCoroutine(AnimateDeckSpawnCoroutine(cardObjectMap, deckTransform));

        var (dealtHandsData, remainingDeckData) = DealCards(players.Count, deck);
        deck = remainingDeckData;

        var index = 0;
        foreach (Player player in players)
        {
            bool showFace = player.id == human.id;
            player.hands = dealtHandsData[index];
            yield return StartCoroutine(AssignCardsToPositionsCoroutine(player.hands, allPlayersHandsTransform[index], showFace));
            index++;
        }
        yield return StartCoroutine(RepositionRemainingDeckCoroutine(remainingDeckData));
        gameClass.Initialize(players, deck);
        gameClass.canFold = true;
        if (gameClass.currentControl.id == gameClass.computerPlayer.id)
        {
            gameClass.StartComputerTurn();
        }
    }

    public IEnumerator StartSetup(int startIndex, string humanId)
    {
        if (playersHandPosition == null || opponentsHandPosition == null)
        {
            Debug.LogError("Player's Hand or Opponent's Hand transform list is not assigned in the Inspector!");
            yield break;
        }
        if (playersHandPosition.Count == 0 || opponentsHandPosition.Count == 0)
        {
            Debug.LogError("Player's Hand or Opponent's Hand transform list is assigned but empty!");
            yield break;
        }
        if (deckTransform == null)
        {
            Debug.LogError("Deck Transform is not assigned in the Inspector!");
            yield break;
        }
        if (cardPrefab == null)
        {
            Debug.LogError("Card Prefab is not assigned in the Inspector!");
            yield break;
        }

        bool needsShuffling = deck == null || deck.Count < players.Count * 5;

        if (needsShuffling)
        {
            // gameClass._allPlayedCardsInCurrentDeal.Clear();
            deck.Clear();
            DestroyAllCards();
            List<Card> initialDeckData = CreateDeck();
            deck = ShuffleDeck(initialDeckData);
            SpawnCardsInScene(deckTransform, deck);
            // audioSource.PlayOneShot(shuffleSound);
            yield return StartCoroutine(AnimateDeckSpawnCoroutine(cardObjectMap, deckTransform));
        }

        var (dealtHandsData, remainingDeckData) = DealCards(players.Count, deck);
        deck = remainingDeckData;

        for (int i = 0; i < players.Count; i++)
        {
            var index = (startIndex + i) % players.Count;
            bool showFace = players[index].id == humanId;
            players[index].hands = dealtHandsData[index];
            yield return StartCoroutine(AssignCardsToPositionsCoroutine(players[index].hands, allPlayersHandsTransform[index], showFace));
        }

        if (remainingDeckData.Count >= 22)
            yield return StartCoroutine(RepositionRemainingDeckCoroutine(remainingDeckData));
        gameClass.canFold = true;
        if (gameClass.currentControl.id == gameClass.computerPlayer.id)
        {
            gameClass.StartComputerTurn();
        }
    }

    #region Unchanged Methods
    private int GetRankValue(string rank)
    {
        return rank switch
        {
            "6" => 6,
            "7" => 7,
            "8" => 8,
            "9" => 9,
            "10" => 10,
            "J" => 11,
            "Q" => 12,
            "K" => 13,
            _ => 0,
        };
    }

    public List<Card> CreateDeck()
    {
        Sprite cardBack = Resources.Load<Sprite>("cardBack");
        List<Card> newDeck = new();
        foreach (string suit in suits)
        {
            foreach (string rank in ranks)
            {
                Card card = new() { suit = suit, rank = rank, value = GetRankValue(rank) };
                string spritePath = rank + "_" + suit;
                Sprite cardSprite = Resources.Load<Sprite>(spritePath);
                if (cardSprite != null)
                {
                    card.sprite = cardSprite;
                    card.cardBack = cardBack;
                    newDeck.Add(card);
                }
                else
                {
                    Debug.LogError($"Sprite not found: Resources/{spritePath}.");
                }
            }
        }
        return newDeck;
    }

    public List<Card> ShuffleDeck(List<Card> deckToShuffle)
    {
        System.Random rng = new(); int n = deckToShuffle.Count;
        while (n > 1)
        {
            n--; int k = rng.Next(n + 1);
            (deckToShuffle[k], deckToShuffle[n]) = (deckToShuffle[n], deckToShuffle[k]);
        }
        return deckToShuffle;
    }

    public void SpawnCardsInScene(Transform spawnParent, List<Card> cardsToSpawn)
    {
        foreach (Card card in cardsToSpawn)
        {
            // Instantiate at the deck's position initially
            GameObject go = Instantiate(cardPrefab, spawnParent.position, spawnParent.rotation, spawnParent);
            go.name = $"Card_{card.rank}_{card.suit}";

            SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
            if (sr != null) { sr.sprite = card.cardBack; }
            else { Debug.LogError($"Card Prefab '{cardPrefab.name}' missing SpriteRenderer.", cardPrefab); }

            CardUI ui = go.GetComponent<CardUI>();
            if (ui != null) { ui.cardData = card; }
            else { Debug.LogError($"Card Prefab '{cardPrefab.name}' missing CardUI.", cardPrefab); }

            if (go.GetComponent<Collider2D>() == null)
            {
                go.AddComponent<BoxCollider2D>();
                Debug.LogWarning($"Added BoxCollider2D to '{go.name}'.", go);
            }

            if (!cardObjectMap.ContainsKey(card))
            {
                cardObjectMap.Add(card, go);
            }
            else
            {
                Debug.LogWarning($"Duplicate card data: {card.rank} of {card.suit}. Destroying duplicate GO.", go);
                Destroy(go); continue;
            }
        }
    }

    public (List<List<Card>> dealtHands, List<Card> remainingDeck) DealCards(int totalPlayersHands, List<Card> currentDeck)
    {
        List<List<Card>> dealtHands = new();
        for (int i = 0; i < totalPlayersHands; i++) { dealtHands.Add(new List<Card>()); }

        int cardsInFirstRound = 3;
        int cardsInSecondRound = 2;

        // Deal First Round
        for (int i = 0; i < totalPlayersHands; i++)
        {
            for (int j = 0; j < cardsInFirstRound; j++)
            {
                if (currentDeck.Count > 0)
                {
                    Card cardToDeal = currentDeck[0];
                    dealtHands[i].Add(cardToDeal);
                    currentDeck.RemoveAt(0);
                }
                else
                {
                    Debug.LogWarning("Deck empty during first dealing round."); goto EndDeal;
                }
            }
        }

        // Deal Second Round
        for (int i = 0; i < totalPlayersHands; i++)
        {
            for (int j = 0; j < cardsInSecondRound; j++)
            {
                if (currentDeck.Count > 0)
                {
                    Card cardToDeal = currentDeck[0];
                    dealtHands[i].Add(cardToDeal);
                    currentDeck.RemoveAt(0);
                }
                else
                {
                    Debug.LogWarning("Deck empty during second dealing round."); goto EndDeal;
                }
            }
        }

    EndDeal:;
        return (dealtHands, currentDeck);
    }
    #endregion

    public IEnumerator AssignCardsToPositionsCoroutine(List<Card> dealtHand, List<Transform> transform, bool showFace = false)
    {
        if (dealtHand.Count != transform.Count)
        {
            Debug.LogError("Mismatch between dealt hands and hand transform lists!");
            yield break;
        }
        for (int i = 0; i < dealtHand.Count; i++)
        {
            Card cardData = dealtHand[i];
            Transform targetTransform = transform[i];
            if (cardObjectMap.TryGetValue(cardData, out GameObject cardGO))
            {
                if (showFace)
                {
                    SpriteRenderer sr = cardGO.GetComponent<SpriteRenderer>();
                    if (sr != null) { sr.sprite = cardData.sprite; }
                    cardGO.GetComponent<CardUI>().isClickable = true;
                }
                StartCoroutine(MoveCardCoroutine(cardGO, targetTransform.position, targetTransform.rotation, moveDuration, "Cards", i));
                audioSource.PlayOneShot(dealSound);
                yield return new WaitForSeconds(dealDelay);
            }
            else
            {
                Debug.LogError($"Failed to find GameObject for dealt card: {cardData.rank} of {cardData.suit}.", this);
            }
        }
    }

    IEnumerator RepositionRemainingDeckCoroutine(List<Card> remainingCards)
    {
        float stackOffsetZ = 0.1f;
        int index = 0;

        if (deckTransform == null)
        {
            Debug.LogError("Cannot reposition remaining deck, deckTransform is null!");
            yield break;
        }

        foreach (Card cardData in remainingCards)
        {
            if (cardObjectMap.TryGetValue(cardData, out GameObject cardGO))
            {
                Vector3 targetPosition = deckTransform.position + new Vector3(-20 + index * stackOffsetZ, 0, -index * stackOffsetZ);
                Quaternion targetRotation = deckTransform.rotation;

                yield return StartCoroutine(MoveCardCoroutine(cardGO, targetPosition, targetRotation, moveDuration / 5, "Deck", index));
                audioSource.PlayOneShot(dealSound);
                cardGO.transform.SetParent(deckTransform);

                SpriteRenderer sr = cardGO.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.sortingLayerName = "Deck";
                    sr.sortingOrder = index;
                }
            }
            else
            {
                Debug.LogError($"Could not find GameObject in map for remaining deck card: {cardData.rank} of {cardData.suit}", this);
            }
            index++;

        }
    }


    IEnumerator MoveCardCoroutine(GameObject cardToMove, Vector3 targetPosition, Quaternion targetRotation, float duration, string targetSortingLayer, int targetSortingOrder)
    {
        Vector3 startPosition = cardToMove.transform.position;
        Quaternion startRotation = cardToMove.transform.rotation;
        float elapsedTime = 0f;

        SpriteRenderer sr = cardToMove.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.sortingLayerName = "MovingCard";
            sr.sortingOrder = 100;
        }


        while (elapsedTime < duration)
        {
            if (cardToMove == null) yield break;

            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration);
            t = Mathf.SmoothStep(0f, 1f, t);

            cardToMove.transform.position = Vector3.Lerp(startPosition, targetPosition, t);
            cardToMove.transform.rotation = Quaternion.Lerp(startRotation, targetRotation, t);

            yield return null; // Wait for the next frame
        }

        // Ensure final position/rotation is exact & reset sorting/parenting
        if (cardToMove != null)
        {
            cardToMove.transform.position = targetPosition;
            cardToMove.transform.rotation = targetRotation;

            if (sr != null && !string.IsNullOrEmpty(targetSortingLayer))
            {
                sr.sortingLayerName = targetSortingLayer;
                sr.sortingOrder = targetSortingOrder;
            }
        }
    }

    /// <summary>
    /// Initiates a visual flourish animation for all spawned cards.
    /// Cards fly up to random positions and then return to the deck spawn point.
    /// </summary>
    /// <param name="cardsToAnimate">The dictionary mapping Card data to their GameObjects.</param>
    /// <param name="deckSpawnPoint">The transform representing the deck's position and rotation.</param>
    public IEnumerator AnimateDeckSpawnCoroutine(Dictionary<Card, GameObject> cardsToAnimate, Transform deckSpawnPoint)
    {
        if (deckSpawnPoint == null)
        {
            Debug.LogError("Deck Spawn Point is null for animation.");
            yield break;
        }
        if (cardsToAnimate == null || cardsToAnimate.Count == 0)
        {
            Debug.LogWarning("No cards provided for spawn animation.");
            yield break;
        }

        // Create a list of GameObjects to animate, shuffle for randomness
        List<GameObject> cardGOs = new(cardsToAnimate.Values);
        System.Random rng = new();
        int n = cardGOs.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            // Swap elements
            (cardGOs[k], cardGOs[n]) = (cardGOs[n], cardGOs[k]);
        }


        // Start the animation for each card with a slight delay
        for (int i = 0; i < cardGOs.Count; i++)
        {
            GameObject cardGO = cardGOs[i];
            if (cardGO != null)
            {
                // Start the individual card's up-and-down animation coroutine.
                // We don't wait for this inner coroutine to finish here, just trigger it.
                StartCoroutine(AnimateSingleCardSpawnFlourish(cardGO, deckSpawnPoint.position, deckSpawnPoint.rotation));

                // Wait a short time before starting the next card's animation
                yield return new WaitForSeconds(spawnAnimationCardDelay);
            }
        }

        // Calculate a reasonable time to wait for the *last* card's animation to likely finish.
        // This ensures the game doesn't proceed before the visual flourish is mostly done.
        float approxWaitTime = spawnAnimationUpDuration + spawnAnimationDownDuration;
        yield return new WaitForSeconds(approxWaitTime); // Wait for the duration of one full animation after the last one starts
    }

    /// <summary>
    /// Helper coroutine to animate a single card flying up to a random position and returning.
    /// </summary>
    private IEnumerator AnimateSingleCardSpawnFlourish(GameObject cardGO, Vector3 finalPosition, Quaternion finalRotation)
    {
        if (cardGO == null) yield break; // Safety check

        Vector3 startPosition = cardGO.transform.position; // Should be the deck position where it was spawned
        Quaternion startRotation = cardGO.transform.rotation; // Should be the deck rotation

        // --- Calculate random peak position and rotation ---
        Vector2 randomHorizontalOffset = Random.insideUnitCircle * spawnAnimationSpread;
        Vector3 peakPosition = startPosition + new Vector3(randomHorizontalOffset.x, spawnAnimationHeight, randomHorizontalOffset.y); // Use Y for height, X/Z for spread relative to deck orientation
        // More dramatic random rotation
        Quaternion peakRotation = startRotation * Quaternion.Euler(
            Random.Range(-spawnAnimationRotationIntensity, spawnAnimationRotationIntensity),
            Random.Range(-spawnAnimationRotationIntensity, spawnAnimationRotationIntensity),
            Random.Range(-spawnAnimationRotationIntensity, spawnAnimationRotationIntensity)
        );

        // --- Setup Sorting for Animation ---
        SpriteRenderer sr = cardGO.GetComponent<SpriteRenderer>();
        int originalOrder = 0;
        string originalLayer = "Default"; // Assuming cards start on Default layer after spawn
        if (sr != null)
        {
            originalOrder = sr.sortingOrder;
            originalLayer = sr.sortingLayerName;
            // Use a high sorting order on a layer rendered above others during movement
            sr.sortingLayerName = "MovingCard"; // Ensure this layer exists in Project Settings > Tags and Layers > Sorting Layers
            sr.sortingOrder = 150 + Random.Range(0, 50); // High base order + random offset
        }

        // --- Animate Up ---
        float elapsedTime = 0f;
        while (elapsedTime < spawnAnimationUpDuration)
        {
            if (cardGO == null) yield break; // Object might be destroyed
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / spawnAnimationUpDuration);
            t = Mathf.SmoothStep(0f, 1f, t); // Ease in/out

            cardGO.transform.position = Vector3.Lerp(startPosition, peakPosition, t);
            cardGO.transform.rotation = Quaternion.Lerp(startRotation, peakRotation, t);
            yield return null;
        }
        // Ensure it reaches the peak (important if duration is very short or frame rate is low)
        if (cardGO == null) yield break;
        cardGO.transform.position = peakPosition;
        cardGO.transform.rotation = peakRotation;

        // --- Animate Down ---
        elapsedTime = 0f; // Reset timer for the down movement
        Vector3 currentPosition = peakPosition; // Starting position for down move is the peak
        Quaternion currentRotation = peakRotation;

        while (elapsedTime < spawnAnimationDownDuration)
        {
            if (cardGO == null) yield break;
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / spawnAnimationDownDuration);
            // Apply a different easing for the return, e.g., ease out (starts faster, slows down at end)
            t = 1f - Mathf.Pow(1f - t, 3); // Ease-out cubic

            cardGO.transform.position = Vector3.Lerp(currentPosition, finalPosition, t);
            cardGO.transform.rotation = Quaternion.Lerp(currentRotation, finalRotation, t);
            yield return null;
        }

        if (cardGO == null) yield break; // Final check

        // --- Finalize Position, Rotation, and Sorting ---
        cardGO.transform.position = finalPosition;
        cardGO.transform.rotation = finalRotation;

        if (sr != null)
        {
            // Reset sorting to original, or set to a default "Deck" state if preferred.
            // Using originalLayer/Order allows flexibility if cards were spawned with specific sorting initially.
            // You might want to explicitly set it to "Deck" layer here if RepositionRemainingDeckCoroutine doesn't run immediately after.
            sr.sortingLayerName = originalLayer; // Reset to whatever it was before animation
            sr.sortingOrder = originalOrder;     // Reset order
            // Alternatively, if you have a dedicated "Deck" layer:
            // sr.sortingLayerName = "Deck";
            // sr.sortingOrder = 0; // Or some base order for cards in the deck pile
        }
    }

    public void ResetGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    public void DestroyAllCards()
    {
        foreach (var cardObject in cardObjectMap.Values)
        {
            if (cardObject != null)
            {
                Destroy(cardObject);
            }
        }

        cardObjectMap.Clear();
    }
}
