using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public bool isGameOver = false;
    public float timeAlive = 0f;

    private void Awake()
    {
        // For a single-scene game, just replace the Instance every time the scene loads.
        Instance = this;
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

        var pop = FindObjectOfType<PopulationManager>();
        if (pop != null)
        {
            pop.OnGameOver();
        }
    }

    public void ResetGame()
    {
        isGameOver = false;
        timeAlive = 0f;
        Time.timeScale = 1f;

        // Reload the current scene from scratch
        Scene current = SceneManager.GetActiveScene();
        SceneManager.LoadScene(current.name);
    }
}
