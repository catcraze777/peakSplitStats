using System;
using System.IO;
using System.Reflection;
using Peak;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;

namespace SplitsStats;

public class RunSaveManager
{
    public static RunTime targetRun { get { if (SettingsManager.useAverageRun) return averageRun; else return fastestRun; } }
    public static float targetShore { get { if (SettingsManager.useAverageRun) return averageShore; else return fastestShore; } }
    public static float targetTropics { get { if (SettingsManager.useAverageRun) return averageTropics; else return fastestTropics; } }
    public static float targetAlpmesa { get { if (SettingsManager.useAverageRun) return averageAlpmesa; else return fastestAlpmesa; } }
    public static float targetCaldera { get { if (SettingsManager.useAverageRun) return averageCaldera; else return fastestCaldera; } }
    public static float targetKiln { get { if (SettingsManager.useAverageRun) return averageKiln; else return fastestKiln; } }
    public static float targetNadir { get { if (SettingsManager.useAverageRun) return averageNadir; else return fastestNadir; } }
    public static float targetBiome4 { get { if (SettingsManager.useAverageRun) return averageBiome4; else return fastestBiome4; } }
    public static float targetSegment(Segment indexSegment)
    {
        if (SettingsManager.useAverageRun) return averageSegment(indexSegment);
        else return fastestSegment(indexSegment);
    } 


    public static RunTime fastestRun { get; private set; }
    public static float fastestShore { get; private set; }
    public static float fastestTropics { get; private set; }
    public static float fastestAlpmesa { get; private set; }
    public static float fastestCaldera { get; private set; }
    public static float fastestKiln { get; private set; }
    public static float fastestNadir { get; private set; }
    public static float fastestBiome4 { get; private set; }
    public static float fastestSegment(Segment indexSegment)
    {
        switch(indexSegment)
        {
            case Segment.Beach:
                return fastestShore;
            case Segment.Tropics:
                return fastestTropics;
            case Segment.Alpine:
                return fastestAlpmesa;
            case Segment.Caldera:
                return fastestCaldera;
            case Segment.TheKiln:
                return fastestKiln;
            case Segment.Void:
                return fastestNadir;
            default:
                throw new IndexOutOfRangeException();
        }
    } 

    public static RunTime averageRun { get; private set; }
    public static float averageShore { get; private set; }
    public static float averageTropics { get; private set; }
    public static float averageAlpmesa { get; private set; }
    public static float averageCaldera { get; private set; }
    public static float averageKiln { get; private set; }
    public static float averageNadir { get; private set; }
    public static float averageBiome4 { get; private set; }
    public static float averageSegment(Segment indexSegment)
    {
        switch(indexSegment)
        {
            case Segment.Beach:
                return averageShore;
            case Segment.Tropics:
                return averageTropics;
            case Segment.Alpine:
                return averageAlpmesa;
            case Segment.Caldera:
                return averageCaldera;
            case Segment.TheKiln:
                return averageKiln;
            case Segment.Void:
                return averageNadir;
            default:
                throw new IndexOutOfRangeException();
        }
    } 

    public static int totalAttempts { get; private set; }
    public static float SumOfBest
    {
        get
        {
            if (fastestShore < 0.0) return -1.0f;
            if (fastestTropics < 0.0) return -1.0f;
            if (fastestAlpmesa < 0.0) return -1.0f;
            if (fastestCaldera < 0.0) return -1.0f;
            if (fastestKiln < 0.0) return -1.0f;

            bool nadirRequired = targetRun != null && targetRun.ascentDifficulty >= 8;
            if (nadirRequired && fastestNadir < 0.0) return -1.0f;

            return fastestShore + fastestTropics + fastestAlpmesa + fastestCaldera + fastestKiln + (nadirRequired ? fastestNadir : 0.0f); 
        } 
    }

    /// <summary>
    /// The run info of the current run the player is actively on.
    /// The object reference will get assigned and unasigned by RunSaveManager.
    /// Edit the object properties to change what will get saved.
    /// </summary>
    public static RunTime currentRun { get; private set; }

    /// <summary>
    /// The function used by <c>GetRunRecords()</c> by default if none are provided.
    /// </summary>
    public static Func<RunTime, bool> CategorizationFunc = null;

    /// <summary>
    /// A default function for <c>GetRunRecords()</c> to explicitly use all records regardless of what <c>this.CategorizationFunc</c> has stored.
    /// </summary>
    public static readonly Func<RunTime, bool> IncludeAllRuns = static _ => true;

