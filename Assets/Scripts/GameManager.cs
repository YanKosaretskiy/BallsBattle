using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameManager : MonoBehaviour
{
    public GameObject startMenuPanel;
    public GameObject gameOverPanel;
    public TextMeshProUGUI gameOverText;

    public GameObject playerBall;
    public GameObject enemyBall;


    private void Awake()
    {
        Time.timeScale = 1f;
        startMenuPanel.SetActive(true);
        gameOverPanel.SetActive(false);
        playerBall.SetActive(false);
        enemyBall.SetActive(false);
    }

    private void OnEnable() => Ball.OnBallDied += HandleBallDied;
    private void OnDisable() => Ball.OnBallDied -= HandleBallDied;

    public void StartGame()
    {
        startMenuPanel.SetActive(false);
        playerBall.SetActive(true);
        enemyBall.SetActive(true);
    }

    private void HandleBallDied(Ball loser)
    {
        Time.timeScale = 0.5f;
        gameOverPanel.SetActive(true);
        gameOverText.text = loser.isPlayer ? "Stasis Win!" : "Bladesman Win!";
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
