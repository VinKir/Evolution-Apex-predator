using UnityEngine;

public class MenuHelper : MonoBehaviour
{
    public void PauseGame()
    {
        TimeController.PauseGame();
    }

    public void ResumeGame()
    {
        TimeController.ResumeGame();
    }
}