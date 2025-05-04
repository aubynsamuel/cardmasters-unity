using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public GameHistoryUI historyUI;
    [HideInInspector] public List<Card> deck = new();

    [Header("Game Settings")]
    public int targetScore = 10;

    [HideInInspector] public Player humanPlayer;
    [HideInInspector] public Player computerPlayer;
    private List<Player> players = new();

    [Header("UI References")]
    public TMPro.TextMeshProUGUI messageText;
    public TMPro.TextMeshProUGUI playersScoresText;
    public GameObject startBtn;

    // Game state variables
    private readonly List<Play> currentPlays = new();
    private Card currentLeadCard = null;
    private int cardsPlayed = 0;
    private bool gameOver = false;
    // private readonly List<GameHistoryEntry> gameHistory = new();
    private int accumulatedPoints = 0;
    private string lastPlayedSuit = null;
    private Player currentControl;
    private bool canPlayCard = false;
    [HideInInspector] public GameObject computerCardTODestroy;
    [HideInInspector] public GameObject humanCardTODestroy;

    public CardsSetup cardsSetup;

    // Card symbols for display
    private readonly Dictionary<string, string> suitSymbols = new()
    {
        { "diamond", "♦" },
        { "spade", "♠" },
        { "heart", "♥" },
        { "club", "♣" }
    };

    void Start()
    {
        UpdateScores();
    }
    public void Initialize(List<Player> players, List<Card> deck)
    {
        this.players = players;
        this.deck = deck;
        humanPlayer = players[1];
        computerPlayer = players[0];
        currentControl = players[^1];
        StartGame();
    }

    public void StartGame()
    {
        bool needsShuffle = deck == null || deck.Count < players.Count * 5;
        int currentControlIndex = players.FindIndex(p => p.id == currentControl.id);
        int nextControlIndex = (currentControlIndex + 1) % players.Count;
        currentControl = players[nextControlIndex];

        cardsPlayed = 0;
        currentLeadCard = null;
        currentPlays.Clear();
        UpdateMessage(needsShuffle ? "Shuffling cards..." : "");
        gameOver = false;
        startBtn.SetActive(currentControl.id == computerPlayer.id);
        // gameHistory.Clear();
        historyUI.ClearMessages();
        accumulatedPoints = 0;
        lastPlayedSuit = null;
        currentControl = players[nextControlIndex];
        StartGameSequence();
    }

    public void StartGameSequence()
    {
        if (players.Any(p => p.score > 0))
        {
            StartCoroutine(cardsSetup.StartSetup(players.IndexOf(currentControl)));
            deck = cardsSetup.deck;
            Debug.Log("Deck creation called");
        }
        canPlayCard = currentControl.id == humanPlayer.id;
        UpdateMessage(currentControl.id == computerPlayer.id
                  ? "Press 'Start' to start"
                  : "Play a card to start");
    }

    public void StartButtonClicked()
    {
        Debug.Log("Start Btn Clicked");
        StartCoroutine(ComputerTurnDelay());
    }

    private IEnumerator ComputerTurnDelay()
    {
        startBtn.SetActive(false);
        yield return new WaitForSeconds(1.5f);
        Debug.Log("Performing start click action");
        ComputerTurn();
    }

    public void ComputerTurn()
    {
        // Debug.Log("Called Computer Turn");
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
        cardsSetup.cardObjectMap.TryGetValue(cardToPlay, out GameObject cardGO);
        cardGO.GetComponent<CardUI>().ComputerPlay();
        computerPlayer.hands.Remove(cardToPlay);
        computerCardTODestroy = cardGO;

        // Play the card
        PlayCard(computerPlayer, cardToPlay);

        // Setup for human turn
        canPlayCard = true;
        UpdateMessage("It's your turn to play.");
    }

    public bool HumanPlayCard(Card card)
    {
        if (gameOver)
        {
            UpdateMessage("Game is over. No more plays allowed.");
            return false;
        }

        if (!canPlayCard)
        {
            UpdateMessage("It is not your turn to play.");
            return false;
        }

        if (currentPlays.Count == 0 && currentControl.id != humanPlayer.id)
        {
            UpdateMessage("It is not your turn to play.");
            return false;
        }

        if (currentPlays.Any((play) => play.player.id == humanPlayer.id))
        {
            UpdateMessage("You have already played in this round.");
            return false;
        }

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
            yield return new WaitForSeconds(0.5f);
            ComputerTurn();
        }
    }

    private void PlayCard(Player player, Card card)
    {
        // Create play record
        Play play = new() { player = player, card = card };

        // Update round state
        currentPlays.Add(play);
        currentLeadCard ??= card;

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
        yield return new WaitForSeconds(1f);
        FinishRound();
    }

    private void FinishRound()
    {
        Play firstPlay = currentPlays[0];
        Play secondPlay = currentPlays[1];
        Player newControl;
        string resultMessage;
        int pointsEarned;

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
        int newAccumulatedPoints;
        string newLastPlayedSuit = lastPlayedSuit;

        if (currentControl.id != newControl.id)
        {
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

                yield return new WaitForSeconds(1f);
                Debug.Log("Computer Playing");
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
        Destroy(computerCardTODestroy);
        Destroy(humanCardTODestroy);
    }

    private void HandleGameOver(Player winningPlayer, int newAccumulatedPoints, int pointsEarned)
    {
        canPlayCard = false;
        gameOver = true;
        startBtn.SetActive(false);


        int finalPoints = newAccumulatedPoints == 0 ? pointsEarned : newAccumulatedPoints;
        int humanScore = 0;
        int computerScore = 0;

        // Award points to winner
        if (winningPlayer.id == computerPlayer.id)
        {
            computerScore += computerPlayer.score + finalPoints;
            computerPlayer.score = computerScore;
        }
        else
        {
            humanScore += humanPlayer.score + finalPoints;
            humanPlayer.score = humanScore;
        }
        UpdateScores();

        // Update game message
        UpdateMessage(
            winningPlayer.id == humanPlayer.id
            ? $"🏆 You won this game with {finalPoints} points! 🏆"
            : $"🏆 {computerPlayer.name} won this game with {finalPoints} points! 🏆"
        );

        // Check if target score reached
        Debug.Log($"Computer {computerScore} Human {humanScore}");
        if (computerScore < targetScore && humanScore < targetScore)
        {
            StartCoroutine(StartNextGame());
        }
        else
        {
            Player winner = humanScore >= targetScore ? humanPlayer : computerPlayer;
            if (winner.id == humanPlayer.id)
                cardsSetup.audioSource.PlayOneShot(cardsSetup.winSound);
            else
                cardsSetup.audioSource.PlayOneShot(cardsSetup.loseSound);
            UpdateMessage($"Game Over! {winner.name} won");
        }
    }

    private IEnumerator StartNextGame()
    {
        yield return new WaitForSeconds(2f);
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
        // gameHistory.Add(new GameHistoryEntry { message = message, importance = important });
        historyUI.AddMessage(message, important);
    }

    private void UpdateMessage(string message)
    {
        if (messageText != null)
            messageText.text = message;
        // Debug.Log("Game Message: " + message);
    }

    private void UpdateScores()
    {
        playersScoresText.text = $"Computer {computerPlayer.score} vs {humanPlayer.score} Human";
    }
}