    private static List<RunTime> runStorage;

    /// <summary> Try to read the save or load the default when creating the RunSaveManager object. </summary>
    public static void InitRunSaveManager()
    {
        if (SplitsStatsPlugin.Logger != null) SplitsStatsPlugin.Logger.LogInfo($"Initializing Run Save Manager...");
        if (jsonFilePath == null) GetFilePaths();
        try
        {
            TryReadSave();
        }
        catch (Exception ex)
        {
            if (SplitsStatsPlugin.Logger != null) SplitsStatsPlugin.Logger.LogError($"Unable to read saved runs! An error occured: {ex.GetType()} {ex.Message}");
            runStorage = new List<RunTime>();
        }
        GetRunRecords();
    }

    /// <summary> Start a new run and assign it to this.currentRun. Edit this.currentRun and that data will be saved. </summary>
    /// <returns> A copy of the reference of the object stored in this.currentRun </returns>
    public static RunTime StartNewRun()
    {
        if (IsRunActive())
        {
            if (SplitsStatsPlugin.Logger != null) SplitsStatsPlugin.Logger.LogError($"Tried to start a run when one is already started!");
            return null;
        }
        SplitsStatsPlugin.Logger.LogInfo($"Starting new run...");
        currentRun = new RunTime();
        currentRun.UpdateTimeString();
        runStorage.Add(new RunTime(currentRun));

        // Initialize current run settings.
        currentRun.playerCount = PhotonNetwork.PlayerList.Length;
        currentRun.levelName = SceneManager.GetActiveScene().name;
        currentRun.gameVersion = "v" + Application.version;
        currentRun.ascentDifficulty = Ascents.currentAscent;
        currentRun.isRealTime = SettingsManager.isRealTime;

        if (RunSettings.IsCustomRun)
        {
            currentRun.customRun = true;

            // Calculate FNV-1a hash code for the run settings, which is persistent between application launches.
            uint settingsHash = 0x811c9dc5;
            foreach (RunSettings.SETTINGTYPE currSetting in Enum.GetValues(typeof(RunSettings.SETTINGTYPE)))
            {
                settingsHash ^= (uint)RunSettings.GetValue(currSetting);
                settingsHash *= 0x01000193;
            }

            currentRun.customRunSettingsHash = settingsHash;
        }
        else currentRun.customRun = false;

        if (SplitsStatsPlugin.hasTerrainRandomiser)
        {
            currentRun.wasRandomized = TerrainRandomiserInteractor.shouldRandomise();
            currentRun.seed = TerrainRandomiserInteractor.masterSeed();
        }

        SplitsStatsPlugin.Logger.LogInfo("Loaded run information!");
        return currentRun;
    }

