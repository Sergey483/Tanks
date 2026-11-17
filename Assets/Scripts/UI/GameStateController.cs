using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameStateController : MonoBehaviour
{
    [SerializeField] private TankHealth playerHealth;

    [Header("UI")]
    [SerializeField] private TMP_Text killText;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject victoryPanel;
    [SerializeField] private Button[] restartButtons;

    [Header("Restart")]
    [SerializeField] private KeyCode restartKey = KeyCode.R;

    private readonly List<TankHealth> enemies = new List<TankHealth>();
    private int kills;
    private bool isFinished;

    private void Awake()
    {
        foreach (EnemyTankAI enemy in FindObjectsByType<EnemyTankAI>(FindObjectsSortMode.None))
        {
            if (enemy.TryGetComponent(out TankHealth health))
                enemies.Add(health);
        }

        foreach (Button button in restartButtons)
            button.onClick.AddListener(Restart);

        SetPanel(gameOverPanel, false);
        SetPanel(victoryPanel, false);
    }

    private void OnEnable()
    {
        if (playerHealth != null)
            playerHealth.Died += HandlePlayerDied;

        foreach (TankHealth enemy in enemies)
            enemy.Died += HandleEnemyDied;
    }

    private void OnDisable()
    {
        if (playerHealth != null)
            playerHealth.Died -= HandlePlayerDied;

        foreach (TankHealth enemy in enemies)
        {
            if (enemy != null)
                enemy.Died -= HandleEnemyDied;
        }
    }

    private void Start()
    {
        UpdateKillText();
    }

    private void Update()
    {
        if (isFinished && Input.GetKeyDown(restartKey))
            Restart();
    }

    private void HandleEnemyDied()
    {
        kills++;
        UpdateKillText();

        if (!isFinished && kills >= enemies.Count)
            Finish(victoryPanel);
    }

    private void HandlePlayerDied()
    {
        if (!isFinished)
            Finish(gameOverPanel);
    }

    private void Finish(GameObject panel)
    {
        isFinished = true;
        SetPanel(panel, true);
    }

    private void UpdateKillText()
    {
        if (killText != null)
            killText.text = "Знищено: " + kills + " / " + enemies.Count;
    }

    private void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private static void SetPanel(GameObject panel, bool isVisible)
    {
        if (panel != null)
            panel.SetActive(isVisible);
    }
}
