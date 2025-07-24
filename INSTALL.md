# Installation and use of NOBlackBox
[1 The mod](./INSTALL.md#the-mod)

[2 The Tacview assets](./INSTALL.md#the-tacview-assets)

[3 Using NOBlackBox](./INSTALL.md#using-noblackbox)

[4 Configuring NoBlackBox](./INSTALL.md#configuring-noblackbox)


## The mod
The mod works with the following loaders:

	- BepInEx 5

### Installation with BepInEx
0. [Download](https://github.com/KopterBuzz/NOBlackBox/releases) the latest release of the mod. Unzip it somewhere.
1. Download and install the latest release of [BepInEx 5](https://github.com/BepInEx/BepInEx/releases/latest) for Nuclear Option.

	You need the latest "BepInEx_win_x64" release of BepInEx 5.

	[BepInEx Installation Guide.](https://docs.bepinex.dev/articles/user_guide/installation/index.html#where-to-download-bepinex)
2. After installing BepInEx, run the game once so BepInEx can generate its default directory and file structure.
3. After BepInEx has been installed, change this setting in BepInEx.cfg:
```
[Chainloader]
HideManagerGameObject = true
```
3. Copy the NOBlackBox folder file to the game's BepInEx plugins folder.
4. Enjoy :)

## The Tacview assets
1. [Install Tacview](https://www.tacview.net/download/latest/en/) and run it once to make sure it settles. :)
2. Unzip the TacviewAssets Archive found in the [mod release](https://github.com/KopterBuzz/NOBlackBox/releases/latest).
3. Copy all content to %ProgramData%\Tacview\Data
- Windows might ask you if you want to merge the folders. Let it happen.
4. Restart Tacview and in its settings, Change Terrain to Falcon 4.0. We currently hijack the Falcon 4.0 support to inject Nuclear Option Terrain into Tacview.

## Switching between Different Tacview MAPS

### If you have 1.9.5 Beta 11 and use NOBlackBox 0.3.6 or newer to create recordimgs, you no longer need to manually switch maps for Tacview.
### If you are using Tacview 1.9.4 Stable, or an older Beta, follow the below procedure:

At the moment, Tacview can only load ONE Nuclear Option Terrain at a time.
To switch between different Terrains do this:

Check the name of the Replay File. The terrain name is in last segment after the mission name, and before the file type (if you have file types set to visible on your computer).
	
Something like these:

```
2025-05-24T18-31-49_Free Flight - Heartland_Terrain1.acmi
2025-05-24T18-35-07_Terminal Control_Terrain_naval.acmi
```

At the moment Nuclear option has Two Terrains: Terrain1 and Terrain_naval.

In the future there might be more built-in Terrains, and also Custom terrains (with future mod support, hopefully ^^).
	
If you followed the Installation steps correctly, in %ProgramData%\Tacview\Data folder, you should see a Terrain folder.
	
In %ProgramData%\Tacview\Data\Terrain, you should see the Textures and a Custom folders.
	
In %ProgramData%\Tacview\Data\Terrain\Textures, there will be a few files that look like this:

```
Terrain1.png
Terrain1_CustomTextureList.xml
Terrain_naval.png
Terrain_naval_CustomTextureList.xml
```

Notice how the XML files are named <Terrain_Name>_CustomTextureList.xml

To tell Tacview which Terrain texture to load, rename the desired one to `CustomTextureList.xml` - it is case sensitive (at least for the file type), so please make sure spelling is correct.

Now let's go to %ProgramData%\Tacview\Data\Terrain\Custom to sort out the elevation map.

Similarly, you will see a set of files like this:

```
NOBlackBox_heightmap_Terrain1.data
NOBlackBox_heightmap_Terrain_naval.data
Terrain1_CustomHeightmapList.xml
Terrain_naval_CustomHeightmapList.xml
```

To tell Tacview which Terrain Elevation map to load, rename the desired one to `CustomHeightmapList.xml` - it is case sensitive (at least for the file type), so please make sure spelling is correct.

If you did everything right, after restarting Tacview the desired Terrain will be loaded.

## Using NOBlackBox
The mod will be enabled by default after installation.

Flight recording automatically starts when a mission is loaded, and automatically stops when you return to the main menu.

The recording will be saved to disk when you go back to the main menu.

You will find the Tacview recordings in the %localappdata%low\Shockfront\NuclearOption\Replays directory.

## Using the Heightmap Generator

You can use NOBlackBox to generate Tacview-compatible elevation maps and textures for your Terrains.

The Heightmap Generator is DISABLED by default. To Enable it, set EnableHeightmapGenerator Configuration Setting to `true`.

To start the Heightmap Generator, load any mission, then once the mission has loaded, press the GenerateHeightmapKey, which is F10 by default.

Please note that with the default settings this will take 2-3 minutes, if you increased the resolution and the sampling rate, it can take significantly longer with no real benefit to quality.

ALSO THERE IS A MEMORY LEAK INTRODUCED BY THE TEXTURE GENERATOR, QUIT THE GAME IMMEDIATELY AFTER THE PROCESS FINISHED OR IT WILL EAT ALL YOUR RAM.

## Configuring NOBlackBox
All configurable settings conform to BepInEx Configuration Standards.

As a result, you can install and use [BepInEx Configuration Manager](https://github.com/BepInEx/BepInEx.ConfigurationManager) ([Latest Release](https://github.com/BepInEx/BepInEx.ConfigurationManager/releases), download the BepInEx5 version) to configure all settings at runtime from its GUI (F1 key by default), or you can choose to edit the .cfg file as explained below.

The Following parameters are configurable in BepInEx/config/xzy.KopteBuzz.NOBlackBox.cfg. You have to load the mod at least once for the config file to exist.

Default values are automatically set.

### UpdateRate

The number of times per second NOBlackBox will record events. Max Value: 1000

Please do note that higher values impact performance.

Default value: 5

### OutputPath
The location where Tacview files will be saved. Must be a valid folder path.

MUST END WITH /

Default value: %LocalAppData%Low/Shockfront/NuclearOption/Replays/

### AutoSaveInterval

Time interval (in seconds) for automatically updating the Tacview file. Min value: 60

Default value: 60

### UseMissionTime

Toggles between using Server clock and Mission clock for recordings.

### RecordSpeed

Toggles recording True Airspeed and Mach number.

### RecordAOA

Toggles recording Angle of Attack.

### RecordAGL

Toggles recording AGL.

### RecordRadarMode

Toggles recording radar mode changes.

### RecordLandingGear

Toggles recording Landing Gear changes.

### RecordPilotHead

Toggles recording pilot head movement.

### EnableAutoSaveCountDown

Toggles the GUI countdown display for when will NOBlackBox autosave next.

### TextColorR,TextColorG,TextColorB,TextColorA

RGB Color and Transparency for the GUI Text Items displayed by NOBlackBox

### EnableHeightmapGenerator

Enable/Disable the Heightmap Generator
When disabled, hitting the associated hotkey will do nothing.

### HeightMapResolution

Resolution of the Heightmap and Texture output the Heightmap Generator produces. Must be divisible by 4.

Default is 4096, this should be adequate. The bigger the resolution, the more time it takes to scan.

### MetersPerScan

Sample rate of the Heightmap Generator. Must be at least 1 (this will be very slow)

### GenerateHeightMapKey

Key binding for the hotkey that triggers the Heightmap Generator.

