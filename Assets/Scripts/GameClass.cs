using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

public class GameManager : MonoBehaviour
{
    [Header("Game Settings")]
    public List<Card> deck = new();
    public int targetScore = 10;
    public float computerThinkTime = 1.5f;
    public float roundDisplayTime = 1.5f;

    [Header("Player Setup")]
    public Player humanPlayer;
    public Player computerPlayer;
    private List<Player> players = new();

    [Header("UI References")]
    public TMPro.TextMeshProUGUI messageText;

    // Game state variables
    private List<Play> currentPlays = new();
    private Card currentLeadCard = null;
    private int cardsPlayed = 0;
    private bool gameOver = false;
    private List<GameHistoryEntry> gameHistory = new();
    private int accumulatedPoints = 0;
    private string lastPlayedSuit = null;
    private Player currentControl;
    private bool canPlayCard = false;

    // Card symbols for display
    private readonly Dictionary<string, string> suitSymbols = new()
    {
        { "diamond", "♦" },
        { "spade", "♠" },
        { "heart", "♥" },
        { "club", "♣" }
    };

    public void Initialize(List<Player> players, List<Card> deck)
    {
        this.players = players;
        this.deck = deck;
        humanPlayer = players[1];
        computerPlayer = players[0];
        currentControl = players[0];
        StartGame();
    }

    public void StartGame()
    {
        int currentControlIndex = players.FindIndex(p => p.id == currentControl.id);
        int nextControlIndex = (currentControlIndex + 1) % players.Count;
        currentControl = players[nextControlIndex];

        cardsPlayed = 0;
        currentLeadCard = null;
        currentPlays.Clear();
        gameHistory.Clear();
        accumulatedPoints = 0;
        lastPlayedSuit = null;
        gameOver = false;
    }

    public void StartButtonClicked()
    {
        StartCoroutine(ComputerTurnDelay());
    }

    private IEnumerator ComputerTurnDelay()
    {
        yield return new WaitForSeconds(1.5f);
        ComputerTurn();
    }

    public void ComputerTurn()
    {
        if (computerPlayer.hands.Count == 0)
            return;

        int remainingRounds = 5 - cardsPlayed;
        Card cardToPlay;

        if (currentControl.id == computerPlayer.id)
        {
            // AI leads the round
            cardToPlay = CardAI.ChooseCardAI(computerPlayer.hands, null, remainingRounds);
        }
        else
        {
            // AI follows human's lead
            if (currentLeadCard == null)
                return;

            cardToPlay = CardAI.ChooseCardAI(computerPlayer.hands, currentLeadCard, remainingRounds);
        }

        // Remove card from hand
        computerPlayer.hands.Remove(cardToPlay);
        // UpdateUI();

        // Play the card
        PlayCard(computerPlayer, cardToPlay);

        // Setup for human turn
        canPlayCard = true;
        UpdateMessage("It's your turn to play.");
    }

    public bool HumanPlayCard(Card card)
    {
        // if (gameOver)
        // {
        //     UpdateMessage("Game is over. No more plays allowed.");
        //     return false;
        // }

        // if (!canPlayCard)
        // {
        //     UpdateMessage("It is not your turn to play.");
        //     return false;
        // }

        // if (currentPlays.Count == 0 && currentControl.id != humanPlayer.id)
        // {
        //     UpdateMessage("It is not your turn to play.");
        //     return false;
        // }

        // Check if player must follow suit
        if (currentLeadCard != null)
        {
            string requiredSuit = currentLeadCard.suit;
            bool hasRequired = humanPlayer.hands.Any(c => c.suit == requiredSuit);

            if (hasRequired && card.suit != requiredSuit)
            {
                UpdateMessage($"You must play a {suitSymbols[requiredSuit]} if you have one");
                return false;
            }
            else
            {
                canPlayCard = false;
            }
        }
        else
        {
            canPlayCard = false;
        }

        // Remove card from hand
        int handIndex = humanPlayer.hands.IndexOf(card);
        humanPlayer.hands.RemoveAt(handIndex);
        // UpdateUI();

        // Delay before playing the card (animation time)
        StartCoroutine(DelayedHumanPlay(humanPlayer, card));
        return true;
    }

    private IEnumerator DelayedHumanPlay(Player player, Card card)
    {
        yield return new WaitForSeconds(0.3f);

        PlayCard(player, card);

        bool isLeading = currentPlays.Count == 1;
        if (isLeading)
        {
            UpdateMessage($"{computerPlayer.name} is thinking...");
            yield return new WaitForSeconds(computerThinkTime);
            ComputerTurn();
        }
    }

    private void PlayCard(Player player, Card card)
    {
        // Create play record
        Play play = new() { player = player, card = card };

        // Update round state
        currentPlays.Add(play);
        if (currentLeadCard == null)
        {
            currentLeadCard = card;
        }

        // Add to game history
        AddToGameHistory($"{player.name} played {card.rank}{suitSymbols[card.suit]}", false);

        // If both players have played, finish the round
        if (currentPlays.Count == 2)
        {
            StartCoroutine(FinishRoundDelay());
        }
    }

    private IEnumerator FinishRoundDelay()
    {
        yield return new WaitForSeconds(roundDisplayTime);
        FinishRound();
    }

