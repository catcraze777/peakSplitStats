using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.IO;
using System.Collections.Generic;
using Zorro.Core;

namespace SplitsStats;

public class SplitsManager : MonoBehaviour
{
    public static SplitsManager Instance;

    public RectTransform topLeftInfoObject;
    public RectTransform topRightInfoObject;

    public static BaseUIComponentList topLeftComponents;
    public static BaseUIComponentList topRightComponents;
    public void AddInfoComponentToSide(BaseUIComponent input)
    {
        switch (input.uiPosition)
        {
            case UIComponentPosition.TopLeft:
                topLeftComponents.Add(input);
                input.transform.parent = topLeftInfoObject;
                break;
            case UIComponentPosition.TopRight:
                topRightComponents.Add(input);
                input.transform.parent = topRightInfoObject;
                break;
            default:
                throw new NotSupportedException($"Alignment of value {input.uiPosition} not supported!");
        }
    }
    public Dictionary<Segment, TimerComponent> splitTimers;

    public static readonly Dictionary<Segment,string> splitLabels = new Dictionary<Segment,string>
    {
        {Segment.Beach,     "Shore"},
        {Segment.Tropics,   "Tropics"},
        {Segment.Alpine,    "Alpmesa"},
        {Segment.Caldera,   "Caldera"},
        {Segment.TheKiln,   "The Kiln"}
    };

    public static readonly Dictionary<Segment,Color> splitColors = new Dictionary<Segment,Color>
    {
        {Segment.Beach,     new Color(1.0f, 0.923f, 0.632f)},
        {Segment.Tropics,   new Color(0.557f, 1.0f, 0.566f)},
        {Segment.Alpine,    new Color(0.25f, 1.0f, 0.984f)},
        {Segment.Caldera,   new Color(1.0f, 0.578f, 0.25f)},
        {Segment.TheKiln,   new Color(1.0f, 0.344f, 0.25f)}
    };

    public const string stopwatchImgPath = "img_stopwatch.png";
    public const string heightImgPath = "img_height.png";
    public const string campfireImgPath = "img_campfire.png";
    public const string peakImgPath = "img_peak.png";

    public const string HEIGHT_STAT_NAME = "Height Stat";
    public const string CAMPFIRE_STAT_NAME = "Campfire Stat";

    public static readonly Dictionary<Segment, string> splitImgPaths = new Dictionary<Segment, string>
    {
        {Segment.Beach,     "img_shore.png"},
        {Segment.Tropics,   "img_tropics.png"},
        {Segment.Alpine,    "img_alpmesa.png"},
        {Segment.Caldera,   "img_caldera.png"},
        {Segment.TheKiln,   "img_kiln.png"}
    };

    public const float INITIAL_COLOR_SCALE = 0.7f;
    public const float INACTIVE_COLOR_SCALE = 0.5f;
    public const float ICON_SIZE_SCALE = 1.1f;

    public const float PIVOT_Y = 0.4f;

    public static float PACE_TRIGGER_DISTANCE { get { return SettingsManager.paceTriggerDistance; } }

    public TimerComponent mainTimer;

    private static MapHandler currMapHandler;

