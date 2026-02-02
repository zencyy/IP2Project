using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ProgressHUD : MonoBehaviour
{
    public static ProgressHUD Instance;

    [Header("UI References")]
    public TMP_Text phaseLabel;  // e.g. "Subject Blocks"
    public TMP_Text countText;   // e.g. "3 / 10"
    public Slider progressBar;   // Optional visual bar

    void Awake()
    {
        Instance = this;
    }

    public void UpdateProgress(string phaseName, int current, int target)
    {
        // 1. Update Label
        if (phaseLabel != null) phaseLabel.text = phaseName;

        // 2. Update Text Numbers
        if (countText != null) countText.text = $"{current} / {target}";

        // 3. Update Slider Bar
        if (progressBar != null)
        {
            progressBar.maxValue = target;
            progressBar.value = current;
        }
    }
}