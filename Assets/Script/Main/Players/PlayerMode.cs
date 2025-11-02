using UnityEngine;

public class PlayerModeLoader : MonoBehaviour
{
    public GameObject player1;
    public GameObject player2;

    void Start()
    {
        if (GameMode.Instance.playerCount == 1)
        {
            player1.SetActive(true);
            player2.SetActive(false);
        }
        else
        {
            player1.SetActive(true);
            player2.SetActive(true);
        }
    }
}