    /// <summary>
    /// Use RunSaveManager to set the target times for the timers.
    /// </summary>
    public void SetRunTargets()
    {
        setupCheck();
        if (RunSaveManager.fastestRun.finalTime > 0.0f && mainTimer != null)    mainTimer.targetRunTime = RunSaveManager.fastestRun.finalTime;

        if (RunSaveManager.fastestRun.shoreTime > 0.0f      &&  splitTimers.ContainsKey(Segment.Beach))        splitTimers[Segment.Beach].targetRunTime =   RunSaveManager.fastestRun.shoreTime;
        if (RunSaveManager.fastestRun.tropicsTime > 0.0f    &&  splitTimers.ContainsKey(Segment.Tropics))      splitTimers[Segment.Tropics].targetRunTime = RunSaveManager.fastestRun.tropicsTime + splitTimers[Segment.Beach].targetRunTime;
        if (RunSaveManager.fastestRun.alpmesaTime > 0.0f    &&  splitTimers.ContainsKey(Segment.Alpine))       splitTimers[Segment.Alpine].targetRunTime =  RunSaveManager.fastestRun.alpmesaTime + splitTimers[Segment.Tropics].targetRunTime;
        if (RunSaveManager.fastestRun.calderaTime > 0.0f    &&  splitTimers.ContainsKey(Segment.Caldera))      splitTimers[Segment.Caldera].targetRunTime = RunSaveManager.fastestRun.calderaTime + splitTimers[Segment.Alpine].targetRunTime;
        if (RunSaveManager.fastestRun.kilnTime > 0.0f       &&  splitTimers.ContainsKey(Segment.TheKiln))      splitTimers[Segment.TheKiln].targetRunTime = RunSaveManager.fastestRun.kilnTime + splitTimers[Segment.Caldera].targetRunTime;

        if (RunSaveManager.fastestShore > 0.0f      &&  splitTimers.ContainsKey(Segment.Beach))        splitTimers[Segment.Beach].recordTime =   RunSaveManager.fastestShore;
        if (RunSaveManager.fastestTropics > 0.0f    &&  splitTimers.ContainsKey(Segment.Tropics))      splitTimers[Segment.Tropics].recordTime = RunSaveManager.fastestTropics;
        if (RunSaveManager.fastestAlpmesa > 0.0f    &&  splitTimers.ContainsKey(Segment.Alpine))       splitTimers[Segment.Alpine].recordTime =  RunSaveManager.fastestAlpmesa;
        if (RunSaveManager.fastestCaldera > 0.0f    &&  splitTimers.ContainsKey(Segment.Caldera))      splitTimers[Segment.Caldera].recordTime = RunSaveManager.fastestCaldera;
        if (RunSaveManager.fastestKiln > 0.0f       &&  splitTimers.ContainsKey(Segment.TheKiln))      splitTimers[Segment.TheKiln].recordTime = RunSaveManager.fastestKiln;
    }

    public const float HEADER_FONT_SIZE = 50f;
    public const float HEIGHT_STAT_FONT_SIZE = 32f;
    public const float INACTIVE_FONT_SIZE = 30f;
    public const float ACTIVE_FONT_SIZE = 38f;
    public const float LINE_SPACING = 5f;
    public const float HEADER_SPACING = 8f;
    public const float ICON_TEXT_SPACING = 10f;
    public static readonly Vector2 UI_OFFSET = new Vector2(15f, -25f);

    public bool isSetup {get; private set;}

    /// <summary>
    /// Method to ensure this object is ready for functions that assume it is.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown if SetupManager() hasn't been called yet for this object.
    /// </exception>
    private void setupCheck()
    {
        if (!isSetup) throw new InvalidOperationException("Please use SetupManager() before using any function of SplitsManager!");
    }

    /// <summary>
    /// Method that creates and initializes the SplitsManager GameObject using a GUIManager to copy components from.
    /// </summary>
    /// <param name="guiManager"> The GUIManager to copy components from and act as a parent. </param>
    /// <param name="componentList"> A list of InfoComponentTemplate objects containing the data needed to create an info panel. </param>
    /// <returns>The SplitsManager component attached to the created GameObject.</returns>
    public static SplitsManager CreateSplitsManager(GUIManager guiManager, List<InfoComponentTemplate> componentList = null)
    {
        GameObject splitsManagerGameObject = new GameObject("SplitsStatsPlugin SplitsManager");
        SplitsManager splitsManagerInstance = splitsManagerGameObject.AddComponent<SplitsManager>();
        splitsManagerInstance.SetupManager(guiManager, componentList);
        return splitsManagerInstance;
    }

