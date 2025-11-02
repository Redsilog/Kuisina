using UnityEngine;

public class GameMode : MonoBehaviour
{
    public static GameMode Instance;
    public int playerCount = 1;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SetPlayers(int count)
    {
        playerCount = count;
    }
}
