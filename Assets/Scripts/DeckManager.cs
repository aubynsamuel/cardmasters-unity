using UnityEngine;
using System.Collections.Generic;

public class DeckManager : MonoBehaviour
{
    // public List<Card> deck = new List<Card>();
    private string[] suits = { "diamond", "spade", "heart", "club" };
    private string[] ranks = { "6", "7", "8", "9", "10", "J", "Q", "K" };

    [Header("Scene Setup")]
    public GameObject cardPrefab;   // Prefab with a SpriteRenderer
    // public Transform spawnParent;   // Just an empty GameObject to organize them


    // You can adjust the values based on your game's rules
    private int GetRankValue(string rank)
    {
        switch (rank)
        {
            case "6": return 6;
            case "7": return 7;
            case "8": return 8;
            case "9": return 9;
            case "10": return 10;
            case "J": return 11;
            case "Q": return 12;
            case "K": return 13;
            default: return 0; // Should not happen with valid ranks
        }
    }

    // void Awake()
    // {
    //     CreateDeck();
    //     ShuffleDeck();
    //     SpawnCardsInScene();

    // }

    public List<Card> CreateDeck()
    {
        List<Card> deck = new List<Card>();
        deck.Clear();
        foreach (string suit in suits)
        {
            foreach (string rank in ranks)
            {
                Card card = new Card();
                card.suit = suit;
                card.rank = rank;
                card.value = GetRankValue(rank);

                // Load the sprite from the Resources folder
                string spritePath = rank + "_" + suit;
                Sprite cardSprite = Resources.Load<Sprite>(spritePath);

                if (cardSprite != null)
                {
                    card.sprite = cardSprite;
                    deck.Add(card);
                }
                else
                {
                    Debug.LogError("Sprite not found for: " + spritePath);
                }
            }
        }

        Debug.Log("Created deck with " + deck.Count + " cards.");
        return deck;
    }

    public List<Card> ShuffleDeck(List<Card> deck)
    {
        // Fisher-Yates (Knuth) shuffle algorithm
        for (int i = deck.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1); // Use Unity's Random
            Card temp = deck[i];
            deck[i] = deck[j];
            deck[j] = temp;
        }
        Debug.Log("Deck shuffled.");
        return deck;
    }

    public void SpawnCardsInScene(Transform spawnParent, List<Card> deck)
    {
        float startX = -5f; // starting X position
        float startY = 0f;  // Y position
        float spacing = 1.5f; // space between cards

        int index = 0;
        foreach (Card card in deck)
        {
            GameObject go = Instantiate(cardPrefab, spawnParent);
            go.transform.position = new Vector3(startX + (index * spacing), startY * index, 0f);

            SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
            if (sr != null)
                sr.sprite = card.sprite;

            var ui = go.GetComponent<CardUI>();
            ui.cardData = card;
            ui.handIndex = deck.IndexOf(card);

            if (go.GetComponent<Collider2D>() == null)
            {
                go.AddComponent<BoxCollider2D>();
            }

            index++;
        }
    }

    public (List<List<Card>> dealtHands, List<Card> remainingDeck) DealCards(List<Player> players, List<Card> deck)
    {
        List<List<Card>> dealtHands = new List<List<Card>>();
        for (int i = 0; i < players.Count; i++)
        {
            dealtHands.Add(new List<Card>());
        }

        // Assuming a 32-card deck and 2 players for now, adjust if needed
        // int cardsPerPlayer = 5; // 3 + 2

        // Deal 3 cards
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < players.Count; j++)
            {
                if (deck.Count > 0)
                {
                    dealtHands[j].Add(deck[0]);
                    deck.RemoveAt(0);
                }
            }
        }

        // Deal 2 cards
        for (int i = 0; i < 2; i++)
        {
            for (int j = 0; j < players.Count; j++)
            {
                if (deck.Count > 0)
                {
                    dealtHands[j].Add(deck[0]);
                    deck.RemoveAt(0);
                }
            }
        }

        // Assign the dealt hands to players
        for (int i = 0; i < players.Count; i++)
        {
            players[i].hands = dealtHands[i];
        }

        Debug.Log("Cards dealt.");
        return (dealtHands, deck);
    }
}