using NuclearOption.Networking;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO.Compression;
using System.IO;
using System.Text;
using UnityEngine;
using System.Linq;

namespace NOBlackBox
{
    internal class NOBlackBoxRecorder : MonoBehaviour
    {
        internal Dictionary<string, string> UnitTypes = new Dictionary<string, string>() { 
            ["Aircraft"]= "Air+Fixedwing",
            ["Building"]= "Ground+Static+Building",
            ["Container"]= "Misc+Container",
            ["CruiseMissile"] = "Weapon+Missile",
            ["Factory"] = "Ground+Static+Building",
            ["GroundVehicle"] = "Ground+Vehicle",
            ["GuidedBomb"] = "Weapon+Bomb",
            ["Missile"] = "Weapon+Missile",
            ["PilotDismounted"] = "Ground+Light+Human",
            ["Ship"] = "Sea+Watercraft+Warship",
            ["VehicleDepot"] = "Ground+Static+Building",
            ["GuidedShell"] = "Weapon+Missile"
        };
        private static float updateCount = 0;
        private static float fixedUpdateCount = 0;
        private static float updateUpdateCountPerSecond;
        private static float updateFixedUpdateCountPerSecond;
        private float timer = 0.0f;
        private const float defaultWaitTime = 0.25f;
        private static StringBuilder sb = new StringBuilder("FileType=text/acmi/tacview\nFileVersion=2.2\n");
        private static string dateStamp = System.DateTime.Now.ToString("MM/dd/yyyy").Replace(":", "-").Replace("/", "-");

        private HashSet<int> knownUnits = new HashSet<int>();
        private Mirage.Collections.SyncList<int> unitIDs = new Mirage.Collections.SyncList<int>();
        private List<int> purgeIDs = new List<int>();

        private static string startTime = "none";
        private static string missionName = "none";

        private bool saving = false;
        

        void Awake()
        {
            DontDestroyOnLoad(this.gameObject);
            StartCoroutine(DebugFrameCounter());
        }

        // Increase the number of calls to Update.
        void Update()
        {
            updateCount += 1;
            timer += Time.deltaTime;
            if (!NetworkManagerNuclearOption.i.Server.Active && !GameManager.LocalPlayer && missionName != "none")
            {
                NOBlackBoxSave();
                return;
            }
            if (NetworkManagerNuclearOption.i.Server.Active && UnitRegistry.allUnits.Count > 0)
            {
                
                if (timer >= defaultWaitTime)
                {
                    NOBlackBoxWrite(true);
                    timer = 0f;
                    return;
                }
            }
            if (!NetworkManagerNuclearOption.i.Server.Active && UnitRegistry.allUnits.Count > 0)
            {

                if (timer >= defaultWaitTime)
                {
                    NOBlackBoxWrite(false);
                    timer = 0f;
                    return;
                }
            }
        }

        // Increase the number of calls to FixedUpdate.
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
            GUI.Label(new Rect(100, 100, 200, 50), "Update: " + updateUpdateCountPerSecond.ToString(), fontSize);
            GUI.Label(new Rect(100, 150, 200, 50), "FixedUpdate: " + updateFixedUpdateCountPerSecond.ToString(), fontSize);
            if (saving)
            {
                GUI.Label(new Rect(100, 500, 200, 50), "SAVING...", fontSize);
            }

        }

        // Update both CountsPerSecond values every second.
        IEnumerator DebugFrameCounter()
        {
            while (true)
            {
                yield return new WaitForSeconds(1);
                updateUpdateCountPerSecond = updateCount;
                updateFixedUpdateCountPerSecond = fixedUpdateCount;
                updateCount = 0;
                fixedUpdateCount = 0;
            }
        }

        IEnumerator SaveTacViewFile(string acmi, string timestamp)
        {
            saving = true;
            string myDocs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            Directory.CreateDirectory(myDocs + "\\NOTacView");
            string ACMIFilePath = myDocs + "\\NOTacView\\" + timestamp + "-" + missionName + "TacView" + ".zip";
            CreateZipWithText(acmi, "missionData.acmi", ACMIFilePath);
            saving = false;
            Flush();
            yield break;
        }

