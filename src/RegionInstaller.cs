using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RegionInstaller
{
    [BepInPlugin(Id, Name, Version)]
    [BepInProcess("Among Us.exe")]
    public class RegionInstallerPlugin : BasePlugin
    {
        public const string Id = "com.nb1x.regioninstaller";
        
#if LITE
        public const string Name = "RegionInstallerLite";
#else
        public const string Name = "RegionInstaller";
#endif

        public const string Version = "1.0.0";

        public static string ConfigPath => Path.Combine(Paths.ConfigPath, "CustomRegions.cfg");
        public Harmony Harmony { get; } = new Harmony(Id);

        public override void Load()
        {
            Log.LogInfo($"[{Name}] Loading {Name} v{Version}...");

            ConfigData configData = ConfigFile.LoadOrCreate(ConfigPath);

            if (configData.GlitchedLobbiesRegion)
            {
                configData.KeepInnerslothRegions = true;
                GlitchedLobbies.AddGlitchedLobbiesRegion(configData.Regions);
            }

            if (configData.KeepInnerslothRegions)
            {
                InnerslothRegions.AddInnerslothRegions(configData.Regions);
            }

            string regionJsonPath = Path.Combine(Application.persistentDataPath, "regionInfo.json");
            RegionFileGenerator.RegenerateRegionFile(regionJsonPath, configData);

            Harmony.PatchAll();

            SceneManager.add_sceneLoaded((Action<Scene, LoadSceneMode>)((scene, _) =>
            {
                if (scene.name == "MainMenu")
                {
                    Log.LogInfo($"[{Name}] MainMenu loaded, injecting regions...");
                    InjectRegions(configData.Regions);
                }
            }));
        }

        private void InjectRegions(List<ParsedRegion> parsedRegions)
        {
            ServerManager serverMngr = DestroyableSingleton<ServerManager>.Instance;
            if (serverMngr == null) return;

            foreach (var reg in parsedRegions)
            {
                if (!reg.IsValid) continue;

                var serverInfo = new ServerInfo("http-1", reg.FullUrl, reg.SelectedPort, reg.Dtls);
                var serversArray = new Il2CppReferenceArray<ServerInfo>(new ServerInfo[] { serverInfo });

                var regionInfo = new StaticHttpRegionInfo(reg.Name, (StringNames)1003, reg.FullUrl, serversArray);

                serverMngr.AddOrUpdateRegion(regionInfo.Cast<IRegionInfo>());
                Log.LogInfo($"[{Name}] Region '{reg.Name}' injected into memory.");
            }
        }
    }

    [HarmonyPatch(typeof(ServerManager.JsonServerData), nameof(ServerManager.JsonServerData.CleanAndMerge))]
    public static class PatchCleanAndMerge
    {
        [HarmonyPrefix]
        public static bool Prefix() => false;
    }
}