    /// <summary>
    /// Method that initializes the SplitsManager using a GUIManager to copy components from.
    /// </summary>
    public void SetupManager(GUIManager guiManager, List<InfoComponentTemplate> componentList = null)
    {
        isSetup = true;
        SplitsStatsPlugin.Logger.LogInfo($"Creating SplitsManager GameObject...");
        topLeftComponents = new BaseUIComponentList();
        topRightComponents = new BaseUIComponentList();

        // Find Ascent UI to duplicate.
        RectTransform ascentUITransform = guiManager.GetComponentInChildren<AscentUI>().GetComponent<RectTransform>();
        if (SettingsManager.showCurrentCategory && SettingsManager.isCategorized) ascentUITransform.sizeDelta = new Vector2(1000f, ascentUITransform.sizeDelta.y);
        topLeftInfoObject = UnityEngine.Object.Instantiate(ascentUITransform, ascentUITransform.parent);
        topLeftInfoObject.name = $"Splits Manager";
        UnityEngine.Object.Destroy(topLeftInfoObject.GetComponent<AscentUI>());

        topRightInfoObject = (RectTransform)UnityEngine.Object.Instantiate<Transform>(ascentUITransform, ascentUITransform.parent);
        topRightInfoObject.name = $"Info Manager";
        UnityEngine.Object.Destroy(topRightInfoObject.GetComponent<AscentUI>());

        // Initialize data of SplitsManager
        Vector2 topLeftPivot = new Vector2(0.0f, 1f);
        topLeftInfoObject.anchorMin = topLeftPivot;
        topLeftInfoObject.anchorMax = topLeftPivot;
        topLeftInfoObject.offsetMin = Vector2.zero;
        topLeftInfoObject.offsetMax = Vector2.zero;
        topLeftInfoObject.pivot = topLeftPivot;
        topLeftInfoObject.anchoredPosition = Vector2.zero;
        topLeftInfoObject.anchoredPosition += UI_OFFSET;
        topLeftInfoObject.localScale = new Vector3(SettingsManager.uiScaleSize, SettingsManager.uiScaleSize, SettingsManager.uiScaleSize);

        Vector2 topRightPivot = new Vector2(1.0f, 1f);
        topRightInfoObject.anchorMin = topRightPivot;
        topRightInfoObject.anchorMax = topRightPivot;
        topRightInfoObject.offsetMin = Vector2.zero;
        topRightInfoObject.offsetMax = Vector2.zero;
        topRightInfoObject.pivot = topRightPivot;
        topRightInfoObject.anchoredPosition = Vector2.zero;
        topRightInfoObject.anchoredPosition += new Vector2(-UI_OFFSET.x - 4f, UI_OFFSET.y - 5f);
        topRightInfoObject.localScale = new Vector3(SettingsManager.uiScaleSize, SettingsManager.uiScaleSize, SettingsManager.uiScaleSize);

        Destroy(topLeftInfoObject.GetComponent<TMP_Text>());
        Destroy(topRightInfoObject.GetComponent<TMP_Text>());

        TimerComponent CreateTimerComponent(InfoComponentTemplate template, bool addToSide = true)
        {
            TimerComponent newComponent;
            switch (template.position)
            {
                case UIComponentPosition.TopLeft:
                    newComponent = TimerComponent.CreateTimerComponent(template);
                    break;
                case UIComponentPosition.TopRight:
                    newComponent = TimerComponent.CreateTimerComponent(template);
                    break;
                default:
                    throw new NotSupportedException($"Alignment of value {template.position} not supported!");
            }
            if (addToSide) AddInfoComponentToSide(newComponent);
            return newComponent;
        }

        // Create the main timer.
        if (SettingsManager.timersEnabled)
        {
            InfoComponentTemplate mainTimerTemplate = new("Main Timer", icon: SplitsStatsPlugin.LoadSprite(stopwatchImgPath), initialFontSize: HEADER_FONT_SIZE, position: UIComponentPosition.TopLeft);
            mainTimer = CreateTimerComponent(mainTimerTemplate, false);
            mainTimer.SetInactiveColor(new Color(0.7f, 0.7f, 0.7f));
            mainTimer.SetPaceTextActive(false);
            mainTimer.transform.parent = topLeftInfoObject;
        }

        InfoComponent CreateInfoComponent(InfoComponentTemplate template, bool addToSide = true)
        {
            InfoComponent newComponent;
            switch (template.position)
            {
                case UIComponentPosition.TopLeft:
                    newComponent = InfoComponent.CreateInfoComponent(template);
                    break;
                case UIComponentPosition.TopRight:
                    newComponent = InfoComponent.CreateInfoComponent(template);
                    break;
                default:
                    throw new NotSupportedException($"Alignment of value {template.position} not supported!");
            }
            if (addToSide) AddInfoComponentToSide(newComponent);
            return newComponent;
        }

        // Create the height status.
        if (SettingsManager.showCurrentHeight)
        {
            InfoComponentTemplate heightTemplate = new(HEIGHT_STAT_NAME, GetHeightText, SplitsStatsPlugin.LoadSprite(heightImgPath));
            CreateInfoComponent(heightTemplate);
        }

        // Create the campfire status.
        if (SettingsManager.showDistanceFromFire)
        {
            InfoComponentTemplate campfireTemplate = new(CAMPFIRE_STAT_NAME, GetDistanceToObjectiveString, SplitsStatsPlugin.LoadSprite(campfireImgPath), color: new Color(0.845f, 0.762f, 0.73f));
            campfireComponent = CreateInfoComponent(campfireTemplate);

        }

        // Add any remaining custom objects.
        if (componentList != null)
        {
            foreach (InfoComponentTemplate currTemplate in componentList)
            {
                CreateInfoComponent(currTemplate);
            }
        }

        // Create a timer for each segment.
        if (SettingsManager.segmentTimersEnabled && SettingsManager.timersEnabled)
        {
            splitTimers = new Dictionary<Segment, TimerComponent>();
            foreach (Segment currSegment in Enum.GetValues(typeof(Segment)))
            {
                if (currSegment == Segment.Peak) break;
                SplitsStatsPlugin.Logger.LogInfo($"Creating {currSegment} Timer...");

                // Create the timer.
                InfoComponentTemplate splitTimerTemplate = new($"{currSegment} Split Timer", icon: SplitsStatsPlugin.LoadSprite(splitImgPaths[currSegment]), initialFontSize: INACTIVE_FONT_SIZE, position: UIComponentPosition.TopLeft, priority: 10 * ((int)currSegment + 1));
                splitTimers[currSegment] = CreateTimerComponent(splitTimerTemplate);

                splitTimers[currSegment].precisionDigits = SettingsManager.precisionInTimer;

                Color segmentColor = SettingsManager.useColorSegments ? splitColors[currSegment] : Color.white;
                splitTimers[currSegment].SetInitialColor(segmentColor * INITIAL_COLOR_SCALE);
                splitTimers[currSegment].SetActiveColor(segmentColor);
                splitTimers[currSegment].SetInactiveColor(segmentColor * INACTIVE_COLOR_SCALE);
                splitTimers[currSegment].SetPaceTextActive(false);
                splitTimers[currSegment].SetSortingPriority(10 * ((int)currSegment + 1));

                SplitsStatsPlugin.Logger.LogInfo($"{currSegment} Timer created!");
            }
        }

        UpdateTimerPositions();

        // Find current MapHandler
        foreach (GameObject currGameObject in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            currMapHandler = currGameObject.GetComponentInChildren<MapHandler>();
            if (currMapHandler != null) break;
        }

        Instance = this;
    }

