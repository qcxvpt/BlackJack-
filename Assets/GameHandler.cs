using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

[System.Serializable]
public class Card
{
    public Sprite sprite;
    public string rank;
    public int value;
    public bool faceup;

    // Method to get the current sprite based on the faceup value
    public Sprite GetSprite()
    {
        return faceup ? sprite : GameHandler.Instance.Blank;
    }
}

public class GameHandler : MonoBehaviour
{
    public static GameHandler Instance;

    public List<Card> Cards = new List<Card>();
    public Sprite Blank;
    public int balance = 1000;
    public Text balance_counter;
    public Text bet_counter;
    public int bet;
    public GameObject cardPrefab;

    public Transform playerDeckPosition;
    public Transform dealerDeckPosition;
    public Transform DeckPile;
    public float xOffset = 30f;

    public List<GameObject> playerHand = new List<GameObject>();
    public List<GameObject> dealerHand = new List<GameObject>();

    private Dictionary<GameObject, Card> cardData = new Dictionary<GameObject, Card>();
    public int playerValueSum = 0;
public int dealerValueSum = 0;
public Text playerValueSumText;
public Text dealerValueSumText;
public GameObject startPanel;
public List<Chip> Chips = new List<Chip>();
public List<Chip> betPile = new List<Chip>();
public Transform pilePosition;
private float yOffset = 0.3f;
public GameObject Interface;
public GameObject chipPrefab;
public GameObject DoubleButton;
public GameObject InsuranceButton;
public GameObject SplitButton;
public Transform SplitDeckPosition;
public List<GameObject> playerHand2 = new List<GameObject>(); // Second hand after split
private bool isSplitActive = false;
private bool firstHandPlayed = false;
private float splitOffset = 0.5f;
public GameObject resultPanel;
public GameObject[] resultJudge;
public GameObject loadingObject;
public GameObject valueHolder;
private int highscore = 0;
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
[System.Serializable]
public class Chip
{
    public GameObject chip;
    public int value;
}

void Start()
{
    balance = PlayerPrefs.GetInt("Total",balance);
    highscore = PlayerPrefs.GetInt("Record", highscore);
    if(balance == 0)
    {
        balance = 1000;
    }
    loadingObject.SetActive(false);
    Interface.SetActive(false);
    startPanel.SetActive(true);
    resultPanel.SetActive(false);
    valueHolder.SetActive(false);
    ShuffleDeck();
    UpdateChipVisibility();
    for(int i = 0; i < resultJudge.Length; i++)
    {
        resultJudge[i].SetActive(false);
    }
}
    public void LoadScene()
    {
        SceneManager.LoadScene("Login");
    }
public void UpdateChipVisibility()
{
    foreach (Chip chip in Chips)
    {
        chip.chip.SetActive(chip.value <= balance);
    }
}

