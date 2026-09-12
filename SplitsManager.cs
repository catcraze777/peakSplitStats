using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.IO;
using System.Collections.Generic;
using Zorro.Core;
using Peak;

namespace SplitsStats;

public class SplitsManager : MonoBehaviour
{
    public static SplitsManager Instance;

    public RectTransform topLeftInfoObject;
    public RectTransform topRightInfoObject;
    public Vector2 TopLeftInfoObjectPosition
    {
        get
        {
            return UI_OFFSET;
        }
    }
    public Vector2 TopRightInfoObjectPosition
    {
        get
        {
            return new Vector2(-UI_OFFSET.x - 4f, UI_OFFSET.y - 5f);
        }
    }

    public static BaseUIComponentList topLeftComponents;
    public static BaseUIComponentList topRightComponents;
    public void AddInfoComponentToSide(BaseUIComponent input)
    {
        switch (input.uiPosition)
        {
            case UIComponentPosition.TopLeft:
                topLeftComponents.Add(input);
                input.transform.SetParent(topLeftInfoObject);
                break;
            case UIComponentPosition.TopRight:
                topRightComponents.Add(input);
                input.transform.SetParent(topRightInfoObject);
                break;
            default:
                throw new NotSupportedException($"Alignment of value {input.uiPosition} not supported!");
        }
    }
    public Dictionary<Segment, TimerComponent> splitTimers;

    public static readonly Dictionary<Segment,string> segmentLabels = new Dictionary<Segment,string>
    {
        {Segment.Beach,     "Shore"},
        {Segment.Tropics,   "Biome 2"},
        {Segment.Alpine,    "Biome 3"},
        {Segment.Caldera,   "Biome 4-1"},
        {Segment.TheKiln,   "Biome 4-2"}
    };

    public static readonly Dictionary<Segment, Color> segmentColors = new Dictionary<Segment, Color>
    {
        {Segment.Beach,     new Color(0.984f, 1.0f, 0.719f)},
        {Segment.Tropics,   new Color(0.80f, 0.8f, 0.80f)},
        {Segment.Alpine,    new Color(0.80f, 0.8f, 0.80f)},
        {Segment.Caldera,   new Color(0.80f, 0.8f, 0.80f)},
        {Segment.TheKiln,   new Color(0.80f, 0.8f, 0.80f)},
        {Segment.Void,      new Color(0.716f, 0.675f, 0.758f)}
    };
    
    // Nasty nested dictionary because the caldera and kiln segments share biome types, so cannot index correct color with biome type alone.
    public static readonly Dictionary<Segment,Dictionary<Biome.BiomeType, Color>> biomeColors = new Dictionary<Segment, Dictionary<Biome.BiomeType, Color>>
    {
        {
            Segment.Beach, new Dictionary<Biome.BiomeType, Color>
            {
                {Biome.BiomeType.Shore,     new Color(0.984f, 1.0f, 0.719f)}
            }
        },
        {
            Segment.Tropics, new Dictionary<Biome.BiomeType, Color>
            {
                {Biome.BiomeType.Tropics,   new Color(0.557f, 1.0f, 0.566f)},
                {Biome.BiomeType.Roots,     new Color(1.0f, 0.661f, 0.557f)}
            }
        },
        {
            Segment.Alpine, new Dictionary<Biome.BiomeType, Color>
            {
                {Biome.BiomeType.Alpine,   new Color(0.25f, 1.0f, 0.984f)},
                {Biome.BiomeType.Mesa,     new Color(1.0f, 0.923f, 0.632f)}
            }
        },
        {
            Segment.Caldera, new Dictionary<Biome.BiomeType, Color>
            {
                {Biome.BiomeType.Volcano,   new Color(1.0f, 0.578f, 0.25f)},
                {Biome.BiomeType.Swamp,     new Color(0.882f, 0.539f, 1.0f)}
            }
        },
        {
            Segment.TheKiln, new Dictionary<Biome.BiomeType, Color>
            {
                {Biome.BiomeType.Volcano,   new Color(1.0f, 0.344f, 0.25f)},
                {Biome.BiomeType.Swamp,     new Color(0.594f, 0.539f, 1.0f)}
            }
        },
        {
            Segment.Void, new Dictionary<Biome.BiomeType, Color>
            {
                {Biome.BiomeType.Void,      new Color(0.716f, 0.675f, 0.758f)}
            }
        }
    };

