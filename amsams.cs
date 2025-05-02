using UnityEngine;
using System.Collections; // Required for Coroutines
using System.Collections.Generic;

public class CardsSetup : MonoBehaviour
{
    [Header("Hand Positions")]
    [Tooltip("List of Transforms representing card positions in the player's hand.")]
    public List<Transform> playersHand;
    [Tooltip("List of Transforms representing card positions in the opponent's hand.")]
    public List<Transform> opponentsHand;

    public Transform playSpot; // Keep if used elsewhere, otherwise can be removed if only for dealing animation start

    [Header("Animation Settings")]
    [Tooltip("How fast the cards move to their positions.")]
    public float moveDuration = 0.5f; // Duration of the card movement animation
    [Tooltip("Delay between dealing each card visually.")]
    public float dealDelay = 0.1f; // Stagger the start of each card's movement

    [HideInInspector] // Hide from inspector as it's set in Start
    public List<List<Transform>> allPlayersHands;

    [Header("Deck Setup")]
    [Tooltip("Transform where the deck pile is located.")]
    public Transform deckTransform;
    [Tooltip("The prefab to use for instantiating cards.")]
    public GameObject cardPrefab;

    // Deck data
    private readonly string[] suits = { "diamond", "spade", "heart", "club" };
    private readonly string[] ranks = { "6", "7", "8", "9", "10", "J", "Q", "K" };
    public List<Card> deck = new();

    private readonly Dictionary<Card, GameObject> cardObjectMap = new();

    // Make Start a Coroutine to allow waiting for animations
    IEnumerator Start()
    {
        if (playersHand == null || opponentsHand == null)
        {
            Debug.LogError("Player's Hand or Opponent's Hand transform list is not assigned in the Inspector!");
            yield break; // Stop execution if setup is invalid
        }
        if (playersHand.Count == 0 || opponentsHand.Count == 0)
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

        allPlayersHands = new List<List<Transform>> { playersHand, opponentsHand };

        // 1. Create & Shuffle Deck Data
        List<Card> initialDeckData = CreateDeck();
        deck = ShuffleDeck(initialDeckData);

        // 2. Spawn ALL Card GameObjects based on the full shuffled deck and map them
        // Cards are initially spawned at the deck position
        SpawnCardsInScene(deckTransform, deck);

        // 3. Deal Card Data
        int numberOfPlayers = allPlayersHands.Count;
        var (dealtHandsData, remainingDeckData) = DealCards(numberOfPlayers, deck);

        Debug.Log($"Dealt cards. Player Hand Count: {dealtHandsData[0].Count}, Opponent Hand Count: {dealtHandsData[1].Count}, Remaining Deck: {remainingDeckData.Count}");

        // --- Animation Phase ---
        // 4. Animate dealt GameObjects to Hand Positions using the map
        // We wait for this coroutine to finish before proceeding
        yield return StartCoroutine(AssignCardsToPositionsCoroutine(dealtHandsData));

        // 5. Animate the visual representation of the remaining deck back to the pile
        // We wait for this coroutine to finish as well
        yield return StartCoroutine(RepositionRemainingDeckCoroutine(remainingDeckData));

        Debug.Log("Card setup and animations complete.");
    }

