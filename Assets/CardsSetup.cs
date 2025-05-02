using UnityEngine;
using System.Collections.Generic;

public class CardsSetup : MonoBehaviour
{
    [Header("Hand Positions")]
    [Tooltip("List of Transforms representing card positions in the player's hand.")]
    public List<Transform> playersHand;
    [Tooltip("List of Transforms representing card positions in the opponent's hand.")]
    public List<Transform> opponentsHand;

    public Transform playSpot;
    public float moveSpeed = 5f;

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

    void Start()
    {
        if (playersHand == null || opponentsHand == null)
        {
            Debug.LogError("Player's Hand or Opponent's Hand transform list is not assigned in the Inspector!");
            return;
        }
        if (playersHand.Count == 0 || opponentsHand.Count == 0)
        {
            Debug.LogError("Player's Hand or Opponent's Hand transform list is assigned but empty!");
            return;
        }
        if (deckTransform == null)
        {
            Debug.LogError("Deck Transform is not assigned in the Inspector!");
            return;
        }
        if (cardPrefab == null)
        {
            Debug.LogError("Card Prefab is not assigned in the Inspector!");
            return;
        }

        allPlayersHands = new List<List<Transform>> { playersHand, opponentsHand };

        // 1. Create & Shuffle Deck Data
        List<Card> initialDeckData = CreateDeck();
        deck = ShuffleDeck(initialDeckData);

        // 2. Spawn ALL Card GameObjects based on the full shuffled deck and map them
        SpawnCardsInScene(deckTransform, deck); // Spawns GOs, populates cardObjectMap

        // 3. Deal Card Data
        int numberOfPlayers = allPlayersHands.Count;
        // Pass 'deck' directly. DealCards will remove cards from it, leaving 'deck' as the remaining cards.
        var (dealtHandsData, remainingDeckData) = DealCards(numberOfPlayers, deck);
        // Note: 'deck' and 'remainingDeckData' now reference the same list object.

        Debug.Log($"Dealt cards. Player Hand Count: {dealtHandsData[0].Count}, Opponent Hand Count: {dealtHandsData[1].Count}, Remaining Deck: {remainingDeckData.Count}");

        // 4. Assign dealt GameObjects to Hand Positions using the map
        AssignCardsToPositions(dealtHandsData);

        // 5. Handle the visual representation of the remaining deck
        RepositionRemainingDeck(remainingDeckData);
    }

    private int GetRankValue(string rank)
    {
        // Using a switch expression for conciseness
        return rank switch
        {
            "6" => 6,
            "7" => 7,
            "8" => 8,
            "9" => 9,
            "10" => 10,
            "J" => 11, // Assuming Jack is 11 for value
            "Q" => 12, // Assuming Queen is 12
            "K" => 13, // Assuming King is 13
            _ => 0,    // Default case, should not happen with defined ranks
        };
    }

    public List<Card> CreateDeck()
    {
        List<Card> newDeck = new();
        foreach (string suit in suits)
        {
            foreach (string rank in ranks)
            {
                Card card = new()
                {
                    suit = suit,
                    rank = rank,
                    value = GetRankValue(rank)
                };

                string spritePath = rank + "_" + suit;
                Sprite cardSprite = Resources.Load<Sprite>(spritePath);

                if (cardSprite != null)
                {
                    card.sprite = cardSprite;
                    newDeck.Add(card);
                }
                else
                {
                    Debug.LogError($"Sprite not found at path: Resources/{spritePath}. Make sure sprites exist and path is correct.");
                }
            }
        }

        Debug.Log("Created deck data with " + newDeck.Count + " cards.");
        return newDeck;
    }