    /// <summary> Resumes a quicksave and loads the last saved run to this.currentRun after validating it. Edit this.currentRun and that data will be saved. 
    /// If no runs have been loaded, the last run doesn't fit the quicksave, or we aren't loading from a quicksave this will return null.
    /// IF A RUN IS ACTIVE THIS WILL OVERWRITE IT'S DATA IF SUCCESSFUL.</summary>
    /// <returns> A copy of the reference of the object stored in this.currentRun or null if unsuccessful.</returns>
    public static RunTime ResumeQuicksave()
    {
        if (!Quicksave.ShouldUseSaveData)
        {
            if (SplitsStatsPlugin.Logger != null) SplitsStatsPlugin.Logger.LogError($"Tried to resume a run when we aren't resuming from a save!");
            return null;
        }

        if (!IsRunActive()) StartNewRun();

        if (runStorage.Count <= 1)
        {
            if (SplitsStatsPlugin.Logger != null) SplitsStatsPlugin.Logger.LogError($"Tried to resume a run when there is no run to load!");
            return null;
        }

        SplitsStatsPlugin.Logger.LogInfo($"Attempting to resume last run...");

        RunTime lastRun = new RunTime(runStorage[^2]);

        // Verify that the previous saved run settings matches the loaded quicksave's run settings.
        bool settingsIdentical = true;
        if (currentRun.runFinished != lastRun.runFinished) settingsIdentical = false;
        if (currentRun.gameVersion != lastRun.gameVersion) settingsIdentical = false;
        if (currentRun.levelName != lastRun.levelName) settingsIdentical = false;
        if (currentRun.ascentDifficulty != lastRun.ascentDifficulty) settingsIdentical = false;
        if (currentRun.customRun != lastRun.customRun) settingsIdentical = false;
        if (currentRun.customRun && lastRun.customRun && currentRun.customRunSettingsHash != lastRun.customRunSettingsHash) settingsIdentical = false;
        if (currentRun.wasRandomized != lastRun.wasRandomized) settingsIdentical = false;
        if (currentRun.wasRandomized && lastRun.wasRandomized && currentRun.seed != lastRun.seed) settingsIdentical = false;
        if (SettingsManager.categorizeByPlayerCount && currentRun.playerCount != lastRun.playerCount) settingsIdentical = false; // Not sure how resuming multiplayer games works, but I assume you can resume with different players.
        
        if (!settingsIdentical)
        {
            if (SplitsStatsPlugin.Logger != null) SplitsStatsPlugin.Logger.LogError($"Previous run has different run settings, unable to resume run!");
            return null;
        }

        // Make sure that the last run matches the segment we're resuming from.
        for (Segment priorSegment = Segment.Beach; priorSegment < MapHandler.CurrentSegmentNumber; priorSegment++)
        {
            if (lastRun[priorSegment] <= 0.0f)
            {
                if (SplitsStatsPlugin.Logger != null) SplitsStatsPlugin.Logger.LogError($"Previous run has a missing segment time, unable to resume run!");
                return null;
            }
        }
        if (lastRun[MapHandler.CurrentSegmentNumber] > 0.0f)
        {
            if (SplitsStatsPlugin.Logger != null) SplitsStatsPlugin.Logger.LogError($"Previous run has a time already saved for the current segment {MapHandler.CurrentSegmentNumber}, unable to resume run!");
            return null;
        }

        SplitsStatsPlugin.Logger.LogInfo($"Last run successfully resumed!");
        currentRun = lastRun;
        runStorage.RemoveAt(runStorage.Count - 1); // Removes the old fresh run that is overwritten by the previous save.
        SaveRun();
        return currentRun;
    }

    /// <summary> Save the information currently stored in this.currentRun </summary>
    /// <param name="forceSave"> Turn to true to save even when no times are stored in the current run. Defaults to the value specified in the plugin settings.</param>
    /// <returns> A boolean indicating if the save was successful. </returns>
    public static bool SaveRun(bool? forceSave = null)
    {
        bool shouldForceSave = forceSave ?? SettingsManager.saveEmptyRuns;

        if (!IsRunActive())
        {
            if (SplitsStatsPlugin.Logger != null) SplitsStatsPlugin.Logger.LogError($"Tried to save a run when no run has been started!");
            return false;
        }

        runStorage[^1] = new RunTime(currentRun);

        try
        {
            if (currentRun.HasTimes() || shouldForceSave) TryWriteSave();
            return true;
        }
        catch (Exception ex)
        {
            if (SplitsStatsPlugin.Logger != null) SplitsStatsPlugin.Logger.LogError($"Unable to save run! An error occured: {ex.GetType()} {ex.Message}");
            return false;
        }
    }

    /// <summary> Save the information currently stored in this.currentRun and remove the object reference to start a new run later. </summary>
    /// <returns> A boolean indicating if the save was successful and this.currentRun is reset to null. </returns>
    public static bool FinishRun()
    {
        bool successfulSave = SaveRun();
        ClearInternalSavedRuns();

        if (successfulSave)
        {
            currentRun = null;
            return true;
        }
        return false;
    }

    /// <summary> Returns true if a run is currently active and an object exists in this.currentRun. </summary>
    public static bool IsRunActive()
    {
        return currentRun != null;
    }

    /// <summary>
    /// Returns true if run is valid and should be saved.
    /// </summary>
    public static bool IsRunValid()
    {
        if (RunSettings.IsCustomRun) return false;
        if (Peak.Quicksave.ShouldUseSaveData) return false;
        return true;
    }

    // File reading and writing.
    private static string jsonDirectory;
    private static string jsonFilePath;

    private const string saveFileName = "savedRuns.json";
    private static void GetFilePaths()
    {
        jsonDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        jsonFilePath = Path.Combine(jsonDirectory, saveFileName);
    }

    public static void TryReadSave()
    {
        if (!File.Exists(jsonFilePath)) throw new FileNotFoundException($"Could not find file {saveFileName} to load data from!");
        string json = File.ReadAllText(jsonFilePath);
        runStorage = JsonConvert.DeserializeObject<List<RunTime>>(json);
        if (SplitsStatsPlugin.Logger != null) SplitsStatsPlugin.Logger.LogInfo($"Successfully read saved runs!");
    }

