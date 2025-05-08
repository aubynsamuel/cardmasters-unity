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
    public GameObject HumanControlIndicator;
    public GameObject ComputerControlIndicator;
    private List<Player> players = new();

    [Header("UI References")]
    public TMPro.TextMeshProUGUI messageText;
    public TMPro.TextMeshProUGUI humanScoreText;
    public TMPro.TextMeshProUGUI computerScoreText;
    public TMPro.TextMeshProUGUI targetScoreText;
    public TMPro.TextMeshProUGUI targetScoreTextSettings;
    public TMPro.TextMeshProUGUI accumulatedPointsText;

    public float popUpScale = 2f;
    public float popUpDuration = 0.2f;
    // public GameObject startBtn;

    // Game state variables
    private readonly List<Play> currentPlays = new();
    private Card currentLeadCard = null;
    private int cardsPlayed = 0;
    private bool gameOver = false;
    // private readonly List<GameHistoryEntry> gameHistory = new();
    public int accumulatedPoints = 0;
    private string lastPlayedSuit = null;
    public Player currentControl;
    private bool canPlayCard = false;
    public bool canFold = false;
    [HideInInspector] public GameObject computerCardTODestroy;
    [HideInInspector] public GameObject humanCardTODestroy;

    public CardsSetup cardsSetup;
    [SerializeField] private readonly float animationDuration = 0.25f;
    private Vector3 originalScale;

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
        accumulatedPointsText.text = accumulatedPoints.ToString();
        originalScale = ComputerControlIndicator.transform.localScale;
        targetScore = PlayerPrefs.GetInt("targetScore", 10);
        targetScoreText.text = targetScore.ToString();
        targetScoreTextSettings.text = targetScore.ToString();
        UpdateScores(WhatChanged.NOTHING);
        UpdateAccumulatedPoints();
    }

    void UpdateControlIndicator()
    {
        if (currentControl == null || humanPlayer == null || computerPlayer == null)
            return;

        bool isHuman = currentControl.id == humanPlayer.id;
        // human indicator
        StartCoroutine(AnimateScale(
            HumanControlIndicator.transform,
            HumanControlIndicator.transform.localScale,
            isHuman ? originalScale : Vector3.zero,
            animationDuration
        ));
        // computer indicator
        StartCoroutine(AnimateScale(
            ComputerControlIndicator.transform,
            ComputerControlIndicator.transform.localScale,
            isHuman ? Vector3.zero : originalScale,
            animationDuration
        ));
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
        UpdateControlIndicator();

        cardsPlayed = 0;
        currentLeadCard = null;
        currentPlays.Clear();
        UpdateMessage(needsShuffle ? "Shuffling cards..." : "");
        gameOver = false;
        // startBtn.SetActive(currentControl.id == computerPlayer.id);
        // gameHistory.Clear();
        historyUI.ClearMessages();
        accumulatedPoints = 0;
        UpdateAccumulatedPoints();
        lastPlayedSuit = null;
        StartGameSequence();
    }

    public void StartGameSequence()
    {
        if (players.Any(p => p.score > 0))
        {
            StartCoroutine(cardsSetup.StartSetup(players.IndexOf(currentControl), humanPlayer.id));
            deck = cardsSetup.deck;
        }
        canPlayCard = currentControl.id == humanPlayer.id;
        UpdateMessage(currentControl.id == computerPlayer.id
                  ? ""
                  : "Play a card to start");
    }

    public void StartComputerTurn()
    {
        StartCoroutine(ComputerTurnDelay());
    }

    private IEnumerator ComputerTurnDelay()
    {
        // startBtn.SetActive(false);
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
        cardsSetup.cardObjectMap.TryGetValue(cardToPlay, out GameObject cardGO);
        cardGO.GetComponent<CardUI>().ComputerPlay();
        computerPlayer.hands.Remove(cardToPlay);
        computerCardTODestroy = cardGO;

        // Play the card
        PlayCard(computerPlayer, cardToPlay);

        // Setup for human turn
        canPlayCard = true;
        UpdateMessage("It's your turn to play");
    }

    public bool HumanPlayCard(Card card)
    {
        if (gameOver)
        {
            UpdateMessage("Game is over");
            return false;
        }

        if (!canPlayCard)
        {
            UpdateMessage("It is not your turn to play");
            return false;
        }

        if (currentPlays.Count == 0 && currentControl.id != humanPlayer.id)
        {
            UpdateMessage("It is not your turn to play");
            return false;
        }

        if (currentPlays.Any((play) => play.player.id == humanPlayer.id))
        {
            UpdateMessage("You have already played");
            return false;
        }

        // Check if player must follow suit
        if (currentLeadCard != null)
        {
            string requiredSuit = currentLeadCard.suit;
            bool hasRequired = humanPlayer.hands.Any(c => c.suit == requiredSuit);

            if (hasRequired && card.suit != requiredSuit)
            {
                UpdateMessage($"You must play a {suitSymbols[requiredSuit]}");
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
        UpdateControlIndicator();
        UpdateMessage(resultMessage);
        AddToGameHistory($"{newControl.name} Won Round {cardsPlayed + 1}", true);
        accumulatedPoints = newAccumulatedPoints;
        UpdateAccumulatedPoints();
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
                UpdateMessage($"{computerPlayer.name} is playing");

                yield return new WaitForSeconds(1f);
                ComputerTurn();
            }
            else
            {
                canPlayCard = true;
                UpdateMessage("It's your turn to play");
            }
        }
    }

    public void Fold()
    {
        if (!canFold) return;
        HandleGameOver(computerPlayer, 0, 1);
        players.ForEach(p =>
        {
            p.hands.ForEach(card =>
            {
                cardsSetup.cardObjectMap.TryGetValue(card, out GameObject cardGO);
                Destroy(cardGO);
            });
            p.hands.Clear();
        });
        currentPlays.ForEach(p =>
        {
            cardsSetup.cardObjectMap.TryGetValue(p.card, out GameObject cardGO);
            Destroy(cardGO);
        });
        currentPlays.Clear();
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
        canFold = false;
        canPlayCard = false;
        gameOver = true;
        // startBtn.SetActive(false);


        int finalPoints = newAccumulatedPoints == 0 ? pointsEarned : newAccumulatedPoints;
        int humanScore = 0;
        int computerScore = 0;

        // Award points to winner
        if (winningPlayer.id == computerPlayer.id)
        {
            computerScore += computerPlayer.score + finalPoints;
            computerPlayer.score = computerScore;
            UpdateScores(WhatChanged.COMPUTER);
        }
        else
        {
            humanScore += humanPlayer.score + finalPoints;
            humanPlayer.score = humanScore;
            UpdateScores(WhatChanged.HUMAN);
        }

        // Update game message
        UpdateMessage(
            winningPlayer.id == humanPlayer.id
            ? $"You won this game +{finalPoints}"
            : $"{computerPlayer.name} won this game +{finalPoints}"
        );

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
            UpdateMessage($"Game Over {winner.name} won");
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
    }

    private void UpdateScores(WhatChanged whatChanged)
    {
        humanScoreText.text = humanPlayer.score.ToString();
        computerScoreText.text = computerPlayer.score.ToString();
        if (whatChanged.Equals(WhatChanged.HUMAN))
            StartCoroutine(AnimateScorePop(humanScoreText.transform));
        else if (whatChanged.Equals(WhatChanged.COMPUTER))
            StartCoroutine(AnimateScorePop(computerScoreText.transform));
    }
    private void UpdateAccumulatedPoints()
    {
        if (accumulatedPoints > 0)
            accumulatedPointsText.gameObject.SetActive(true);
        else
            accumulatedPointsText.gameObject.SetActive(false);
        accumulatedPointsText.text = $"+{accumulatedPoints}";
        StartCoroutine(AnimateScorePop(accumulatedPointsText.transform));
    }

    public void IncreaseTargetScore()
    {
        if (targetScore < 30)
        {
            targetScore++;
            targetScoreText.text = targetScore.ToString();
            targetScoreTextSettings.text = targetScore.ToString();
            PlayerPrefs.SetInt("targetScore", targetScore);
        }
    }
    public void DecreaseTargetScore()
    {
        if (targetScore > 1)
        {
            targetScore--;
            targetScoreText.text = targetScore.ToString();
            targetScoreTextSettings.text = targetScore.ToString();
            PlayerPrefs.SetInt("targetScore", targetScore);
        }
    }
    public void Exit()
    {
        Application.Quit();
    }

    IEnumerator AnimateScorePop(Transform textTransform)
    {
        Vector3 originalScale = textTransform.localScale;
        Vector3 popUpVector = originalScale * popUpScale;

        float timer = 0f;
        while (timer < popUpDuration)
        {
            timer += Time.deltaTime;
            float t = timer / popUpDuration;
            textTransform.localScale = Vector3.Lerp(originalScale, popUpVector, t);
            yield return null;
        }

        timer = 0f;
        while (timer < popUpDuration)
        {
            timer += Time.deltaTime;
            float t = timer / popUpDuration;
            textTransform.localScale = Vector3.Lerp(popUpVector, originalScale, t);
            yield return null;
        }

        textTransform.localScale = originalScale; // Ensure it returns to the original scale
    }
    private IEnumerator AnimateScale(Transform target, Vector3 from, Vector3 to, float duration)
    {
        float elapsed = 0f;
        target.localScale = from;
        target.gameObject.SetActive(true);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            target.localScale = Vector3.Lerp(from, to, t);
            yield return null;
        }

        target.localScale = to;

        if (to == Vector3.zero)
            target.gameObject.SetActive(false);
    }

}

public enum WhatChanged
{
    HUMAN, COMPUTER, NOTHING
}