using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Timer : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI timeText;
    [SerializeField] private Image progressImage; // circular image set to Filled (Radial360)

    // Durations in seconds
    private readonly int smallWork = 60;        // 1 minute
    private readonly int smallBreak = 30;       // 30 seconds
    private readonly int mediumWork = 5 * 60;   // 5 minutes
    private readonly int mediumBreak = 60;      // 1 minute
    private readonly int pomWork = 25 * 60;     // 25 minutes
    private readonly int pomBreak = 5 * 60;     // 5 minutes
    private readonly int longBreak = 20 * 60;   // 20 minutes

    // Repeat counts
    private readonly int smallRepeat = 2;
    private readonly int mediumRepeat = 2;
    private readonly int pomRepeat = 4;

    // Levels: 0 = small, 1 = medium, 2 = pomodoro
    private int currentLevel = 0;
    private int currentRepeatCount = 0; // how many cycles completed at this level

    private bool isWork = true;
    private float timer = 0f;
    private float currentPeriodDuration = 1f; // duration of the current period in seconds

    void Start()
    {
        // start with small work
        currentLevel = 0;
        currentRepeatCount = 0;
        isWork = true;
        StartPeriod(GetCurrentWorkDuration(), true);
        UpdateUI();
    }

    void Update()
    {
        if (timer > 0f)
        {
            timer -= Time.deltaTime;
            if (timer < 0f) timer = 0f;
            UpdateUI();
        }
        else
        {
            // period finished, transition
            OnPeriodFinished();
        }
    }

    private void OnPeriodFinished()
    {
        if (isWork)
        {
            // finished a work period -> start break
            isWork = false;
            // For pomodoro level, after finishing a work period we go to a normal break
            // but long break logic is handled after finishing the following break
            StartPeriod(GetCurrentBreakDuration(), false);
        }
        else
        {
            // finished a break period -> one cycle complete for this level
            currentRepeatCount++;

            if (currentLevel == 2)
            {
                // pomodoro level
                if (currentRepeatCount >= pomRepeat)
                {
                    // completed 4 pomodoro cycles -> take a long break then reset repeat count
                    currentRepeatCount = 0;
                    isWork = false;
                    StartPeriod(longBreak, false);
                    // After long break we should resume pomodoro work cycles (stay at level 2)
                    // We'll start work when long break finishes in OnPeriodFinished
                }
                else
                {
                    // continue another pomodoro work period
                    isWork = true;
                    StartPeriod(GetCurrentWorkDuration(), true);
                }
            }
            else
            {
                // small or medium level
                if ((currentLevel == 0 && currentRepeatCount >= smallRepeat) || (currentLevel == 1 && currentRepeatCount >= mediumRepeat))
                {
                    // advance to next level
                    currentLevel = Mathf.Min(currentLevel + 1, 2);
                    currentRepeatCount = 0;
                }

                // start next work period (either next of same level or new level)
                isWork = true;
                StartPeriod(GetCurrentWorkDuration(), true);
            }
        }

        UpdateUI();
    }

    private int GetCurrentWorkDuration()
    {
        return currentLevel switch
        {
            0 => smallWork,
            1 => mediumWork,
            2 => pomWork,
            _ => pomWork,
        };
    }

    private int GetCurrentBreakDuration()
    {
        return currentLevel switch
        {
            0 => smallBreak,
            1 => mediumBreak,
            2 => pomBreak,
            _ => pomBreak,
        };
    }

    private void StartPeriod(int seconds, bool work)
    {
        isWork = work;
        timer = seconds;
        currentPeriodDuration = Mathf.Max(1f, seconds);
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (statusText != null)
        {
            statusText.text = isWork ? "ì‹Æ’†" : "‹xŒe’†";
        }

        if (timeText != null)
        {
            int totalSeconds = Mathf.CeilToInt(timer);
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            timeText.text = string.Format("{0:D2}:{1:D2}", minutes, seconds);
        }

        if (progressImage != null)
        {
            // fillAmount goes from 1 (full) to 0 (empty) as time passes
            float fill = currentPeriodDuration > 0f ? Mathf.Clamp01(timer / currentPeriodDuration) : 0f;
            progressImage.fillAmount = fill;
        }
    }
}
