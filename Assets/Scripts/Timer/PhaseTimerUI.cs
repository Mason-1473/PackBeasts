using UnityEngine;
using TMPro;
using Photon.Pun;

public class PhaseTimerUI : MonoBehaviourPun
{
    [SerializeField] private TextMeshProUGUI timerText;
    private GameManager gameManager;

    private void Start()
    {
        gameManager = GameManager.Instance;
    }

    private void Update()
    {
        if (gameManager != null)
        {
            float remainingTime = gameManager.GetPhaseRemainingTime();
            int minutes = Mathf.FloorToInt(remainingTime / 60);
            int seconds = Mathf.FloorToInt(remainingTime % 60);
            string phaseString = gameManager.currentPhase.ToString();
            if (gameManager.currentPhase == GamePhase.Day)
            {
                phaseString += $" ({gameManager.currentSubPhase})";
            }
            timerText.text = $"Phase: {phaseString}\nTime Left: {minutes:00}:{seconds:00}";
        }
    }

    [PunRPC]
    private void UpdateTimer(float remainingTime)
    {
        int minutes = Mathf.FloorToInt(remainingTime / 60);
        int seconds = Mathf.FloorToInt(remainingTime % 60);
        string phaseString = gameManager.currentPhase.ToString();
        if (gameManager.currentPhase == GamePhase.Day)
        {
            phaseString += $" ({gameManager.currentSubPhase})";
        }
        timerText.text = $"Phase: {phaseString}\nTime Left: {minutes:00}:{seconds:00}";
    }
}