    void Update()
    {
        UpdateChipVisibility();
        bet_counter.text = "Bet: " + bet.ToString() + "$";
        balance_counter.text = "Cash: "+balance.ToString()+"$";
        UpdatePlayerBet();
        if(balance > highscore)
        {
            highscore = balance;
            PlayerPrefs.SetInt("Record",highscore);
        }
    }
    public void Hit(bool isPlayer)
{
    if (isPlayer)
    {
        DealCardToPlayer(false);
        StartCoroutine(RotateAnimation(playerHand[playerHand.Count-1]));
        AdjustDealerAcesValue(true);
    }
    else
    {
        DealCardToDealer(false);
        StartCoroutine(RotateAnimation(dealerHand[dealerHand.Count-1]));
    }
    if(InsuranceButton.activeSelf)
    {
        InsuranceButton.SetActive(false);
    }
    if(DoubleButton.activeSelf)
    {
        DoubleButton.SetActive(false);
    }
    if(SplitButton.activeSelf)
    {
        SplitButton.SetActive(false);
    }
}
public void DoubleDown()
{
    if (balance >= bet) // Check if the player has enough balance to double the bet
    {
        balance -= bet; // Deduct the same amount as the current bet from the balance
        bet *= 2; // Double the current bet
        balance_counter.text = balance.ToString("F2");
        UpdateChipVisibility();

        Hit(true); // Deal one more card to the player
        Stand(true); // Automatically stand after doubling down
    }
}
public void Insurance()
{
    InsuranceButton.SetActive(false);
    Card dealerCard = cardData[dealerHand[0]];
        if(dealerCard.value == 10)
        {
            Debug.Log("Dealer BlackJack revealed");
            balance += bet;
        }
        else
        {
        balance -= bet / 2;
        balance_counter.text = balance.ToString("F2");
        Debug.Log("Dealer blackjack not found. Insurance lost!");
        }
}
public void Split()
{
    if (playerHand.Count == 2)
    {
        SplitButton.SetActive(false); // Disable the split button after splitting
        bet += bet*2;
        GameObject splitCard = playerHand[1];
        playerHand.RemoveAt(1);
        playerHand2.Add(splitCard);
        splitCard.transform.position = SplitDeckPosition.position + new Vector3(splitOffset * playerHand2.Count, 0, 0);

        // Automatically deal another card to the player's current deck
        DealCardToPlayer(false);
        StartCoroutine(RotateAnimation(playerHand[playerHand.Count-1]));

        isSplitActive = true;
    }
}

private void CheckSplitCondition()
{
    bool conditionMet = false;

    // Split condition: Player's initial two cards have the same rank
    if (playerHand.Count == 2)
    {
        Card card1 = cardData[playerHand[0]];
        Card card2 = cardData[playerHand[1]];
        if (card1.rank == card2.rank)
        {
            conditionMet = true;
        }
    }

    SplitButton.SetActive(conditionMet);
}

private void CheckDoubleDownCondition()
{
    bool conditionMet = false;
    int playerTotal = 0;
    bool hasAce = false;

    // Calculate player's hand total and check for Ace
    foreach (GameObject cardObject in playerHand)
    {
        Card card = cardData[cardObject];
        playerTotal += card.value;
        if (card.rank == "Ace")
        {
            hasAce = true;
        }
    }

    // Calculate dealer's visible card total
    int dealerVisibleTotal = 0;
    if (dealerHand.Count > 0)
    {
        Card dealerCard = cardData[dealerHand[0]];
        dealerVisibleTotal = dealerCard.value;
    }

    // Double down conditions:
    // 1. Player's total is 9, 10, or 11 without an Ace
    // 2. Player's total is 16, 17, or 18 with an Ace (Soft 16, 17, 18)
    // 3. Dealer's visible card is not an Ace
    // 4. Balance is enough to double the bet
    if (balance >= bet && dealerVisibleTotal != 11)
    {
        if ((playerTotal == 9 || playerTotal == 10 || playerTotal == 11) && !hasAce)
        {
            conditionMet = true;
        }
        else if (hasAce && (playerTotal == 16 || playerTotal == 17 || playerTotal == 18))
        {
            conditionMet = true;
        }
    }

    DoubleButton.SetActive(conditionMet);
}
private void CheckInsuranceCondition()
{
    bool conditionMet = false;
    if (dealerHand.Count >= 2 && balance >= bet/2)
    {
        Card secondDealerCard = cardData[dealerHand[1]];
        if (secondDealerCard.rank == "Ace")
        {
            conditionMet = true;
        }
    }

    InsuranceButton.SetActive(conditionMet);
}
public void Stand(bool isPlayer)
{
    if (isPlayer)
    {
        if (isSplitActive)
        {
            if (!firstHandPlayed)
            {
                firstHandPlayed = true;
                SwitchHands(); // Switch to the split hand
            }
            else
            {
                StartCoroutine(DealerPlay()); // Dealer plays after both hands have been played
            }
        }
        else
        {
            // Start the dealer's play after revealing the first card
            StartCoroutine(DealerPlay());
        }
    }
}

private IEnumerator DealerPlay()
{
    // Reveal the first card if it's not already revealed
    if (dealerHand.Count > 0 && !cardData[dealerHand[0]].faceup)
    {
        yield return RotateAnimation(dealerHand[0]);
        UpdateDealerValueSum();

        // Check if dealer should stand after revealing the first card
        if (dealerValueSum > playerValueSum && dealerValueSum < 21)
        {
            if(firstHandPlayed)
            {
                SwitchHands();
                UpdatePlayerValueSum();
            }
            else
            {
            Stand(false);
            }
        }
    }

    // Dealer hits until the total is 17 or higher
    while (dealerValueSum < 17)
    {
        Hit(false);
        yield return new WaitForSeconds(1f); // Wait a bit between dealer hits for better gameplay experience

        // Adjust Aces if dealer value exceeds 21
        if (dealerValueSum > 21 || dealerValueSum < playerValueSum)
        {
            AdjustDealerAcesValue(false);
        }
    }

    DetermineWinner();
}
private void AdjustDealerAcesValue(bool isPlayer)
{
    if(isPlayer)
    {
    foreach (GameObject cardObject in playerHand)
    {
        Card card = cardData[cardObject];
        if (card.rank == "Ace" && card.value == 11)
        {
            card.value = 1;
            UpdatePlayerValueSum();
            if (dealerValueSum <= 21)
            {
                break;
            }
        }
    }
    }
    else
    {
    foreach (GameObject cardObject in dealerHand)
    {
        Card card = cardData[cardObject];
        if (card.rank == "Ace" && card.value == 11)
        {
            card.value = 1;
            UpdateDealerValueSum();
            if (dealerValueSum <= 21)
            {
                break;
            }
        }
    }
    }
}

private void DetermineWinner()
{
    if (playerValueSum > dealerValueSum && playerValueSum <= 21)
    {
        Interface.SetActive(false);
        resultJudge[1].SetActive(true);
        Debug.Log("Player Wins!");
        balance += bet*2;
    }
    else if (dealerValueSum > playerValueSum && dealerValueSum <= 21)
    {
        Interface.SetActive(false);
        resultJudge[2].SetActive(true);
        Debug.Log("Dealer Wins!");
    }
    else if(dealerValueSum == playerValueSum)
    {
        Interface.SetActive(false);
        resultJudge[4].SetActive(true);
        Debug.Log("Push! It's a tie!");
        balance+=bet;
    }
    if (isSplitActive && firstHandPlayed)
    {
        bet /= 2;
        SwitchHands();
        firstHandPlayed = false;
        DetermineWinner();
    }
    resultPanel.SetActive(true);
}