    public List<Card> ShuffleDeck(List<Card> deckToShuffle)
    {
        System.Random rng = new();
        int n = deckToShuffle.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            // Swap elements
            (deckToShuffle[k], deckToShuffle[n]) = (deckToShuffle[n], deckToShuffle[k]);
        }
        Debug.Log("Deck data shuffled.");
        return deckToShuffle;
    }

    public void SpawnCardsInScene(Transform spawnParent, List<Card> cardsToSpawn)
    {
        int index = 0;

        foreach (Card card in cardsToSpawn)
        {
            GameObject go = Instantiate(cardPrefab, spawnParent.position, spawnParent.rotation, spawnParent); // Instantiate at parent's pos/rot
            go.name = $"Card_{card.rank}_{card.suit}"; // Give meaningful name in hierarchy

            SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sprite = card.sprite;
            }
            else
            {
                Debug.LogError($"Card Prefab '{cardPrefab.name}' is missing a SpriteRenderer component.", cardPrefab);
            }


            CardUI ui = go.GetComponent<CardUI>();
            if (ui != null)
            {
                ui.cardData = card;
                // ui.handIndex will be set later when assigned to a hand
            }
            else
            {
                Debug.LogError($"Card Prefab '{cardPrefab.name}' is missing a CardUI component.", cardPrefab);
            }

            if (go.GetComponent<Collider2D>() == null)
            {
                go.AddComponent<BoxCollider2D>();
                Debug.LogWarning($"Added BoxCollider2D to spawned card '{go.name}' as none was found.", go);
            }

            // *** Add the card data and its GameObject to the map ***
            if (!cardObjectMap.ContainsKey(card))
            {
                cardObjectMap.Add(card, go);
            }
            else
            {
                Debug.LogWarning($"Duplicate card data detected when adding to map: {card.rank} of {card.suit}. Destroying duplicate GameObject.", go);
                Destroy(go); // Destroy the newly created duplicate GameObject
                continue; // Skip next steps for this duplicate card
            }
            index++;
        }
        Debug.Log($"Spawned {cardObjectMap.Count} card GameObjects.");
    }

    public (List<List<Card>> dealtHands, List<Card> remainingDeck) DealCards(int totalPlayersHands, List<Card> currentDeck)
    {
        List<List<Card>> dealtHands = new();
        for (int i = 0; i < totalPlayersHands; i++)
        {
            dealtHands.Add(new List<Card>());
        }

        int cardsToDealPerPlayer = 5;
        int cardsInFirstRound = 3;
        int cardsInSecondRound = 2;

        // Deal First Round (e.g., 3 cards)
        for (int i = 0; i < totalPlayersHands; i++) // Deal one card at a time to each player
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
                    Debug.LogWarning("Deck ran out of cards during first dealing round.");
                    break; // Exit inner loop if deck is empty
                }
            }
            if (currentDeck.Count == 0) break; // Exit outer loop if deck is empty
        }

        // Deal Second Round (e.g., 2 cards)
        for (int i = 0; i < totalPlayersHands; i++) // Deal one card at a time to each player
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
                    Debug.LogWarning("Deck ran out of cards during second dealing round.");
                    break; // Exit inner loop if deck is empty
                }
            }
            if (currentDeck.Count == 0) break; // Exit outer loop if deck is empty
        }


        Debug.Log($"Dealing complete. {totalPlayersHands} hands dealt with {cardsToDealPerPlayer} cards each (approx if deck ran out).");
        // The input 'currentDeck' list is now the remaining deck
        return (dealtHands, currentDeck);
    }

    // Moves the GameObjects associated with dealt cards to their hand positions
    public void AssignCardsToPositions(List<List<Card>> dealtHands)
    {
        if (dealtHands.Count != allPlayersHands.Count)
        {
            Debug.LogError("Mismatch between number of dealt hands and available hand transform lists!");
            return;
        }

        // Loop through each hand that was dealt
        for (int handIndex = 0; handIndex < dealtHands.Count; handIndex++)
        {
            List<Card> currentHandCards = dealtHands[handIndex];
            List<Transform> currentHandTransforms = allPlayersHands[handIndex];

            if (currentHandCards.Count > currentHandTransforms.Count)
            {
                Debug.LogWarning($"Hand {handIndex} has more cards ({currentHandCards.Count}) than available positions ({currentHandTransforms.Count}). Some cards may overlap or not be positioned correctly.");
            }

            // Loop through the cards within the current hand
            for (int cardIndex = 0; cardIndex < currentHandCards.Count; cardIndex++)
            {
                // Check if there's a transform position available for this card index
                if (cardIndex >= currentHandTransforms.Count)
                {
                    Debug.LogWarning($"Ran out of defined positions for hand {handIndex} at card index {cardIndex}. Card '{currentHandCards[cardIndex].rank}_{currentHandCards[cardIndex].suit}' will not be moved to a specific hand slot.", this);
                    continue; // Continue to the next card, maybe it finds a GO but doesn't move it to a hand slot
                }

                Card cardData = currentHandCards[cardIndex];
                Transform targetTransform = currentHandTransforms[cardIndex]; // Get the target position/transform marker

                // Look up the GameObject using the Card data
                if (cardObjectMap.TryGetValue(cardData, out GameObject cardGO))
                {
                    // Move the GameObject to the target transform's position and rotation
                    cardGO.transform.position = targetTransform.position;
                    cardGO.transform.rotation = targetTransform.rotation;

                    CardUI ui = cardGO.GetComponent<CardUI>();
                    if (ui != null) ui.handIndex = cardIndex;

                    SpriteRenderer sr = cardGO.GetComponent<SpriteRenderer>();
                    if (sr != null)
                    {
                        sr.sortingLayerName = "Cards"; // Make sure this layer exists
                        sr.sortingOrder = cardIndex;
                    }
                }
                else
                {
                    // This indicates a logic error somewhere (card dealt but no GameObject mapped)
                    Debug.LogError($"Failed to find GameObject in map for card: {cardData.rank} of {cardData.suit}. Was it spawned correctly?", this);
                }
            }
        }
        Debug.Log("Finished assigning dealt card positions.");
    }

    // Move the GameObjects for the cards still in the deck back to the deck pile visual
    void RepositionRemainingDeck(List<Card> remainingCards)
    {
        float stackOffsetZ = 0.1f; // Very small offset for visual stacking depth
        int index = 0;

        if (deckTransform == null)
        {
            Debug.LogError("Cannot reposition remaining deck, deckTransform is null!");
            return;
        }

        foreach (Card cardData in remainingCards)
        {
            if (cardObjectMap.TryGetValue(cardData, out GameObject cardGO))
            {
                // Move to deck position with slight offset for stacking
                cardGO.transform.position = deckTransform.position + new Vector3(-20 + index * stackOffsetZ, 0, -index * stackOffsetZ);
                cardGO.transform.rotation = deckTransform.rotation; // Match deck rotation
                cardGO.transform.SetParent(deckTransform); // Parent them back to the deck transform

                SpriteRenderer sr = cardGO.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.sortingLayerName = "Deck"; // Example layer
                    sr.sortingOrder = index;      // Stack order
                }
            }
            else
            {
                Debug.LogError($"Could not find GameObject in map for remaining deck card: {cardData.rank} of {cardData.suit}", this);
            }
            index++;
        }
        Debug.Log($"Repositioned {index} remaining card GameObjects to the deck area.");
    }
}
