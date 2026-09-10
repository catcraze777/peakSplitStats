using System;
using System.Linq;
using System.Collections.Generic;
using BepInEx.Configuration;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.SceneManagement;
using Zorro.Settings;
using System.Collections.Specialized;
using System.ComponentModel;

namespace SplitsStats;

public class SettingsManager
{
    public static ConfigFile config;

    public static ConfigEntry<bool> isRealTimeConfig;
    public static bool isRealTime { get { return isRealTimeConfig?.Value ?? false; } private set { if (isRealTimeConfig != null) isRealTimeConfig.Value = value; } }

    public static ConfigEntry<bool> segmentTimersEnabledConfig;
    public static bool segmentTimersEnabled { get { return segmentTimersEnabledConfig?.Value ?? true; } private set { if (segmentTimersEnabledConfig != null) segmentTimersEnabledConfig.Value = value; } }

    public static ConfigEntry<bool> sharedBiomeIconsConfig;
    public static bool sharedBiomeIcons { get { return sharedBiomeIconsConfig?.Value ?? false; } private set { if (sharedBiomeIconsConfig != null) sharedBiomeIconsConfig.Value = value; } }
    
    public static ConfigEntry<bool> hiddenSegmentsConfig;
    public static bool hiddenSegments { get { return hiddenSegmentsConfig?.Value ?? false; } private set { if (hiddenSegmentsConfig != null) hiddenSegmentsConfig.Value = value; } }

    public static ConfigEntry<bool> timersEnabledConfig;
    public static bool timersEnabled { get { return timersEnabledConfig?.Value ?? true; } private set { if (timersEnabledConfig != null) timersEnabledConfig.Value = value; } }

    public static ConfigEntry<float> uiScaleSizeConfig;
    public static float uiScaleSize { get { return uiScaleSizeConfig?.Value ?? 1.0f; } private set { if (uiScaleSizeConfig != null) uiScaleSizeConfig.Value = value; } }

    public static ConfigEntry<bool> showCurrentAttemptNumberConfig;
    public static bool showCurrentAttemptNumber { get { return showCurrentAttemptNumberConfig?.Value ?? true; } private set { if (showCurrentAttemptNumberConfig != null) showCurrentAttemptNumberConfig.Value = value; } }

    public static ConfigEntry<bool> showCurrentHeightConfig;
    public static bool showCurrentHeight { get { return showCurrentHeightConfig?.Value ?? true; } private set { if (showCurrentHeightConfig != null) showCurrentHeightConfig.Value = value; } }

    public static ConfigEntry<bool> showDistanceFromFireConfig;
    public static bool showDistanceFromFire { get { return showDistanceFromFireConfig?.Value ?? true; } private set { if (showDistanceFromFireConfig != null) showDistanceFromFireConfig.Value = value; } }



    public static ConfigEntry<bool> enablePaceConfig;
    public static bool enablePace { get { return enablePaceConfig?.Value ?? true; } private set { if (enablePaceConfig != null) enablePaceConfig.Value = value; } }

    public static ConfigEntry<bool> disablePaceCustomRunsConfig;
    public static bool disablePaceCustomRuns { get { return disablePaceCustomRunsConfig?.Value ?? true; } private set { if (disablePaceCustomRunsConfig != null) disablePaceCustomRunsConfig.Value = value; } }

    public static ConfigEntry<bool> useAverageRunConfig;
    public static bool useAverageRun { get { return useAverageRunConfig?.Value ?? false; } private set { if (useAverageRunConfig != null) useAverageRunConfig.Value = value; } }

    public static ConfigEntry<bool> showPaceOnStartConfig;
    public static bool showPaceOnStart { get { return showPaceOnStartConfig?.Value ?? false; } private set { if (showPaceOnStartConfig != null) showPaceOnStartConfig.Value = value; } }

    public static ConfigEntry<float> paceTriggerDistanceConfig;
    public static float paceTriggerDistance { get { return paceTriggerDistanceConfig?.Value ?? 170f; } private set { if (paceTriggerDistanceConfig != null) paceTriggerDistanceConfig.Value = value; } }
    public const float MINIMUM_TRIGGER_DISTANCE = 5.0f;
    public static bool showPaceNearGoals { get { return paceTriggerDistance > MINIMUM_TRIGGER_DISTANCE; } }

