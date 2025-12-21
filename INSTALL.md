# Installation and use of NOBlackBox
[1 The mod](./INSTALL.md#the-mod)

[2 The Tacview assets](./INSTALL.md#the-tacview-assets)

[3 Using NOBlackBox](./INSTALL.md#using-noblackbox)

[4 Configuring NoBlackBox](./INSTALL.md#configuring-noblackbox)


## The mod
The mod works with the following loaders:

	- BepInEx 5

### Installation with BepInEx

###### It is known that this mod will not work if you are running Nuclear Option or Nuclear Option Dedicated Server on Windows with the Built-In Administrator account.
###### If you are facing this issue I highly recommend you stop using the Built-In Administrator to run the game.
###### Furthermore, this is considered an unsupported configuration and I will not be able to provide technical assistance.

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
4. Copy the NOBlackBox folder to the game's BepInEx plugins folder.
5. Enjoy :)

## The Tacview assets
###### Unlike previous versions of Tacview, If you have 1.9.5 or newer, you no longer need to manually switch maps for Tacview.
###### Tacview 1.9.5 also comes with Nuclear Option Assets by default, and Nuclear Option has a faster release cycle than Tacview 1, at least currently, so the assets that come with Tacview might be out of date.
###### Therefore, it might still be beneficial to install the latest assets from source with the below instructions.
1. [Install Tacview](https://www.tacview.net/download/latest/en/) and run it once to make sure it settles. :)
2. Download [Tacview Assets for NOBlackBox](https://github.com/KopterBuzz/NOBlackBoxTacviewAssets/archive/refs/heads/main.zip) 
3. Unzip the downloaded Archive.
5. Inside the unzipped directory, you should see two scripts, install.bat and TacviewAssetSetup.ps1

	Open their Properties from the right-click drop-down menu (right-click/show more options/Properties), and at the bottom of the General Tab, tick Unblock then click Apply and Ok.
4. Open Windows PowerShell as Administrator
5. Run ```Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass -Force``` 
6. Browse to the unzipped archive, where the install scripts are
7. run ```.\install.bat``` 
8. If it was running, restart Tacview. Change Terrain Layer to Nuclear Option.


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

DEPRECATED USE THE REST OF THE UPDATE AND DISCOVERY RATE SETTINGS TO CUSTOMIZE UPDATE FREQUENCIES

The number of times per second NOBlackBox will record events. Max Value: 1000

Please do note that higher values impact performance.

Default value: 5

### Unit Discovery Rate

Time interval in seconds to discover Units.

Default value: 1

### BulletSim Discovery Rate

Time interval in seconds to discover objects that fire bullets.

Default value: 1

### Shockwave Discovery Rate

Time interval in seconds to discover explosion shockwaves.

Default value: 0.5

### Aircraft Update Rate

Time interval in seconds to update Aircraft.

Default value: 0.2

### Vehicle Update Rate

Time interval in seconds to update Vehicles and Ships.

Default value: 1

### Munition Update Rate

Time interval in seconds to update Bombs, Missiles and Rockets.

Default value: 0.2

### Shockwave Update Rate

Time interval in seconds to update Shockwave Propagation.

Default value: 0.016

### Tracer Update Rate

Time interval in seconds to Projectile Tracers.

Default value: 1

### Flare Update Rate

Time interval in seconds to update Flares.

Default value: 1

### Building Update Rate

Time interval in seconds to update Buildings.

Default value: 1

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

