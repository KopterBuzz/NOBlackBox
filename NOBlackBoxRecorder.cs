using System;
using System.Collections;
using System.Collections.Generic;
using System.IO.Compression;
using System.IO;
using System.Text;
using UnityEngine;
using System.Linq;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace NOBlackBox
{
    internal class NOBlackBoxRecorder : MonoBehaviour
    {
        internal Dictionary<string, string> RecordTypes = new Dictionary<string, string>()
        {
            ["Aircraft"] = "Air+Fixedwing",
            ["Building"] = "Ground+Static+Building",
            ["Container"] = "Misc+Container",
            ["CruiseMissile"] = "Weapon+Missile",
            ["Factory"] = "Ground+Static+Building",
            ["GroundVehicle"] = "Ground+Vehicle",
            ["GuidedBomb"] = "Weapon+Bomb",
            ["Missile"] = "Weapon+Missile",
            ["PilotDismounted"] = "Ground+Light+Human+Air+Parachutist",
            ["Ship"] = "Sea+Watercraft+Warship",
            ["VehicleDepot"] = "Ground+Static+Building",
            ["GuidedShell"] = "Weapon+Missile",
            ["tracer(Clone)"] = "Projectile+Bullet",
            ["IRFlare(Clone)"] = "Misc+Decoy+Flare"
        };
        internal Dictionary<string, HashSet<string>> unitTypesToPoll = new Dictionary<string, HashSet<string>>()
        {
            ["HIGH"] = new HashSet<string>() {
                "Aircraft",
                "Missile",
                "GuidedBomb",
                "GuidedShell",
                "CruiseMissile",
                "Container"
            },
            ["LOW"] = new HashSet<string>()
            {
                "Building",
                "Factory",
                "Ship",
                "VehicleDepot",
                "GroundVehicle",
                "PilotDismounted"
            },
            ["ALL"] = new HashSet<string>()
            {
                "Aircraft",
                "Missile",
                "GuidedBomb",
                "GuidedShell",
                "CruiseMissile",
                "Container",
                "Building",
                "Factory",
                "Ship",
                "VehicleDepot",
                "GroundVehicle",
                "PilotDismounted",
                "tracer(Clone)",
                "IRFlare(Clone)"
            }
        };
        private static float updateCount = 1;
        private static float fixedUpdateCount = 1;
        private static float updateUpdateCountPerSecond;
        private static float updateFixedUpdateCountPerSecond;
        private static float timer = 0.0f;
        private static float defaultWaitTime = 0.2f;

        private static StringBuilder sb = new StringBuilder("FileType=text/acmi/tacview\nFileVersion=2.2\n");
        private static string dateStamp = System.DateTime.Now.ToString("MM/dd/yyyy");
        private readonly Regex dateStampPattern = new Regex(":|/|\\.");
        private static string referenceTime;

        private static int recordedScreenWidth;
        private static float guiAnchorLeft, guiAnchorRight;

        private static HashSet<int> knownUnits = new HashSet<int>();
        private static Mirage.Collections.SyncList<int> unitIDs = new Mirage.Collections.SyncList<int>();
        private static List<int> purgeIDs = new List<int>();
        private static List<Player> players = new List<Player>();
        private static Dictionary<int, string> playerAircraftList = new Dictionary<int, string>();

        static List<GameObject> bullets;
        static List<GameObject> flares;
        static List<GameObject>? stuff;
        private readonly Regex bulletsFlaresPattern = new Regex("tracer\\(Clone\\)|IRFlare\\(Clone\\)");
        static Dictionary<int, NonUnitRecord> NonUnitRegistry = new Dictionary<int, NonUnitRecord>();


        private static string startTime = "none";
        private static string missionName = "none";

        private static bool saving = false;
        private static bool recording = false;

        private static int tick = 0;
        private static string pollType = "ALL";
        private static void UpdatePlayerAircraftList()
        {
            playerAircraftList.Clear();
            if (!players.Any())
            {
                Debug.Log("NO PLAYERS");
                return;
            }
            foreach (Player player in players)
            {
                //Debug.Log("Adding ID " + player.Aircraft.persistentID.ToString() + " NAME " + player.PlayerName);
                try
                {
                    playerAircraftList.Add(player.Aircraft.persistentID, player.PlayerName);
                }
                catch
                {
                    //nothing
                }
                
            }
        }
        private static void UpdateGuiAnchors()
        {
            recordedScreenWidth = Screen.width;
            guiAnchorLeft = (int)Math.Round(0.03 * recordedScreenWidth);
            guiAnchorRight = (int)Math.Round(0.7 * recordedScreenWidth);
        }
        void Awake()
        {
            LoadingManager.MissionUnloaded += StopRecording;
            LoadingManager.MissionLoaded += StartRecording;
            DontDestroyOnLoad(this.gameObject);
            UpdateGuiAnchors();
            StartCoroutine(DebugFrameCounter());
        }
        // Increase the number of calls to Update.
        void Update()
        {
            UpdateGuiAnchors();
            FindBullets();
            updateCount += 1;
            timer += Time.deltaTime;
            if (timer >= defaultWaitTime && recording)
            {
                tick += 1;
                if (tick == 5)
                {
                    pollType = "ALL";
                    timer = 0.0f;
                    tick = 0;
                }
                else
                {
                    pollType = "HIGH";
                }
                NOBlackBoxWrite(true);
                return;
            }
        }
        void FixedUpdate()
        {
            fixedUpdateCount += 1;
        }
        void OnEnable()
        {
            Flush();
            DontDestroyOnLoad(this.gameObject);
        }

        void OnDisable()
        {
            StopAllCoroutines();
            Flush();
        }
        // Show the number of calls to both messages.
        void OnGUI()
        {
            GUIStyle fontSize = new GUIStyle(GUI.skin.GetStyle("label"));
            fontSize.fontSize = 24;
            GUI.Label(new Rect(guiAnchorRight, 100, 200, 50), "Update: " + updateUpdateCountPerSecond.ToString(), fontSize);
            GUI.Label(new Rect(guiAnchorRight, 150, 200, 50), "FixedUpdate: " + updateFixedUpdateCountPerSecond.ToString(), fontSize);
            if (saving)
            {
                GUI.Label(new Rect(guiAnchorRight, 500, 200, 50), "SAVING...", fontSize);
            }
            if (recording)
            {
                GUI.Label(new Rect(guiAnchorRight, 300, 200, 50), "REC", fontSize);
            }
            GUI.Label(new Rect(guiAnchorRight, 400, 200, 50), "Bullets: " + bullets.Count().ToString(), fontSize);

        }
        // Update both CountsPerSecond values every second.
        IEnumerator DebugFrameCounter()
        {
            while (true)
            {
                yield return new WaitForSeconds(1);
                updateUpdateCountPerSecond = updateCount;
                updateFixedUpdateCountPerSecond = fixedUpdateCount;
                updateCount = 1;
                fixedUpdateCount = 1;
            }
        }
        private static void SaveTacViewFile(string acmi, string timestamp)
        {
            saving = true;
            string myDocs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            Directory.CreateDirectory(myDocs + "\\NOTacView");
            string ACMIFilePath = myDocs + "\\NOTacView\\" + timestamp + "-" + missionName + "TacView" + ".zip";
            CreateZipWithText(acmi, "missionData.acmi", ACMIFilePath);
            saving = false;
            Flush();
        }
        private void NOBlackBoxWrite(bool server)
        {
            if (missionName == "none")
            {
                List<Faction> factionList = FactionRegistry.factions;
                foreach (Faction faction in factionList)
                {
                    Debug.Log("DEBUG FACTION LIST: " + faction.name);
                }
                missionName = MissionManager.CurrentMission.Name;
                startTime = NOBlackBoxHelper.TimeOfDay(LevelInfo.i.timeOfDay);
                referenceTime = dateStampPattern.Replace(dateStamp, "-");
                string startTimeString = "0,ReferenceTime=" + referenceTime + "T" + startTime + "Z\n";
                sb.Append(startTimeString);
            }
            players.Clear();
            if (FactionRegistry.HqFromName("Primeva").GetPlayers(false).Any())
            {
                players.AddRange(FactionRegistry.HqFromName("Primeva").GetPlayers(false));
            }
            if (FactionRegistry.HqFromName("Boscali").GetPlayers(false).Any())
            {
                players.AddRange(FactionRegistry.HqFromName("Boscali").GetPlayers(false));
            }
            if (players.Any())
            {
                UpdatePlayerAircraftList();
            }


            unitIDs.Clear();
            if (FactionRegistry.HqFromName("Primeva").factionUnits.Any())
            {
                unitIDs.AddRange(FactionRegistry.HqFromName("Primeva").factionUnits);
            }
            if (FactionRegistry.HqFromName("Boscali").factionUnits.Any())
            {
                unitIDs.AddRange(FactionRegistry.HqFromName("Boscali").factionUnits);
            }
                
            if (!unitIDs.Any())
            {
                Debug.Log("[NOBLACKBOX]: NO UNITS!!!!");
                return;
            }
            sb.Append("#" + MissionManager.i.NetworkmissionTime.ToString(CultureInfo.InvariantCulture) + "\n");
            for (int i = 0; i < unitIDs.Count; i++)
            {

                ProcessUnit(unitIDs[i]);
                knownUnits.Add(unitIDs[i]);
            }
            foreach (int id in knownUnits)
            {
                if (!unitIDs.Contains(id))
                {
                    purgeIDs.Add(id);
                    sb.Append("-" + id + "\n");
                }
            }
            for (int i = 0; i < purgeIDs.Count; i++)
            {
                knownUnits.Remove(purgeIDs[i]);
            }
            purgeIDs.Clear();
        }
        private void ProcessUnit(int unitId)
        {
            Unit unit = null;
            UnitRegistry.TryGetUnit(unitId, out unit);
            if (null != unit)
            {
                try
                {
                    if (null != unit.NetworkHQ.faction)
                    {
                        if (!knownUnits.Contains(unitId))
                        {
                            sb.Append(TacViewACMIUnit(unit, true));
                        }
                        if (knownUnits.Contains(unitId) && unit.speed != 0 && unitTypesToPoll[pollType].Contains(unit.GetType().Name))
                        {
                            sb.Append(TacViewACMIUnit(unit, false));
                        }
                    }
                    return;
                }
                catch
                {
                    return;
                }
            }
            return;
        }
        public static void CreateZipWithText(string content, string entryName, string destinationZipPath)
        {
            // Create a memory stream to hold the zip archive in memory
            using (MemoryStream memoryStream = new MemoryStream())
            {
                // Create a new zip archive in the memory stream
                using (ZipArchive archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
                {
                    // Create a new entry in the zip archive
                    ZipArchiveEntry entry = archive.CreateEntry(entryName);

                    // Write the string content to the entry
                    using (StreamWriter writer = new StreamWriter(entry.Open(), Encoding.UTF8))
                    {
                        writer.Write(content);
                    }
                }
                // Save the zip archive to disk
                using (FileStream fileStream = new FileStream(destinationZipPath, FileMode.Create, FileAccess.Write))
                {
                    memoryStream.Seek(0, SeekOrigin.Begin);
                    memoryStream.CopyTo(fileStream);
                }
            }
            Console.WriteLine("Zip archive created successfully.");
        }
        private static void NOBlackBoxSave()
        {
            string timestamp = System.DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss").Replace(":", "-").Replace("/", "-");
            SaveTacViewFile(sb.ToString(), timestamp);
        }

        private static void FindBullets()
        {
            try
            {
                bullets = Resources.FindObjectsOfTypeAll<GameObject>().Where(obj => obj.name == "tracer(Clone)").ToList();
            }
            catch
            {
                bullets = null;
            }
            
        }

        private static void FindBulletsAndFlares()
        {
            stuff.Clear();
            stuff.AddRange(Resources.FindObjectsOfTypeAll<GameObject>().Where(obj => obj.name == "tracer(Clone)").ToList());
            stuff.AddRange(Resources.FindObjectsOfTypeAll<GameObject>().Where(obj => obj.name == "IRFlare(Clone)").ToList());

            foreach (GameObject obj in stuff)
            {
                int id = obj.GetInstanceID();

                if (!NonUnitRegistry.ContainsKey(id))
                {
                    string name = obj.name;
                    Vector3 pos = obj.transform.position;

                    NonUnitRecord rec = new NonUnitRecord(id,name,pos);
                } else
                {
                    Vector3 pos = obj.transform.position;
                    NonUnitRegistry[id].pos.Set(pos.x,pos.y,pos.z);
                }
            }
        }

        public string TacViewACMIUnit(Unit unit, bool firstReport)
        {
            string color = "Cyan";
            if (unit.NetworkHQ.faction.factionName == "Boscali") { color = "Blue"; }
            if (unit.NetworkHQ.faction.factionName == "Primeva") { color = "Red"; }
            /*
             * T = Longitude | Latitude | Altitude | Roll | Pitch | Yaw
             */
            string idhex = unit.persistentID.ToString("X");
            float[] latlon = NOBlackBoxHelper.ConvertUnityToLatLong(unit.GlobalPosition().x, unit.GlobalPosition().y, unit.GlobalPosition().z);
            Vector3 euler = unit.transform.rotation.eulerAngles;
            euler.z = Mathf.Abs(euler.z - 360);
            euler.x = Mathf.Abs(euler.x - 360);
            string output = null;
            if (firstReport)
            {
                string unitType = RecordTypes[unit.GetType().Name];
                output = unit.persistentID + ",T=" +
                    latlon[1].ToString(CultureInfo.InvariantCulture) + "|" +
                    latlon[0].ToString(CultureInfo.InvariantCulture) + "|" +
                    unit.GlobalPosition().y.ToString(CultureInfo.InvariantCulture) + "|" +
                    euler.z.ToString(CultureInfo.InvariantCulture) + "|" +
                    euler.x.ToString(CultureInfo.InvariantCulture) + "|" +
                    euler.y.ToString(CultureInfo.InvariantCulture) + "," +
                    "Name=" + unit.name + "," +
                    "Coalition=" + unit.NetworkHQ.faction.factionName + "," +
                    "Color=" + color + "," +
                    "Type=" + unitType;
                if (playerAircraftList.ContainsKey(unit.persistentID))
                {
                    output += ",CallSign=" + playerAircraftList[unit.persistentID];
                }
                output += "\n";
            }
            else
            {
                output = unit.persistentID + ",T=" +
                    latlon[1].ToString(CultureInfo.InvariantCulture) + "|" +
                    latlon[0].ToString(CultureInfo.InvariantCulture) + "|" +
                    unit.GlobalPosition().y.ToString(CultureInfo.InvariantCulture) + "|" +
                    euler.z.ToString(CultureInfo.InvariantCulture) + "|" +
                    euler.x.ToString(CultureInfo.InvariantCulture) + "|" +
                    euler.y.ToString(CultureInfo.InvariantCulture) + "\n";
            }
            return output;
        }
        public static void StartRecording()
        {
            Debug.Log("[NOBLACKBOX]: START RECORDING");
            recording = true;
        }
        public static void StopRecording()
        {
            Debug.Log("[NOBLACKBOX]: STOP RECORDING");
            recording = false;
            NOBlackBoxSave();
        }
        public static void Flush()
        {
            timer = 0.0f;
            defaultWaitTime = 0.2f;
            startTime = "none";
            missionName = "none";
            referenceTime = null;

            knownUnits = new HashSet<int>();
            unitIDs = new Mirage.Collections.SyncList<int>();
            purgeIDs = new List<int>();
            players = new List<Player>();
            playerAircraftList = new Dictionary<int, string>();

            recording = false;

            tick = 0;
            pollType = "ALL";

            dateStamp = System.DateTime.Now.ToString("MM/dd/yyyy").Replace(":", "-").Replace("/", "-");
            sb.Clear();
            sb = new StringBuilder("FileType=text/acmi/tacview\nFileVersion=2.2\n");
        }
    }
}