    public const string stopwatchImgPath = "img_stopwatch.png";
    public const string heightImgPath = "img_height.png";
    public const string campfireImgPath = "img_campfire.png";
    public const string peakImgPath = "img_peak.png";
    public const string peakGateImgPath = "img_peak_gate.png";

    public const string ATTEMPT_STAT_NAME = "Attempt Stat";
    public const string HEIGHT_STAT_NAME = "Height Stat";
    public const string CAMPFIRE_STAT_NAME = "Campfire Stat";

    public static readonly Dictionary<Segment, string> segmentImgPaths = new Dictionary<Segment, string>
    {
        {Segment.Beach,     "img_shore.png"},
        {Segment.Tropics,   "img_biome2.png"},
        {Segment.Alpine,    "img_biome3.png"},
        {Segment.Caldera,   "img_biome4.png"},
        {Segment.TheKiln,   "img_biome5.png"},
        {Segment.Void,      "img_nadir.png"}
    };

    // Nasty nested dictionary because the caldera and kiln segments share biome types, so cannot index correct image with biome type alone.
    public static readonly Dictionary<Segment, Dictionary<Biome.BiomeType, string>> biomeImgPaths = new Dictionary<Segment, Dictionary<Biome.BiomeType, string>>
    {
        {
            Segment.Beach, new Dictionary<Biome.BiomeType, string>
            {
                {Biome.BiomeType.Shore,     "img_shore.png"}
            }
        },
        {
            Segment.Tropics, new Dictionary<Biome.BiomeType, string>
            {
                {Biome.BiomeType.Tropics,   "img_tropics.png"},
                {Biome.BiomeType.Roots,     "img_roots.png"}
            }
        },
        {
            Segment.Alpine, new Dictionary<Biome.BiomeType, string>
            {
                {Biome.BiomeType.Alpine,   "img_alpine.png"},
                {Biome.BiomeType.Mesa,     "img_mesa.png"}
            }
        },
        {
            Segment.Caldera, new Dictionary<Biome.BiomeType, string>
            {
                {Biome.BiomeType.Volcano,   "img_caldera.png"},
                {Biome.BiomeType.Swamp,     "img_gloom.png"}
            }
        },
        {
            Segment.TheKiln, new Dictionary<Biome.BiomeType, string>
            {
                {Biome.BiomeType.Volcano,   "img_kiln.png"},
                {Biome.BiomeType.Swamp,     "img_citadel.png"}
            }
        },
        {
            Segment.Void, new Dictionary<Biome.BiomeType, string>
            {
                {Biome.BiomeType.Void,      "img_nadir.png"}
            }
        }
    };

    public static (string, Color) getSegmentBiomeImagePathAndColor(Segment inputSegment)
    {
        // Get the current segment's biome icon and color.
        int segmentIdx = inputSegment >= Segment.Peak ? (int)inputSegment - 1 : (int)inputSegment;
        Biome.BiomeType currBiomeType = MapHandler.GetBiomeForSegment(segmentIdx);
        string currTimerImgPath  =  biomeImgPaths[inputSegment][currBiomeType];
        Color currTimerColor     =  biomeColors[inputSegment][currBiomeType];

        return (currTimerImgPath, currTimerColor);
    }

    public const float INITIAL_COLOR_SCALE = 0.7f;
    public const float INACTIVE_COLOR_SCALE = 0.5f;
    public const float ICON_SIZE_SCALE = 1.1f;

    public const float PIVOT_Y = 0.5f;
    public const float PIVOT_Y_ICON = 0.6f;

    public static float PACE_TRIGGER_DISTANCE { get { return SettingsManager.paceTriggerDistance; } }

    public TimerComponent mainTimer;

