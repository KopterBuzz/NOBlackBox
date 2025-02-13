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
2. After BepInEx has been installed, change this setting in BepInEx.cfg:
```
[Caching]
HideManagerGameObject = true
```
3. Copy the NOBlackBox.dll file to the game's BepInEx plugins folder.
4. Enjoy :)

## The Tacview assets
1. [Install TacView](https://www.tacview.net/download/latest/en/) and run it once to make sure it settles. :)
2. Unzip the TacViewAssets Archive found in the [mod release](https://github.com/KopterBuzz/NOBlackBox/releases/latest).
3. Copy all content to %ProgramData%\Tacview\Data
	
- Windows might ask you if you want to merge the folders. Let it happen.
4. Restart Tacview and in its settings, Change Terrain to Falcon 4.0. We currently hijack the Falcon 4.0 support to inject Nuclear Option Terrain into Tacview.
## Using NOBlackBox
The mod will be enabled by default after installation.

Flight recording automatically starts when a mission is loaded, and automatically stops when you return to the main menu.

The recording will be saved to disk when you go back to the main menu.

You will find the TacView recordings in the %localappdata%low\Shockfront\NuclearOption\Replays directory.

## Configuring NOBlackBox
The Following parameters are configurable in BepInEx/xzy.KopteBuzz.NOBlackBox.cfg. You have to load the mod at least once for the config file to exist.

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

Time interval for automatically updating the Tacview file. Min value: 60

Default value: 60