    private void UpdatePlayerBet()
{
    int totalBet = 0;
    foreach (Chip chip in betPile)
    {
        totalBet += chip.value;
    }
    bet = totalBet;
}

    public void AllIn()
{
    while (balance > 0)
    {
        Chip highestChip = null;
        foreach (Chip chip in Chips)
        {
            if (chip.value <= balance && (highestChip == null || chip.value > highestChip.value))
            {
                highestChip = chip;
            }
        }

        if (highestChip == null) break;

        ChipBet(highestChip.value, highestChip.chip.GetComponent<Image>().sprite, highestChip.chip.GetComponent<Image>().color);
    }
}
public void Reset()
{
    foreach (Chip chip in betPile)
    {
        balance += chip.value;
        Destroy(chip.chip);
    }
    betPile.Clear();
    balance_counter.text = balance.ToString("F2");
    UpdateChipVisibility();
}

public void ChipBet(int currency, Sprite buttonSprite, Color buttonColor)
{
    StartCoroutine(HandleChipBet(currency, buttonSprite, buttonColor));
}
private IEnumerator HandleChipBet(int currency, Sprite buttonSprite, Color buttonColor)
{
    // Find the chip with the specified currency value
    Chip chipTemplate = Chips.Find(chip => chip.value == currency);
    if (chipTemplate == null)
    {
        Debug.LogError("No chip with the specified value found!");
        yield break;
    }

    // Create a new chip sprite instance
    GameObject newChip = new GameObject("Chip");
    SpriteRenderer spriteRenderer = newChip.AddComponent<SpriteRenderer>();
    spriteRenderer.sprite = buttonSprite;
    spriteRenderer.color = buttonColor;
    spriteRenderer.sortingOrder = 15;

    // Set the initial position and scale of the chip
    newChip.transform.position = pilePosition.position; // Start position, adjust if necessary
    newChip.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);

