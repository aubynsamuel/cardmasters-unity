using System.Collections.Generic;
using System.Linq;

public static class CardAI
{
    /// <summary>
    /// AI logic for selecting which card to play
    /// </summary>
    /// <param name="hand">The current cards in the AI's hand</param>
    /// <param name="leadCard">The leading card in the current round, or null if AI is leading</param>
    /// <param name="remainingRounds">Number of rounds left in the current game</param>
    /// <returns>The card the AI should play</returns>
    public static Card ChooseCardAI(List<Card> hand, Card leadCard, int remainingRounds)
    {
        // If AI is leading/is in control (no lead card)
        if (leadCard == null)
        {
            // Debug.Log("[CardAI] AI is leading");
            if (remainingRounds <= 2)
            {
                // In final 2 rounds, play highest cards to secure control
                return hand.OrderByDescending(card => card.value).First();
            }
            else
            {
                // Otherwise play lowest card to preserve high cards
                return hand.OrderBy(card => card.value).First();
            }
        }
        // If AI is following
        else
        {
            // Debug.Log("[CardAI] AI is following");
            string requiredSuit = leadCard.suit;
            List<Card> cardsOfSuit = hand.Where(card => card.suit == requiredSuit).ToList();

            // If AI has cards of the required suit
            if (cardsOfSuit.Count > 0)
            {
                // Debug.Log("[CardAI] AI has the required suit");
                // Find cards that can win
                List<Card> winningCards = cardsOfSuit.Where(card => card.value > leadCard.value).ToList();

                if (winningCards.Count > 0)
                {
                    // Not sound will be removed in the future to make AI better
                    if (remainingRounds <= 2)
                    {
                        // Play highest winner in final rounds
                        return winningCards.OrderByDescending(card => card.value).First();
                    }
                    else
                    {
                        // Play lowest winner in early rounds
                        return winningCards.OrderBy(card => card.value).First();
                    }
                }
                else
                {
                    // Can't win, so play lowest card of required suit
                    return cardsOfSuit.OrderBy(card => card.value).First();
                }
            }
            // If AI doesn't have required suit
            else
            {
                // Play lowest value card to minimize loss
                // Debug.Log("[CardAI] AI doesn't have the required suit");
                return hand.OrderBy(card => card.value).First();
            }
        }
    }
}