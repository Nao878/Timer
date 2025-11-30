using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Timer : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI timeText;
    [SerializeField] private Image progressImage; // circular image set to Filled (Radial360)
    [SerializeField] private GameObject hitokotoObject; // shown only during break
    [SerializeField] private GameObject sagyouObject; // shown only during work

    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip workClip;
    [SerializeField] private AudioClip breakClip;

    // 回転させる reload オブジェクトと回転速度（度/秒）
    [SerializeField] private GameObject reloadObject;
    [SerializeField] private float reloadRotationSpeed = 90f;

    // 作業中に色を維持するオブジェクト群（6つ）
    [SerializeField] private GameObject[] colorObjects = new GameObject[6];

    // 保存する元の色
    private Color[] originalColors;
    private int currentColoredIndex = -1; // 現在色が変わらないオブジェクトのインデックス
    private int elapsedMinutesPassed = 0; // 作業期間開始から経過した分数（分単位）

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
        // try to find an AudioSource if not set in Inspector
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        // save original colors
        InitializeOriginalColors();

        // start with small work
        currentLevel = 0;
        currentRepeatCount = 0;
        isWork = true;
        StartPeriod(GetCurrentWorkDuration(), true);
        UpdateUI();
    }

    void Update()
    {
        // reload オブジェクトを回転させる
        if (reloadObject != null)
        {
            // UI の場合は Z 軸回転（Vector3.forward）が自然
            reloadObject.transform.Rotate(Vector3.forward, reloadRotationSpeed * Time.deltaTime, Space.Self);
        }

        if (timer > 0f)
        {
            // check minute-based color change only during work
            if (isWork)
            {
                float elapsed = currentPeriodDuration - timer;
                int minutesPassed = (int)(elapsed / 60f);
                if (minutesPassed > elapsedMinutesPassed)
                {
                    elapsedMinutesPassed = minutesPassed;
                    ChooseRandomColored();
                }
            }

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

        // play sound for starting work or break
        if (audioSource != null)
        {
            AudioClip clip = work ? workClip : breakClip;
            if (clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }

        UpdateUI();

        // work 開始時に、オブジェクトがアクティブになった後でランダムに色を決める
        if (work)
        {
            elapsedMinutesPassed = 0;
            ChooseRandomColored();
        }
        else
        {
            // 休憩では元の色に戻す
            RestoreAllColors();
            currentColoredIndex = -1;
        }
    }

    private void UpdateUI()
    {
        if (statusText != null)
        {
            statusText.text = isWork ? "作業中" : "休憩中";
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
            // show progress only during work periods
            progressImage.enabled = isWork;

            if (isWork)
            {
                // fillAmount goes from 1 (full) to 0 (empty) as time passes
                float fill = currentPeriodDuration > 0f ? Mathf.Clamp01(timer / currentPeriodDuration) : 0f;
                progressImage.fillAmount = fill;
            }
        }

        if (hitokotoObject != null)
        {
            // show Hitokoto only during break
            hitokotoObject.SetActive(!isWork);
        }

        if (sagyouObject != null)
        {
            // show Sagyou only during work
            sagyouObject.SetActive(isWork);
        }
    }

    // -------------------- color handling --------------------
    private void InitializeOriginalColors()
    {
        if (colorObjects == null) return;
        originalColors = new Color[colorObjects.Length];
        for (int i = 0; i < colorObjects.Length; i++)
        {
            originalColors[i] = GetColorFromObject(colorObjects[i]);
        }
    }

    private Color GetColorFromObject(GameObject obj)
    {
        if (obj == null) return Color.white;
        var img = obj.GetComponent<Image>();
        if (img != null) return img.color;
        var sr = obj.GetComponent<SpriteRenderer>();
        if (sr != null) return sr.color;
        var r = obj.GetComponent<Renderer>();
        if (r != null) return r.material.color;
        return Color.white;
    }

    private void SetColorToObject(GameObject obj, Color col)
    {
        if (obj == null) return;
        var img = obj.GetComponent<Image>();
        if (img != null) { img.color = col; return; }
        var sr = obj.GetComponent<SpriteRenderer>();
        if (sr != null) { sr.color = col; return; }
        var r = obj.GetComponent<Renderer>();
        if (r != null)
        {
            // use material (instanced) to avoid changing shared material globally
            r.material.color = col;
        }
    }

    private void RestoreAllColors()
    {
        if (colorObjects == null || originalColors == null) return;
        for (int i = 0; i < colorObjects.Length; i++)
        {
            SetColorToObject(colorObjects[i], originalColors[i]);
        }
    }

    private void ChooseRandomColored()
    {
        if (colorObjects == null || colorObjects.Length == 0) return;
        int n = colorObjects.Length;
        int chosen = Random.Range(0, n);
        currentColoredIndex = chosen;
        for (int i = 0; i < n; i++)
        {
            if (i == chosen)
            {
                // keep original color
                SetColorToObject(colorObjects[i], originalColors != null && i < originalColors.Length ? originalColors[i] : Color.white);
            }
            else
            {
                // set to black
                SetColorToObject(colorObjects[i], Color.black);
            }
        }
    }
}