    public static void TryWriteSave()
    {
        string json = JsonConvert.SerializeObject(runStorage, Formatting.Indented);
        File.WriteAllText(jsonFilePath, json);
        if (SplitsStatsPlugin.Logger != null) SplitsStatsPlugin.Logger.LogInfo($"Successfully wrote saved runs!");
    }

    /// <summary>
    /// Clears the internally saved records to be reloaded again. This does NOT erase runs or their data, it can be loaded again using GetRunRecords().
    /// </summary>
    private static void ClearInternalSavedRuns()
    {
        fastestRun = new RunTime();
        fastestShore = -1.0f;
        fastestTropics = -1.0f;
        fastestAlpmesa = -1.0f;
        fastestCaldera = -1.0f;
        fastestKiln = -1.0f;
        fastestNadir = -1.0f;
        fastestBiome4 = -1.0f;

        averageRun = new RunTime();
        averageShore = -1.0f;
        averageTropics = -1.0f;
        averageAlpmesa = -1.0f;
        averageCaldera = -1.0f;
        averageKiln = -1.0f;
        averageNadir = -1.0f;
        averageBiome4 = -1.0f;

        totalAttempts = 0;
    }

    /// <summary>
    /// Fetch the records from all currently loaded runs to use for splits and other purposes.
    /// </summary>
    /// <param name="InputCategorizationFunc">
    /// A function used to allow for loading records based on categories.
    /// Takes a RunTime as input and returns a bool indicating if the run should be considered for records or not.
    /// By default (param set to null), <c>this.CategorizationFunc</c> is used. If <c>this.CategorizationFunc</c> is null (by default it is), then all runs are considered.
    /// To explicitly use all runs, use <c>RunSaveManager.IncludeAllRuns</c> for the parameter.
    /// </param>
    public static void GetRunRecords(Func<RunTime, bool> InputCategorizationFunc = null)
    {
        if (SplitsStatsPlugin.Logger != null) SplitsStatsPlugin.Logger.LogInfo($"Loading run records...");

        ClearInternalSavedRuns();
        int numCompleteRuns = 0;

        averageShore = 0.0f;
        int numShores = 0;
        averageTropics = 0.0f;
        int numTropics = 0;
        averageAlpmesa = 0.0f;
        int numAlpmesa = 0;
        averageCaldera = 0.0f;
        int numCaldera = 0;
        averageKiln = 0.0f;
        int numKiln = 0;
        averageNadir = 0.0f;
        int numNadir = 0;
        averageBiome4 = 0.0f;
        int numBiome4 = 0;
        averageRun.finalTime = 0.0f;
        totalAttempts = 0;

        foreach (RunTime run in runStorage)
        {
            if (InputCategorizationFunc != null)
            {
                if (!InputCategorizationFunc(run)) continue;
            }
            else if (CategorizationFunc != null)
            {
                if (!CategorizationFunc(run)) continue;
            }

            if (IsRunActive() && run == runStorage[^1]) continue;

            totalAttempts++;

            if (run.runFinished && run.finalTime > 0.0f)
            {
                if (fastestRun.finalTime == -1.0f || run.finalTime < fastestRun.finalTime)
                    fastestRun = new RunTime(run);
                
                averageRun.finalTime += run.finalTime;
                numCompleteRuns++;
            }

            if (run.shoreTime > 0.0f)
            {
                if (fastestShore == -1.0f || run.shoreTime < fastestShore)
                    fastestShore = run.shoreTime;
                
                averageShore += run.shoreTime;
                numShores++;
            }

            if (run.tropicsTime > 0.0f)
            {
                if (fastestTropics == -1.0f || run.tropicsTime < fastestTropics)
                    fastestTropics = run.tropicsTime;
                
                averageTropics += run.tropicsTime;
                numTropics++;
            }

            if (run.alpmesaTime > 0.0f)
            {
                if (fastestAlpmesa == -1.0f || run.alpmesaTime < fastestAlpmesa)
                    fastestAlpmesa = run.alpmesaTime;

                averageAlpmesa += run.alpmesaTime;
                numAlpmesa++;
            }

            if (run.calderaTime > 0.0f)
            {
                if (fastestCaldera == -1.0f || run.calderaTime < fastestCaldera)
                    fastestCaldera = run.calderaTime;

                averageCaldera += run.calderaTime;
                numCaldera++;
            }

            if (run.kilnTime > 0.0f)
            {
                if (fastestKiln == -1.0f || run.kilnTime < fastestKiln)
                    fastestKiln = run.kilnTime;

                averageKiln += run.kilnTime;
                numKiln++;
            }

            if (run.nadirTime > 0.0f)
            {
                if (fastestNadir == -1.0f || run.nadirTime < fastestNadir)
                    fastestNadir = run.nadirTime;

                averageNadir += run.nadirTime;
                numNadir++;
            }
            
            if (run.calderaTime > 0.0f && run.kilnTime > 0.0f)
            {
                float biome4Time = run.calderaTime + run.kilnTime;
                if (fastestBiome4 == -1.0f || biome4Time < fastestBiome4)
                    fastestBiome4 = biome4Time;

                averageBiome4 += biome4Time;
                numBiome4++;
            }
        }

        if (numShores > 0) averageShore /= numShores;
        else averageShore = -1.0f;

        if (numTropics > 0) averageTropics /= numTropics; 
        else averageTropics = -1.0f;

        if (numAlpmesa > 0) averageAlpmesa /= numAlpmesa;
        else averageAlpmesa = -1.0f;

        if (numCaldera > 0) averageCaldera /= numCaldera;
        else averageCaldera = -1.0f;

        if (numKiln > 0) averageKiln /= numKiln;
        else averageKiln = -1.0f;

        if (numNadir > 0) averageNadir /= numNadir;
        else averageNadir = -1.0f;

        if (numBiome4 > 0) averageBiome4 /= numBiome4;
        else averageBiome4 = -1.0f;

        if (numCompleteRuns > 0) { averageRun.finalTime /= numCompleteRuns; averageRun.runFinished = true; }
        else { averageRun.finalTime = -1.0f; averageRun.runFinished = false; }

        averageRun.shoreTime = averageShore;
        averageRun.tropicsTime = averageTropics;
        averageRun.alpmesaTime = averageAlpmesa;
        averageRun.calderaTime = averageCaldera;
        averageRun.kilnTime = averageKiln;
        averageRun.nadirTime = averageNadir;

        if (SplitsStatsPlugin.Logger != null) SplitsStatsPlugin.Logger.LogInfo($"Loaded times from {totalAttempts} record(s)!");
    }
}

