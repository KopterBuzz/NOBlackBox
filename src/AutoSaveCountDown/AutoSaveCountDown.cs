using UnityEngine;


namespace NOBlackBox
{
    internal class AutoSaveCountDown : MonoBehaviour
    {
        public float countDown;
        internal float timer;
        void Awake()
        {
            countDown = (float)Configuration.AutoSaveInterval;
            timer = 0f;
        }
        void Update()
        {
            timer += Time.deltaTime;
            if ( timer >= 1.0 )
            {
                countDown -= timer;
                timer = 0f;
            }
            
            if (ACMIWriter.lastUpdate.TotalSeconds > Configuration.AutoSaveInterval && (ACMIWriter.lastUpdate.TotalSeconds % Configuration.AutoSaveInterval < 1))
            {
                countDown = (float)Configuration.AutoSaveInterval;
            }
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
                                   "Next AutoSave: " + countDown, fontSize);
            }
        }
    }
}
