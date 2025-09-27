using TMPro;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace SplitsStats;

public class TimerComponent : InfoComponent
{
    private TMP_Text tmpTextPace;

    //
    //  Relates to the displayed time.
    //

    /// <summary>
    /// Read-Only: A float representing the time this timer was started at. As is, this float represents how long in seconds since starting the game.
    /// </summary>
    public float startTime { get; private set; }

    /// <summary>
    /// Read-Only: A float representing the time this timer was started at. As is, this float represents how long, in seconds, since starting the game.
    /// </summary>
    public float endTime { get; private set; }

    /// <summary>
    /// Read-Only: A float representing the current time displayed. Actively updates if the timer is running, is constant if the timer is stopped. As is, this float represents how long, in seconds, the timer has ran for.
    /// </summary>
    public float currTime { get { return totalTime == -1.0f ? GetCurrentTime() - startTime : totalTime; } }

    /// <summary>
    /// Read-Only: A float representing the total time a timer tracked from start to end. Returns -1.0f if the timer is currently running, unlike currtime. As is, this float represents how long, in seconds, the timer has ran for.
    /// </summary>
    public float totalTime { get { if (endTime > 0.0f) return endTime - startTime; return -1.0f; } }

    /// <summary>
    /// A float stored representing the gold split target time. As is, this float represents the length, in seconds, of the record time. Set to a negative number to disable using a record time for gold splits.
    /// </summary>
    public float recordTime;

    /// <summary>
    /// Read-Only: A boolean indicating if the timer is currently running.
    /// </summary>
    public bool timerOn { get; private set; }

    /// <summary>
    /// A boolean indicating if the timer should always show the hour digit even if it would display as zero.
    /// </summary>
    public bool showHour;

    /// <summary>
    /// The number of decimal digits to display in the time, set to 0 or less to display only whole seconds.
    /// </summary>
    public uint precisionDigits;

    /// <summary>
    /// A boolean that determines if this timer uses real time or in-game time. Can be set externally to adjust only if the timer isn't running.
    /// </summary>
    /// <exception cref="InvalidOperationException">Raised when setting this parameter while the timer is currently running.</exception>
    public bool isRealTime
    {
        get
        {
            return _isRealTime;
        }
        set
        {
            if (timerOn) throw new InvalidOperationException("Timer is currently running, can't change the type of time!");
            _isRealTime = value;
        }
    }
    private bool _isRealTime;

    //
    //  Relates to the entire run time.
    //

    /// <summary>
    /// A static time that stores a start time to reference to determine how long an entire run has lasted without needing to reference other timers. As is, this float represents how long, in seconds, since starting the game.
    /// </summary>
    public static float runStartTime = -1.0f;

    /// <summary>
    /// Read-Only: A float representing the time between when a run is started and this timer's current time. Actively updates if the timer is running, is constant if the timer is stopped. As is, this float represents how long, in seconds, the timer has ran for.
    /// </summary>
    public float currRunTime { get { return endTime == -1.0f ? GetCurrentTime() - runStartTime : endTime - runStartTime; } }

    /// <summary>
    /// A float stored representing the current run's target split time. As is, this float represents the length, in seconds, of the record time. Set to a negative number to disable using a run time and displaying the interval pace.
    /// </summary>
    public float targetRunTime = -1.0f;

    /// <summary>
    /// Read-Only: A float stored representing the current run's pace. As is, this float represents the difference between currRunTime and targetRunTime (negative is faster, positive is slower). If targetRunTime isn't set, this returns null.
    /// </summary>
    public float? currPace { get { if (targetRunTime > 0.0) return currRunTime - targetRunTime; return null; } }

    private const float minimumPaceTextOffset = 40f;
    private const float triggerPaceTextOffsetAdjustment = 35f;

    public Color initialColor {get; private set;}
    public Color activeColor {get; private set;}
    public Color inactiveColor {get; private set;}

    public static readonly Color redSplitColor = new Color(1.0f, 0.553f, 0.553f);
    public static readonly Color greenSplitColor = new Color(0.687f, 1.0f, 0.605f);
    public static readonly Color goldSplitColor = new Color(1.0f, 0.896f, 0.583f);

    /// <summary>
    /// Read-Only: A boolean indicating if the timer has ever been started since it's construction. Read-only.
    /// </summary>
    public bool neverStarted { get; private set; }