    public static void SetAlignment(RectTransform currObject, UIComponentPosition position)
    {
        switch (position)
        {
            case UIComponentPosition.TopLeft:
                Vector2 topLeftPivot = new Vector2(0.0f, 1f);
                currObject.anchorMin = topLeftPivot;
                currObject.anchorMax = topLeftPivot;
                currObject.pivot = new Vector2(0.0f, PIVOT_Y);
                return;
            case UIComponentPosition.TopRight:
                Vector2 topRightPivot = new Vector2(1.0f, 1f);
                currObject.anchorMin = topRightPivot;
                currObject.anchorMax = topRightPivot;
                currObject.pivot = new Vector2(1.0f, PIVOT_Y);
                return;
            default:
                throw new NotSupportedException($"Alignment of value {position} not supported!");
        }
    }

    private void Update()
    {
        // Update timer positions if they're being animated right now.
        UpdateTimerPositions();
        if (SettingsManager.timersEnabled && SettingsManager.segmentTimersEnabled && SettingsManager.showPaceNearGoals && SettingsManager.paceTextEnabled) ShowPaceNearGoals();
    }

    /// <summary>
    /// Call to update the position of the timers based on their sizes. Best to call after changing font sizes.
    /// </summary>
    public void UpdateTimerPositions()
    {
        setupCheck();

        float currentYPos = 0f;
        float currFontSize;

        void AppendObject(RectTransform currObject, float objectHeight)
        {
            currentYPos += objectHeight * (1f - PIVOT_Y);

            currObject.anchoredPosition = Vector2.zero;
            currObject.anchoredPosition += new Vector2(0f, -currentYPos);

            currentYPos += objectHeight * PIVOT_Y + LINE_SPACING;
        }

        void AppendInfo(RectTransform currObject, BaseUIComponent currInfo)
        {
            if (currObject == null || currInfo == null) return;
            currFontSize = currInfo.GetHeight();
            AppendObject(currObject, currFontSize * 0.83f);
        }

        void AppendFromList(BaseUIComponentList list)
        {
            foreach (BaseUIComponent currComponent in list)
            {
                RectTransform currTransform = currComponent.rectTransform;
                AppendInfo(currTransform, currComponent);
            }
        }

        // Move main timer.
        if (mainTimer != null)
        {
            RectTransform currTransform = mainTimer.rectTransform;
            AppendInfo(currTransform, mainTimer);
            currentYPos += HEADER_SPACING - LINE_SPACING;
        }

        // Move split timers.
        AppendFromList(topLeftComponents);

        // Move info stats.
        currentYPos = 0;
        AppendFromList(topRightComponents);
    }

