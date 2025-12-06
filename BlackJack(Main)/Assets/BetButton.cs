using UnityEngine;
using UnityEngine.UI;

public class BetButton : MonoBehaviour
{
    public int currencyValue;
    public GameHandler gameHandler;

    void Start()
    {
        GetComponent<Button>().onClick.AddListener(() => {
            Image buttonImage = GetComponent<Image>();
            gameHandler.ChipBet(currencyValue, buttonImage.sprite, buttonImage.color);
        });
    }
}