    // --- Existing Methods (CreateDeck, ShuffleDeck, GetRankValue, SpawnCardsInScene, DealCards) remain unchanged ---
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
                    card.sprite = cardSprite; newDeck.Add(card);
                }
                else
                {
                    Debug.LogError($"Sprite not found: Resources/{spritePath}.");
                }
            }
        }
        Debug.Log("Created deck data with " + newDeck.Count + " cards.");
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
        Debug.Log("Deck data shuffled.");
        return deckToShuffle;
    }

    public void SpawnCardsInScene(Transform spawnParent, List<Card> cardsToSpawn)
    {
        // int index = 0; // No longer needed here for positioning
        foreach (Card card in cardsToSpawn)
        {
            // Instantiate at the deck's position initially
            GameObject go = Instantiate(cardPrefab, spawnParent.position, spawnParent.rotation, spawnParent);
            go.name = $"Card_{card.rank}_{card.suit}";

            SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
            if (sr != null) { sr.sprite = card.sprite; }
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
            // index++; // No longer needed
        }
        Debug.Log($"Spawned {cardObjectMap.Count} card GameObjects at Deck position.");
    }

    public (List<List<Card>> dealtHands, List<Card> remainingDeck) DealCards(int totalPlayersHands, List<Card> currentDeck)
    {
        List<List<Card>> dealtHands = new();
        for (int i = 0; i < totalPlayersHands; i++) { dealtHands.Add(new List<Card>()); }

        int cardsToDealPerPlayer = 5; // Total cards per player
        int cardsInFirstRound = 3;
        int cardsInSecondRound = 2;

        // Deal First Round
        for (int j = 0; j < cardsInFirstRound; j++)
        { // Card index within round
            for (int i = 0; i < totalPlayersHands; i++)
            { // Player index
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
        for (int j = 0; j < cardsInSecondRound; j++)
        { // Card index within round
            for (int i = 0; i < totalPlayersHands; i++)
            { // Player index
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
        Debug.Log($"Dealing complete. {totalPlayersHands} hands dealt. (~{cardsToDealPerPlayer} cards each).");
        return (dealtHands, currentDeck);
    }
    #endregion

    // --- Coroutine to handle assigning cards to hand positions ---
    public IEnumerator AssignCardsToPositionsCoroutine(List<List<Card>> dealtHands)
    {
        if (dealtHands.Count != allPlayersHands.Count)
        {
            Debug.LogError("Mismatch between dealt hands and hand transform lists!");
            yield break; // Exit coroutine
        }

        int totalCardsDealt = 0;
        foreach (var hand in dealtHands) totalCardsDealt += hand.Count;
        int cardsAnimated = 0;

        // We need to deal cards one by one to each player, like in the DealCards logic,
        // to get the correct visual dealing order.
        int maxCardsInAnyHand = 0;
        foreach (var hand in dealtHands)
        {
            if (hand.Count > maxCardsInAnyHand) maxCardsInAnyHand = hand.Count;
        }

        for (int cardDealIndex = 0; cardDealIndex < maxCardsInAnyHand; cardDealIndex++)
        {
            for (int handIndex = 0; handIndex < dealtHands.Count; handIndex++)
            {
                // Check if this hand actually received a card in this 'round' of dealing
                if (cardDealIndex < dealtHands[handIndex].Count)
                {
                    List<Card> currentHandCards = dealtHands[handIndex];
                    List<Transform> currentHandTransforms = allPlayersHands[handIndex];

                    // Get the specific card dealt in this iteration
                    Card cardData = currentHandCards[cardDealIndex];

                    // Ensure there is a target transform defined for this card index
                    if (cardDealIndex >= currentHandTransforms.Count)
                    {
                        Debug.LogWarning($"Ran out of defined positions for hand {handIndex} at card index {cardDealIndex}. Card '{cardData.rank}_{cardData.suit}' will not be animated to a hand slot.", this);
                        continue; // Skip animation for this card if no slot
                    }

                    Transform targetTransform = currentHandTransforms[cardDealIndex];

                    if (cardObjectMap.TryGetValue(cardData, out GameObject cardGO))
                    {
                        // --- Start the movement coroutine for this specific card ---
                        StartCoroutine(MoveCardCoroutine(cardGO, targetTransform.position, targetTransform.rotation, moveDuration, "Cards", cardDealIndex));

                        // Update CardUI handIndex immediately (doesn't need animation)
                        CardUI ui = cardGO.GetComponent<CardUI>();
                        if (ui != null) ui.handIndex = cardDealIndex;

                        cardsAnimated++;

                        // --- Wait before dealing the next card ---
                        yield return new WaitForSeconds(dealDelay);
                    }
                    else
                    {
                        Debug.LogError($"Failed to find GameObject for dealt card: {cardData.rank} of {cardData.suit}.", this);
                    }
                }
            }
        }

        // Optional: Wait for the last card's animation to finish if needed elsewhere
        // float remainingWait = (totalCardsDealt - cardsAnimated) * dealDelay + moveDuration;
        // yield return new WaitForSeconds(remainingWait > 0 ? remainingWait : 0);

        Debug.Log($"Finished starting animations for {cardsAnimated} dealt cards.");
    }


    // --- Coroutine to handle repositioning remaining deck cards ---
    IEnumerator RepositionRemainingDeckCoroutine(List<Card> remainingCards)
    {
        float stackOffsetZ = 0.03f; // Smaller offset for tighter stacking
        int index = 0;

        if (deckTransform == null)
        {
            Debug.LogError("Cannot reposition remaining deck, deckTransform is null!");
            yield break;
        }

        Debug.Log($"Starting repositioning animation for {remainingCards.Count} remaining deck cards.");

        // Animate remaining cards back to the deck pile visually
        foreach (Card cardData in remainingCards)
        {
            if (cardObjectMap.TryGetValue(cardData, out GameObject cardGO))
            {
                // Calculate target position with stacking offset
                Vector3 targetPosition = deckTransform.position + new Vector3(0, 0, -index * stackOffsetZ); // Stack along Z
                Quaternion targetRotation = deckTransform.rotation;

                // --- Start the movement coroutine ---
                // We pass null for sorting layer/order initially, will set parenting/sorting after move.
                yield return StartCoroutine(MoveCardCoroutine(cardGO, targetPosition, targetRotation, moveDuration, null, 0)); // Don't need sorting order during move

                // --- After movement, set parent and sorting ---
                cardGO.transform.SetParent(deckTransform); // Parent them back to the deck transform *after* moving

                SpriteRenderer sr = cardGO.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.sortingLayerName = "Deck"; // Set final layer
                    sr.sortingOrder = index;      // Set final stack order
                }

                // Optional small delay between repositioning animations if desired
                // yield return new WaitForSeconds(0.02f);
            }
            else
            {
                Debug.LogError($"Could not find GO for remaining deck card: {cardData.rank} of {cardData.suit}", this);
            }
            index++;
        }
        Debug.Log($"Finished animations for {index} remaining deck cards.");
    }


    // --- Generic Coroutine to move a single card ---
    IEnumerator MoveCardCoroutine(GameObject cardToMove, Vector3 targetPosition, Quaternion targetRotation, float duration, string targetSortingLayer, int targetSortingOrder)
    {
        Vector3 startPosition = cardToMove.transform.position;
        Quaternion startRotation = cardToMove.transform.rotation;
        float elapsedTime = 0f;

        // Detach temporarily if parented, to avoid weird movements if parent moves (like deck)
        // Transform originalParent = cardToMove.transform.parent;
        // cardToMove.transform.SetParent(null); // Move in world space

        SpriteRenderer sr = cardToMove.GetComponent<SpriteRenderer>();
        // Optionally set a "Moving" layer/order during transit
        if (sr != null)
        {
            sr.sortingLayerName = "MovingCard"; // Ensure this layer exists and is above others
            sr.sortingOrder = 100; // High number to be on top while moving
        }


        while (elapsedTime < duration)
        {
            // Prevent errors if the object is destroyed mid-animation
            if (cardToMove == null) yield break;

            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration);
            t = Mathf.SmoothStep(0f, 1f, t); // Apply easing for smoother start/end

            cardToMove.transform.position = Vector3.Lerp(startPosition, targetPosition, t);
            cardToMove.transform.rotation = Quaternion.Lerp(startRotation, targetRotation, t);

            yield return null; // Wait for the next frame
        }

        // Ensure final position/rotation is exact & reset sorting/parenting
        if (cardToMove != null)
        {
            cardToMove.transform.position = targetPosition;
            cardToMove.transform.rotation = targetRotation;
            // cardToMove.transform.SetParent(originalParent); // Re-attach if detached (handle in calling coroutine instead)

            // Set the final sorting order passed into the coroutine
            if (sr != null && !string.IsNullOrEmpty(targetSortingLayer))
            {
                sr.sortingLayerName = targetSortingLayer;
                sr.sortingOrder = targetSortingOrder;
            }
        }
    }
}
