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
    public List<Transform> playersHandPosition;
    public List<Transform> opponentsHandPosition;

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
    public Dictionary<Card, GameObject> cardObjectMap = new();
    public GameManager gameClass;

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

        List<Card> initialDeckData = CreateDeck();
        deck = ShuffleDeck(initialDeckData);

        SpawnCardsInScene(deckTransform, deck);

        int numberOfPlayers = allPlayersHandsTransform.Count;
        var (dealtHandsData, remainingDeckData) = DealCards(numberOfPlayers, deck);
        deck = remainingDeckData;

        Player human = new("You", "1234");
        Player computer = new("Computer", "5678");

        players = new List<Player> { computer, human };

        var index = 0;
        foreach (Player player in players)
        {
            player.hands = dealtHandsData[index];
            yield return StartCoroutine(AssignCardsToPositionsCoroutine(player.hands, allPlayersHandsTransform[index]));
            index++;
        }

        Debug.Log($"Dealt cards. Player Hand Count: {dealtHandsData[0].Count}, Opponent Hand Count: {dealtHandsData[1].Count}, Remaining Deck: {remainingDeckData.Count}");

        // yield return StartCoroutine(AssignCardsToPositionsCoroutine(dealtHandsData));

        yield return StartCoroutine(RepositionRemainingDeckCoroutine(remainingDeckData));
        gameClass.Initialize(players, deck);

        Debug.Log("Card setup and animations complete.");
    }

    public IEnumerator StartSetup()
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

        if (deck == null || deck.Count < players.Count * 5)
        {
            deck.Clear();
            DestroyAllCards();
            Debug.Log("Creating New Deck");
            List<Card> initialDeckData = CreateDeck();
            deck = ShuffleDeck(initialDeckData);
            SpawnCardsInScene(deckTransform, deck);
        }

        var (dealtHandsData, remainingDeckData) = DealCards(players.Count, deck);
        deck = remainingDeckData;

        var index = 0;
        foreach (Player player in players)
        {
            player.hands = dealtHandsData[index];
            yield return StartCoroutine(AssignCardsToPositionsCoroutine(player.hands, allPlayersHandsTransform[index]));
            index++;
        }

        Debug.Log($"Dealt cards. Player Hand Count: {dealtHandsData[0].Count}, Opponent Hand Count: {dealtHandsData[1].Count}, Remaining Deck: {remainingDeckData.Count}");
        if (remainingDeckData.Count >= 22)
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
    public IEnumerator AssignCardsToPositionsCoroutine(List<Card> dealtHand, List<Transform> transform)
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
                Vector3 targetPosition = deckTransform.position + new Vector3(-20 + index * stackOffsetZ, 0, -index * stackOffsetZ);
                Quaternion targetRotation = deckTransform.rotation;

                yield return StartCoroutine(MoveCardCoroutine(cardGO, targetPosition, targetRotation, moveDuration / 5, null, 0));
                audioSource.PlayOneShot(dealSound);
                cardGO.transform.SetParent(deckTransform);

                SpriteRenderer sr = cardGO.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.sortingLayerName = "Deck";
                    sr.sortingOrder = index + 1;
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