public class RunTime
{
    public string runDate;

    public bool isRealTime;

    public bool runFinished;
    public float finalTime;

    public float shoreTime;
    public float tropicsTime;
    public float alpmesaTime;
    public float calderaTime;
    public float kilnTime;
    public float nadirTime;

    public string gameVersion;
    public string levelName;
    public int ascentDifficulty;
    public int playerCount;
    public bool customRun;
    public uint customRunSettingsHash;

    public bool wasRandomized;
    public int seed;

    public static string GetDateString()
    {
        return DateTime.Now.ToString("g", CultureInfo.CurrentCulture);
    }

    public void UpdateTimeString()
    {
        runDate = GetDateString();
    }

    /// <summary> Default constructor. </summary>
    public RunTime()
    {
        isRealTime = false;

        runFinished = false;
        finalTime = -1.0f;

        shoreTime = -1.0f;
        tropicsTime = -1.0f;
        alpmesaTime = -1.0f;
        calderaTime = -1.0f;
        kilnTime = -1.0f;
        nadirTime = -1.0f;

        gameVersion = "";
        levelName = "";
        ascentDifficulty = 0;
        playerCount = 0;
        customRun = false;
        customRunSettingsHash = 0;

        wasRandomized = false;
        seed = 0;
    }

    /// <summary> Copy constructor. </summary>
    public RunTime(RunTime original)
    {
        runDate = original.runDate;

        isRealTime = original.isRealTime;

        runFinished = original.runFinished;
        finalTime = original.finalTime;

        shoreTime = original.shoreTime;
        tropicsTime = original.tropicsTime;
        alpmesaTime = original.alpmesaTime;
        calderaTime = original.calderaTime;
        kilnTime = original.kilnTime;
        nadirTime = original.nadirTime;

        gameVersion = original.gameVersion;
        levelName = original.levelName;
        ascentDifficulty = original.ascentDifficulty;
        playerCount = original.playerCount;
        customRun = original.customRun;
        customRunSettingsHash = original.customRunSettingsHash;

        wasRandomized = original.wasRandomized;
        seed = original.seed;
    }

