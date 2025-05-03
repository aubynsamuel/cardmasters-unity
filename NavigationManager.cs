using UnityEngine;
using System.Collections.Generic;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    public GameObject splashPanel;
    public GameObject mainMenuPanel;
    public GameObject gamePanel;
    public GameObject multiplayerLobbyPanel;
    public GameObject roomPanel;
    public GameObject multiplayerGamePanel;
    public GameObject gameOverPanel;
    public GameObject rulesPanel;
    public GameObject statsPanel;
    public GameObject settingsPanel;
    public GameObject profilePanel;
    public GameObject authPanel;

    private List<GameObject> allPanels;

    void Awake()
    {
        Instance = this;
        allPanels = new List<GameObject> {
            splashPanel, mainMenuPanel, gamePanel, multiplayerLobbyPanel,
            roomPanel, multiplayerGamePanel, gameOverPanel, rulesPanel,
            statsPanel, settingsPanel, profilePanel, authPanel
        };
    }

    public void ShowPanel(GameObject target)
    {
        foreach (var panel in allPanels)
            panel.SetActive(panel == target);
    }

    public void ShowSplash() => ShowPanel(splashPanel);
    public void ShowMainMenu() => ShowPanel(mainMenuPanel);
    public void ShowGame() => ShowPanel(gamePanel);
    public void ShowMultiplayerLobby() => ShowPanel(multiplayerLobbyPanel);
    public void ShowRoom() => ShowPanel(roomPanel);
    public void ShowMultiplayerGame() => ShowPanel(multiplayerGamePanel);
    public void ShowGameOver() => ShowPanel(gameOverPanel);
    public void ShowRules() => ShowPanel(rulesPanel);
    public void ShowStats() => ShowPanel(statsPanel);
    public void ShowSettings() => ShowPanel(settingsPanel);
    public void ShowProfile() => ShowPanel(profilePanel);
    public void ShowAuth() => ShowPanel(authPanel);
}
