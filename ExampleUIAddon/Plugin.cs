using SplitsStats;
using BepInEx;
using BepInEx.Logging;
using UnityEngine;

namespace SplitsStatsTestingPatch;

[BepInPlugin("net.catcraze777.plugins.splitsstatstestingaddon", MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency("net.catcraze777.plugins.splitsstats")]
public class TestingAddon : BaseUnityPlugin
{
    internal static new ManualLogSource Logger;

    public const string PLUGIN_GUID = "net.catcraze777.plugins.splitsstatstestingaddon";

    private void Awake()
    {
        // Plugin startup logic
        Logger = base.Logger;
        Logger.LogInfo($"Plugin {PLUGIN_GUID} is loaded!");

        InfoComponentAddon simpleAddon = new("Testing Addon", TextToDisplay: () => "Hello!", initialFontSize: SplitsManager.HEIGHT_STAT_FONT_SIZE * 0.7f,
                                                            color: new Color(1.0f, 0.0f, 1.0f), icon: SplitsStatsPlugin.LoadSprite("img/test.png"), priority: -1);

        SplitsStatsPlugin.AddCustomStat(simpleAddon);
    }
}
