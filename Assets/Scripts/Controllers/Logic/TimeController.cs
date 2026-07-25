using UnityEngine;

public static class TimeController
{
    private static bool isPaused = false;

    public static void PauseGame()
    {
        if (isPaused) return;

        Time.timeScale = 0f;
        isPaused = true;
    }
    
    public static void ResumeGame()
    {
        if (!isPaused) return;

        Time.timeScale = 1f;
        isPaused = false;
    }

    public static void TogglePause()
    {
        if (isPaused)
            ResumeGame();
        else
            PauseGame();
    }

    public static bool IsPaused()
    {
        return isPaused;
    }
}