    public static ConfigEntry<float> paceTimeTriggerConfig;
    public static float paceTimeTrigger { get { return paceTimeTriggerConfig?.Value ?? -60f; } private set { if (paceTimeTriggerConfig != null) paceTimeTriggerConfig.Value = value; } }
    public const float MAXIMUM_TRIGGER_TIME = 3600.0f;
    public static bool showPaceOnTimeTrigger { get { return paceTimeTrigger < MAXIMUM_TRIGGER_TIME; } }

    public static ConfigEntry<bool> showPaceOnEndConfig;
    public static bool showPaceOnEnd { get { return showPaceOnEndConfig?.Value ?? true; } private set { if (showPaceOnEndConfig != null) showPaceOnEndConfig.Value = value; } }

    public static ConfigEntry<bool> showRunPaceConfig;
    public static bool showRunPace { get { return showRunPaceConfig?.Value ?? false; } private set { if (showRunPaceConfig != null) showRunPaceConfig.Value = value; } }

    public static bool paceTextEnabled { get { return enablePace && (!RunSettings.IsCustomRun || !disablePaceCustomRuns) && (showRunPace || showPaceOnEnd || showPaceOnTimeTrigger || showPaceNearGoals || showPaceOnStart); } }



    public static ConfigEntry<bool> showCurrentCategoryConfig;
    public static bool showCurrentCategory { get { return showCurrentCategoryConfig?.Value ?? false; } private set { if (showCurrentCategoryConfig != null) showCurrentCategoryConfig.Value = value; } }

    public static ConfigEntry<bool> categorizeByLevelConfig;
    public static bool categorizeByLevel { get { return categorizeByLevelConfig?.Value ?? false; } private set { if (categorizeByLevelConfig != null) categorizeByLevelConfig.Value = value; } }

    public static ConfigEntry<bool> categorizeByGameVersionConfig;
    public static bool categorizeByGameVersion { get { return categorizeByGameVersionConfig?.Value ?? false; } private set { if (categorizeByGameVersionConfig != null) categorizeByGameVersionConfig.Value = value; } }

    public static ConfigEntry<bool> categorizeByPlayerCountConfig;
    public static bool categorizeByPlayerCount { get { return categorizeByPlayerCountConfig?.Value ?? true; } private set { if (categorizeByPlayerCountConfig != null) categorizeByPlayerCountConfig.Value = value; } }

    public static ConfigEntry<bool> categorizeByAscentConfig;
    public static bool categorizeByAscent { get { return categorizeByAscentConfig?.Value ?? true; } private set { if (categorizeByAscentConfig != null) categorizeByAscentConfig.Value = value; } }

    public static ConfigEntry<bool> categorizeByTerrainRandomizerConfig;
    public static bool categorizeByTerrainRandomizer { get { return categorizeByTerrainRandomizerConfig?.Value ?? true; } private set { if (categorizeByTerrainRandomizerConfig != null) categorizeByTerrainRandomizerConfig.Value = value; } }

    public static ConfigEntry<bool> categorizeBySeedConfig;
    public static bool categorizeBySeed { get { return categorizeBySeedConfig?.Value ?? false; } private set { if (categorizeBySeedConfig != null) categorizeBySeedConfig.Value = value; } }

    public static ConfigEntry<bool> categorizeByCustomRunConfig;
    public static bool categorizeByCustomRun { get { return categorizeByCustomRunConfig?.Value ?? true; } private set { if (categorizeByCustomRunConfig != null) categorizeByCustomRunConfig.Value = value; } }

    public static bool isCategorized { get { return categorizeByLevel || categorizeByGameVersion || categorizeByAscent || categorizeByPlayerCount || categorizeByTerrainRandomizer || categorizeBySeed || categorizeByCustomRun; } }

    public static bool hasVisibleCategoryLabel { get { return Ascents.currentAscent != 0 || (showCurrentCategory && (categorizeByPlayerCount || categorizeByLevel || categorizeByCustomRun || (categorizeByTerrainRandomizer && RunSaveManager.currentRun.wasRandomized))); } }