    // Smoothly move the chip to the pile position
    Vector3 targetPosition = pilePosition.position + new Vector3(0, yOffset * betPile.Count, 0);
    StartCoroutine(MoveChipToPosition(newChip, targetPosition));

    // Add the new chip to the bet pile
    Chip newChipData = new Chip { chip = newChip, value = currency };
    betPile.Add(newChipData);

    // Deduct the value from the player's balance
    balance -= currency;
    balance_counter.text = balance.ToString("F2");
    UpdateChipVisibility();

    yield return null;
}

private IEnumerator MoveChipToPosition(GameObject chipObject, Vector3 targetPosition)
{
    float duration = 0.5f; // movement duration
    Vector3 startPosition = chipObject.transform.position;

    float time = 0;
    while (time < duration)
    {
        chipObject.transform.position = Vector3.Lerp(startPosition, targetPosition, time / duration);
        time += Time.deltaTime;
        yield return null;
    }
    chipObject.transform.position = targetPosition;
}

    void ShuffleDeck()
    {
        for (int i = 0; i < Cards.Count; i++)
        {
            Card temp = Cards[i];
            int randomIndex = Random.Range(i, Cards.Count);
            Cards[i] = Cards[randomIndex];
            Cards[randomIndex] = temp;
        }
    }

    public IEnumerator RotateAnimation(GameObject cardObject)
    {
        Card card = cardData[cardObject];

        // Rotate 90 degrees on the Y axis
        yield return RotateCard(cardObject, 90);

        // Change the faceup value
        card.faceup = !card.faceup;

        // Update the card sprite
        UpdateCardSprite(cardObject, card.GetSprite());

        // Rotate another 90 degrees to complete the rotation
        yield return RotateCard(cardObject, 90);
        UpdatePlayerValueSum();
        UpdateDealerValueSum();
    }
    public void Restart()
    {
        PlayerPrefs.SetInt("Total", balance);
        loadingObject.SetActive(true);
        StartCoroutine(Loader());
    }
    private IEnumerator Loader()
    {
        float duration = 1f;
        float time = 0f;
        while (time < duration)
        {
            time += Time.deltaTime;
            yield return null;
        }
        SceneManager.LoadScene("SampleScene");
    }
    private IEnumerator RotateCard(GameObject cardObject, float angle)
    {
        float duration = 0.2f; // rotation duration
        Quaternion startRotation = cardObject.transform.rotation;
        Quaternion endRotation = startRotation * Quaternion.Euler(0, angle, 0);

        float time = 0;
        while (time < duration)
        {
            cardObject.transform.rotation = Quaternion.Lerp(startRotation, endRotation, time / duration);
            time += Time.deltaTime;
            yield return null;
        }
        cardObject.transform.rotation = endRotation;
    }

    private void UpdateCardSprite(GameObject cardObject, Sprite newSprite)
    {
        cardObject.GetComponent<SpriteRenderer>().sprite = newSprite;
    }

    public void DealInitialCards()
    {
        if(bet > 0)
        {
        startPanel.SetActive(false);
        StartCoroutine(DealCardsSequence());
        }
        else
        {
            Debug.Log("Place a bet first!");
        }
    }

    private IEnumerator DealCardsSequence()
    {
        // Deal first card to player face down, then rotate face up
        DealCardToPlayer(false);
        yield return RotateAnimation(playerHand[0]);

        // Deal second card to player face down, then rotate face up
        DealCardToPlayer(false);
        yield return RotateAnimation(playerHand[1]);

        // Deal third card to dealer face up
        DealCardToDealer(false);
        yield return new WaitForSeconds(0.5f);

        // Deal fourth card to dealer face down
        DealCardToDealer(false);
        yield return RotateAnimation(dealerHand[1]);
        valueHolder.SetActive(true);
        Interface.SetActive(true);
        CheckDoubleDownCondition();
        CheckInsuranceCondition();
        CheckSplitCondition();
    }

    private void DealCardToPlayer(bool faceup)
    {
        DealCard(playerDeckPosition, playerHand, faceup);
    }

    private void DealCardToDealer(bool faceup)
    {
        DealCard(dealerDeckPosition, dealerHand, faceup);
    }
