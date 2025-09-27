# peakSplitStats
A speedrunning timer for Peak, with many built in features! Designed for casual speedrunners as a client-side only mod to help visualize your current pace and keep track of past times automatically. Also includes a base MonoBehavior for other modders that would like to display their own info within this mod!

## Features
* Very Customizable
* Automatic Splits
* Saves Runtimes to use for Green/Red/Gold splits.
* Categorizes runs to only display relevant records.
* Edited End Screen to Display the run's final pace.
* Extra info for current height and distance from next campfire/peak.
* Option to edit the current ascent text to display run category info.
* Supports the Terrain Randomiser for run categorization.
* Generalized components for other modders looking to add custom info to display.

# Screenshots
![splitStatsTimerExample1](https://github.com/user-attachments/assets/102e836c-d57d-46cd-a85b-46f327771419)
<img width="348" height="422" alt="PEAK_23_17-27-20_871" src="https://github.com/user-attachments/assets/a0f41fc2-ea1e-4a2a-8f93-ef450d5bd3c2" />

# Modder Info
If you'd like to add a custom component, please look into the BaseUIComponent abstract class and the InfoComponent class for documentation and how to implement your own custom UI element within SplitStats. An example plugin that adds a simple InfoComponent is provided in the ExampleUIAddon directory of this repository. Once you've properly made your component, add it into SplitStats using SplitStats.AddCustomStat(InfoComponentTemplate addonTemplate) or SplitStats.AddCustomComponent(BaseUIComponent newComponent), depending on how you chose to implement your UI element.

The results displayed from the example addon:

<img width="268" height="204" alt="PEAK_25_23-27-26_512" src="https://github.com/user-attachments/assets/81f441ee-4467-4a9f-9fd2-4c2f68a197bd" />

