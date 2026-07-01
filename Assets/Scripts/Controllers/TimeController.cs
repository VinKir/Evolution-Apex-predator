using UnityEngine;

public static class TimeController
{
    private static bool isPaused = false;

    /// <summary>
    /// Поставить игру на паузу
    /// </summary>
    public static void PauseGame()
    {
        if (isPaused) return;

        Time.timeScale = 0f;
        isPaused = true;
    }

    /// <summary>
    /// Снять игру с паузы
    /// </summary>
    public static void ResumeGame()
    {
        if (!isPaused) return;

        Time.timeScale = 1f;
        isPaused = false;
    }

    /// <summary>
    /// Переключение паузы
    /// </summary>
    public static void TogglePause()
    {
        if (isPaused)
            ResumeGame();
        else
            PauseGame();
    }

    /// <summary>
    /// Проверка состояния
    /// </summary>
    public static bool IsPaused()
    {
        return isPaused;
    }
}