    /// <summary> Index the stored segment times with the corresponding Segment Enum. </summary>
    public float this[Segment indexSegment]
    {
        get
        {
            switch (indexSegment)
            {
                case Segment.Beach:
                    return shoreTime;
                case Segment.Tropics:
                    return tropicsTime;
                case Segment.Alpine:
                    return alpmesaTime;
                case Segment.Caldera:
                    return calderaTime;
                case Segment.TheKiln:
                    return kilnTime;
                case Segment.Void:
                    return nadirTime;
                default:
                    throw new IndexOutOfRangeException();
            }
        }
        set
        {
            switch (indexSegment)
            {
                case Segment.Beach:
                    shoreTime = value;
                    break;
                case Segment.Tropics:
                    tropicsTime = value;
                    break;
                case Segment.Alpine:
                    alpmesaTime = value;
                    break;
                case Segment.Caldera:
                    calderaTime = value;
                    break;
                case Segment.TheKiln:
                    kilnTime = value;
                    break;
                case Segment.Void:
                    nadirTime = value;
                    break;
                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }

    /// <summary> Run equality: true iff all times and game properties are equal. Run metadata, such as runDate, is not considered.</summary>
    public override bool Equals(object obj)
    {
        if (obj == null || this.GetType() != obj.GetType()) return false;

        RunTime otherRun = (RunTime)obj;
        if (otherRun.runFinished != this.runFinished) return false;
        if (otherRun.finalTime != this.finalTime) return false;

        if (otherRun.shoreTime != this.shoreTime) return false;
        if (otherRun.tropicsTime != this.tropicsTime) return false;
        if (otherRun.alpmesaTime != this.alpmesaTime) return false;
        if (otherRun.calderaTime != this.calderaTime) return false;
        if (otherRun.kilnTime != this.kilnTime) return false;
        if (otherRun.nadirTime != this.nadirTime) return false;

        if (otherRun.gameVersion != this.gameVersion) return false;
        if (otherRun.levelName != this.levelName) return false;
        if (otherRun.ascentDifficulty != this.ascentDifficulty) return false;
        if (otherRun.playerCount != this.playerCount) return false;
        if (otherRun.isRealTime != this.isRealTime) return false;
        if (otherRun.customRun != this.customRun) return false;

        if (otherRun.wasRandomized != this.wasRandomized) return false;
        if (otherRun.seed != this.seed) return false;

        return true;
    }

    static System.Random HashCodeStartingRandom { get { return new System.Random(1337); } }
    public override int GetHashCode()
    {
        // Since we start from the same seed every time, this should always produce identical outputs.
        System.Random hashRandom = HashCodeStartingRandom;
        float GetRandomFloat()
        {
            return (float)hashRandom.NextDouble();
        }

        float currHashFloat = runFinished ? -1_234f : 9_876f;
        currHashFloat *= GetRandomFloat();
        currHashFloat += finalTime * GetRandomFloat();

        currHashFloat += shoreTime * GetRandomFloat();
        currHashFloat += tropicsTime * GetRandomFloat();
        currHashFloat += alpmesaTime * GetRandomFloat();
        currHashFloat += calderaTime * GetRandomFloat();
        currHashFloat += kilnTime * GetRandomFloat();
        currHashFloat += nadirTime * GetRandomFloat();

        currHashFloat += gameVersion.GetHashCode() * GetRandomFloat();
        currHashFloat += levelName.GetHashCode() * GetRandomFloat();
        currHashFloat += ascentDifficulty * GetRandomFloat();
        currHashFloat += playerCount * GetRandomFloat();
        currHashFloat += isRealTime ? 23493.18f : 1.483f;
        currHashFloat += customRun ? 1896.18f : 846.21f;
        currHashFloat += customRunSettingsHash % 100_000.0f;

        currHashFloat += wasRandomized ? 492.6784f * seed : 38025f;

        byte[] rawData = BitConverter.GetBytes(currHashFloat);
        return BitConverter.ToInt32(rawData);
    }

    /// <summary> Returns true if any time info is stored in this object (has info that should be saved). </summary> 
    public bool HasTimes()
    {
        if (runFinished && finalTime > 0.0f) return true;
        if (shoreTime > 0.0f) return true;
        if (tropicsTime > 0.0f) return true;
        if (alpmesaTime > 0.0f) return true;
        if (calderaTime > 0.0f) return true;
        if (kilnTime > 0.0f) return true;
        if (nadirTime > 0.0f) return true;
        return false;
    }
}

public enum Ascent : int
{
    Tenderfoot = -1,
    Default = 0,
    Ascent1 = 1,
    Ascent2 = 2,
    Ascent3 = 3,
    Ascent4 = 4,
    Ascent5 = 5,
    Ascent6 = 6,
    Ascent7 = 7,
    Ascent8 = 8
}