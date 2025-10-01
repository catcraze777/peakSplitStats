using System;
using System.IO;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;
using UnityEngine;

namespace SplitsStats;

public class RunSaveManager
{
    public static RunTime targetRun { get { if (SettingsManager.useAverageRun) return averageRun; else return fastestRun; } }
    public static float targetShore { get { if (SettingsManager.useAverageRun) return averageShore; else return fastestShore; } }
    public static float targetTropics { get { if (SettingsManager.useAverageRun) return averageTropics; else return fastestTropics; } }
    public static float targetAlpmesa { get { if (SettingsManager.useAverageRun) return averageAlpmesa; else return fastestAlpmesa; } }
    public static float targetCaldera { get { if (SettingsManager.useAverageRun) return averageCaldera; else return fastestCaldera; } }
    public static float targetKiln { get { if (SettingsManager.useAverageRun) return averageKiln; else return fastestKiln; } }

    public static RunTime fastestRun { get; private set; }
    public static float fastestShore { get; private set; }
    public static float fastestTropics { get; private set; }
    public static float fastestAlpmesa { get; private set; }
    public static float fastestCaldera { get; private set; }
    public static float fastestKiln { get; private set; }

    public static RunTime averageRun { get; private set; }
    public static float averageShore { get; private set; }
    public static float averageTropics { get; private set; }
    public static float averageAlpmesa { get; private set; }
    public static float averageCaldera { get; private set; }
    public static float averageKiln { get; private set; }

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
        currentRun = new RunTime();
        currentRun.UpdateTimeString();
        runStorage.Add(new RunTime());
        return currentRun;
    }

    /// <summary> Save the information currently stored in this.currentRun </summary>
    /// <returns> A boolean indicating if the save was successful. </returns>
    public static bool SaveRun()
    {
        if (!IsRunActive())
        {
            if (SplitsStatsPlugin.Logger != null) SplitsStatsPlugin.Logger.LogError($"Tried to save a run when no run has been started!");
            return false;
        }

        runStorage[^1] = new RunTime(currentRun);

        try
        {
            if (currentRun.HasTimes()) TryWriteSave();
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
    }

    public static void TryWriteSave()
    {
        string json = JsonConvert.SerializeObject(runStorage, Formatting.Indented);
        File.WriteAllText(jsonFilePath, json);
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
        fastestRun = new RunTime();
        fastestShore = -1.0f;
        fastestTropics = -1.0f;
        fastestAlpmesa = -1.0f;
        fastestCaldera = -1.0f;
        fastestKiln = -1.0f;

        averageRun = new RunTime();
        averageRun.finalTime = 0.0f;
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

            if (run.runFinished && (fastestRun.finalTime == -1.0f || run.finalTime < fastestRun.finalTime))
            {
                fastestRun = new RunTime(run);
                averageRun.finalTime += run.finalTime;
                numCompleteRuns++;
            }

            if (run.shoreTime > 0.0f && (fastestShore == -1.0f || run.shoreTime < fastestShore))
            {
                fastestShore = run.shoreTime;
                averageShore += run.shoreTime;
                numShores++;
            }

            if (run.tropicsTime > 0.0f && (fastestTropics == -1.0f || run.tropicsTime < fastestTropics))
            {
                fastestTropics = run.tropicsTime;
                averageTropics += run.tropicsTime;
                numTropics++;
            }

            if (run.alpmesaTime > 0.0f && (fastestAlpmesa == -1.0f || run.alpmesaTime < fastestAlpmesa))
            {
                fastestAlpmesa = run.alpmesaTime;
                averageAlpmesa += run.alpmesaTime;
                numAlpmesa++;
            }

            if (run.calderaTime > 0.0f && (fastestCaldera == -1.0f || run.calderaTime < fastestCaldera))
            {
                fastestCaldera = run.calderaTime;
                averageCaldera += run.calderaTime;
                numCaldera++;
            }

            if (run.kilnTime > 0.0f && (fastestKiln == -1.0f || run.kilnTime < fastestKiln))
            {
                fastestKiln = run.kilnTime;
                averageKiln += run.kilnTime;
                numKiln++;
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

        if (numCompleteRuns > 0) { averageRun.finalTime /= numCompleteRuns; averageRun.runFinished = true; }
        else { averageRun.finalTime = -1.0f; averageRun.runFinished = false; }

        averageRun.shoreTime = averageShore;
        averageRun.tropicsTime = averageTropics;
        averageRun.alpmesaTime = averageAlpmesa;
        averageRun.calderaTime = averageCaldera;
        averageRun.kilnTime = averageKiln;
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

    public string gameVersion;
    public string levelName;
    public int ascentDifficulty;
    public int playerCount;

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

        gameVersion = "";
        levelName = "";
        ascentDifficulty = 0;
        playerCount = 0;

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

        gameVersion = original.gameVersion;
        levelName = original.levelName;
        ascentDifficulty = original.ascentDifficulty;
        playerCount = original.playerCount;

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

        if (otherRun.gameVersion != this.gameVersion) return false;
        if (otherRun.levelName != this.levelName) return false;
        if (otherRun.ascentDifficulty != this.ascentDifficulty) return false;
        if (otherRun.playerCount != this.playerCount) return false;

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

        currHashFloat += gameVersion.GetHashCode() * GetRandomFloat();
        currHashFloat += levelName.GetHashCode() * GetRandomFloat();
        currHashFloat += ascentDifficulty * GetRandomFloat();
        currHashFloat += playerCount * GetRandomFloat();

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
    Ascent7 = 7
}