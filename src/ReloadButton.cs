using System;
using System.IO;
using HarmonyLib;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RegionInstaller
{
    [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start))]
    public static class ReloadButtonPatch
    {
        private static PassiveButton refreshButton;

        public static void Postfix(MainMenuManager __instance)
        {
            if (__instance == null) return;

            if (refreshButton == null)
            {
                refreshButton = CreateOrangeRefreshButton(__instance);
            }
        }

        private static PassiveButton CreateOrangeRefreshButton(MainMenuManager menu)
        {
            var parent = menu.transform.Find("MainUI/AspectScaler/LeftPanel/Main Buttons");
            if (parent == null) return null;

            var button = Object.Instantiate(menu.quitButton, parent);
            button.name = "RefreshRegionsButton";

            button.transform.localPosition = new Vector3(2f, 4.05f, 1f);
            button.transform.localScale = new Vector3(0.8f, 1f, 1f);

            var aspect = button.GetComponent<AspectPosition>();
            if (aspect != null)
            {
                aspect.enabled = false;
            }

            button.OnClick = new();
            button.OnClick.AddListener((Action)(() =>
            {
                ConfigData configData = ConfigFile.LoadOrCreate(RegionInstallerPlugin.ConfigPath);

#if LITE
                bool shouldDisableInnersloth = false;
#else
                bool shouldDisableInnersloth = RegionApi.IsInnerslothForcedDisabled();
#endif

                if (!shouldDisableInnersloth)
                {
                    if (configData.GlitchedLobbiesRegion)
                    {
                        configData.KeepInnerslothRegions = true;
                        GlitchedLobbies.AddGlitchedLobbiesRegion(configData.Regions);
                    }

                    if (configData.KeepInnerslothRegions)
                    {
                        InnerslothRegions.AddInnerslothRegions(configData.Regions);
                    }
                }

                string regionJsonPath = Path.Combine(Application.persistentDataPath, "regionInfo.json");
                RegionFileGenerator.RegenerateRegionFile(regionJsonPath, configData, shouldDisableInnersloth);

                if (ServerManager.Instance != null)
                {
                    ServerManager.Instance.LoadServers();
                }

                Object.FindObjectOfType<RegionTextMonitor>()?.Start();
                if (Object.FindObjectOfType<RegionMenu>() is { } regionMenu)
                {
                    regionMenu.OnDisable();
                    regionMenu.OnEnable();
                }
            }));

            var buttonText = button.transform.Find("FontPlacer/Text_TMP").GetComponent<TMP_Text>();
            
            if (buttonText.TryGetComponent<TextTranslatorTMP>(out var translator))
            {
                Object.Destroy(translator);
            }

            buttonText.text = "Refresh";
            buttonText.fontSize = buttonText.fontSizeMax = buttonText.fontSizeMin = 3.5f;
            buttonText.enableWordWrapping = false;
            buttonText.horizontalAlignment = HorizontalAlignmentOptions.Center;

            var normalSprite = button.inactiveSprites.GetComponent<SpriteRenderer>();
            var hoverSprite = button.activeSprites.GetComponent<SpriteRenderer>();
            normalSprite.color = new Color32(255, 140, 0, byte.MaxValue);
            hoverSprite.color = new Color32(255, 165, 50, byte.MaxValue);

            button.gameObject.SetActive(true);
            return button;
        }
    }
}