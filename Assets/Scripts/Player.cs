using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class Player
{
    public string name;
    public string id; // Unique identifier for the player
    public List<Card> hands;
    public int score;
    // You can add PlayerStatus if needed later
    // public PlayerStatus status;

    public Player(string name, string id)
    {
        this.name = name;
        this.id = id;
        hands = new List<Card>();
        score = 0;
    }
}

// Optional: If you need PlayerStatus, create an enum
public enum PlayerStatus
{
    NOT_READY,
    READY,
    IN_GAME,
    VIEWING_RESULTS
}