    /// <summary>
    /// Create and return a TimerComponent attached to a new gameObject parented to a provided transform.
    /// </summary>
    /// <param name="template">A template to use to fill in basic data needed for constructing the internal processes and game objects.</param>
    /// <param name="parent">The parent transform to set as this TimerComponent's gameObject's parent.</param>
    /// <returns>A TimerComponent attached to a newly created gameObject.</returns>
    public static TimerComponent CreateTimerComponent(InfoComponentTemplate template, Transform parent)
    {
        return CreateTimerComponent<TimerComponent>(template, parent);
    }

    /// <summary>
    /// Create and return a TimerComponent attached to a new gameObject parented to a provided transform. Use a template to allow subclasses to reuse object construction in the superclass.
    /// </summary>
    /// <typeparam name="T">TimerComponent or one of its defined subclasses.</typeparam>
    /// <param name="template">A template to use to fill in basic data needed for constructing the internal processes and game objects.</param>
    /// <param name="parent">The parent transform to set as this TimerComponent's gameObject's parent.</param>
    /// <returns>A TimerComponent attached to a newly created gameObject. Matches type of template if using a subclass.</returns>
    public static T CreateTimerComponent<T>(InfoComponentTemplate template, Transform parent) where T : TimerComponent
    {
        T currComponent = CreateInfoComponent<T>(template, parent);

        return currComponent;
    }

    private void initializeComponent()
    {
        uiPosition = UIComponentPosition.TopLeft;
        startTime = -1.0f;
        endTime = -1.0f;
        recordTime = -1.0f;
        timerOn = false;
        isRealTime = SettingsManager.isRealTime;
        neverStarted = true;
        showHour = false;
        precisionDigits = 1;

        initialColor = new Color(0.7f, 0.7f, 0.7f);
        activeColor = Color.white;
        inactiveColor = new Color(0.4f, 0.4f, 0.4f);
    }

    public TimerComponent()
    {
        initializeComponent();
    }

    /// <summary>
    /// The Monobehavior Start Script: This is responsible for creating and modifying child gameObjects needed for the UI component, if they don't already exist.
    /// </summary>
    public override void Start()
    {
        base.Start();

        TMP_Text tempComponentStorage = null;
        foreach (Transform child in textRectTransform)
        {
            tempComponentStorage = child.GetComponent<TMP_Text>();
            if (tempComponentStorage != null) break;
        }

        // If we couldn't find the text box we wanted then create one.
        if (tempComponentStorage == null)
        {
            RectTransform paceTextRectTransform = UnityEngine.Object.Instantiate<RectTransform>(textRectTransform, textRectTransform);
            paceTextRectTransform.anchoredPosition = Vector2.zero;
            paceTextRectTransform.name = "Pace Text";
            tmpTextPace = paceTextRectTransform.GetComponent<TMP_Text>();
        }
        else tmpTextPace = tempComponentStorage;

        tmpTextPace.autoSizeTextContainer = true;
        tmpTextPace.textWrappingMode = (TextWrappingModes)0;
        tmpTextPace.alignment = uiPosition == UIComponentPosition.TopLeft ? TextAlignmentOptions.Left : TextAlignmentOptions.Right;
        tmpTextPace.lineSpacing = 0f;
        tmpTextPace.fontSize = 70f;
        tmpTextPace.outlineColor = new Color32((byte)0, (byte)0, (byte)0, byte.MaxValue);
        tmpTextPace.outlineWidth = 0f; //0.055f;
        tmpTextPace.color = initialColor;
        tmpTextPace.text = "";
        tmpTextPace.transform.localPosition = Vector3.zero;
        tmpTextPace.gameObject.SetActive(false);

        SetCurrColor(initialColor);

        this.gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(INITIAL_HEIGHT, INITIAL_HEIGHT * 2.0f);
        UpdateText(0.0f);
    }

    /// <summary>
    /// Convert a time (in seconds) to a string based on this object's showHour and precisionDigits properties.
    /// You can set these properties manually prior to calling this.
    /// </summary>
    /// <param name="totalSeconds"> The time in seconds to convert to a string. </param>
    /// <returns> The time represented as a string. </returns>
    private string GetTimeString(float totalSeconds)
    {
        return TimerComponent.GetTimeString(totalSeconds, showHour, true, precisionDigits);
    }

