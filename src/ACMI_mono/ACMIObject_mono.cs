using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace NOBlackBox
{
    internal class ACMIObject_mono : MonoBehaviour
    {
        public long unitId;
        public long tacviewId;
        internal float timer = 0f;
        internal float fx, fy, fz, fax, fay, faz;
        internal bool disabled = false;

        public event Action<string, long[], string>? OnEvent;

        public Dictionary<string, string> props = [];

        public virtual void Init()
        {

        }
        void Awake()
        {

        }
        void FixedUpdate()
        {

        }
        void Reset()
        {

        }
        void OnDisable()
        {

        }
    }
}