    private static GameObject ascentUIObject;

    private static float getAscentUIHeight()
    {
        TMP_Text ascentText = ascentUIObject.GetComponent<TMP_Text>();

        if (!ascentUIObject.activeSelf || ascentText.text.Trim().Length <= 0) return 0.0f;
        return ascentText.GetPreferredValues().y;
    }

    /// <summary>
    /// Use RunSaveManager to set the target times for the timers.
    /// </summary>
    public void SetRunTargets()
    {
        if (SettingsManager.disablePaceCustomRuns && RunSaveManager.currentRun.customRun)
        {
            SplitsStatsPlugin.Logger.LogInfo($"Playing custom run with pace text disabled, not setting run targets.");
            return;
        }
        
        setupCheck();

        SplitsStatsPlugin.Logger.LogInfo($"Loading run targets...");

        if (RunSettings.isMiniRun && mainTimer != null)
        {
            Segment minirunBiome = (Segment)RunSettings.GetValue(RunSettings.SETTINGTYPE.MiniRunBiome);
            float targetTime = -1.0f;
            float recordTime = -1.0f;
            if (minirunBiome == Segment.Caldera)
            {
                targetTime = RunSaveManager.targetBiome4;
                if (SettingsManager.useAverageRun) recordTime = RunSaveManager.fastestBiome4;
            }
            else
            {
                targetTime = RunSaveManager.targetSegment(minirunBiome);
                if (SettingsManager.useAverageRun) recordTime = RunSaveManager.fastestSegment(minirunBiome);
            }
            mainTimer.targetRunTime = targetTime;
            mainTimer.recordTime = recordTime;
        }
        else if (RunSaveManager.targetRun.finalTime > 0.0f && mainTimer != null)
        {
            mainTimer.targetRunTime = RunSaveManager.targetRun.finalTime;
            mainTimer.recordTime = SettingsManager.useAverageRun ? RunSaveManager.fastestRun.finalTime : RunSaveManager.SumOfBest;
        }

        if (RunSaveManager.targetRun.shoreTime > 0.0f      && splitTimers.ContainsKey(Segment.Beach))         splitTimers[Segment.Beach].targetRunTime   = RunSaveManager.targetRun.shoreTime;
        if (RunSaveManager.targetRun.tropicsTime > 0.0f    &&  splitTimers.ContainsKey(Segment.Tropics))      splitTimers[Segment.Tropics].targetRunTime = RunSaveManager.targetRun.tropicsTime + (SettingsManager.useAverageRun ? 0.0f : splitTimers[Segment.Beach].targetRunTime);
        if (RunSaveManager.targetRun.alpmesaTime > 0.0f    &&  splitTimers.ContainsKey(Segment.Alpine))       splitTimers[Segment.Alpine].targetRunTime  = RunSaveManager.targetRun.alpmesaTime + (SettingsManager.useAverageRun ? 0.0f : splitTimers[Segment.Tropics].targetRunTime);
        if (RunSaveManager.targetRun.calderaTime > 0.0f    &&  splitTimers.ContainsKey(Segment.Caldera))      splitTimers[Segment.Caldera].targetRunTime = RunSaveManager.targetRun.calderaTime + (SettingsManager.useAverageRun ? 0.0f : splitTimers[Segment.Alpine].targetRunTime);
        if (RunSaveManager.targetRun.kilnTime > 0.0f       &&  splitTimers.ContainsKey(Segment.TheKiln))      splitTimers[Segment.TheKiln].targetRunTime = RunSaveManager.targetRun.kilnTime    + (SettingsManager.useAverageRun ? 0.0f : splitTimers[Segment.Caldera].targetRunTime);
        if (RunSaveManager.targetRun.nadirTime > 0.0f      &&  splitTimers.ContainsKey(Segment.Void))         splitTimers[Segment.Void].targetRunTime    = RunSaveManager.targetRun.nadirTime   + (SettingsManager.useAverageRun ? 0.0f : splitTimers[Segment.TheKiln].targetRunTime);

        if (RunSaveManager.fastestShore > 0.0f      && splitTimers.ContainsKey(Segment.Beach))         splitTimers[Segment.Beach].recordTime   = RunSaveManager.fastestShore;
        if (RunSaveManager.fastestTropics > 0.0f    &&  splitTimers.ContainsKey(Segment.Tropics))      splitTimers[Segment.Tropics].recordTime = RunSaveManager.fastestTropics;
        if (RunSaveManager.fastestAlpmesa > 0.0f    &&  splitTimers.ContainsKey(Segment.Alpine))       splitTimers[Segment.Alpine].recordTime  = RunSaveManager.fastestAlpmesa;
        if (RunSaveManager.fastestCaldera > 0.0f    &&  splitTimers.ContainsKey(Segment.Caldera))      splitTimers[Segment.Caldera].recordTime = RunSaveManager.fastestCaldera;
        if (RunSaveManager.fastestKiln > 0.0f       && splitTimers.ContainsKey(Segment.TheKiln))       splitTimers[Segment.TheKiln].recordTime = RunSaveManager.fastestKiln;
        if (RunSaveManager.fastestNadir > 0.0f      &&  splitTimers.ContainsKey(Segment.Void))         splitTimers[Segment.Void].recordTime    = RunSaveManager.fastestNadir;
        
        SplitsStatsPlugin.Logger.LogInfo($"Loaded run targets!");
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
        SplitsStatsPlugin.Logger.LogInfo($"Creating SplitsManager GameObject...");
        topLeftComponents = new BaseUIComponentList();
        topRightComponents = new BaseUIComponentList();
        _flagPolePosition = Vector3.zero;
        campfirePositions = new Dictionary<Segment, Vector3>();

        // Find Ascent UI to duplicate.
        SplitsStatsPlugin.Logger.LogInfo($"Creating SplitsManager object anchors...");
        SplitsManager.ascentUIObject = guiManager.GetComponentInChildren<AscentUI>().gameObject;
        RectTransform ascentUITransform = SplitsManager.ascentUIObject.GetComponent<RectTransform>();
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
        topLeftInfoObject.localScale = new Vector3(SettingsManager.uiScaleSize, SettingsManager.uiScaleSize, SettingsManager.uiScaleSize);

        Vector2 topRightPivot = new Vector2(1.0f, 1f);
        topRightInfoObject.anchorMin = topRightPivot;
        topRightInfoObject.anchorMax = topRightPivot;
        topRightInfoObject.offsetMin = Vector2.zero;
        topRightInfoObject.offsetMax = Vector2.zero;
        topRightInfoObject.pivot = topRightPivot;
        topRightInfoObject.localScale = new Vector3(SettingsManager.uiScaleSize, SettingsManager.uiScaleSize, SettingsManager.uiScaleSize);

        UpdateInfoObjects();

        Destroy(topLeftInfoObject.GetComponent<TMP_Text>());
        Destroy(topRightInfoObject.GetComponent<TMP_Text>());

        SplitsStatsPlugin.Logger.LogInfo($"Initialized SplitsManager object anchors, beginning to load components...");

        // Helper function to add timer components to the screen based on their configured position.
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
            SplitsStatsPlugin.Logger.LogInfo($"Created new \"{template.name}\" timer component...");
            return newComponent;
        }

