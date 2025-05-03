using UnityEngine;
using System.Collections; // Required for Coroutines
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class CardsSetup : MonoBehaviour
{
    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip dealSound;

    [Header("Hand Positions")]
    public List<Transform> playersHand;
    public List<Transform> opponentsHand;

    public Transform playSpot;
    public Transform playSpot2;
    public float moveSpeed = 50f;

    [Header("Animation Settings")]
    [Tooltip("How fast the cards move to their positions.")]
    public float moveDuration = 0.5f;
    [Tooltip("Delay between dealing each card visually.")]
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
    private readonly Dictionary<Card, GameObject> cardObjectMap = new();

    IEnumerator Start()
    {
        if (playersHand == null || opponentsHand == null)
        {
            Debug.LogError("Player's Hand or Opponent's Hand transform list is not assigned in the Inspector!");
            yield break;
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

        allPlayersHandsTransform = new List<List<Transform>> { playersHand, opponentsHand };

        List<Card> initialDeckData = CreateDeck();
        deck = ShuffleDeck(initialDeckData);

        SpawnCardsInScene(deckTransform, deck);

        int numberOfPlayers = allPlayersHandsTransform.Count;
        var (dealtHandsData, remainingDeckData) = DealCards(numberOfPlayers, deck);

        Player human = new("You", "1234");
        Player computer = new("Computer", "5678");
        players = new List<Player> { human, computer };
        var index = 0;
        foreach (Player player in players)
        {
            player.hands = dealtHandsData[index];
            index++;
        }

        Debug.Log($"Dealt cards. Player Hand Count: {dealtHandsData[0].Count}, Opponent Hand Count: {dealtHandsData[1].Count}, Remaining Deck: {remainingDeckData.Count}");

        yield return StartCoroutine(AssignCardsToPositionsCoroutine(dealtHandsData));

        yield return StartCoroutine(RepositionRemainingDeckCoroutine(remainingDeckData));

        Debug.Log("Card setup and animations complete.");
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
        Debug.Log($"Dealing complete. {totalPlayersHands} hands dealt. (~{cardsToDealPerPlayer} cards each).");
        return (dealtHands, currentDeck);
    }
    #endregion

    // Moves the GameObjects associated with dealt cards to their hand positions
    public IEnumerator AssignCardsToPositionsCoroutine(List<List<Card>> dealtHands)
    {
        if (dealtHands.Count != allPlayersHandsTransform.Count)
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
                    List<Transform> currentHandTransforms = allPlayersHandsTransform[handIndex];

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
                        audioSource.PlayOneShot(dealSound);

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
        Debug.Log($"Finished starting animations for {cardsAnimated} dealt cards.");
    }

    // Move the GameObjects for the cards still in the deck back to the deck pile visual
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
                Vector3 targetPosition = deckTransform.position + new Vector3(-20, 0, -index * stackOffsetZ);
                Quaternion targetRotation = deckTransform.rotation;

                yield return StartCoroutine(MoveCardCoroutine(cardGO, targetPosition, targetRotation, moveDuration / 5, null, 0));
                audioSource.PlayOneShot(dealSound);
                // cardGO.transform.position = deckTransform.position + new Vector3(-20 + index * stackOffsetZ, 0, -index * stackOffsetZ);
                // cardGO.transform.rotation = deckTransform.rotation; // Match deck rotation
                cardGO.transform.SetParent(deckTransform); // Parent them back to the deck transform

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
        Debug.Log($"Finished animations for {index} remaining deck cards.");
    }


    // --- Generic Coroutine to move a single card ---
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
            // Prevent errors if the object is destroyed mid-animation
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
    public void ResetGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
