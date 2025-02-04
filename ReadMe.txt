NOBlackBox - NOBlackBox - TacView Recorder for Nuclear Option
----------------------------------------------------------
# AUTHORS:					 	 #
# 7ep3s		Programming,TacView customization	 #
# TunderTunder	TacView customization		 	 #
----------------------------------------------------------
--------------------------------------------------
# 		INSTALLING THE MOD	         #
--------------------------------------------------
1. Download and install Unity Mod Manager (https://www.nexusmods.com/site/mods/21)
2. Open UMM folder and open 'UnityModManagerConfig.xml'
3. Go to end of file and paste the below, between the last </GameInfo> and </Config> : 
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
4. Launch Unity Mod Manager and enable 'Nuclear Option' in the drop down list
5. Then click install
6. Go to the Mod tab 
7. Drag & Drop the NOBlackBox_<versionNumber> zip file in the "Install" zone
--------------------------------------------------
# 		INSTALLING TacView Assets        #
--------------------------------------------------
1. Install TacView and run it once to make sure it settles. :)
2. Unzip the TacViewAssets Archive
3. Copy all content to %ProgramData%\Tacview\Data
	ALLOW THE FOLDERS TO MERGE!
4. Change Terrain to Falcon 4.0 in TacView's Settings. This is how we can include the Terrain Texture at the moment.
------------------------------------------
# 		Using NOBlackBox         #
------------------------------------------
The mod will be enabled by default after installation.

You can toggle the mod on/off in UMM menu.

When the mod is active, Starting a Single Player game, or Hosting a Multiplayer game will enable recording.

If you join someone else's Multiplayer game, at the moment it can only start recording when you join a faction.

The recording will be saved to disk when you go back to the main menu.
You will find the TacView recordings in %USERPROFILE%\Documents\NOTacView.
------------------------------------------
# 		CHANGELOG	         #
------------------------------------------
#0.1.2 - 03 Feb 2025

Fixes:
-Fixed Issue with inverted pitch and roll.
NONE OF THESE FIXES ARE APPLICABLE TO OLD TACVIEW FILES.
Changes:
-Added meshes to Tacview integration.
##########################################
#0.1.1 - 03 Feb 2025

Changes:
-Added Initial Terrain Height Map
##########################################
#0.1.0 - 03 Feb 2025

Rewrote the entire thing from scratch.
Fixes:
-Fixed Issue with units not facing the right way in TacView.
-Fixed Issue with dead aircraft/ordnance not decluttering in TacView.
-Fixed Issue with inaccurate time, mission clock wasn't being read correctly so the TacView recordings were always "sped up".
NONE OF THESE FIXES ARE APPLICABLE TO OLD TACVIEW FILES.
Changes:
-Added basic TacView visual integration - Unit Database, Terrain Texture for the current map. More to come later.
-When Hosting Multiplayer, or playing Single Player, all units are recorded. (this was not possible in previous versions)
-If you join a Multiplayer Game as a Client, the mod will only record what is visible on your datalink only. (same as the previous version)
-Offloaded the saving of the recording to a Unity Coroutine, so saving large recordings will no longer freeze the game while trying to return to Main Menu.
##########################################
#0.0.1 - When the world was young, can't remember the date

Initial release. IT IS RAW.