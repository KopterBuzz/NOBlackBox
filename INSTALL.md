# Installation of NOBlackBox

## The mod
0. [Download](https://github.com/KopterBuzz/NOBlackBox/releases) the latest release of the mod.
1. Download and install [Unity Mod Manager](https://www.nexusmods.com/site/mods/21).
2. Launch Unity Mod Manager and enable 'Nuclear Option' in the drop down list. If it is there, skip Steps 3 and 4. If its not there, follow them.
3. Open UMM folder and open 'UnityModManagerConfig.xml'
4. Go to end of file and paste the below, between the last </GameInfo> and </Config> : 
```
	<GameInfo Name="Nuclear Option">
		<Folder>Nuclear Option</Folder>
		<ModsDirectory>Mods</ModsDirectory>
		<ModInfo>Info.json</ModInfo>
		<GameExe>NuclearOption.exe</GameExe>
		<EntryPoint>[UnityEngine.UI.dll]UnityEngine.EventSystems.EventSystem.cctor:After</EntryPoint>
		<StartingPoint>[Assembly-CSharp.dll]MainMenu.Start:After</StartingPoint>
		<UIStartingPoint>[Assembly-CSharp.dll]MainMenu.Start:After</UIStartingPoint>
		<MinimalManagerVersion>0.27.8</MinimalManagerVersion>
	</GameInfo>
```
5. Then click install
6. Go to the Mod tab 
7. Drag & Drop the NOBlackBox_x.y.z.zip file in the "Install" zone
## The Tacview assets
1. [Install TacView](https://www.tacview.net/download/latest/en/) and run it once to make sure it settles. :)
2. Unzip the TacViewAssets Archive found in the [mod release](https://github.com/KopterBuzz/NOBlackBox/releases).
3. Copy all content to %ProgramData%\Tacview\Data
	Windows might ask you if you want to merge the folders. Let it happen.
4. Restart Tacview and in its settings, Change Terrain to Falcon 4.0. We currently hijack the Falcon 4.0 support to inject Nuclear Option Terrain into Tacview.