    /// <summary>
    /// Convert a time (in seconds) to a string.
    /// </summary>
    /// <param name="totalSeconds"> The time in seconds to convert to a string. </param>
    /// <param name="showHour"> Should the hours be shown even if there are zero hours? </param>
    /// <param name="showMinute"> Should the minutes be shown even if minutes and hours are zero? </param>
    /// <param name="precisionDigits"> Should hundreths of a second be shown? </param>
    /// <param name="showPositiveSign"> If true, a "+" is appended to the front of the time if it is positive. A "-" will always be present for negative times. </param>
    /// <returns> The time represented as a string. </returns>
    public static string GetTimeString(float totalSeconds, bool showHour = false, bool showMinute = true, uint precisionDigits = 1, bool showPositiveSign = false)
    {
        bool isNegativeTime = totalSeconds < 0.0f;
        if (isNegativeTime) totalSeconds = Math.Abs(totalSeconds);
        int num = Mathf.FloorToInt(totalSeconds);
        int num2 = num / 3600;
        int num3 = num % 3600 / 60;
        int num4 = num % 60;
        string timeString = num2 > 0 || showHour ? $"{num2}:{num3:00}:{num4:00}" : $"{num3}:{num4:00}";
        if (!showMinute && num3 <= 0 && num2 <= 0) timeString =  $"{num4}";
        if (precisionDigits > 0) timeString += "." + $"{Mathf.FloorToInt(MathF.Pow(10f, precisionDigits) * (totalSeconds % 1f))}".PadLeft((int)precisionDigits,'0');
        if (isNegativeTime) timeString = "-" + timeString;
        else if (showPositiveSign) timeString = "+" + timeString;
        return timeString;
    }

    /// <summary>
    /// Change the initial color of the text prior to being started for the first time.
    /// </summary>
    /// <param name="newColor"> The new color of the text. </param>
    /// <returns> A boolean indicating if the change had an immediate visible impact. </returns>
    public bool SetInitialColor(Color newColor)
    {
        initialColor = newColor;
        if (neverStarted && tmpText != null) 
        {
            return SetCurrColor(newColor);
        }
        return false;
    }

    /// <summary>
    /// Change the color of the text that is used if the timer is actively counting.
    /// </summary>
    /// <param name="newColor"> The new color of the text. </param>
    /// <returns> A boolean indicating if the change had an immediate visible impact. </returns>
    public bool SetActiveColor(Color newColor)
    {
        activeColor = newColor;
        if (timerOn && tmpText != null)
        {
            return SetCurrColor(newColor);
        }
        return false;
    }
    
    /// <summary>
    /// Change the color of the text that is used if the timer has been stopped after being active previously.
    /// </summary>
    /// <param name="newColor"> The new color of the text. </param>
    /// <returns> A boolean indicating if the change had an immediate visible impact. </returns>
    public bool SetInactiveColor(Color newColor)
    {
        inactiveColor = newColor;
        if (!neverStarted && !timerOn && tmpText != null)
        {
            return SetCurrColor(newColor);
        }
        return false;
    }

    /// <summary>
    /// Change the color of the text.
    /// </summary>
    /// <param name="newColor"> The new color of the text. </param>
    /// <returns> A boolean indicating if the change was successful. </returns>
    public bool SetCurrColor(Color newColor)
    {
        bool colorChanged = false;

        if (tmpText != null)
        {
            tmpText.color = newColor;
            colorChanged = true;
        }
        if (iconImage != null)
        {
            iconImage.color = newColor;
            colorChanged = true;
        }
        return colorChanged;
    }

    /// <summary>
    /// Set the pace text active or not.
    /// </summary>
    /// <param name="isActive">The new active state for the pace text.</param>
    public void SetPaceTextActive(bool isActive)
    {
        if (tmpTextPace != null)
        {
            tmpTextPace.gameObject.SetActive(isActive);
            
            if (tmpText != null)
            {
                float textWidth = tmpText.GetPreferredValues().x;
                int direction = uiPosition == UIComponentPosition.TopLeft ? 1 : -1;
                tmpTextPace.transform.localPosition = new Vector3(direction * (textWidth + minimumPaceTextOffset), 10.0f, 0.0f);
            }
            else tmpTextPace.transform.localPosition = Vector3.zero;
        }
    }

    /// <summary>
    /// Return if the pace text is active or not.
    /// </summary>
    /// <returns>The current active state for the pace text.</returns>
    public bool GetPaceTextActive()
    {
        if (tmpTextPace != null)
        {
            return tmpTextPace.gameObject.activeSelf;
        }
        return false;
    }