    /// <summary>
    /// Call to update the visibility of the split pace/intervals based on how close the player is to finishing the run/segment.
    /// </summary>
    public void ShowPaceNearGoals()
    {
        if (currMapHandler != null)
        {
            Segment currSegment = currMapHandler.GetCurrentSegment();
            if (currSegment >= Segment.Peak) return;

            if (!splitTimers.ContainsKey(currSegment)) return;
            TimerComponent currTimer = splitTimers[currSegment];

            MapHandler.MapSegment currMapSegment = currMapHandler.segments[(int)currSegment];
            Transform currCampfire = currMapSegment?.segmentCampfire?.GetComponentInChildren<Campfire>()?.transform;
            Transform currStatue = currMapSegment?.segmentParent?.GetComponentInChildren<RespawnChest>()?.transform;
            Vector3 currCharacterPosition = GetLocalCharacterPosition();
            if (currCharacterPosition == Vector3.zero) return;

            if (currSegment == Segment.TheKiln)
            {
                if (!currTimer.GetPaceTextActive() && Character.localCharacter.refs.stats.heightInMeters >= 1900f - (PACE_TRIGGER_DISTANCE * 0.7f))
                    currTimer.SetPaceTextActive(true);
            }
            else if (currCharacterPosition != null && currCampfire != null)
            {
                Vector3 separationVector = (currSegment == Segment.Caldera && currStatue != null) ? currCharacterPosition - currStatue.position : currCharacterPosition - currCampfire.position;
                float distanceSeparated = separationVector.magnitude * CharacterStats.unitsToMeters;

                if (distanceSeparated <= PACE_TRIGGER_DISTANCE && !currTimer.GetPaceTextActive())
                    currTimer.SetPaceTextActive(true);
            }
        }
    }

    /// <summary>
    /// Get the position of the local character's position. Also accounts for if the player is dead and a ghost.
    /// </summary>
    /// <returns>Position of the local character. Returns Vector3.zero if the local character couldn't be found.</returns>
    public static Vector3 GetLocalCharacterPosition()
    {
        try
        {
            if (Character.localCharacter?.Ghost?.transform != null) return Character.localCharacter.Ghost.transform.position;
            else if (Character.localCharacter?.refs.hip.Rig.transform != null && Character.localCharacter?.data.dead == false) return Character.localCharacter.refs.hip.Rig.transform.position;
            else return Vector3.zero;
        }
        catch (NullReferenceException)
        {
            return Vector3.zero;
        }
    }

    /// <summary>
    /// Get the position of the player's next objective (campfires then the peak).
    /// </summary>
    /// <returns>Position of the next objective. Returns Vector3.zero if the objective's object couldn't be found.</returns>
    public Vector3 GetNextObjectivePosition()
    {
        if (currMapHandler != null)
        {
            Segment currSegment = currMapHandler.GetCurrentSegment();
            if (currSegment >= Segment.Peak) return Vector3.zero;

            MapHandler.MapSegment currMapSegment = currMapHandler.segments[(int)currSegment];
            Transform currCampfire = currMapSegment?.segmentCampfire?.GetComponentInChildren<Campfire>()?.transform;

            if (currSegment == Segment.TheKiln)
            {
                GameObject volcanoSegmentObject = currMapSegment.segmentParent.transform.parent.gameObject;
                foreach (Transform child in volcanoSegmentObject.GetComponentsInChildren<Transform>())
                {
                    if (child.gameObject.name == "Flag Pole") return child.position;
                }
            }
            else if (currCampfire != null)
                return currCampfire.position;
        }
        return Vector3.zero;
    }

    /// <summary>
    /// Returns text representing the height/altitude of the local character, or null if the local character couldn't be found.
    /// </summary>
    public static string GetHeightText()
    {
        Vector3 currCharacterPosition = GetLocalCharacterPosition();
        if (currCharacterPosition != Vector3.zero) return $"{(int)(currCharacterPosition.y * CharacterStats.unitsToMeters)}m";
        return null;
    }