    public static ConfigEntry<int> timerHorizontalOffsetConfig;
    public static int timerHorizontalOffset { get { return timerHorizontalOffsetConfig?.Value ?? 0; } private set { if (timerHorizontalOffsetConfig != null) timerHorizontalOffsetConfig.Value = value; } }

    public static ConfigEntry<int> timerVerticalOffsetConfig;
    public static int timerVerticalOffset { get { return timerVerticalOffsetConfig?.Value ?? 0; } private set { if (timerVerticalOffsetConfig != null) timerVerticalOffsetConfig.Value = value; } }

    public static Vector2 timerVectorOffset { get { return new Vector2(timerHorizontalOffset, timerVerticalOffset); } }

    public static ConfigEntry<int> statsHorizontalOffsetConfig;
    public static int statsHorizontalOffset { get { return statsHorizontalOffsetConfig?.Value ?? 0; } private set { if (statsHorizontalOffsetConfig != null) statsHorizontalOffsetConfig.Value = value; } }

    public static ConfigEntry<int> statsVerticalOffsetConfig;
    public static int statsVerticalOffset { get { return statsVerticalOffsetConfig?.Value ?? 0; } private set { if (statsVerticalOffsetConfig != null) statsVerticalOffsetConfig.Value = value; } }

    public static Vector2 statsVectorOffset { get { return new Vector2(statsHorizontalOffset, statsVerticalOffset); } }

    public static ConfigEntry<bool> statsAutoAdjustConfig;
    public static bool statsAutoAdjust { get { return statsAutoAdjustConfig?.Value ?? true; } private set { if (statsAutoAdjustConfig != null) statsAutoAdjustConfig.Value = value; } }

    public static ConfigEntry<bool> canEditEndScreenTimeConfig;
    public static bool canEditEndScreenTime { get { return canEditEndScreenTimeConfig?.Value ?? true; } private set { if (canEditEndScreenTimeConfig != null) canEditEndScreenTimeConfig.Value = value; } }

    public static ConfigEntry<bool> onlyShowFinalRunPaceIfRecordConfig;
    public static bool onlyShowFinalRunPaceIfRecord { get { return onlyShowFinalRunPaceIfRecordConfig?.Value ?? false; } private set { if (onlyShowFinalRunPaceIfRecordConfig != null) onlyShowFinalRunPaceIfRecordConfig.Value = value; } }

    public static ConfigEntry<bool> alwaysShowNadirSegmentConfig;
    public static bool alwaysShowNadirSegment { get { return alwaysShowNadirSegmentConfig?.Value ?? false; } private set { if (alwaysShowNadirSegmentConfig != null) alwaysShowNadirSegmentConfig.Value = value; } }

    public static ConfigEntry<int> precisionInTimerConfig;
    public static int precisionInTimer { get { return precisionInTimerConfig?.Value ?? 1; } private set { if (precisionInTimerConfig != null) precisionInTimerConfig.Value = value; } }

    public static ConfigEntry<bool> useInGameTimingConfig;
    public static bool useInGameTiming { get { return useInGameTimingConfig?.Value ?? false; } private set { if (useInGameTimingConfig != null) useInGameTimingConfig.Value = value; } }

    public static ConfigEntry<bool> useColorSegmentsConfig;
    public static bool useColorSegments { get { return useColorSegmentsConfig?.Value ?? true; } private set { if (useColorSegmentsConfig != null) useColorSegmentsConfig.Value = value; } }

    public static ConfigEntry<bool> useColorPaceConfig;
    public static bool useColorPace { get { return useColorPaceConfig?.Value ?? true; } private set { if (useColorPaceConfig != null) useColorPaceConfig.Value = value; } }

    public static ConfigEntry<bool> saveEmptyRunsConfig;
    public static bool saveEmptyRuns { get { return saveEmptyRunsConfig?.Value ?? true; } private set { if (saveEmptyRunsConfig != null) saveEmptyRunsConfig.Value = value; } }

    