    private void FinishRound()
    {
        Play firstPlay = currentPlays[0];
        Play secondPlay = currentPlays[1];
        Player newControl;
        string resultMessage;
        int pointsEarned = 0;

        if (currentLeadCard == null)
            return;

        string leadSuit = currentLeadCard.suit;
        Card followerCard = secondPlay.card;

        // Determine round winner
        if (followerCard.suit == leadSuit && followerCard.value > currentLeadCard.value)
        {
            newControl = secondPlay.player;
            resultMessage = secondPlay.player.id == computerPlayer.id
                ? $"{computerPlayer.name} wins the round."
                : "You win the round.";
        }
        else
        {
            newControl = firstPlay.player;
            resultMessage = firstPlay.player.id == humanPlayer.id
                ? "You win the round."
                : $"{computerPlayer.name} wins the round.";
        }

        // Calculate points
        int newAccumulatedPoints = accumulatedPoints;
        string newLastPlayedSuit = lastPlayedSuit;

        if (currentControl.id != newControl.id)
        {
            newAccumulatedPoints = 0;
            newLastPlayedSuit = null;
        }

        Card winningCard = newControl.id == firstPlay.player.id ? firstPlay.card : secondPlay.card;

        bool isControlTransfer =
            currentControl.id != newControl.id &&
            (winningCard.rank == "6" || winningCard.rank == "7") &&
            winningCard.suit == leadSuit;

        if (isControlTransfer)
        {
            pointsEarned = 1;
            newAccumulatedPoints = 0;
        }
        else if (newControl.id == currentControl.id)
        {
            int cardPoints = CalculateCardPoints(winningCard);
            if (winningCard.rank == "6" || winningCard.rank == "7")
            {
                if (lastPlayedSuit == winningCard.suit)
                {
                    pointsEarned = cardPoints;
                    newAccumulatedPoints = pointsEarned;
                }
                else
                {
                    pointsEarned = cardPoints;
                    newAccumulatedPoints = accumulatedPoints + pointsEarned;
                }
            }
            else
            {
                pointsEarned = 1;
                newAccumulatedPoints = 0;
            }
        }
        else
        {
            pointsEarned = 1;
            newAccumulatedPoints = 0;
        }

        if (winningCard.rank == "6" || winningCard.rank == "7")
        {
            newLastPlayedSuit = winningCard.suit;
        }

        // Update game state
        currentControl = newControl;
        UpdateMessage(resultMessage);
        AddToGameHistory($"{newControl.name} Won Round {cardsPlayed + 1}", true);
        accumulatedPoints = newAccumulatedPoints;
        lastPlayedSuit = newLastPlayedSuit;

        // Reset round and prepare for next
        StartCoroutine(PrepareNextRound(newControl, newAccumulatedPoints, pointsEarned));
    }

    private IEnumerator PrepareNextRound(Player newControl, int newAccumulatedPoints, int pointsEarned)
    {
        yield return new WaitForSeconds(roundDisplayTime);

        // Reset round state
        ResetRound();
        int newRoundsPlayed = cardsPlayed + 1;
        cardsPlayed = Mathf.Min(newRoundsPlayed, 5);

        if (newRoundsPlayed >= 5)
        {
            HandleGameOver(newControl, newAccumulatedPoints, pointsEarned);
        }
        else
        {
            if (newControl.id == computerPlayer.id)
            {
                canPlayCard = false;
                UpdateMessage($"{computerPlayer.name} is playing.");

                yield return new WaitForSeconds(computerThinkTime);
                ComputerTurn();
            }
            else
            {
                canPlayCard = true;
                UpdateMessage("It's your turn to play.");
            }
        }
    }

    private void ResetRound()
    {
        canPlayCard = false;
        currentLeadCard = null;
        currentPlays.Clear();
    }

    private void HandleGameOver(Player winningPlayer, int newAccumulatedPoints, int pointsEarned)
    {
        canPlayCard = false;
        gameOver = true;

        int finalPoints = newAccumulatedPoints == 0 ? pointsEarned : newAccumulatedPoints;
        int humanScore = humanPlayer.score;
        int computerScore = computerPlayer.score;

        // Award points to winner
        if (winningPlayer.id == computerPlayer.id)
        {
            computerScore += finalPoints;
            computerPlayer.score = computerScore;
        }
        else
        {
            humanScore += finalPoints;
            humanPlayer.score = humanScore;
        }

        // Update game message
        UpdateMessage(
            winningPlayer.id == humanPlayer.id
            ? $"🏆 You won this game with {finalPoints} points! 🏆"
            : $"🏆 {computerPlayer.name} won this game with {finalPoints} points! 🏆"
        );

        // Check if target score reached
        if (computerScore < targetScore && humanScore < targetScore)
        {
            // Continue to next game
            StartCoroutine(StartNextGame());
        }
        else
        {
            Player winner = humanScore >= targetScore ? humanPlayer : computerPlayer;
            UpdateMessage($"Game Over! {winner.name} won the match!");
        }
    }

    private IEnumerator StartNextGame()
    {
        yield return new WaitForSeconds(2.0f);
        StartGame();
    }

    private int CalculateCardPoints(Card card)
    {
        if (card.rank == "6") return 3;
        if (card.rank == "7") return 2;
        return 1;
    }

    private void AddToGameHistory(string message, bool important)
    {
        gameHistory.Add(new GameHistoryEntry { message = message, importance = important });
    }

    private void UpdateMessage(string message)
    {
        if (messageText != null)
            messageText.text = message;
        Debug.Log("Game Message: " + message);
    }
}