    /// <summary>
    /// Returns text representing the local character's distance away from a campfire/peak, or null if the local character or objective object couldn't be found.
    /// </summary>
    public string GetDistanceToObjectiveString()
    {
        Vector3 currCharacterPos = GetLocalCharacterPosition();
        Vector3 currObjectivePos = GetNextObjectivePosition();
        if (currCharacterPos == Vector3.zero || currObjectivePos == Vector3.zero) return null;
        return $"{(int)((currCharacterPos - currObjectivePos).magnitude * CharacterStats.unitsToMeters)}m";
    }

    private InfoComponent campfireComponent = null;
    public void ChangeCampfireIcon(Sprite newIcon)
    {
        if (campfireComponent == null) return;
        campfireComponent.iconRectTransform.GetComponent<UnityEngine.UI.Image>().sprite = newIcon;
    }

    /// <summary>
    /// Start the timer of a specific segment at the current time.
    /// </summary>
    /// <param name="targetSegment"> The target segment to start a timer for. </param>
    /// <returns> A boolean indicating if the timer was successfully started. 
    /// If the timer was started previously and hasn't been stopped yet then this will return false and have no affect on the timer. </returns>
    public bool StartTimer(Segment targetSegment)
    {
        setupCheck();
        if (!splitTimers.ContainsKey(targetSegment)) return false;
        return splitTimers[targetSegment].StartTimer();
    }

    /// <summary>
    /// Start the timer of a specific segment and set it's start time to a specified time.
    /// </summary>
    /// <param name="targetSegment"> The target segment to start a timer for. </param>
    /// <param name="startTime"> The start time to save into the timer. </param>
    /// <returns> A boolean indicating if the timer was successfully modified. 
    /// If the timer was started previously and hasn't been stopped yet then this will return false and have no affect on the timer and it's data. </returns>
    public bool StartTimerAtTime(Segment targetSegment, float startTime)
    {
        setupCheck();
        if (!splitTimers.ContainsKey(targetSegment)) return false;
        return splitTimers[targetSegment].StartAtTime(startTime);
    }

    /// <summary>
    /// End the timer of a specific segment at the current time.
    /// </summary>
    /// <param name="targetSegment"> The target segment to end a timer for. </param>
    /// <returns> A boolean indicating if the timer was successfully stopped. 
    /// If the timer was stopped previously and hasn't been started yet then this will return false and have no affect on the timer. </returns>
    public bool EndTimer(Segment targetSegment)
    {
        setupCheck();
        if (!splitTimers.ContainsKey(targetSegment)) return false;
        return splitTimers[targetSegment].EndTimer();
    }

    /// <summary>
    /// End the timer of a specific segment and set it's end time to a specified time.
    /// </summary>
    /// <param name="targetSegment"> The target segment to end a timer for. </param>
    /// <param name="endTime"> The end time to save into the timer. </param>
    /// <returns> A boolean indicating if the timer was successfully modified. 
    /// If the timer was stopped previously and hasn't been started yet then this will return false and have no affect on the timer and it's data. </returns>
    public bool EndTimerAtTime(Segment targetSegment, float endTime)
    {
        setupCheck();
        if (!splitTimers.ContainsKey(targetSegment)) return false;
        return splitTimers[targetSegment].EndAtTime(endTime);
    }

    /// <summary>
    /// Set the font size for a specific segment's timer.
    /// </summary>
    /// <param name="targetSegment"> The target segment of the timer to change font size. </param>
    /// <param name="newFontSize"> The new font size to set. </param>
    /// <returns> A boolean indicating if the timer was successfully modified. </returns>
    public bool SetTimerFontSize(Segment targetSegment, float newFontSize)
    {
        setupCheck();
        if (!splitTimers.ContainsKey(targetSegment)) return false;
        splitTimers[targetSegment].SetHeight(newFontSize);
        return true;
    }

    /// <summary>
    /// Get the font size for a specific segment's timer.
    /// </summary>
    /// <param name="targetSegment"> The target segment of the timer to get font size from. </param>
    /// <returns> The font size of the desired segment or -1.0f if unable to find/read the font size. </returns>
    public float GetTimerFontSize(Segment targetSegment)
    {
        setupCheck();
        if (!splitTimers.ContainsKey(targetSegment)) return -1.0f;
        return splitTimers[targetSegment].GetHeight();
    }
}