    public static void InitSettingsManager(ConfigFile inputConfig)
    {
        config = inputConfig;
        if (SplitsStatsPlugin.Logger != null) SplitsStatsPlugin.Logger.LogInfo($"Initialized Settings Manager!");
    }

    public static void LoadConfigBindings()
    {
        if (config == null) return;
        
        timersEnabledConfig = config.Bind("1. General", "Enable Timer", timersEnabled, "Show the main speedrunning timer.");
        segmentTimersEnabledConfig = config.Bind("1. General", "Show Segment Timers", segmentTimersEnabled, "Show the times for individual biome segments.");
        sharedBiomeIconsConfig = config.Bind("1. General", "Use Shared Biome Icons", sharedBiomeIcons, "Segment timers' icons initially use shared icons that don't reveal the run's selected biome until they're reached. Set to false to always display the run's biomes at the start of each run.");
        hiddenSegmentsConfig = config.Bind("1. General", "Hidden Segments", hiddenSegments, "If true, segment timers are hidden from displaying until their corresponding timer begins as the run progresses.");
        isRealTimeConfig = config.Bind("1. General", "Use Real Time", isRealTime, "Use real system time instead of in-game time. Doing so will allow the timer to keep running if the game is paused when playing solo.");
        uiScaleSizeConfig = config.Bind("1. General", "UI Scale Multiplier", uiScaleSize, "Scale the size of the mod's UI. Default is 1.0 (100% the original size)");
        showCurrentAttemptNumberConfig = config.Bind("1. General", "Show Current Attempt Number", showCurrentAttemptNumber, "Show the player's current run attempt number. This number is based on the number of saved runs that fit the categorization settings. Will never show if the run cannot be saved due to a custom or loaded run.");
        showCurrentHeightConfig = config.Bind("1. General", "Show Current Height", showCurrentHeight, "Show the player's current height/altitude.");
        showDistanceFromFireConfig = config.Bind("1. General", "Show Distance From Campfire", showDistanceFromFire, "Show the player's current distance from the next campfire or the Peak if in The Kiln.");

        enablePaceConfig = config.Bind("2. Run Pace/Intervals", "Enable Pace/Intervals", enablePace, "Display how far ahead or behind you are from your best record next to each timer. The runs used for pacing are based on the categorization settings.");
        disablePaceCustomRunsConfig = config.Bind("2. Run Pace/Intervals", "Disable Pace/Intervals for Custom Runs", disablePaceCustomRuns, "Set to true to hide the pace/interval during custom runs and miniruns.");
        useAverageRunConfig = config.Bind("2. Run Pace/Intervals", "Display Average Pace", useAverageRun, "Set to true to display your average times instead of the record time. Gold splits now display personal bests.");
        showPaceOnStartConfig = config.Bind("2. Run Pace/Intervals", "Show On Timer/Segment Start", showPaceOnStart, "Show the current run pace/interval as soon as the timer starts.");
        showPaceOnEndConfig = config.Bind("2. Run Pace/Intervals", "Show On Segment Ends", showPaceOnEnd, "Show the pace/intervals when each segment time is ended.");
        paceTriggerDistanceConfig = config.Bind("2. Run Pace/Intervals", "Show When Near End", paceTriggerDistance, "Trigger distance for when the player nears the next campfire/key point. The full run pace/interval displays when reaching the Peak. Set to less than 5.0 to disable.");
        paceTimeTriggerConfig = config.Bind("2. Run Pace/Intervals", "Show At Specific Time", paceTimeTrigger, "Show the pace/interval when it reaches the specified time (in seconds). For example, set this to -60.0 if you'd like the pace timer to display when the current segment's pace reaches -1:00.0 from personal best. The full run pace/interval displays when reaching the Peak. Set to more than 3600.0 (one hour) to disable.");
        showRunPaceConfig = config.Bind("2. Run Pace/Intervals", "Always Show Run Pace/Interval", showRunPace, "Always show the pace of the entire run.");

        showCurrentCategoryConfig = config.Bind("3. Categorizing", "Show Current Category", showCurrentCategory, "Edit the ascent text to also display the current category based on categorization settings.");
        categorizeByLevelConfig = config.Bind("3. Categorizing", "By Daily Mountain", categorizeByLevel, "Separate runs between different mountains. More precisely, separates runs between different unity scenes, game versions, and if they're randomized using Terrain Randomizer.");
        categorizeByAscentConfig = config.Bind("3. Categorizing", "By Ascent", categorizeByAscent, "Separate runs between ascent difficulties.");
        categorizeByPlayerCountConfig = config.Bind("3. Categorizing", "By Player Count", categorizeByPlayerCount, "Separate runs between different player counts.");
        categorizeByGameVersionConfig = config.Bind("3. Categorizing", "By Game Version", categorizeByGameVersion, "Separate runs between game versions/updates.");
        categorizeByTerrainRandomizerConfig = config.Bind("3. Categorizing", "By Terrain Randomiser", categorizeByTerrainRandomizer, "Separate runs depending on if the Terrain Randomiser mod is used.");
        categorizeBySeedConfig = config.Bind("3. Categorizing", "By Seed", categorizeBySeed, "Separate runs by seed number if the Terrain Randomiser mod is used.");
        categorizeByCustomRunConfig = config.Bind("3. Categorizing", "By Custom Run", categorizeByCustomRun, "Separate custom runs and miniruns from normal ascents. Custom runs must have the same settings to be categorized together when displaying past records/averages.");

        timerHorizontalOffsetConfig = config.Bind("4. Misc", "Timer Horizontal Offset", timerHorizontalOffset, "Adjust the timers' positions in the top left horizontally.");
        timerVerticalOffsetConfig = config.Bind("4. Misc", "Timer Vertical Offset", timerVerticalOffset, "Adjust the timers' positions in the top left vertically.");
        statsHorizontalOffsetConfig = config.Bind("4. Misc", "Extra Stats Horizontal Offset", statsHorizontalOffset, "Adjust the stats' positions in the top right horizontally.");
        statsVerticalOffsetConfig = config.Bind("4. Misc", "Extra Stats Vertical Offset", statsVerticalOffset, "Adjust the stats' positions in the top right vertically.");
        statsAutoAdjustConfig = config.Bind("4. Misc", "Extra Stats Automatically Adjust Position", statsAutoAdjust, "Adjust the stats' positions in the top right based on if the ascent text is active. Enabled by default but can be disabled if manually moving it's location.");
        canEditEndScreenTimeConfig = config.Bind("4. Misc", "Edit End Screen Time", canEditEndScreenTime, "Allow this mod to change the end-game results' displayed run time to add precision, show final pace from record if enabled, and show real time if enabled.");
        onlyShowFinalRunPaceIfRecordConfig = config.Bind("4. Misc", "Only Show Final Pace If Record", onlyShowFinalRunPaceIfRecord, "Only show the final pace on the end-game results if the run is a new record.");
        alwaysShowNadirSegmentConfig = config.Bind("4. Misc", "Always Show Nadir Segment Timer", alwaysShowNadirSegment, "Enable so the nadir timer is always visible as a segment, regardless of if nadir was entered or if the run is for ascent 8.");
        precisionInTimerConfig = config.Bind("4. Misc", "Digits of Precision", precisionInTimer, "Set the number of decimal digits to display in the timers.");
        useInGameTimingConfig = config.Bind("4. Misc", "Use Vanilla Timing", useInGameTiming, "Set to true to use the timing methods of the base game. Disables the timer starting when the player opens their eyes.");
        useColorSegmentsConfig = config.Bind("4. Misc", "Color Segment Timers", useColorSegments, "Color each segment to match the biome, otherwise color them white.");
        useColorPaceConfig = config.Bind("4. Misc", "Color Pace/Interval Text", useColorPace, "Color each segment's pace/interval time to match with the pace, otherwise color them white. (Use Green/Red/Gold Splits)");
        saveEmptyRunsConfig = config.Bind("4. Misc", "Save Empty Times", saveEmptyRuns, "Allows saving runs that have no times to save (died/quit before finishing the Shore). Set to true if you'd like these runs to count for the attempts counter.");

        if (SplitsStatsPlugin.Logger != null) SplitsStatsPlugin.Logger.LogInfo($"Loaded Settings Config Bindings!");
    }
}