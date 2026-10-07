using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace RegionInstaller
{
    public static class RegionApi
    {
#if LITE
        public static void AddRegion(string name, string address, bool https, bool dtls, ushort port1, ushort port2)
        {
            throw new NotSupportedException("RegionInstallerLite does not include the API, please use default RegionInstaller.");
        }

        public static void DisableInnerslothRegions()
        {
            throw new NotSupportedException("RegionInstallerLite does not include the API, please use default RegionInstaller.");
        }

        public static void DisableDtlsForAllRegions()
        {
            throw new NotSupportedException("RegionInstallerLite does not include the API, please use default RegionInstaller.");
        }

        public static void ReloadRegionInfo()
        {
            throw new NotSupportedException("RegionInstallerLite does not include the API, please use default RegionInstaller.");
        }
#else
        private static readonly List<ParsedRegion> DynamicRegions = new();
        private static bool forceDisableInnersloth = false;
        private static bool forceDisableDtls = false;

        public static void AddRegion(string name, string address, bool https, bool dtls, ushort port1, ushort port2)
        {
            var region = new ParsedRegion
            {
                Name = name,
                Address = address,
                Https = https,
                Dtls = dtls,
                Port1 = port1,
                Port2 = port2
            };

            if (region.IsValid)
            {
                DynamicRegions.Add(region);
                TriggerRefresh();
                Debug.Log($"[RegionInstaller.Api] Dynamic region '{name}' added successfully.");
            }
            else
            {
                Debug.LogError($"[RegionInstaller.Api] Failed to add invalid region '{name}'.");
            }
        }

        public static void DisableInnerslothRegions()
        {
            if (forceDisableInnersloth)
            {
                Debug.LogWarning("[RegionInstaller.Api] Innersloth regions are already disabled by another plugin/API call!");
                return;
            }

            forceDisableInnersloth = true;
            TriggerRefresh();
            Debug.Log("[RegionInstaller.Api] Innersloth regions forcefully disabled via API.");
        }

        public static void DisableDtlsForAllRegions()
        {
            if (forceDisableDtls)
            {
                Debug.LogWarning("[RegionInstaller.Api] Dtls is already disabled by another plugin/API call!");
                return;
            }

            forceDisableDtls = true;
            TriggerRefresh();
            Debug.Log("[RegionInstaller.Api] DTLS forcefully disabled for all regions via API.");
        }

        public static void ReloadRegionInfo()
        {
            TriggerRefresh();
            Debug.Log("[RegionInstaller.Api] Region info manually reloaded via API.");
        }

        internal static List<ParsedRegion> GetDynamicRegions() => DynamicRegions;

        internal static bool IsInnerslothForcedDisabled() => forceDisableInnersloth;

        private static void TriggerRefresh()
        {
            try
            {
                ConfigData configData = ConfigFile.LoadOrCreate(RegionInstallerPlugin.ConfigPath);

                if (forceDisableDtls)
                {
                    foreach (var reg in configData.Regions)
                    {
                        reg.Dtls = false;
                    }
                    foreach (var reg in DynamicRegions)
                    {
                        reg.Dtls = false;
                    }
                }

                configData.Regions.AddRange(DynamicRegions);

                string regionJsonPath = Path.Combine(Application.persistentDataPath, "regionInfo.json");
                RegionFileGenerator.RegenerateRegionFile(regionJsonPath, configData, forceDisableInnersloth);

                if (ServerManager.Instance != null)
                {
                    ServerManager.Instance.LoadServers();
                }

                UnityEngine.Object.FindObjectOfType<RegionTextMonitor>()?.Start();
                if (UnityEngine.Object.FindObjectOfType<RegionMenu>() is { } regionMenu)
                {
                    regionMenu.OnDisable();
                    regionMenu.OnEnable();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RegionInstaller.Api] Error during dynamic refresh: {ex.Message}");
            }
        }
#endif
    }
}