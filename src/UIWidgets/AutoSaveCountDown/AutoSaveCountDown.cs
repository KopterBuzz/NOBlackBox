using System;
using UnityEngine;


namespace NOBlackBox
{
    internal class AutoSaveCountDown : MonoBehaviour
    {
        public double countDown;
        internal float timer;
        private double secondsSinceLastUpdate;
        void Awake()
        {
        }
        void Update()
        {
            secondsSinceLastUpdate = (DateTime.Now - ACMIWriter.lastFlushTime).TotalSeconds;
            countDown = Configuration.AutoSaveInterval - secondsSinceLastUpdate;
        }
        void OnGUI()
        {
            GUIStyle fontSize = new GUIStyle(GUI.skin.GetStyle("label"));
            fontSize.fontSize = 24;
            fontSize.normal.textColor = new Color(  Configuration.TextColorR.Value,
                                                    Configuration.TextColorG.Value,
                                                    Configuration.TextColorB.Value,
                                                    Configuration.TextColorA.Value);

            if (Plugin.isRecording && Configuration.EnableAutoSaveCountDown.Value)
            {
                GUI.Label(new Rect((Configuration.AutoSaveCountDownX.Value * Plugin.recordedScreenWidth),
                                   (Configuration.AutoSaveCountDownY.Value * Plugin.recordedScreenHeight),
                                   400,
                                   50),
                                   $"Next AutoSave: {countDown:N1} sec", fontSize);
            }
            GUI.Label(new Rect((0.2f * Plugin.recordedScreenWidth), (0.2f * Plugin.recordedScreenHeight), 400, 50), "REC", fontSize);
        }
    }
}