        private void NOBlackBoxWrite(bool server)
        {
            if (
                    GameManager.gameState == GameManager.GameState.Editor ||
                    GameManager.gameState == GameManager.GameState.Encyclopedia ||
                    MissionManager.Runner == null ||
                    GameplayUI.GameIsPaused
                )
            {
                return;
            }
            if (!server && (DynamicMap.i == null || DynamicMap.i.HQ == null))
            {
                return;
            }
            if (missionName == "none")
            {
                List<Faction> factionList = FactionRegistry.factions;
                foreach (Faction faction in factionList) {
                    Debug.Log("DEBUG FACTION LIST: " + faction.name);
                }
                missionName = MissionManager.CurrentMission.Name;
                startTime = NOBlackBoxHelper.TimeOfDay(LevelInfo.i.timeOfDay);
                string startTimeString = "0,ReferenceTime=" + dateStamp + "T" + startTime + "Z\n";
                sb.Append(startTimeString);               
            }

            unitIDs.Clear();
            if (server)
            {
                unitIDs.AddRange(FactionRegistry.HqFromName("Primeva").factionUnits);
                unitIDs.AddRange(FactionRegistry.HqFromName("Boscali").factionUnits);
            }
            else
            {
                unitIDs.AddRange(GameManager.LocalFactionHQ.factionUnits);
                if (DynamicMap.i.HQ.trackingDatabase.Any()) {
                    foreach (KeyValuePair<int,TrackingInfo> info in DynamicMap.i.HQ.trackingDatabase)
                    {
                        try
                        {
                            unitIDs.Add(info.Value.id);
                        }
                        catch
                        {
                            Debug.Log("Failed to process unit from DynamicMap.i.HQ.trackingDatabase");
                        }
                    }
                }

            }

            sb.Append("#" + MissionManager.i.NetworkmissionTime.ToString()+"\n");

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
                            sb.Append(TacViewACMI(unit, true));
                        }
                        if (knownUnits.Contains(unitId) && unit.speed != 0)
                        {
                            sb.Append(TacViewACMI(unit, false));
                        }
                    }
                    return;
                } catch
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

        private void NOBlackBoxSave()
        {
            string timestamp = System.DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss").Replace(":", "-").Replace("/", "-");
            //string myDocs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            //Directory.CreateDirectory(myDocs + "\\NOTacView");
            //string ACMIFilePath = myDocs + "\\NOTacView\\" + timestamp + "-" + missionName + "TacView" + ".zip";
            //CreateZipWithText(sb.ToString(), "missionData.acmi", ACMIFilePath);
            StartCoroutine(SaveTacViewFile(sb.ToString(), timestamp));
        }

        public string TacViewACMI(Unit unit,bool firstReport)
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
                string unitType = UnitTypes[unit.GetType().Name];
                output = unit.persistentID + ",T=" +
                    latlon[1].ToString() + "|" +
                    latlon[0].ToString() + "|" +
                    unit.GlobalPosition().y.ToString() + "|" +
                    euler.z.ToString() + "|" +
                    euler.x.ToString() + "|" +
                    euler.y.ToString() + "," +
                    "Name=" + unit.name + "," +
                    "Coalition=" + unit.NetworkHQ.faction.factionName + "," +
                    "Color=" + color + "," +
                    "Type=" + unitType + "\n";
            }
            else
            {
                output = unit.persistentID + ",T=" +
                    latlon[1].ToString() + "|" +
                    latlon[0].ToString() + "|" +
                    unit.GlobalPosition().y.ToString() + "|" +
                    euler.z.ToString() + "|" +
                    euler.x.ToString() + "|" +
                    euler.y.ToString() + "\n";
            }

            return output;
        }
        public static void Flush() {
            updateCount = 0;
            fixedUpdateCount = 0;
            updateFixedUpdateCountPerSecond = 0;
            updateUpdateCountPerSecond = 0;
            startTime = "none";
            missionName = "none";
            dateStamp = System.DateTime.Now.ToString("MM/dd/yyyy").Replace(":", "-").Replace("/", "-");
            StringBuilder sb = new StringBuilder("FileType=text/acmi/tacview\nFileVersion=2.2\n");
        }
    }
}
