using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Bootstrap;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using Photon.Pun;
using System.Reflection;
using System.IO;
using System.Runtime.CompilerServices;

namespace SplitsStats;

[BepInPlugin("net.catcraze777.plugins.splitsstats", MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency("com.snosz.terrainrandomiser", BepInDependency.DependencyFlags.SoftDependency)]
public class SplitsStatsPlugin : BaseUnityPlugin
{
    public static SplitsStatsPlugin Instance;
    internal static new ManualLogSource Logger;

    private static Harmony _harmony;

    public const string PLUGIN_GUID = "net.catcraze777.plugins.splitsstats";

    private static SplitsManager splitsManagerInstance;

    private static GameObject animManagerGameObject;
    private static AnimationManager animManager;

    private static bool _hasTerrainRandomiser = false;
    public static bool hasTerrainRandomiser { get { return _hasTerrainRandomiser; } private set { _hasTerrainRandomiser = value; } }

    private const float FONT_CHANGE_DURATION = 0.4f;
    private const bool alwaysSave = true;

    internal static List<InfoComponentTemplate> customStats = [];

    internal static List<BaseUIComponent> customUIComponents = [];

    /// <summary>
    /// Add a custom InfoComponent to the SplitsStats UI using an InfoComponentTemplate.
    /// </summary>
    /// <param name="addonTemplate">The template containing info for the created InfoComponent</param>
    public static void AddCustomStat(InfoComponentTemplate addonTemplate)
    {
        customStats.Add(addonTemplate);
    }

    /// <summary>
    /// Add a custom BaseUIComponent to the SplitsStats UI. Should be a RectTransform gameObject with a BaseUIComponent subclass component and it's recommended to ensure all resources used by the component are all children of the component's transform.
    /// </summary>
    /// <param name="newComponent">The custom component to add to the UI.</param>
    public static void AddCustomComponent(BaseUIComponent newComponent)
    {
        customUIComponents.Add(newComponent);
    }

    /// <summary>
    /// Load a sprite from a filepath relative to the calling assembly location.
    /// </summary>
    /// <param name="relativeImgPath"> The file location of the image relative to the calling assembly location. </param>
    public static Sprite LoadSprite(string relativeImgPath)
    {
        try
        {
            string pluginFolder = Path.GetDirectoryName(Assembly.GetCallingAssembly().Location);
            string imgPath = Path.Combine(pluginFolder, relativeImgPath);
            byte[] fileData = System.IO.File.ReadAllBytes(imgPath);

            Texture2D tempTexture = new Texture2D(1, 1);
            tempTexture.LoadImage(fileData);

            return Sprite.Create(tempTexture, new Rect(0, 0, tempTexture.width, tempTexture.height), new Vector2(0.5f, 0.5f));
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Return if the input run is in the same category as the currently active run based on the config settings.
    /// </summary>
    /// <param name="otherRunTime">The time to compare to the active run's category</param>
    /// <returns>Returns true if the <c>otherRunTime</c> is in the same category as the current run. If no run is currently active, this always returns true.</returns>
    public static bool CategorizeByCurrRunConfig(RunTime otherRunTime)
    {
        RunTime currRunTime = RunSaveManager.currentRun;
        if (currRunTime == null) return true;

        if ((SettingsManager.categorizeByGameVersion || SettingsManager.categorizeByLevel) && currRunTime.gameVersion != otherRunTime.gameVersion) return false;
        if (SettingsManager.categorizeByPlayerCount && currRunTime.playerCount != otherRunTime.playerCount) return false;
        if (SettingsManager.categorizeByAscent && currRunTime.ascentDifficulty != otherRunTime.ascentDifficulty) return false;
        if (SettingsManager.categorizeByLevel && currRunTime.levelName != otherRunTime.levelName || currRunTime.wasRandomized != otherRunTime.wasRandomized) return false;
        if ((SettingsManager.categorizeByTerrainRandomizer || SettingsManager.categorizeByLevel) && currRunTime.wasRandomized != otherRunTime.wasRandomized) return false;
        if (SettingsManager.categorizeBySeed && currRunTime.seed != otherRunTime.seed) return false;
        if (SettingsManager.categorizeByCustomRun && currRunTime.customRun != otherRunTime.customRun) return false;
        return true;
    }

    private static DateTime REFERENCE_REAL_TIME = DateTime.UtcNow;
    public static float GetCurrentRealTime()
    {
        return (float)(DateTime.UtcNow - REFERENCE_REAL_TIME).TotalSeconds;
    }

    [HarmonyPatch(typeof(GUIManager), "Start")]
    private class GUIManagerStartPatcher
    {
        private static void Postfix(GUIManager __instance)
        {
            try
            {
                if (SceneManager.GetActiveScene().name != "Airport" && __instance != null)
                {
                    Logger.LogInfo("Starting GUIManager.Start Postfix!");

                    Instance.Config.Reload();

                    splitsManagerInstance = SplitsManager.CreateSplitsManager(__instance, customStats);
                    foreach (BaseUIComponent currComponent in customUIComponents) splitsManagerInstance.AddInfoComponentToSide(currComponent);

                    if (RunSaveManager.IsRunActive())
                    {
                        RunSaveManager.FinishRun();
                        Logger.LogInfo("Ended an active run found stored in the RunSaveManager...");
                    }

                    animManagerGameObject = new GameObject("SplitsStatsPlugin AnimationManager");
                    animManager = animManagerGameObject.AddComponent<AnimationManager>();
                    Logger.LogInfo("Created AnimationManager...");

                    RunSaveManager.StartNewRun();
                    RunSaveManager.currentRun.playerCount = PhotonNetwork.PlayerList.Length;
                    RunSaveManager.currentRun.levelName = SceneManager.GetActiveScene().name;
                    RunSaveManager.currentRun.gameVersion = "v" + Application.version;
                    RunSaveManager.currentRun.ascentDifficulty = Ascents.currentAscent;
                    RunSaveManager.currentRun.customRun = RunSettings.IsCustomRun;

                    if (hasTerrainRandomiser)
                    {
                        RunSaveManager.currentRun.wasRandomized = TerrainRandomiserInteractor.shouldRandomise();
                        RunSaveManager.currentRun.seed = TerrainRandomiserInteractor.masterSeed();
                    }
                    Logger.LogInfo("Loaded run information...");

                    RunSaveManager.GetRunRecords(CategorizeByCurrRunConfig);
                    splitsManagerInstance.SetRunTargets();
                    Logger.LogInfo("Loaded run targets...");

                    Logger.LogInfo($"GUIManager.Start Postfix successfully completed!");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError((object)($"Error in GUIManager.Start patch: {ex.GetType()}" + ex.Message + $"\n{ex.Source}\n{ex.TargetSite}\n{ex.StackTrace}"));
            }
        }
    }
    
    [HarmonyPatch(typeof(RunManager), "StartRun")]
    private class RunManagerStartRunPatcher
    {
        private static void Postfix(RunManager __instance)
        {
            try
            {
                if (SceneManager.GetActiveScene().name != "Airport" && __instance != null)
                {
                    Logger.LogInfo("Starting RunManager.StartRun Postfix!");

                    float startTime = SettingsManager.isRealTime ? GetCurrentRealTime() - RunManager.Instance.TimeSinceRunStarted : Time.time - RunManager.Instance.TimeSinceRunStarted;//__instance.GetFirstTimelineInfo().time;
                    splitsManagerInstance.mainTimer.SetPaceTextActive(SettingsManager.showRunPace && SettingsManager.paceTextEnabled);
                    splitsManagerInstance.mainTimer.StartRunAtTime(startTime);
                    splitsManagerInstance.mainTimer.SetHeight(SplitsManager.HEADER_FONT_SIZE);
                    splitsManagerInstance.SetTimerHidden(Segment.Beach, false);
                    splitsManagerInstance.StartTimerAtTime(Segment.Beach, startTime);
                    splitsManagerInstance.SetTimerFontSize(Segment.Beach, SplitsManager.ACTIVE_FONT_SIZE);
                    splitsManagerInstance.UpdateTimerPositions();
                    Logger.LogInfo($"Started shore timer!");

                    SplitsManager.FindFlagPole();

                    Logger.LogInfo($"RunManager.StartRun Postfix successfully completed!");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError((object)($"Error in RunManager.StartRun patch: {ex.GetType()}" + ex.Message + $"\n{ex.Source}\n{ex.TargetSite}\n{ex.StackTrace}"));
            }
        }
    }

    [HarmonyPatch(typeof(AscentUI), "Start")]
    private class AscentUIPatcher
    {
        private static void Postfix(AscentUI __instance)
        {
            try
            {
                if (SettingsManager.showCurrentCategory && SettingsManager.isCategorized)
                {
                    Logger.LogInfo("Starting AscentUI.Start Postfix!");

                    __instance.gameObject.SetActive(true);
                    if (SettingsManager.categorizeByPlayerCount)
                    {
                        __instance.text.text += $"   {RunSaveManager.currentRun.playerCount} SCOUT";
                        if (RunSaveManager.currentRun.playerCount > 1) __instance.text.text += "S";
                        Logger.LogInfo("Added player count category text!");
                    }
                    if (SettingsManager.categorizeByLevel)
                    {
                        if (RunSaveManager.currentRun.wasRandomized)
                            if (!TerrainRandomiserInteractor.autoRandomise() && SettingsManager.categorizeBySeed) __instance.text.text += $"   SEEDED";
                            else __instance.text.text += $"   RANDOM";
                        else __instance.text.text += $"   {RunSaveManager.currentRun.levelName.Replace("Level_", "DAILY #")}";
                        Logger.LogInfo("Added level category text!");
                    }
                    else if (RunSaveManager.currentRun.wasRandomized && SettingsManager.categorizeByTerrainRandomizer)
                    {
                        if (!TerrainRandomiserInteractor.autoRandomise() && SettingsManager.categorizeBySeed) __instance.text.text += $"   SEEDED";
                        else __instance.text.text += $"   RANDOM";
                        Logger.LogInfo("Added randomizer category text!");
                    }

                    Logger.LogInfo($"AscentUI.Start Postfix successfully completed!");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError((object)($"Error in AscentUI.Start patch: {ex.GetType()}" + ex.Message + $"\n{ex.Source}\n{ex.TargetSite}\n{ex.StackTrace}"));
            }
        }

    }

    [HarmonyPatch(typeof(MapHandler), "GoToSegment")]
    private class MapHandlerPatcher
    {
        private static void Postfix(MapHandler __instance, ref Segment s)
        {
            try
            {
                Logger.LogInfo("Starting MapHandler.GoToSegment Postfix!");

                if (RunSettings.isMiniRun && s != Segment.TheKiln)
                {
                    splitsManagerInstance.mainTimer.EndTimer();
                    splitsManagerInstance.UpdateTimerPositions();
                    return;
                }

                Logger.LogInfo($"Split reached, entering {s}!");

                splitsManagerInstance.StartTimer(s);
                if (splitsManagerInstance.splitTimers.ContainsKey(s))
                {
                    if (SettingsManager.hiddenSegments)
                    {
                        splitsManagerInstance.splitTimers[s].IsHidden = false;
                        splitsManagerInstance.splitTimers[s].SetHeight(0.0f);
                    }
                    animManager.LerpTimerFontSize(splitsManagerInstance.splitTimers[s], SplitsManager.ACTIVE_FONT_SIZE, FONT_CHANGE_DURATION);
                }
                Logger.LogInfo($"Started {s} timer!");

                splitsManagerInstance.EndTimer(s - 1);
                if (splitsManagerInstance.splitTimers.ContainsKey(s - 1))
                {
                    animManager.LerpTimerFontSize(splitsManagerInstance.splitTimers[s - 1], SplitsManager.INACTIVE_FONT_SIZE, FONT_CHANGE_DURATION);
                    RunSaveManager.currentRun[s - 1] = splitsManagerInstance.splitTimers[s - 1].totalTime;
                    RunSaveManager.SaveRun();
                }
                Logger.LogInfo($"Stopped {s - 1} timer!");
                
                if (s == Segment.TheKiln)
                {
                    Sprite newSprite = LoadSprite(SplitsManager.peakImgPath);
                    if (newSprite != null) splitsManagerInstance.ChangeCampfireIcon(newSprite);
                    Logger.LogInfo($"Updated campfire icon to flag!");
                }
                splitsManagerInstance.UpdateTimerPositions();

                Logger.LogInfo($"MapHandler.GoToSegment Postfix successfully completed!");
            }
            catch (Exception ex)
            {
                Logger.LogError((object)($"Error in MapHandler.GoToSegment patch: {ex.GetType()}" + ex.Message + $"\n{ex.Source}\n{ex.TargetSite}\n{ex.StackTrace}"));
            }
        }

    }

    [HarmonyPatch(typeof(RunManager), "EndGame")]
    private class RunManagerPatcher
    {
        private static void Postfix(RunManager __instance)
        {
            try
            {
                Logger.LogInfo("Starting RunManager.EndGame Postfix!");

                splitsManagerInstance.mainTimer.EndTimer();

                if (!SettingsManager.segmentTimersEnabled)
                {
                    Logger.LogInfo("Segment timers disabled, RunManager.EndGame Postfix successfully completed!");
                    return;
                }

                foreach (Segment currSegment in Enum.GetValues(typeof(Segment)))
                {
                    if (currSegment == Segment.Peak) break;

                    Logger.LogInfo($"Stopping {currSegment} timer...");
                    TimerComponent currTimer = splitsManagerInstance.splitTimers[currSegment];
                    bool paceTextOriginalStatus = currTimer.GetPaceTextActive();
                    currTimer.EndTimer();
                    currTimer.SetPaceTextActive(paceTextOriginalStatus);
                    currTimer.SetCurrColor(currTimer.inactiveColor);
                    if (splitsManagerInstance.splitTimers.ContainsKey(currSegment))
                        animManager.LerpTimerFontSize(splitsManagerInstance.splitTimers[currSegment], SplitsManager.INACTIVE_FONT_SIZE, FONT_CHANGE_DURATION);

                    Logger.LogInfo($"Successfully stopped {currSegment} timer!");
                }
                splitsManagerInstance.UpdateTimerPositions();

                Logger.LogInfo("RunManager.EndGame Postfix successfully completed!");
            }
            catch (Exception ex)
            {
                Logger.LogError((object)($"Error in RunManager.EndGame patch: {ex.GetType()}" + ex.Message + $"\n{ex.Source}\n{ex.TargetSite}\n{ex.StackTrace}"));
            }
        }
    }

    [HarmonyPatch(typeof(MountainProgressHandler), "TriggerReached")]
    private class MountainProgressHandlerPatcher
    {
        private static void Postfix(EndScreen __instance, MountainProgressHandler.ProgressPoint progressPoint)
        {
            try
            {
                if (progressPoint.title == "PEAK")
                {
                    Logger.LogInfo($"Starting MountainProgressHandler.TriggerReached postfix!");

                    if (RunSettings.isMiniRun)
                    {
                        Logger.LogInfo($"Peak reached, stopping timer for minirun...");
                        splitsManagerInstance.mainTimer.EndTimer();
                        splitsManagerInstance.UpdateTimerPositions();
                        Logger.LogInfo($"Timer stopped, TriggerReached Postfix successfully completed!");
                        return;
                    }

                    if (SettingsManager.showPaceNearGoals && SettingsManager.paceTextEnabled)
                    {
                        Logger.LogInfo($"Peak reached, showing main timer pace text");
                        splitsManagerInstance.mainTimer.SetPaceTextActive(true);
                    }

                    if (splitsManagerInstance.splitTimers.ContainsKey(Segment.TheKiln))
                    {
                        Logger.LogInfo($"Peak reached, stopping {Segment.TheKiln} timer...");
                        splitsManagerInstance.EndTimer(Segment.TheKiln);
                        animManager.LerpTimerFontSize(splitsManagerInstance.splitTimers[Segment.TheKiln], SplitsManager.INACTIVE_FONT_SIZE, FONT_CHANGE_DURATION);
                        splitsManagerInstance.UpdateTimerPositions();
                        Logger.LogInfo($"{Segment.TheKiln} timer stopped!");

                        RunSaveManager.currentRun[Segment.TheKiln] = splitsManagerInstance.splitTimers[Segment.TheKiln].totalTime;
                        RunSaveManager.SaveRun();
                    }

                    Logger.LogInfo("MountainProgressHandler.TriggerReached Postfix successfully completed!");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError((object)($"Error in MountainProgressHandler.TriggerReached patch: {ex.GetType()}" + ex.Message + $"\n{ex.Source}\n{ex.TargetSite}\n{ex.StackTrace}"));
            }
        }
    }

    [HarmonyPatch(typeof(EndScreen), "GetTimeString")]
    private class EndScreenPatcher
    {
        private static void Postfix(EndScreen __instance, ref string __result, float totalSeconds)
        {
            try
            {
                Logger.LogInfo($"Starting EndScreen.GetTimeString postfix!");

                bool hasWon = Character.localCharacter.refs.stats.won || Character.localCharacter.refs.stats.somebodyElseWon;
                if (hasWon && RunSaveManager.IsRunActive())
                {
                    Logger.LogInfo($"Congrats on the win!");

                    RunSaveManager.currentRun.finalTime = totalSeconds;
                    RunSaveManager.currentRun.runFinished = true;
                }
                else if (RunSaveManager.IsRunActive() && alwaysSave)
                {
                    Logger.LogInfo($"Saving run due to debug variable \"alwaysSave\" being set to true...");

                    RunSaveManager.currentRun.finalTime = totalSeconds;
                    RunSaveManager.currentRun.runFinished = false;
                }
                RunSaveManager.FinishRun();
                Logger.LogInfo($"Run finished...");

                if (SettingsManager.canEditEndScreenTime)
                {
                    Logger.LogInfo($"Editting end screen...");

                    __result += $"." + $"{Mathf.FloorToInt(SettingsManager.precisionInTimer * (totalSeconds % 1f))}".PadLeft((int)SettingsManager.precisionInTimer, '0');
                    if (SettingsManager.isRealTime) __result = TimerComponent.GetTimeString(splitsManagerInstance.mainTimer.totalTime, true, true, SettingsManager.precisionInTimer);
                    __instance.endTime.fontSizeMax = __instance.endTime.fontSize;
                    __instance.endTime.enableAutoSizing = true;
                    RectTransform textTransform = __instance.endTime.gameObject.GetComponent<RectTransform>();
                    textTransform.sizeDelta = new Vector2(-22f, textTransform.sizeDelta.y);
                    textTransform.pivot = Vector2.one;
                    textTransform.anchoredPosition = new Vector2(-9f, textTransform.anchoredPosition.y);
                    Logger.LogInfo($"Editted time string text and object!");

                    float? currPaceNullable = splitsManagerInstance.mainTimer.currPace;
                    float currPace;
                    if (hasWon && SettingsManager.paceTextEnabled && currPaceNullable != null && (!SettingsManager.onlyShowFinalRunPaceIfRecord || (float)currPaceNullable <= 0.0f))
                    {
                        currPace = (float)currPaceNullable;
                        Logger.LogInfo($"Adding pace text to end screen...");

                        RectTransform newPaceTextObject = UnityEngine.Object.Instantiate(textTransform, textTransform.parent);
                        newPaceTextObject.sizeDelta += new Vector2(-40f, 0f);
                        newPaceTextObject.anchoredPosition = new Vector2(newPaceTextObject.anchoredPosition.x, 0f);
                        newPaceTextObject.gameObject.SetActive(true);
                        TMP_Text newPaceText = newPaceTextObject.GetComponent<TMP_Text>();
                        newPaceText.fontSizeMax = 18f;
                        newPaceText.text = TimerComponent.GetTimeString(currPace, false, false, SettingsManager.precisionInTimer > 0 ? 1 : 0, true);

                        if (SettingsManager.useColorPace)
                        {
                            if (currPace <= 0.0f) newPaceText.color = new Color(0.323f, 0.413f, 0.308f);
                            else newPaceText.color = new Color(0.4f, 0.208f, 0.208f);
                        }

                        textTransform.anchoredPosition += new Vector2(0f, -2f);

                        Logger.LogInfo($"Pace text added!");
                    }
                }

                Logger.LogInfo("EndScreen.GetTimeString Postfix successfully completed!");
            }
            catch (Exception ex)
            {
                Logger.LogError((object)($"Error in EndScreen.GetTimeString patch: {ex.GetType()}" + ex.Message + $"\n{ex.Source}\n{ex.TargetSite}\n{ex.StackTrace}"));
            }
        }
    }

    private void Awake()
    {
        Instance = this;

        // Plugin startup logic
        Logger = base.Logger;
        Logger.LogInfo($"Plugin {PLUGIN_GUID} is loaded!");

        SettingsManager.InitSettingsManager(Config);
        SettingsManager.LoadConfigBindings();

        RunSaveManager.InitRunSaveManager();

        hasTerrainRandomiser = Chainloader.PluginInfos.ContainsKey("com.snosz.terrainrandomiser");

        _harmony = new Harmony(PLUGIN_GUID);
        _harmony.PatchAll();
    }
}

public class TerrainRandomiserInteractor
{
    //[MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    private static TerrainRandomiser.MapSettings CurrSettings
    { 
        get
        {
            if (!SplitsStatsPlugin.hasTerrainRandomiser) return null;
            else if (PhotonNetwork.IsMasterClient) return TerrainRandomiser.Plugin.Instance?.mapSettings;
            else return TerrainRandomiser.Plugin.Instance?.roomMapSettings;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public static bool shouldRandomise()
    {
        if (SplitsStatsPlugin.hasTerrainRandomiser)
        {
            return CurrSettings?.enableRandomiser ?? false;
        }
        else return false;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public static bool autoRandomise()
    {
        if (SplitsStatsPlugin.hasTerrainRandomiser)
        {
            return CurrSettings?.autoRandomSeed ?? false;
        }
        else return false;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public static int masterSeed()
    {
        if (SplitsStatsPlugin.hasTerrainRandomiser)
        {
            return CurrSettings?.seed ?? -1;
        }
        else return -1;
    }
}