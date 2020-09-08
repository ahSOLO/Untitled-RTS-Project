using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    // Game Variables
    public enum GameState { GAME, MENU, OTHER1, OTHER2 };
    public GameState gameState = GameState.GAME;
    public enum OrderState { DEFAULT, MOVE, ATTACK, BUILD, OTHER1, OTHER2 };
    public OrderState orderState = OrderState.DEFAULT;
    public static GameManager gM; // Singleton Variable

    // Start is called before the first frame update
    void Start()
    {
        // Set up Singleton
        if (gM == null) gM = this;
        else Destroy(gameObject);
    }

    // Update is called once per frame
    void Update()
    {

    }
}