private void DealCard(Transform deckPosition, List<GameObject> hand, bool faceup)
{
    if (Cards.Count == 0)
    {
        Debug.LogError("No cards left in the deck!");
        return;
    }

    Card card = Cards[0];
    Cards.RemoveAt(0);
    card.faceup = faceup;

    Quaternion initialRotation = Quaternion.Euler(0, -180, 0);
    GameObject cardObject = Instantiate(cardPrefab, DeckPile.position, initialRotation, deckPosition);
    cardObject.GetComponent<SpriteRenderer>().sortingOrder = 9;
    UpdateCardSprite(cardObject, card.GetSprite());
    hand.Add(cardObject);
    cardData[cardObject] = card;

    // Smoothly move the card to its final position
    StartCoroutine(MoveCardToPosition(cardObject, deckPosition.position + new Vector3(xOffset * hand.Count, 0, 0)));
    if(deckPosition == playerDeckPosition)
    {
        UpdatePlayerValueSum();
    }
    else if(deckPosition == dealerDeckPosition)
    {
        UpdateDealerValueSum();
    }
}

private void SwitchHands()
{
    List<GameObject> temp = playerHand;
    playerHand = playerHand2;
    playerHand2 = temp;

    // Update card positions for visual clarity
    foreach (GameObject card in playerHand)
    {
        card.transform.position = playerDeckPosition.position + new Vector3(xOffset * playerHand.IndexOf(card), 0, 0);
    }
    foreach (GameObject card in playerHand2)
    {
        card.transform.position = SplitDeckPosition.position + new Vector3(splitOffset * playerHand2.IndexOf(card), 0, 0);
    }
}

private void UpdatePlayerValueSum()
{
    playerValueSum = 0;
    foreach (GameObject cardObject in playerHand)
    {
        Card card = cardData[cardObject];
                    // Set the value of Ace based on the toggle state
        if (card.faceup)
        {
            playerValueSum += card.value;
        }
    }
    playerValueSumText.text = playerValueSum.ToString();

    if(playerValueSum == 21 && playerHand.Count == 2)
    {
        StopCoroutine(DealCardsSequence());
        Interface.SetActive(true);
        Debug.Log("Player got Blackjack!");
        resultPanel.SetActive(true);
        Interface.SetActive(false);
        resultJudge[0].SetActive(true);
        balance+=bet*3;
    }
    else if(playerValueSum > 21)
    {
        if(isSplitActive)
        {
            AdjustDealerAcesValue(true);
            SwitchHands();
            UpdatePlayerValueSum();
            firstHandPlayed = true;
            isSplitActive = false;
        }
        else
        {
        AdjustDealerAcesValue(true);
        Debug.Log("Player Bust! Dealer Wins!");
        resultPanel.SetActive(true);
        Interface.SetActive(false);
        resultJudge[3].SetActive(true);
        }
    }
}
private void UpdateDealerValueSum()
{
    dealerValueSum = 0;
    foreach (GameObject cardObject in dealerHand)
    {
        Card card = cardData[cardObject];
        if (card.faceup)
        {
            dealerValueSum += card.value;
        }
    }
    dealerValueSumText.text = dealerValueSum.ToString();
    if(dealerValueSum == 21)
    {
        Debug.Log("Dealer got Blackjack!");
        resultPanel.SetActive(true);
        Interface.SetActive(false);
        resultJudge[2].SetActive(true);
    }
    else if(dealerValueSum > 21)
    {
        AdjustDealerAcesValue(false);
        Debug.Log("Dealer Bust! Player Wins!");
        resultPanel.SetActive(true);
        Interface.SetActive(false);
        resultJudge[1].SetActive(true);
        balance+=bet*2;
    }
}

    private IEnumerator MoveCardToPosition(GameObject cardObject, Vector3 targetPosition)
    {
        float duration = 0.2f; // movement duration
        Vector3 startPosition = cardObject.transform.position;

        float time = 0;
        while (time < duration)
        {
            cardObject.transform.position = Vector3.Lerp(startPosition, targetPosition, time / duration);
            time += Time.deltaTime;
            yield return null;
        }
        cardObject.transform.position = targetPosition;
    }

}
