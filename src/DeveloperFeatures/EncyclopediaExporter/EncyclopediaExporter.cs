using Mirage.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml.Linq;
using UnityEngine.XR;

namespace NOBlackBox
{

    internal struct UnitTacviewInfo
    {
        public string prefabName;
        public string name;
        public string unitName;
        public string code;
        public string tacviewACMIType;
        public string tacviewXMLBase;
        public string tacviewXMLShape;

        public UnitTacviewInfo(string PrefabName,string Name,string UnitName, string Code, string TacviewACMIType, string TacviewXMLBase)
        {
            prefabName = PrefabName;
            name = Name;
            unitName = UnitName;
            code = Code;
            tacviewACMIType = TacviewACMIType;
            tacviewXMLBase = TacviewXMLBase;
            tacviewXMLShape = $"{PrefabName}.obj";
        }

        public override string ToString()
        {
            return $"{prefabName},{name},{unitName},{code},{tacviewACMIType},{tacviewXMLBase},{tacviewXMLShape}";
        }
    }
    internal static class EncyclopediaExporter
    {
        private static string outputDir = Path.Combine(BepInEx.Paths.PluginPath, "NOBlackBox\\Developer\\NOBlackBox_EncyclopediaExports");

        private static string KnownUnitsCSV = Path.Combine(outputDir, "KnownUnits.csv");
        private static string UnknownUnitsCSV = Path.Combine(outputDir, "UnknownUnits.csv");

        private static string aircraftFileCSV = Path.Combine(outputDir, "Aircraft.csv");
        private static string groundFileCSV = Path.Combine(outputDir, "Ground.csv");
        private static string shipFileCSV = Path.Combine(outputDir, "Ship.csv");
        private static string buildingFileCSV = Path.Combine(outputDir, "Building.csv");
        private static string missileFileCSV = Path.Combine(outputDir, "Missile.csv");
        private static string otherFileCSV = Path.Combine(outputDir, "Other.csv");

        private static string aircraftFileXML = Path.Combine(outputDir, "Aircraft.xml");
        private static string groundFileXML = Path.Combine(outputDir, "Ground.xml");
        private static string shipFileXML = Path.Combine(outputDir, "Ship.xml");
        private static string buildingFileXML = Path.Combine(outputDir, "Building.xml");
        private static string missileFileXML = Path.Combine(outputDir, "Missile.xml");
        private static string otherFileXML = Path.Combine(outputDir, "Other.xml");

        private static string KnownUnitsXML = Path.Combine(outputDir, "NuclearOption.xml");


        private static string unitCSVHeader = "prefabName,name,unitName,code,TacviewACMIType,TacviewXMLBase,TacviewXMLShape";
        private static Dictionary<string, UnitTacviewInfo> knownUnits = new Dictionary<string, UnitTacviewInfo>();
        private static Dictionary<string, UnitTacviewInfo> unknownUnits = new Dictionary<string, UnitTacviewInfo>();


        private static StreamWriter? output;

        private static void fetchKnownUnitsCSV()
        {
            knownUnits.Clear();
            string[] lines = File.ReadAllLines(KnownUnitsCSV);
            foreach (string line in lines)
            {
                string[] splits = line.Split(',');
                if (splits[0] == "name") { continue; }
                UnitTacviewInfo knownUnit = new UnitTacviewInfo(splits[0], splits[1], splits[2], splits[3], splits[4], splits[5]);
                knownUnits.Add(splits[0], knownUnit);
                Plugin.Logger?.LogInfo(knownUnit.ToString());
            }
        }

        private static void CheckUnitInfo(UnitDefinition def)
        {
            if (!knownUnits.ContainsKey(def.unitPrefab.name))
            {
                UnitTacviewInfo unknownUnit = new UnitTacviewInfo(def.unitPrefab.name,def.name, def.unitName, def.code, "", "");
                unknownUnits.Add(def.unitPrefab.name, unknownUnit);
                Plugin.Logger?.LogInfo($"Found UNKNOWN Unit: {unknownUnit.ToString()}");
            } else
            {
                Plugin.Logger?.LogInfo($"Found known Unit: {knownUnits[def.unitPrefab.name].ToString()}");
            }
            
        }

        private static void WriteUnitListCSV(Dictionary<string, UnitTacviewInfo> unitList, string outPath)
        {
            output = File.CreateText(outPath);
            output.WriteLine(unitCSVHeader);

            foreach (UnitTacviewInfo info in unitList.Values)
            {
                output.WriteLine(info.ToString());
            }
            output.Flush();
            output.Close();
        }

        private static void WriteUnitListXML()
        {
            XDocument doc = new XDocument();
            XElement DefaultPropertiesCollection = new XElement("DefaultPropertiesCollection",
                                                        new XAttribute("LoadingOrder", "1.0"));
            foreach (UnitTacviewInfo info in knownUnits.Values)
            {
                XElement DefaultProperties = new XElement("DefaultProperties",
                    new XAttribute("Id", info.prefabName),
                    new XAttribute("Base", info.tacviewXMLBase),
                        new XElement("Criteria",
                            new XElement("Name", info.prefabName),
                            new XElement("Name", info.name),
                            new XElement("Name", info.unitName)
                        ),
                        new XElement("Properties",
                            new XElement("ShortName",info.code),
                            new XElement("LongName",info.unitName),
                            new XElement("FullName",info.unitName),
                            new XElement("Shape",info.tacviewXMLShape)
                        )
                    );
                DefaultPropertiesCollection.Add(DefaultProperties);
            }
            doc.Add(DefaultPropertiesCollection);
            Plugin.Logger?.LogInfo($"Saving custom tacview custom XML to {KnownUnitsXML}");
            doc.Save(KnownUnitsXML);
        }
        public static void ExportEncyclopediaCSV()
        {
            fetchKnownUnitsCSV();
            foreach (UnitDefinition def in Encyclopedia.i.aircraft)
            {
                CheckUnitInfo(def);
            }
            foreach (UnitDefinition def in Encyclopedia.i.vehicles)
            {
                CheckUnitInfo(def);
            }
            foreach (UnitDefinition def in Encyclopedia.i.ships)
            {
                CheckUnitInfo(def);
            }
            foreach (UnitDefinition def in Encyclopedia.i.buildings)
            {
                CheckUnitInfo(def);
            }
            foreach (UnitDefinition def in Encyclopedia.i.missiles)
            {
                CheckUnitInfo(def);
            }
            foreach (UnitDefinition def in Encyclopedia.i.otherUnits)
            {
                CheckUnitInfo(def);
            }

            WriteUnitListCSV(unknownUnits,UnknownUnitsCSV);
            WriteUnitListCSV(knownUnits, KnownUnitsCSV);
            WriteUnitListXML();
        }
    }
}