        // Create the main timer.
        if (SettingsManager.timersEnabled)
        {
            InfoComponentTemplate mainTimerTemplate = new("Main Timer", icon: SplitsStatsPlugin.LoadSprite(stopwatchImgPath), initialFontSize: HEADER_FONT_SIZE, position: UIComponentPosition.TopLeft);
            mainTimer = CreateTimerComponent(mainTimerTemplate, false);
            mainTimer.SetInactiveColor(new Color(0.7f, 0.7f, 0.7f));
            mainTimer.SetPaceTextActive(false);
            mainTimer.transform.SetParent(topLeftInfoObject);
        }

        // Helper function to add generic info components to the screen based on their configured position.
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
            SplitsStatsPlugin.Logger.LogInfo($"Created new \"{template.name}\" info component...");
            return newComponent;
        }

        // Create the height status.
        if (SettingsManager.showCurrentAttemptNumber && RunSaveManager.IsRunValid())
        {
            InfoComponentTemplate attemptNumberTemplate = new(ATTEMPT_STAT_NAME, () => $"Attempt {RunSaveManager.totalAttempts + 1}");
            CreateInfoComponent(attemptNumberTemplate);
        }

        // Create the height status.
        if (SettingsManager.showCurrentHeight)
        {
            InfoComponentTemplate heightTemplate = new(HEIGHT_STAT_NAME, GetHeightText, SplitsStatsPlugin.LoadSprite(heightImgPath), color: new Color(0.845f, 0.833f, 0.73f));
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
        splitTimers = new Dictionary<Segment, TimerComponent>();
        if (SettingsManager.segmentTimersEnabled && SettingsManager.timersEnabled && !RunSettings.isMiniRun)
        {
            foreach (Segment currSegment in Enum.GetValues(typeof(Segment)))
            {
                if (currSegment == Segment.Peak) continue;
                SplitsStatsPlugin.Logger.LogInfo($"Creating {currSegment} Timer...");

                // Get the current segment's biome icon and color.
                (string currTimerImgPath, Color currTimerColor) = SettingsManager.sharedBiomeIcons ? (segmentImgPaths[currSegment], segmentColors[currSegment]) : getSegmentBiomeImagePathAndColor(currSegment);
                if (!SettingsManager.useColorSegments) currTimerColor = Color.white;

                // Create the timer.
                InfoComponentTemplate splitTimerTemplate = new($"{currSegment} Split Timer", icon: SplitsStatsPlugin.LoadSprite(currTimerImgPath),
                                                                    initialFontSize: INACTIVE_FONT_SIZE, position: UIComponentPosition.TopLeft,
                                                                    isHidden: SettingsManager.hiddenSegments, priority: 10 * ((int)currSegment + 1));
                
                if (currSegment == Segment.Void && ! SettingsManager.alwaysShowNadirSegment) splitTimerTemplate.isHidden = true;
                splitTimers[currSegment] = CreateTimerComponent(splitTimerTemplate);

                splitTimers[currSegment].precisionDigits = SettingsManager.precisionInTimer;
                splitTimers[currSegment].SetInitialColor(currTimerColor * INITIAL_COLOR_SCALE);
                splitTimers[currSegment].SetActiveColor(currTimerColor);
                splitTimers[currSegment].SetInactiveColor(currTimerColor * INACTIVE_COLOR_SCALE);
                splitTimers[currSegment].SetPaceTextActive(false);
                splitTimers[currSegment].SetSortingPriority(10 * ((int)currSegment + 1));

                SplitsStatsPlugin.Logger.LogInfo($"Configured {currSegment} Timer!");
            }
        }

        UpdateTimerPositions();

        Instance = this;
        SplitsStatsPlugin.Logger.LogInfo($"Finished creating SplitsManager object!");

        isSetup = true;
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
        UpdateInfoObjects();
        if (SettingsManager.timersEnabled && SettingsManager.segmentTimersEnabled && SettingsManager.showPaceNearGoals && SettingsManager.paceTextEnabled) ShowPaceNearGoals();
    }

    /// <summary>
    /// Call to update the positions of the root info objects based on the user's config.
    /// </summary>
    public void UpdateInfoObjects()
    {
        topLeftInfoObject.anchoredPosition = Vector2.zero;
        topLeftInfoObject.anchoredPosition += TopLeftInfoObjectPosition;
        topLeftInfoObject.anchoredPosition += SettingsManager.timerVectorOffset;

        topRightInfoObject.anchoredPosition = Vector2.zero;
        topRightInfoObject.anchoredPosition += TopRightInfoObjectPosition;
        topRightInfoObject.anchoredPosition += SettingsManager.statsVectorOffset;
    }

    /// <summary>
    /// Call to update the position of the timers based on their sizes. Best to call after changing font sizes.
    /// </summary>
    public void UpdateTimerPositions()
    {
        try
        {
            setupCheck();
        }
        catch (InvalidOperationException)
        {
            return;
        }

        float currentYPos = 0f;

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
            if (currInfo.IsHidden)
            {
                currObject.anchoredPosition = new Vector2(0.0f, 100_000.0f);
            }
            else
            {
                float currFontSize = currInfo.GetHeight();
                AppendObject(currObject, currFontSize * 0.83f);
            }
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
        currentYPos = SettingsManager.statsAutoAdjust ? getAscentUIHeight() : 0f;
        AppendFromList(topRightComponents);
    }

    /// <summary>
    /// Call to update the visibility of the split pace/intervals based on how close the player is to finishing the run/segment.
    /// </summary>
    public void ShowPaceNearGoals()
    {
        MapHandler currMapHandler = Singleton<MapHandler>.Instance;
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

    public static Vector3 FlagPolePosition
    {
        get
        {
            if (_flagPolePosition == Vector3.zero) FindFlagPole();
            return _flagPolePosition;
        }
    }
    private static Vector3 _flagPolePosition = Vector3.zero;

    public static Vector3 NadirGatePosition
    {
        get
        {
            if (_nadirGatePosition == Vector3.zero) FindNadirGate();
            return _nadirGatePosition;
        }
    }
    private static Vector3 _nadirGatePosition = Vector3.zero;

    public static Dictionary<Segment, Vector3> campfirePositions = new Dictionary<Segment, Vector3>();

    public static Vector3 FindFlagPole()
    {
        SplitsStatsPlugin.Logger.LogInfo($"Attempting to find flag pole...");

        GameObject volcanoSegmentObject = Singleton<MapHandler>.Instance.segments[(int)Segment.TheKiln].segmentParent.transform.parent.gameObject;
        GameObject peakHandlerObject = volcanoSegmentObject.GetComponentInChildren<PeakHandler>()?.gameObject ?? null;

        foreach (Transform child in peakHandlerObject?.GetComponentsInChildren<Transform>() ?? [])
        {
            if (child.gameObject.name == "Flag Pole")
            {
                SplitsStatsPlugin.Logger.LogInfo($"Flag pole found!");
                _flagPolePosition = child.position;
                return _flagPolePosition;
            }
        }
        SplitsStatsPlugin.Logger.LogWarning($"Couldn't find flag pole!");
        return _flagPolePosition;
    }

    public static Vector3 FindNadirGate()
    {
        SplitsStatsPlugin.Logger.LogInfo($"Attempting to find nadir's peak gate...");

        GameObject nadirSegmentObject = Singleton<MapHandler>.Instance.segments[(int)Segment.Void - 1].segmentParent.transform.parent.gameObject;
        _nadirGatePosition = nadirSegmentObject.GetComponentInChildren<PeakGatePortal>()?.transform.position ?? Vector3.zero;

        if (_nadirGatePosition != Vector3.zero) SplitsStatsPlugin.Logger.LogInfo($"Nadir's peak gate found!");
        else SplitsStatsPlugin.Logger.LogWarning($"Couldn't find nadir's peak gate!");

        return _nadirGatePosition;
    }

    public static Vector3 FindCampfire(Segment targetSegment)
    {
        SplitsStatsPlugin.Logger.LogInfo($"Attempting to find {targetSegment} campfire...");

        MapHandler.MapSegment currMapSegment = Singleton<MapHandler>.Instance.segments[(int)targetSegment];
        Transform currCampfire = currMapSegment?.segmentCampfire?.GetComponentInChildren<Campfire>()?.transform;

        if (currCampfire != null)
        {
            SplitsStatsPlugin.Logger.LogInfo($"Campfire found!");

            campfirePositions[targetSegment] = currCampfire.position;
            return campfirePositions[targetSegment];
        }

        SplitsStatsPlugin.Logger.LogWarning($"Couldn't find the campfire!");
        return campfirePositions.ContainsKey(targetSegment) ? campfirePositions[targetSegment] : Vector3.zero;
    }


    /// <summary>
    /// Get the position of the player's next objective (campfires then the peak).
    /// </summary>
    /// <returns>Position of the next objective. Returns Vector3.zero if the objective's object couldn't be found.</returns>
    public Vector3 GetNextObjectivePosition()
    {
        MapHandler currMapHandler = Singleton<MapHandler>.Instance;
        if (currMapHandler != null)
        {
            Segment currSegment = currMapHandler.GetCurrentSegment();
            if (currSegment == Segment.Void) return NadirGatePosition;
            if (currSegment == Segment.TheKiln) return FlagPolePosition;

            return campfirePositions.ContainsKey(currSegment) ? campfirePositions[currSegment] : FindCampfire(currSegment);
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
        if (currCharacterPos == Vector3.zero || currObjectivePos == Vector3.zero) return "Loading...";
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
        if (splitTimers?.ContainsKey(targetSegment) != true) return false;
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
        if (splitTimers?.ContainsKey(targetSegment) != true) return false;
        return splitTimers[targetSegment].StartTimerAtTime(startTime);
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
        if (splitTimers?.ContainsKey(targetSegment) != true) return false;
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
        if (splitTimers?.ContainsKey(targetSegment) != true) return false;
        return splitTimers[targetSegment].EndTimerAtTime(endTime);
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
        if (splitTimers?.ContainsKey(targetSegment) != true) return false;
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
        if (splitTimers?.ContainsKey(targetSegment) != true) return -1.0f;
        return splitTimers[targetSegment].GetHeight();
    }

    /// <summary>
    /// Set the hidden state for a specific segment's timer.
    /// </summary>
    /// <param name="targetSegment"> The target segment of the timer to change font size. </param>
    /// <param name="isHidden"> The new hidden state to set. </param>
    /// <returns> A boolean indicating if the timer was successfully modified. </returns>
    public bool SetTimerHidden(Segment targetSegment, bool isHidden)
    {
        setupCheck();
        if (splitTimers?.ContainsKey(targetSegment) != true) return false;
        splitTimers[targetSegment].IsHidden = isHidden;
        return true;
    }

    /// <summary>
    /// Get the hidden state for a specific segment's timer.
    /// </summary>
    /// <param name="targetSegment"> The target segment of the timer to get font size from. </param>
    /// <returns> Returns the target segment's hidden status, or null if the timer was not found. </returns>
    public bool? GetTimerHidden(Segment targetSegment)
    {
        setupCheck();
        if (splitTimers?.ContainsKey(targetSegment) != true) return null;
        return splitTimers[targetSegment].IsHidden;
    }

    /// <summary>
    /// Update a timer's icon to use shared or specific icons.
    /// </summary>
    /// <param name="targetSegment"> The target segment of the timer to update. </param>
    /// <param name="sharedIcons"> Whether or not to use shared icons. If left null by default, it will use the configured SettingsManager.sharedBiomeIcons value.</param>
    /// <returns> A boolean indicating if the timer was successfully modified. </returns>
    public bool UpdateTimerIcon(Segment targetSegment, bool? sharedIcons = null)
    {
        bool useSharedIcons = sharedIcons ?? SettingsManager.sharedBiomeIcons;
        if (splitTimers?.ContainsKey(targetSegment) != true) return false;

        (string currTimerImgPath, Color currTimerColor) = useSharedIcons ? (segmentImgPaths[targetSegment], segmentColors[targetSegment]) : getSegmentBiomeImagePathAndColor(targetSegment);
        if (!SettingsManager.useColorSegments) currTimerColor = Color.white;

        splitTimers[targetSegment].iconImage.sprite = SplitsStatsPlugin.LoadSprite(currTimerImgPath);
        splitTimers[targetSegment].SetInitialColor(currTimerColor * INITIAL_COLOR_SCALE);
        splitTimers[targetSegment].SetActiveColor(currTimerColor);
        splitTimers[targetSegment].SetInactiveColor(currTimerColor * INACTIVE_COLOR_SCALE);

        return true;
    }
}