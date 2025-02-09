//https://github.com/TYKUHN2/NuclearVOIP/blob/main/LoadingManager.cs
using HarmonyLib;
using Mirage;
using NuclearOption.Networking;
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace NOBlackBox
{
    public class LoadingManager
    {
        private static readonly Harmony harmony = new("xyz.KopterBuzz.NOBlackBox");

        public static event Action? GameLoaded;
        public static event Action? NetworkReady;

        public static event Action? MissionLoaded;
        public static event Action? MissionUnloaded;

        static LoadingManager()
        {
            Type thisType = typeof(LoadingManager);

            Type netManager = typeof(NetworkManagerNuclearOption);
            harmony.Patch(
                netManager.GetMethod("Awake"),
                null,
                HookMethod(NetworkManagerPostfix)
            );

            Type mainMenu = typeof(MainMenu);
            harmony.Patch(
                mainMenu.GetMethod("Start", BindingFlags.NonPublic | BindingFlags.Instance),
                null,
                HookMethod(MainMenuPostfix)
            );
        }

        private static HarmonyMethod HookMethod(Delegate hook)
        {
            return new HarmonyMethod(hook.GetMethodInfo());
        }

        private static void MainMenuPostfix()
        {
            Debug.Log("[NOBLACKBOX]: Reached GameLoaded");
            GameLoaded?.Invoke();

            MethodBase original = harmony.GetPatchedMethods().Where(a => a.DeclaringType == typeof(MainMenu)).First();
            harmony.Unpatch(original, HookMethod(MainMenuPostfix).method);
        }

        private static void NetworkManagerPostfix()
        {
            NetworkManagerNuclearOption.i.Client.Connected.AddListener(ClientConnectCallback);
            NetworkManagerNuclearOption.i.Client.Disconnected.AddListener(ClientDisconectCallback);

            Debug.Log("[NOBLACKBOX]: Reached NetworkReady");
            NetworkReady?.Invoke();
        }

        private static void MissionLoadCallback()
        {
            Debug.Log("[NOBLACKBOX]: Reached MissionLoaded");
            MissionLoaded?.Invoke();
        }

        private static void OnIdentity(NetworkIdentity identity)
        {
            identity.OnStartLocalPlayer.AddListener(MissionLoadCallback);
        }

        private static void ClientConnectCallback(INetworkPlayer player)
        {
            player.OnIdentityChanged += OnIdentity;
        }

        private static void ClientDisconectCallback(ClientStoppedReason reason)
        {
            Debug.Log("[NOBLACKBOX]: Reached MissionUnloaded");
            MissionUnloaded?.Invoke();
        }
    }
}
