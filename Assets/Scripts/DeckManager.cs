using UnityEngine;
using System.Collections.Generic;

public class DeckManager : MonoBehaviour
{
    public List<Card> deck = new List<Card>();

    private string[] suits = { "diamond", "spade", "heart", "club" };
    private string[] ranks = { "6", "7", "8", "9", "10", "J", "Q", "K" };

    [Header("Scene Setup")]
    public GameObject cardPrefab;   // Prefab with a SpriteRenderer
    public Transform spawnParent;   // Just an empty GameObject to organize them


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

    void Awake()
    {
        CreateDeck();
        SpawnCardsInScene();

    }

    void CreateDeck()
    {
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
    }

    void SpawnCardsInScene()
    {
        float startX = -5f; // starting X position
        float startY = 0f;  // Y position
        float spacing = 1.5f; // space between cards

        int index = 0;
        foreach (Card card in deck)
        {
            GameObject go = Instantiate(cardPrefab, spawnParent);
            go.transform.position = new Vector3(startX + (index * spacing), startY, 0f);

            SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
            if (sr != null)
                sr.sprite = card.sprite;

            index++;
        }
    }
}