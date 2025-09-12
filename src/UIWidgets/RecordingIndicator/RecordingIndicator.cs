using UnityEngine;

namespace NOBlackBox
{
    internal class RecordingIndicator : MonoBehaviour
    {

        void Awake()
        {
        }

        void Update()
        {

        }

        void OnGui()
        {

            GUIStyle fontSize = new GUIStyle(GUI.skin.GetStyle("label"));
            fontSize.fontSize = 24;
            fontSize.normal.textColor = new Color(Configuration.TextColorR.Value,
                                                    Configuration.TextColorG.Value,
                                                    Configuration.TextColorB.Value,
                                                    Configuration.TextColorA.Value);
            GUI.Label(new Rect((0.2f * Plugin.recordedScreenWidth),(0.2f * Plugin.recordedScreenHeight),400,50),"REC", fontSize);
        }
    }
}
