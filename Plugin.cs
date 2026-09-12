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
using Peak;

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

        if (currRunTime.isRealTime != otherRunTime.isRealTime) return false;

        if ((SettingsManager.categorizeByGameVersion || SettingsManager.categorizeByLevel) && currRunTime.gameVersion != otherRunTime.gameVersion) return false;
        if (SettingsManager.categorizeByPlayerCount && currRunTime.playerCount != otherRunTime.playerCount) return false;
        if (SettingsManager.categorizeByAscent && currRunTime.ascentDifficulty != otherRunTime.ascentDifficulty) return false;
        if (SettingsManager.categorizeByLevel && currRunTime.levelName != otherRunTime.levelName || currRunTime.wasRandomized != otherRunTime.wasRandomized) return false;
        if ((SettingsManager.categorizeByTerrainRandomizer || SettingsManager.categorizeByLevel) && currRunTime.wasRandomized != otherRunTime.wasRandomized) return false;
        if (SettingsManager.categorizeBySeed && currRunTime.seed != otherRunTime.seed) return false;
        if (SettingsManager.categorizeByCustomRun && !SettingsManager.customRunNormalPace 
                && (currRunTime.customRun != otherRunTime.customRun || currRunTime.customRunSettingsHash != otherRunTime.customRunSettingsHash)) return false;
        if (SettingsManager.customRunNormalPace && otherRunTime.customRun) return false;
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
                // When loading the GUI in the main game...
                if (SceneManager.GetActiveScene().name != "Airport" && __instance != null)
                {
                    Logger.LogInfo("Starting GUIManager.Start Postfix!");

                    Instance.Config.Reload();

                    // Initialize the SplitsManager UI object.
                    splitsManagerInstance = SplitsManager.CreateSplitsManager(__instance, customStats);
                    foreach (BaseUIComponent currComponent in customUIComponents) splitsManagerInstance.AddInfoComponentToSide(currComponent);

                    // If an unsaved run was found, finish and save it.
                    if (RunSaveManager.IsRunActive())
                    {
                        RunSaveManager.FinishRun();
                        Logger.LogWarning("Ended an active run found stored in the RunSaveManager!");
                    }

                    // Initialize the UI animation helper object.
                    animManagerGameObject = new GameObject("SplitsStatsPlugin AnimationManager");
                    animManager = animManagerGameObject.AddComponent<AnimationManager>();
                    Logger.LogInfo("Created AnimationManager!");

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

                    // Start new run.
                    if (Quicksave.ShouldUseSaveData) RunSaveManager.ResumeQuicksave(); 
                    else RunSaveManager.StartNewRun();

                    // Load run records.
                    RunSaveManager.GetRunRecords(CategorizeByCurrRunConfig);
                    splitsManagerInstance.SetRunTargets();

                    // Start our timers at the exact same time as used by the in-game timer.
                    float startTime = SettingsManager.isRealTime ? GetCurrentRealTime() : Time.time;
                    startTime += SettingsManager.useInGameTiming ? -RunManager.Instance.TimeSinceRunStarted : Character.localCharacter.data.fallSeconds;

                    Segment startingSegment = MapHandler.CurrentSegmentNumber;

                    // Start segment timer that we're beginning at.
                    splitsManagerInstance.SetTimerHidden(startingSegment, false);
                    splitsManagerInstance.SetTimerFontSize(startingSegment, SplitsManager.ACTIVE_FONT_SIZE);
                    splitsManagerInstance.UpdateTimerPositions();
                    if (splitsManagerInstance.StartTimerAtTime(startingSegment, startTime))
                        Logger.LogInfo($"Started {startingSegment} timer!");

                    // Load quicksave data (which is not used when starting the starting segment timer).
                    if (Quicksave.ShouldUseSaveData)
                    {
                        Logger.LogInfo("Resuming quicksave, attempting to load previous saved run into timers...");

                        float previousTimeTotal = 0.0f;

                        // Loop through all segments before the one we're starting from.
                        for (Segment previousSegment = Segment.Beach; previousSegment < startingSegment; previousSegment++)
                        {
                            float previousSegmentTime = RunSaveManager.currentRun[previousSegment];

                            // Verify that segment has a saved time.
                            if (previousSegmentTime > 0.0f)
                            {
                                // Add segment time to add to the master timer.
                                previousTimeTotal += previousSegmentTime;

                                // Attempt to change the shared icons of previous segments that have been completed.
                                if (SettingsManager.sharedBiomeIcons && previousSegment != Segment.Void)
                                {
                                    splitsManagerInstance.UpdateTimerIcon(previousSegment, false);
                                    if (previousSegment == Segment.Caldera) splitsManagerInstance.UpdateTimerIcon(Segment.TheKiln, false);
                                }
                            }
                            else
                            {
                                // No saved time found for segment, log error and break.
                                Logger.LogError("Valid previous run not found! Using fresh times instead!");
                                if (previousSegment > Segment.Beach) Logger.LogInfo("For some reason the current run had at least one previous segment time valid for the quicksave, please debug RunTime.ResumeLastRun() this shouldn't be possible!");
                                previousTimeTotal = -1.0f;
                                break;
                            }
                        }
                        // If the segment we're starting from already has a time, an error occured and cancel loading.
                        if (previousTimeTotal > 0.0f && RunSaveManager.currentRun[startingSegment] > 0.0f)
                        {
                            Logger.LogError("For some reason the current run had all previous segments valid for the quicksave except the current starting segment, please debug RunTime.ResumeLastRun() this shouldn't be possible!");
                            previousTimeTotal = -1.0f; 
                        }

                        if (previousTimeTotal > 0.0f)
                        {
                            startTime -= previousTimeTotal;
                            TimerComponent.runStartTime = startTime;

                            float tempRunTime = startTime;
                            for (Segment previousSegment = Segment.Beach; previousSegment < startingSegment; previousSegment++)
                            {
                                float previousSegmentTime = RunSaveManager.currentRun[previousSegment];

                                // Attempt to load segment time into segment timer.
                                if (!splitsManagerInstance.StartTimerAtTime(previousSegment, tempRunTime)
                                    || !splitsManagerInstance.EndTimerAtTime(previousSegment, tempRunTime + previousSegmentTime))
                                {
                                    // If unable to load time into segment timer, send a warning.
                                    Logger.LogWarning($"{previousSegment} segment time found, but unable to load into segment timer!");
                                }
                                tempRunTime += previousSegmentTime;
                            }

                            Logger.LogInfo("Successfully loaded previous saved run!");
                        }
                    }

                    splitsManagerInstance.mainTimer.SetPaceTextActive(SettingsManager.showRunPace && SettingsManager.paceTextEnabled);
                    splitsManagerInstance.mainTimer.StartRunAtTime(startTime);
                    splitsManagerInstance.mainTimer.SetHeight(SplitsManager.HEADER_FONT_SIZE);

                    if (RunSaveManager.currentRun.ascentDifficulty >= 8 && !SettingsManager.hiddenSegments)
                    {
                        Logger.LogInfo("Ascent is 8 or higher, displaying Nadir timer...");
                        if (!splitsManagerInstance.SetTimerHidden(Segment.Void, false)) Logger.LogError("Could not display nadir timer!");
                    }

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
                // Add custom category text to the ascent counter.
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

    private static void TransitionToSegment(Segment s)
    {
        Logger.LogInfo($"Split reached, entering {s}!");

        // Start timer for new segment
        if (splitsManagerInstance.splitTimers.ContainsKey(s))
        {
            if (SettingsManager.hiddenSegments || (s == Segment.Void))
            {
                splitsManagerInstance.splitTimers[s].IsHidden = false;
                splitsManagerInstance.splitTimers[s].SetHeight(0.0f);
            }
            if (SettingsManager.sharedBiomeIcons && s != Segment.Void)
            {
                splitsManagerInstance.UpdateTimerIcon(s, false);
                if (s == Segment.Caldera) splitsManagerInstance.UpdateTimerIcon(Segment.TheKiln, false);
            }
            animManager.LerpTimerFontSize(splitsManagerInstance.splitTimers[s], SplitsManager.ACTIVE_FONT_SIZE, FONT_CHANGE_DURATION);

            splitsManagerInstance.StartTimer(s);
            Logger.LogInfo($"Started {s} timer!");
        }

        // End timer of the previous segment.
        if (splitsManagerInstance.splitTimers.ContainsKey(s - 1))
        {
            splitsManagerInstance.EndTimer(s - 1);
            animManager.LerpTimerFontSize(splitsManagerInstance.splitTimers[s - 1], SplitsManager.INACTIVE_FONT_SIZE, FONT_CHANGE_DURATION);
            if (RunSaveManager.IsRunActive())
            {
                RunSaveManager.currentRun[s - 1] = splitsManagerInstance.splitTimers[s - 1].totalTime;
                RunSaveManager.SaveRun();
            }
            Logger.LogInfo($"Stopped {s - 1} timer!");
        }

        // If transitioning to the final half of biome 4...
        if (s == Segment.TheKiln)
        {
            // If in a minirun save the first half time.
            if (RunSettings.isMiniRun && RunSaveManager.IsRunActive())
            {
                RunSaveManager.currentRun[Segment.Caldera] = splitsManagerInstance.mainTimer.currTime;
                RunSaveManager.SaveRun();
                Logger.LogInfo($"Saved caldera/gloom time for minirun!");
            }

            // Change the objective icon to the peak flag.
            Sprite newSprite = LoadSprite(SplitsManager.peakImgPath);
            if (newSprite != null) splitsManagerInstance.ChangeCampfireIcon(newSprite);
            Logger.LogInfo($"Updated campfire icon to flag!");
        }
        // If transitioning to Nadir...
        else if (s == Segment.Void)
        {
            // If in a minirun save the final half of biome 4. This technically can't happen without cheating in scout's honor but might as well cover it.
            if (RunSettings.isMiniRun && RunSaveManager.IsRunActive())
            {
                RunSaveManager.currentRun[Segment.TheKiln] = splitsManagerInstance.mainTimer.currTime - RunSaveManager.currentRun[Segment.Caldera];
                RunSaveManager.SaveRun();
                Logger.LogInfo($"Saved caldera/gloom time for minirun!");
            }

            // Change the objective icon to the peak gate flag.
            Sprite newSprite = LoadSprite(SplitsManager.peakGateImgPath);
            if (newSprite != null) splitsManagerInstance.ChangeCampfireIcon(newSprite);
            Logger.LogInfo($"Updated campfire icon to peak gate!");
        }
        splitsManagerInstance.UpdateTimerPositions();
    }

    [HarmonyPatch(typeof(MapHandler), "GoToSegment")]
    private class MapHandlerGoToSegmentPatcher
    {
        private static void Postfix(MapHandler __instance, ref Segment s)
        {
            try
            {
                Logger.LogInfo("Starting MapHandler.GoToSegment Postfix!");

                TransitionToSegment(s);

                Logger.LogInfo($"MapHandler.GoToSegment Postfix successfully completed!");
            }
            catch (Exception ex)
            {
                Logger.LogError((object)($"Error in MapHandler.GoToSegment patch: {ex.GetType()}" + ex.Message + $"\n{ex.Source}\n{ex.TargetSite}\n{ex.StackTrace}"));
            }
        }
    }

    [HarmonyPatch(typeof(MapHandler), "JumpToSegment")]
    private class MapHandlerJumpToSegmentPatcher
    {
        private static void Postfix(MapHandler __instance, ref Segment segment)
        {
            try
            {
                Logger.LogInfo("Starting MapHandler.JumpToSegment Postfix!");

                TransitionToSegment(segment);

                // Make sure all timers are stopped.
                foreach (Segment currSegment in new Segment[] { Segment.Beach, Segment.Tropics, Segment.Alpine, Segment.Caldera, Segment.TheKiln })
                {
                    if (splitsManagerInstance.splitTimers.ContainsKey(currSegment) && splitsManagerInstance.splitTimers[currSegment].timerOn)
                    {
                        splitsManagerInstance.EndTimer(currSegment);
                        animManager.LerpTimerFontSize(splitsManagerInstance.splitTimers[currSegment], SplitsManager.INACTIVE_FONT_SIZE, FONT_CHANGE_DURATION);
                        RunSaveManager.currentRun[currSegment] = splitsManagerInstance.splitTimers[currSegment].totalTime;
                        RunSaveManager.SaveRun();
                        Logger.LogInfo($"Stopped {currSegment} timer!");
                    }
                }

                Logger.LogInfo($"MapHandler.JumpToSegment Postfix successfully completed!");
            }
            catch (Exception ex)
            {
                Logger.LogError((object)($"Error in MapHandler.JumpToSegment patch: {ex.GetType()}" + ex.Message + $"\n{ex.Source}\n{ex.TargetSite}\n{ex.StackTrace}"));
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

                // Stop the main timer on game end.
                splitsManagerInstance.mainTimer.EndTimer();

                if (!SettingsManager.segmentTimersEnabled)
                {
                    Logger.LogInfo("Segment timers disabled, RunManager.EndGame Postfix successfully completed!");
                    return;
                }

                // Stop all segment timers on game end.
                foreach (Segment currSegment in Enum.GetValues(typeof(Segment)))
                {
                    if (currSegment == Segment.Peak) continue;

                    if (splitsManagerInstance.splitTimers.ContainsKey(currSegment))
                    {
                        Logger.LogInfo($"Stopping {currSegment} timer...");

                        TimerComponent currTimer = splitsManagerInstance.splitTimers[currSegment];
                        bool paceTextOriginalStatus = currTimer.GetPaceTextActive();
                        currTimer.EndTimer();
                        currTimer.SetPaceTextActive(paceTextOriginalStatus);
                        currTimer.SetCurrColor(currTimer.inactiveColor);
                        animManager.LerpTimerFontSize(splitsManagerInstance.splitTimers[currSegment], SplitsManager.INACTIVE_FONT_SIZE, FONT_CHANGE_DURATION);

                        RunSaveManager.currentRun[currSegment] = currTimer.totalTime;

                        Logger.LogInfo($"Successfully stopped {currSegment} timer!");
                    }  
                }
                splitsManagerInstance.UpdateTimerPositions();
                RunSaveManager.SaveRun();

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
                // Upon reaching the "PEAK" biome text...
                if (progressPoint.title == "PEAK")
                {
                    Logger.LogInfo($"Starting MountainProgressHandler.TriggerReached postfix!");

                    // Show full time pace.
                    if (SettingsManager.showPaceNearGoals && SettingsManager.paceTextEnabled)
                    {
                        Logger.LogInfo($"Peak reached, showing main timer pace text");
                        splitsManagerInstance.mainTimer.SetPaceTextActive(true);
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

                // If the run was completed (regardless of death or win), save the final time as a completed run.
                if (RunSaveManager.IsRunActive())
                {
                    Logger.LogInfo($"Writing final time of completed run...");

                    float finalRunTime = SettingsManager.isRealTime || !SettingsManager.useInGameTiming ? splitsManagerInstance.mainTimer.totalTime : totalSeconds;
                    RunSaveManager.currentRun.finalTime = finalRunTime;

                    // Save individual segment times for miniruns.
                    if (RunSettings.isMiniRun)
                    {
                        if (MapHandler.CurrentSegmentNumber >= Segment.Void) RunSaveManager.currentRun[Segment.Void] = finalRunTime - RunSaveManager.currentRun[Segment.TheKiln] - RunSaveManager.currentRun[Segment.Caldera];
                        else if (MapHandler.CurrentSegmentNumber >= Segment.TheKiln) RunSaveManager.currentRun[Segment.TheKiln] = finalRunTime - RunSaveManager.currentRun[Segment.Caldera];
                        else RunSaveManager.currentRun[(Segment)RunSettings.GetValue(RunSettings.SETTINGTYPE.MiniRunBiome)] = finalRunTime;
                        Logger.LogInfo($"Writing segment time(s) of minirun after ending in segment {MapHandler.CurrentSegmentNumber}...");
                    }

                    RunSaveManager.currentRun.runFinished = hasWon;
                }
                RunSaveManager.FinishRun();
                Logger.LogInfo($"Run finished...");

                // If enabled, edit the end screen's timer text.
                if (SettingsManager.canEditEndScreenTime)
                {
                    Logger.LogInfo($"Editting end screen...");

                    // Update the original time text object.
                    __result += $"." + $"{Mathf.FloorToInt(SettingsManager.precisionInTimer * (totalSeconds % 1f))}".PadLeft((int)SettingsManager.precisionInTimer, '0');
                    if (SettingsManager.isRealTime || !SettingsManager.useInGameTiming) __result = TimerComponent.GetTimeString(splitsManagerInstance.mainTimer.totalTime, true, true, SettingsManager.precisionInTimer);
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

                        // Add the run's final pace text to the end screen.
                        RectTransform newPaceTextObject = UnityEngine.Object.Instantiate(textTransform, textTransform.parent);
                        newPaceTextObject.sizeDelta += new Vector2(-40f, 0f);
                        newPaceTextObject.anchoredPosition = new Vector2(newPaceTextObject.anchoredPosition.x, 0f);
                        newPaceTextObject.gameObject.SetActive(true);
                        TMP_Text newPaceText = newPaceTextObject.GetComponent<TMP_Text>();
                        newPaceText.fontSizeMax = 18f;
                        newPaceText.text = TimerComponent.GetTimeString(currPace, false, false, SettingsManager.precisionInTimer > 0 ? 1 : 0, true);

                        if (SettingsManager.useColorPace)
                        {
                            float currRecordTime = splitsManagerInstance.mainTimer.recordTime;
                            if (currRecordTime > 0.0f && splitsManagerInstance.mainTimer.currTime < currRecordTime) newPaceText.color = TimerComponent.goldSplitColor;
                            else if (currPace <= 0.0f) newPaceText.color = TimerComponent.greenSplitColor;
                            else newPaceText.color = TimerComponent.redSplitColor;
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
    
    [HarmonyPatch(typeof(Quicksave), "SaveNow")]
    private class QuicksavePatcher
    {
        private static void Postfix()
        {
            try
            {
                if (RunSaveManager.IsRunActive())
                {
                    Logger.LogInfo($"Starting Quicksave.SaveNow postfix!");

                    RunSaveManager.TryWriteQuickSave();

                    Logger.LogInfo("Quicksave.SaveNow Postfix successfully completed!");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError((object)($"Error in Quicksave.SaveNow patch: {ex.GetType()}" + ex.Message + $"\n{ex.Source}\n{ex.TargetSite}\n{ex.StackTrace}"));
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