using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager  : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public bool isGameOver = false;
    public float timeAlive = 0f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        if (isGameOver) return;

        timeAlive += Time.deltaTime;
    }

    public void TriggerGameOver()
    {
        if (isGameOver) return;
        isGameOver = true;

        Debug.Log("Game Over! Society lasted " + timeAlive + " seconds.");
    }

    public void ResetGame()
    {
        Time.timeScale = 1f;
        Scene current = SceneManager.GetActiveScene();
        SceneManager.LoadScene(current.buildIndex);
    }
}
