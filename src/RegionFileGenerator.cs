using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using UnityEngine;

namespace RegionInstaller
{
    public static class RegionFileGenerator
    {
        public static void RegenerateRegionFile(string regionFilePath, ConfigData configData)
        {
            try
            {
                bool needsRewrite = true;
                JsonNode? rootNode = null;

                if (File.Exists(regionFilePath) && new FileInfo(regionFilePath).Length > 0)
                {
                    string content = File.ReadAllText(regionFilePath);
                    rootNode = JsonNode.Parse(content);
                    if (rootNode?["Regions"] is JsonArray existingRegions)
                    {
                        if (AreRegionsMatching(existingRegions, configData.Regions, configData.KeepInnerslothRegions))
                        {
                            needsRewrite = false;
                        }
                    }
                }

                if (needsRewrite)
                {
                    if (File.Exists(regionFilePath))
                    {
                        File.Delete(regionFilePath);
                    }

                    Debug.Log("[RegionInstaller] Rewriting regionInfo.json...");
                    rootNode = CreateEmptyRegionStructure();
                    var regionsArray = rootNode["Regions"]!.AsArray();

                    foreach (var reg in configData.Regions)
                    {
                        if (!reg.IsValid) continue;
                        regionsArray.Add(BuildRegionNode(reg.Name, reg.FullUrl, reg.SelectedPort, reg.Dtls));
                    }

                    var options = new JsonSerializerOptions { WriteIndented = true };
                    File.WriteAllText(regionFilePath, rootNode.ToJsonString(options));
                    Debug.Log("Region file reloaded!");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RegionInstaller] Error processing regionInfo.json: {ex.Message}");
            }
        }

        private static bool AreRegionsMatching(JsonArray existingRegions, List<ParsedRegion> parsedRegions, bool keepInnersloth)
        {
            var validCfgRegions = parsedRegions.FindAll(r => r.IsValid);

            int totalExpected = validCfgRegions.Count;
            int totalExisting = 0;

            foreach (var node in existingRegions)
            {
                if (node == null) continue;
                bool isOfficial = InnerslothRegions.IsOfficialInnersloth(node.ToJsonString());
                if (!isOfficial || keepInnersloth)
                {
                    totalExisting++;
                }
            }

            if (totalExisting != totalExpected) return false;

            foreach (var reg in validCfgRegions)
            {
                bool found = false;
                foreach (var node in existingRegions)
                {
                    if (node != null && node.ToJsonString().Contains(reg.FullUrl, StringComparison.OrdinalIgnoreCase))
                    {
                        found = true;
                        break;
                    }
                }
                if (!found) return false;
            }

            return true;
        }

        private static JsonNode CreateEmptyRegionStructure() => new JsonObject { ["CurrentRegionIdx"] = 0, ["Regions"] = new JsonArray() };

        private static JsonNode BuildRegionNode(string name, string host, ushort port, bool dtls)
        {
            return new JsonObject
            {
                ["$type"] = "StaticHttpRegionInfo, Assembly-CSharp",
                ["Name"] = name,
                ["PingServer"] = host,
                ["Servers"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["Name"] = "Http-1",
                        ["Ip"] = host,
                        ["Port"] = port,
                        ["UseDtls"] = dtls,
                        ["Players"] = 0,
                        ["ConnectionFailures"] = 0
                    }
                },
                ["TargetServer"] = null,
                ["TranslateName"] = 1003
            };
        }
    }
}