    /// <summary>
    /// Update the text of the timer. Text shows the time and the timer's label if present.
    /// </summary>
    /// <param name="time"> The time (in seconds) to show in the text. </param>
    private void UpdateText(float time)
    {
        if (tmpText != null) tmpText.text = GetTimeString(time);
        if (tmpTextPace != null && tmpText != null && targetRunTime > 0.0f) 
        {
            float currRunTime = time + (startTime - runStartTime);
            float currRunPace = currRunTime - targetRunTime;
            if (!GetPaceTextActive() && SettingsManager.showPaceOnTimeTrigger && currRunPace >= SettingsManager.paceTimeTrigger) SetPaceTextActive(true);

            tmpTextPace.text = GetTimeString(currRunPace, false, false, precisionDigits > 0u ? 1u : 0u, true);
            float textWidth = tmpText.GetPreferredValues().x;
            float currPaceTextPosition = tmpTextPace.transform.localPosition.x;
            float distanceAwayFromPreferedLocation = currPaceTextPosition - textWidth - minimumPaceTextOffset;
            int direction = uiPosition == UIComponentPosition.TopLeft ? 1 : -1;
            if (Math.Abs(distanceAwayFromPreferedLocation) > triggerPaceTextOffsetAdjustment) tmpTextPace.transform.localPosition = new Vector3(direction * (textWidth + minimumPaceTextOffset), 10.0f, 0.0f);

            if (!SettingsManager.useColorPace) tmpTextPace.color = tmpText.color;
            else if (recordTime > 0.0f && time < recordTime) tmpTextPace.color = goldSplitColor;
            else if (currRunPace <= 0.0f) tmpTextPace.color = greenSplitColor;
            else tmpTextPace.color = redSplitColor;
        }
    }

    public override void Update()
    {
        if (timerOn) UpdateText(currTime);
    }

    /// <summary>
    /// Returns a time (in seconds) depending on what isRealTime is set to.
    /// If true, uses system time. If false, uses game time.
    /// </summary>
    private float GetCurrentTime()
    {
        if (isRealTime) return SplitsStatsPlugin.GetCurrentRealTime();
        else return Time.time;
    }

    /// <summary>
    /// Start this timer at and set TimerComponent.runStartTime to the current time.
    /// Equivalent to <c>StartRunAtTime( GetCurrentTime() );</c>
    /// </summary>
    /// <returns> A boolean indicating if the timer was successfully started.
    /// If the timer is already running then this returns false and has no affect on the timer and it's data.</returns>
    public bool StartRunTimer()
    {
        return StartRunAtTime(GetCurrentTime());
    }

    /// <summary>
    /// Start this timer at the current time.
    /// Equivalent to <c>StartAtTime( GetCurrentTime() );</c>
    /// </summary>
    /// <returns> A boolean indicating if the timer was successfully started.
    /// If the timer is already running then this returns false and has no affect on the timer and it's data.</returns>
    public bool StartTimer()
    {
        return StartAtTime(GetCurrentTime());
    }

    /// <summary>
    /// Start this timer at and set TimerComponent.runStartTime to a specified time.
    /// </summary>
    /// <returns> A boolean indicating if the timer was successfully started.
    /// If the timer is already running then this returns false and has no affect on the timer and it's data.</returns>
    public bool StartRunAtTime(float startingTime)
    {
        if (StartAtTime(startingTime))
        {
            runStartTime = startingTime;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Start this timer and set it's start time to a specified time.
    /// </summary>
    /// <param name="startingTime"> The time to store as the starting time. </param>
    /// <returns> A boolean indicating if the timer was successfully modified.
    /// If the timer is already running then this returns false and has no affect on the timer and it's data.</returns>
    public bool StartAtTime(float startingTime)
    {
        if (timerOn) return false;
        neverStarted = false;
        startTime = startingTime;
        endTime = -1.0f;
        if (SettingsManager.showPaceOnStart) SetPaceTextActive(true);
        SetCurrColor(activeColor);
        return timerOn = true;
    }

    /// <summary>
    /// End this timer at the current time.
    /// Equivalent to <c>EndAtTime( GetCurrentTime() );</c>
    /// </summary>
    /// <returns> A boolean indicating if the timer was successfully stopped.
    /// If the timer is already stopped then this returns false and has no affect on the timer and it's data.</returns>
    public bool EndTimer() 
    {
        return EndAtTime(GetCurrentTime());
    }

    /// <summary>
    /// End this timer and set it's end time to a specified time.
    /// </summary>
    /// <param name="endingTime"> The time to store as the ending time. </param>
    /// <returns> A boolean indicating if the timer was successfully stopped.
    /// If the timer is already stopped then this returns false and has no affect on the timer and it's data.</returns>
    public bool EndAtTime(float endingTime)
    {
        if (!timerOn) return false;
        endTime = endingTime;
        UpdateText(currTime);
        if (SettingsManager.showPaceOnEnd) SetPaceTextActive(true);
        timerOn = false;
        SetCurrColor(inactiveColor);
        if (tmpTextPace != null) tmpTextPace.color = tmpTextPace.color * SplitsManager.INACTIVE_COLOR_SCALE;
        return true;
    }
}