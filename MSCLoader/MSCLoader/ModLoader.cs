﻿using Ionic.Zip;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace MSCLoader
{

    /// <summary>
    /// List of possible scenes
    /// </summary>
    public enum CurrentScene
    {
        /// <summary>
        /// Main Menu
        /// </summary>
        MainMenu,
        /// <summary>
        /// Game Scene
        /// </summary>
        Game,
        /// <summary>
        /// Intro for new game
        /// </summary>
        NewGameIntro,
    }

    /// <summary>
    /// This is main Mod Loader class.
    /// </summary>
    public class ModLoader : MonoBehaviour
    {
        /// <summary>
        /// When true, it prints all errors from Update() and OnGUI() class.
        /// </summary>
        internal static bool LogAllErrors = false;

        /// <summary>
        /// A list of all loaded mods.
        /// </summary>
        public static List<Mod> LoadedMods { get; internal set; }

        /// <summary>
        /// A list of invalid mod files 
        /// (like random dll in Mods Folder that isn't a mod).
        /// </summary>
        internal static List<string> InvalidMods;

        /// <summary>
        /// The instance of ModLoader.
        /// </summary>
        internal static ModLoader Instance;

        /// <summary>
        /// The current version of the ModLoader.
        /// </summary>
        public static readonly string MSCLoader_Ver = "1.1.13";

        /// <summary>
        /// Is this version of ModLoader experimental (this is NOT game experimental branch)
        /// </summary>
#if Debug
        public static readonly bool experimental = true;
#else
        public static readonly bool experimental = false;
#endif

        /// <summary>
        /// Is DevMode active
        /// </summary>
#if DevMode
        public static readonly bool devMode = true;
#else
        public static readonly bool devMode = false;
#endif
        internal static bool unloader = false;
        internal static bool rtmm = false;
        internal static List<string> saveErrors;

        private Mod[] SecondPassMods = new Mod[0];
        private Mod[] OnGUImods = new Mod[0];
        private Mod[] UpdateMods = new Mod[0];
        private Mod[] FixedUpdateMods = new Mod[0];
        private Mod[] OnSaveMods = new Mod[0];

        private string expBuild = Assembly.GetExecutingAssembly().GetName().Version.Revision.ToString();
        private MSCUnloader mscUnloader;

        private static string steamID;
        private static bool loaderPrepared = false;
        private static string ModsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), @"MySummerCar\Mods");
        private static string ConfigFolder = Path.Combine(ModsFolder, "Config");
        private static string SettingsFolder = Path.Combine(ConfigFolder, "Mod Settings");
        internal static string ManifestsFolder = Path.Combine(ConfigFolder, "Mod Metadata");
        private static string AssetsFolder = Path.Combine(ModsFolder, "Assets");
        private string[] upd_tmp;
        private List<string> mod_aulist;
        private GameObject mainMenuInfo;
        private GameObject loading;
        private GameObject loadingMeta;
        private Animation menuInfoAnim;
        private GUISkin guiskin;

        private readonly string serverURL = "http://my-summer-car.ml";
        private readonly string metadataURL = "http://my-summer-car.ml:4000";
        //private readonly string serverURL = "http://localhost/msc2"; //localhost for testing only
        //private readonly string metadataURL = "http://localhost:4000";

        private bool IsDoneLoading = false;
        private bool IsModsLoading = false;
        private bool fullyLoaded = false;
        private bool allModsLoaded = false;
        private bool IsModsResetting = false;
        private bool IsModsDoneResetting = false;
        private static CurrentScene CurrentGameScene;

        void Awake()
        {
            StopAskingForMscoSupport();
            //TG should fix this, but whatever
            if (GameObject.Find("Music") != null)
                GameObject.Find("Music").GetComponent<AudioSource>().Stop();
        }
        /// <summary>
        /// Check if steam is present
        /// </summary>
        /// <returns>Valid steam detected.</returns>
        public static bool CheckSteam()
        {
            if (!string.IsNullOrEmpty(steamID) && steamID != "0")
                return true;
            else
                return false;
        }

        /// <summary>
        /// Check if steam release is from experimental branch
        /// </summary>
        /// <returns>Experimental detected.</returns>
        public static bool CheckIfExperimental()
        {
            if (!CheckSteam())
            {
                System.Console.WriteLine("Cannot check if it is experimental branch or not, because not valid steam installation detected");
                return false;
            }
            bool ret = Steamworks.SteamApps.GetCurrentBetaName(out string Name, 128);
            if (ret)
            {
                if (Name != "default_32bit") //32bit is NOT experimental branch
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Get Current Game Scene
        /// </summary>
        /// <returns>CurrentScene enum</returns>
        public static CurrentScene GetCurrentScene()
        {
            return CurrentGameScene;
        }

        /// <summary>
        /// Check if other ModID is present and enabled
        /// </summary>
        /// <param name="ModID">Mod ID of other mod to check (Case sensitive)</param>
        /// <returns>true if mod ID is present</returns>
        public static bool IsModPresent(string ModID)
        {
            Mod m = LoadedMods.Where(x => x.ID.Equals(ModID)).FirstOrDefault();
            if(m != null)
            {
                if (m.isDisabled)
                    return false;
                else
                    return true;
            }
            return false;
        }

        internal static string GetModSettingsFolder(Mod mod)
        {
            return Path.Combine(SettingsFolder, mod.ID);
        }
        /// <summary>
        /// Mod config folder, use this if you want save something. 
        /// </summary>
        /// <returns>Path to your mod config folder</returns>
        /// <param name="mod">Your mod Class.</param>
        /// <example>Example Code in Mod subclass.
        /// <code source="Examples.cs" region="GetModConfigFolder" lang="C#" />
        /// Example from other than Mod subclass.
        /// <code source="Examples.cs" region="GetModConfigFolder2" lang="C#" />
        /// </example>
        public static string GetModConfigFolder(Mod mod)
        {
            return Path.Combine(SettingsFolder, mod.ID);
        }

        /// <summary>
        /// Mod assets folder, use this if you want load custom content. 
        /// </summary>
        /// <returns>Path to your mod assets folder</returns>
        /// <param name="mod">Your mod Class.</param>
        /// <example>Example Code in Mod subclass.
        /// <code source="Examples.cs" region="GetModAssetsFolder" lang="C#" />
        /// Example from other than Mod subclass.
        /// <code source="Examples.cs" region="GetModAssetsFolder2" lang="C#" />
        /// </example> 
        public static string GetModAssetsFolder(Mod mod)
        {
            if (!mod.UseAssetsFolder)
                ModConsole.Error(string.Format("<b>{0}:</b> Please set variable <b>UseAssetsFolder</b> to <b>true</b>", mod.ID));
            return Path.Combine(AssetsFolder, mod.ID);
        }

        internal static string GetMetadataFolder(string fn) => Path.Combine(ManifestsFolder, fn);
        
        /// <summary>
        /// Main init
        /// </summary>
        public static void Init_NP(string cfg)
        {
            switch (cfg)
            {
                case "GF":
                    Init_GF();
                    break;
                case "MD":
                    Init_MD();
                    break;
                case "AD":
                    Init_AD();
                    break;
                default:
                    Init_GF();
                    break;
            }
        }
        /// <summary>
        /// Initialize ModLoader with Mods folder in My Documents (like it was in 0.1)
        /// </summary>
        public static void Init_MD()
        {
            if (unloader) return;
            ModsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), @"MySummerCar\Mods");
            PrepareModLoader();
        }

        /// <summary>
        /// Initialize ModLoader with Mods folder in Game Folder (near game's exe)
        /// </summary>
        public static void Init_GF()
        {
            if (unloader) return;
            ModsFolder = Path.GetFullPath(Path.Combine("Mods", ""));
            PrepareModLoader();
        }

        /// <summary>
        /// Initialize ModLoader with Mods folder in AppData/LocalLow (near game's save)
        /// </summary>
        public static void Init_AD()
        {
            if (unloader) return;
            ModsFolder = Path.GetFullPath(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"..\LocalLow\Amistech\My Summer Car\Mods"));
            PrepareModLoader();
        }

        private static void PrepareModLoader()
        {
            if (!loaderPrepared)
            {
                loaderPrepared = true;
                GameObject go = new GameObject("MSCLoader", typeof(ModLoader));
                Instance = go.GetComponent<ModLoader>();
                DontDestroyOnLoad(go);
                Instance.Init();
            }
        }
        bool vse = false;
        private void OnLevelWasLoaded(int level)
        {
            if (Application.loadedLevelName == "MainMenu")
            {
                CurrentGameScene = CurrentScene.MainMenu;
                if (GameObject.Find("Music"))
                    GameObject.Find("Music").GetComponent<AudioSource>().Play();
                if (QualitySettings.vSyncCount != 0)
                    vse = true;
                if ((bool)ModSettings_menu.forceMenuVsync.GetValue() && !vse)
                    QualitySettings.vSyncCount = 1; //vsync in menu
                if (IsDoneLoading && GameObject.Find("MSCLoader Info") == null)
                {
                    MainMenuInfo();
                }
                if (allModsLoaded)
                {
                    loaderPrepared = false;
                    mscUnloader.MSCLoaderReset();
                    unloader = true;
                    return;
                }
            }
            else if (Application.loadedLevelName == "Intro")
            {
                CurrentGameScene = CurrentScene.NewGameIntro;

                if (!IsModsDoneResetting && !IsModsResetting)
                {
                    IsModsResetting = true;
                    StartCoroutine(NewGameMods());
                }
            }
            else if (Application.loadedLevelName == "GAME")
            {
                CurrentGameScene = CurrentScene.Game;
                if ((bool)ModSettings_menu.forceMenuVsync.GetValue() && !vse)
                    QualitySettings.vSyncCount = 0;

                menuInfoAnim.Play("fade_out");
                StartLoadingMods(!(bool)ModSettings_menu.syncLoad.GetValue());

            }
        }

        private void StartLoadingMods(bool async)
        {
            if (!allModsLoaded && !IsModsLoading)
            {
                IsModsLoading = true;
                if(async)
                    StartCoroutine(LoadModsAsync());
                else
                    StartCoroutine(LoadMods());

            }
        }

        private void Init()
        {
            //Set config and Assets folder in selected mods folder
            ConfigFolder = Path.Combine(ModsFolder, "Config");
            SettingsFolder = Path.Combine(ConfigFolder, "Mod Settings");
            AssetsFolder = Path.Combine(ModsFolder, "Assets");

            //Move from old to new location if updated from before 1.1
            if (!Directory.Exists(SettingsFolder) && Directory.Exists(ConfigFolder))
            {
                Directory.CreateDirectory(SettingsFolder);
                foreach (string dir in Directory.GetDirectories(ConfigFolder))
                {
                    if (new DirectoryInfo(dir).Name != "Mod Settings")
                    {
                        try
                        {
                            Directory.Move(dir, Path.Combine(SettingsFolder, new DirectoryInfo(dir).Name));
                        }
                        catch (Exception ex)
                        {
                            System.Console.WriteLine(string.Format("{0} (Failed to update folder structure)", ex.Message));
                        }
                    }
                }
            }
            ManifestsFolder = Path.Combine(ConfigFolder, "Mod Metadata");

            if (GameObject.Find("MSCUnloader") == null)
            {
                GameObject go = new GameObject();
                go.name = "MSCUnloader";
                go.AddComponent<MSCUnloader>();
                mscUnloader = go.GetComponent<MSCUnloader>();
                DontDestroyOnLoad(go);
            }
            else
            {
                mscUnloader = GameObject.Find("MSCUnloader").GetComponent<MSCUnloader>();
            }
            if (!IsDoneLoading)
            {
                ModUI.CreateCanvas();
                allModsLoaded = false;
                LoadedMods = new List<Mod>();
                InvalidMods = new List<string>();
                mscUnloader.reset = false;
                if (!Directory.Exists(ModsFolder))
                    Directory.CreateDirectory(ModsFolder);
                if(!Directory.Exists("upd_tmp"))
                    Directory.CreateDirectory("upd_tmp");

                if (!Directory.Exists(ConfigFolder))
                {
                    Directory.CreateDirectory(ConfigFolder);
                    Directory.CreateDirectory(SettingsFolder);
                    Directory.CreateDirectory(ManifestsFolder);
                    Directory.CreateDirectory(Path.Combine(ManifestsFolder, "Mod Icons"));
                }
                if (!Directory.Exists(ManifestsFolder))
                {
                    Directory.CreateDirectory(ManifestsFolder);
                    Directory.CreateDirectory(Path.Combine(ManifestsFolder, "Mod Icons"));

                }
                if (!Directory.Exists(AssetsFolder))
                    Directory.CreateDirectory(AssetsFolder);

                LoadCoreAssets();
                LoadMod(new ModConsole(), MSCLoader_Ver);
                LoadedMods[0].ModSettings();
                LoadMod(new ModSettings_menu(), MSCLoader_Ver);
                LoadedMods[1].ModSettings();
                ModSettings_menu.LoadSettings();                
                IsDoneLoading = true;
                if (experimental)
                    ModConsole.Print(string.Format("<color=green>ModLoader <b>v{0}</b> ready</color> [<color=magenta>Experimental</color> <color=lime>build {1}</color>]", MSCLoader_Ver, expBuild));
                else
                    ModConsole.Print(string.Format("<color=green>ModLoader <b>v{0}</b> ready</color>", MSCLoader_Ver));
                MainMenuInfo();
                upd_tmp = Directory.GetFiles("upd_tmp", "*.zip");
                if (upd_tmp.Length == 0)
                {
                    ContinueInit();
                }
                else
                {
                    UnpackUpdates();
                }
            }
        }
        void ContinueInit()
        {
            LoadReferences();
            PreLoadMods();
            ModConsole.Print(string.Format("<color=orange>Found <color=green><b>{0}</b></color> mods!</color>", LoadedMods.Count - 2));
            try
            {
                if (File.Exists(Path.GetFullPath(Path.Combine("LAUNCHER.exe", ""))) || File.Exists(Path.GetFullPath(Path.Combine("SmartSteamEmu64.dll", ""))) || File.Exists(Path.GetFullPath(Path.Combine("SmartSteamEmu.dll", ""))))
                {
                    ModConsole.Print(string.Format("<color=orange>Hello <color=green><b>{0}</b></color>!</color>", "Murzyn!"));
                    throw new Exception("[EMULATOR] Do What You Want, Cause A Pirate Is Free... You Are A Pirate!");
                    //exclude emulators
                }
                Steamworks.SteamAPI.Init();
                steamID = Steamworks.SteamUser.GetSteamID().ToString();
                ModConsole.Print(string.Format("<color=orange>Hello <color=green><b>{0}</b></color>!</color>", Steamworks.SteamFriends.GetPersonaName()));
                WebClient webClient = new WebClient();
                webClient.Headers.Add("user-agent", string.Format("MSCLoader/{0} ({1})", MSCLoader_Ver, SystemInfo.operatingSystem));
                webClient.DownloadStringCompleted += sAuthCheckCompleted;
                webClient.DownloadStringAsync(new Uri(string.Format("{0}/sauth.php?sid={1}", serverURL, steamID)));
            }
            catch (Exception e)
            {
                steamID = null;
                ModConsole.Error("Steam client doesn't exists.");
                if (devMode)
                    ModConsole.Error(e.ToString());
                System.Console.WriteLine(e);
                if (CheckSteam())
                {
                    System.Console.WriteLine(new AccessViolationException().Message);
                    Environment.Exit(0);
                }
            }
            LoadModsSettings();
            ModSettings_menu.LoadBinds();
            if (OnGUImods.Count() > 0)
                gameObject.AddComponent<ModOnGUI>().modLoader = this;
            if (UpdateMods.Count() > 0)
                gameObject.AddComponent<ModUpdate>().modLoader = this;
            if (FixedUpdateMods.Count() > 0)
                gameObject.AddComponent<ModFixedUpdate>().modLoader = this;
            if (!rtmm)
            {
                if (ModSettings_menu.cfmu_set != 0)
                {
                    string sp = Path.Combine(SettingsFolder, @"MSCLoader_Settings\lastCheck");
                    if (File.Exists(sp))
                    {
                        DateTime lastCheck;
                        string lastCheckS = File.ReadAllText(sp);
                        DateTime.TryParse(lastCheckS, out lastCheck);
                        if ((DateTime.Now - lastCheck).TotalDays >= ModSettings_menu.cfmu_set || (DateTime.Now - lastCheck).TotalDays < 0)
                        {
                            StartCoroutine(CheckForModsUpdates());
                            File.WriteAllText(sp, DateTime.Now.ToString());
                        }
                        else
                        {
                            foreach (Mod mod in LoadedMods.Where(x => !x.ID.StartsWith("MSCLoader_")))
                                ReadMetadata(mod);
                        }
                    }
                    else
                    {
                        StartCoroutine(CheckForModsUpdates());
                        File.WriteAllText(sp, DateTime.Now.ToString());
                    }
                }
                else
                {
                    StartCoroutine(CheckForModsUpdates());
                }
            }

            if (devMode)
                ModConsole.Warning("You are running ModLoader in <color=red><b>DevMode</b></color>, this mode is <b>only for modders</b> and shouldn't be use in normal gameplay.");
            System.Console.WriteLine(SystemInfo.operatingSystem); //operating system version to output_log.txt
            if (saveErrors != null)
            {
                if (saveErrors.Count > 0 && wasSaving)
                {
                    ModUI.ShowMessage(string.Format("Some mod thrown an error during saving{0}Check console for more information!", Environment.NewLine));
                    for (int i = 0; i < saveErrors.Count; i++)
                    {
                        ModConsole.Error(saveErrors[i]);
                    }
                }
                wasSaving = false;
            }
        }

        void UnpackUpdates()
        {
            ModConsole.Print("Unpacking mod updates...");
            for (int i = 0; i < upd_tmp.Length; i++)
            {
                ModConsole.Print($"Unpacking file {upd_tmp[i]}");
                string zip = upd_tmp[i];
                try
                {
                    if (!ZipFile.IsZipFile(zip))
                    {
                        ModConsole.Error($"Failed read zip file {upd_tmp[i]}");
                    }
                    else
                    {
                        ZipFile zip1 = ZipFile.Read(File.Open(zip, FileMode.Open));
                        foreach (ZipEntry zz in zip1)
                        {
                            ModConsole.Print($"Copying new file: {zz.FileName}");
                            zz.Extract(ModsFolder, ExtractExistingFileAction.OverwriteSilently);
                        }
                    }
                    File.Delete(upd_tmp[i]);
                }
                catch (Exception e)
                {
                    ModConsole.Error(e.Message);
                    System.Console.WriteLine(e);
                }
            }
            ModUI.ShowMessage("Updating mods finished.", "Update mods");
            ContinueInit();
        }
    
        private bool cfmuErrored = false;
        private bool cfmuInProgress = false;
        private string cfmuResult = string.Empty;
        IEnumerator CheckForModsUpdates()
        {
            int modUpdCount = 0;
            loadingMeta.transform.GetChild(0).GetComponent<Text>().text = string.Format("Checking for mod updates...");
            loadingMeta.transform.GetChild(1).GetComponent<Slider>().minValue = 1;
            loadingMeta.transform.GetChild(1).GetComponent<Slider>().maxValue = LoadedMods.Count - 2;
            loadingMeta.transform.GetChild(2).GetComponent<Text>().text = string.Format("{0}/{1}", 0, LoadedMods.Count - 2);
            loadingMeta.transform.GetChild(3).GetComponent<Text>().text = string.Format("Connecting...");
            loadingMeta.transform.GetChild(4).GetComponent<Text>().text = "...";
            loadingMeta.SetActive(true);
            mod_aulist = new List<string>();

            Mod[] mod = LoadedMods.Where(x => !x.ID.StartsWith("MSCLoader_")).ToArray();
            for (int i = 0; i < mod.Length; i++)
            {
                if (cfmuErrored)
                {
                    ReadMetadata(mod[i]);
                    continue;
                }
                loadingMeta.transform.GetChild(2).GetComponent<Text>().text = string.Format("{0}/{1}", i+1, LoadedMods.Count - 2);
                loadingMeta.transform.GetChild(1).GetComponent<Slider>().value = i+1;
                loadingMeta.transform.GetChild(3).GetComponent<Text>().text = string.Format("Mod: <color=orange>{0}</color>", mod[i].Name);

                WebClient webClient = new WebClient();
                webClient.Headers.Add("user-agent", string.Format("MSCLoader/{0} ({1})", MSCLoader_Ver, SystemInfo.operatingSystem));
                webClient.DownloadStringCompleted += cfmuDownloadCompl;
                webClient.DownloadStringAsync(new Uri(string.Format("{0}/man/{1}", metadataURL, mod[i].ID)));

                cfmuInProgress = true;
                while (cfmuInProgress)
                    yield return null;
                if (cfmuErrored)
                {
                    ReadMetadata(mod[i]);
                    continue;
                }
                if (!string.IsNullOrEmpty(cfmuResult))
                {
                    if (cfmuResult.StartsWith("error"))
                    {
                        string[] ed = cfmuResult.Split('|');
                        if (ed[0] == "error")
                        {
                            switch (ed[1])
                            {
                                case "0":
                                    System.Console.WriteLine("No metadata for " + mod[i].ID);
                                    yield return null;
                                    continue;
                                case "1":
                                    System.Console.WriteLine("Database connection problem");
                                    yield return null;
                                    continue;
                                default:
                                    System.Console.WriteLine("Unknown error.");
                                    yield return null;
                                    continue;
                            }
                        }
                    }
                    else if (cfmuResult.StartsWith("{"))
                    {
                        try
                        {
                            mod[i].RemMetadata = JsonConvert.DeserializeObject<ModsManifest>(cfmuResult);
                            Version v1 = new Version(mod[i].RemMetadata.version);
                            Version v2 = new Version(mod[i].Version);
                            switch (v1.CompareTo(v2))
                            {
                                case 0:
                                    if (File.Exists(GetMetadataFolder(string.Format("{0}.json", mod[i].ID))) && !cfmuResult.Equals(File.ReadAllText(GetMetadataFolder(string.Format("{0}.json", mod[i].ID)))))
                                    {
                                        File.WriteAllText(GetMetadataFolder(string.Format("{0}.json", mod[i].ID)), cfmuResult);
                                        mod[i].metadata = mod[i].RemMetadata;
                                    }
                                    else
                                    {
                                        File.WriteAllText(GetMetadataFolder(string.Format("{0}.json", mod[i].ID)), cfmuResult);
                                        mod[i].metadata = mod[i].RemMetadata;
                                    }
                                    break;
                                case 1:
                                    mod[i].hasUpdate = true;
                                    modUpdCount++;
                                    if (mod[i].RemMetadata.type != 3)
                                    {
                                        if (File.Exists(GetMetadataFolder(string.Format("{0}.json", mod[i].ID))) && !cfmuResult.Equals(File.ReadAllText(GetMetadataFolder(string.Format("{0}.json", mod[i].ID)))))
                                        {
                                            File.WriteAllText(GetMetadataFolder(string.Format("{0}.json", mod[i].ID)), cfmuResult);
                                            mod[i].metadata = mod[i].RemMetadata;
                                        }
                                        else
                                        {
                                            File.WriteAllText(GetMetadataFolder(string.Format("{0}.json", mod[i].ID)), cfmuResult);
                                            mod[i].metadata = mod[i].RemMetadata;
                                        }
                                    }
                                    if(mod[i].RemMetadata.type == 4)
                                    {
                                        mod_aulist.Add(mod[i].ID);
                                    }
                                    break;
                                case -1:
                                    if (mod[i].RemMetadata.sid_sign != MurzynskaMatematyka(steamID + mod[i].ID) && mod[i].RemMetadata.type == 3)
                                        File.WriteAllText(GetMetadataFolder(string.Format("{0}.json", mod[i].ID)), cfmuResult);
                                    if (File.Exists(GetMetadataFolder(string.Format("{0}.json", mod[i].ID))) && !cfmuResult.Equals(File.ReadAllText(GetMetadataFolder(string.Format("{0}.json", mod[i].ID)))))
                                    {
                                        mod[i].hasUpdate = true;
                                        modUpdCount++;
                                    }
                                    if(!File.Exists(GetMetadataFolder(string.Format("{0}.json", mod[i].ID))))
                                        File.WriteAllText(GetMetadataFolder(string.Format("{0}.json", mod[i].ID)), cfmuResult);
                                    break;
                            }
                            ReadMetadata(mod[i]);
                        }
                        catch (Exception e)
                        {
                            ModConsole.Error(e.Message);
                            System.Console.WriteLine(e);

                        }
                        yield return null;
                        continue;
                    }
                    else
                    {
                        System.Console.WriteLine("Unknown response: " + cfmuResult);
                        yield return null;
                        continue;
                    }
                }
                if (modUpdCount > 0)
                    loadingMeta.transform.GetChild(4).GetComponent<Text>().text = string.Format("<color=green>{0}</color>", modUpdCount);
            }
            if (modUpdCount > 0)
            {
                modUpdates.text = string.Format("<size=20><color=aqua>New Version available for <color=orange>{0}</color> mods.</color></size>", modUpdCount);
                loadingMeta.transform.GetChild(3).GetComponent<Text>().text = string.Format("Done! <color=lime>{0} updates available</color>", modUpdCount);
                if(mod_aulist.Count >0)
                {
                    ModUI.ShowYesNoMessage($"There are updates to mods that can be updated automatically{Environment.NewLine}Mods: <color=aqua>{string.Join(", ", mod_aulist.ToArray())}</color>{Environment.NewLine}{Environment.NewLine}Do you want to download updates now?", "Mod Updates Available", DownloadUpdates);
                }
            }
            else
                loadingMeta.transform.GetChild(3).GetComponent<Text>().text = string.Format("Done!");
            if (cfmuErrored)
                loadingMeta.transform.GetChild(3).GetComponent<Text>().text = string.Format("<color=red>Connection error!</color>");
            yield return new WaitForSeconds(3f);
            if(!dnsaf)
                loadingMeta.SetActive(false);

        }
        private void cfmuDownloadCompl(object sender, DownloadStringCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                cfmuErrored = true;
                cfmuInProgress = false;
                cfmuResult = string.Empty;
                ModConsole.Error("Failed to check for mod updates!");
                ModConsole.Error(e.Error.Message);
                System.Console.WriteLine(e.Error);
            }
            else
            {
                cfmuErrored = false;
                cfmuResult = e.Result;
                cfmuInProgress = false;
            }
        }

        private bool dnsaf = false;
        void DownloadUpdates()
        {
            if (GetCurrentScene() != CurrentScene.MainMenu)
            {
                ModUI.ShowMessage("You can do that only in main menu.", "Mod Updates");
                return;
            }
            else
            {
                dnsaf = true;
                StartCoroutine(DownloadUpdatesC());
            }
        }
        private bool downloadInProgress = false;
       // private bool downloadErrored = false;
        IEnumerator DownloadUpdatesC()
        {
            loadingMeta.transform.GetChild(0).GetComponent<Text>().text = string.Format("Downloading mod updates...");
            loadingMeta.transform.GetChild(1).GetComponent<Slider>().minValue = 1;
            loadingMeta.transform.GetChild(1).GetComponent<Slider>().maxValue = 100;
            loadingMeta.transform.GetChild(2).GetComponent<Text>().text = string.Format("{0}/{1}", 0, mod_aulist.Count);
            loadingMeta.transform.GetChild(3).GetComponent<Text>().text = string.Format("Connecting...");
            loadingMeta.transform.GetChild(4).GetComponent<Text>().text = "...";
            loadingMeta.SetActive(true);
            yield return null;

            for (int i = 0; i < mod_aulist.Count; i++)
            {
                string mod = mod_aulist[i];
                string dwl;
                WebClient getdwl = new WebClient();
                getdwl.Headers.Add("user-agent", string.Format("MSCLoader/{0} ({1})", MSCLoader_Ver, SystemInfo.operatingSystem));
                // webClient.DownloadStringCompleted += WebClient_DownloadStringCompleted; ;
                dwl = getdwl.DownloadString(string.Format("{0}/dwl.php?core={1}", serverURL, mod));
                string[] result = dwl.Split('|');
                if (result[0] == "error")
                {
                    switch (result[1])
                    {
                        case "0":
                            ModConsole.Error($"Failed to download updates: Invalid request");
                            break;
                        case "1":
                            ModConsole.Error($"Failed to download updates: Database connection error");
                            break;
                        case "2":
                            ModConsole.Error($"Failed to download updates: File not found");
                            break;
                        default:
                            ModConsole.Error($"Failed to download updates: Unknown error");
                            break;
                    }

                }
                else if (result[0] == "ok")
                {
                    WebClient webClient = new WebClient();
                    webClient.Headers.Add("user-agent", string.Format("MSCLoader/{0} ({1})", MSCLoader_Ver, SystemInfo.operatingSystem));
                    webClient.DownloadFileCompleted += DownloadModCompleted;
                    webClient.DownloadProgressChanged += DownlaodModProgress;
                    webClient.DownloadFileAsync(new Uri(string.Format("{0}/{1}", serverURL, result[1])),Path.Combine("upd_tmp", $"{mod}.zip"));
                    ModConsole.Print($"Downloading: <color=aqua>{mod}.zip</color>");
                    downloadInProgress = true;
                    while (downloadInProgress)
                    {
                        loadingMeta.transform.GetChild(1).GetComponent<Slider>().value = downloadPercentage;
                        loadingMeta.transform.GetChild(2).GetComponent<Text>().text = string.Format("{0}/{1}", i+1, mod_aulist.Count);
                        loadingMeta.transform.GetChild(3).GetComponent<Text>().text = string.Format($"Downloading... <color=lime>{mod}.zip</color>");
                        loadingMeta.transform.GetChild(4).GetComponent<Text>().text = $"{downloadPercentage}%";
                        yield return null;
                    }
                    yield return new WaitForSeconds(1f);
                }
                else
                {
                    System.Console.WriteLine("Unknown: " + result[0]);
                    ModConsole.Error($"Failed to download updates: Unknown server response.");
                }

            }
            loadingMeta.transform.GetChild(1).GetComponent<Slider>().value = 100;
            loadingMeta.transform.GetChild(3).GetComponent<Text>().text = string.Format($"<color=lime>Download Complete</color>");
            loadingMeta.transform.GetChild(4).GetComponent<Text>().text = $"100%";
            ModUI.ShowMessage("To apply updates, please restart game.", "download completed");
            yield return new WaitForSeconds(3f); 
            loadingMeta.SetActive(false);
        }
        int downloadPercentage = 0;
        private void DownlaodModProgress(object sender, DownloadProgressChangedEventArgs e)
        {
            downloadInProgress = true;
            downloadPercentage = e.ProgressPercentage;
        }

        private void DownloadModCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            downloadPercentage = 100;
            downloadInProgress = false;
            if (e.Error != null)
            {
                ModConsole.Error("Failed to download update file!");
                ModConsole.Error(e.Error.Message);
                System.Console.WriteLine(e.Error);
            }
            else
            {
                ModConsole.Print($"Download finished");
            }
        }

        private void ReadMetadata(Mod mod)
        {
            if (mod.metadata == null && mod.RemMetadata != null)
                mod.metadata = mod.RemMetadata;
            else if (mod.metadata != null && mod.RemMetadata == null)
                mod.RemMetadata = mod.metadata;
            else if (mod.metadata == null && mod.RemMetadata == null)
                return;
            if (mod.metadata.type == 9)
            {
                //Disabled by reason
                mod.isDisabled = true;
                if (!string.IsNullOrEmpty(mod.metadata.msg))
                    ModConsole.Error(string.Format("Mod <b>{0}</b> has been disabled, Reason: <b>{1}</b>", mod.ID, mod.metadata.msg));
                else
                    ModConsole.Error(string.Format("Mod <b>{0}</b> has been disabled, Reason: <i>No reason given...</i>", mod.ID));
                return;
            }
            if (mod.metadata.type == 2)
            {
                //Disabled by user
                mod.isDisabled = true;
                if (!string.IsNullOrEmpty(mod.metadata.msg))
                    ModConsole.Error(string.Format("Mod <b>{0}</b> has been disabled by author, Reason: <b>{1}</b>", mod.ID, mod.metadata.msg));
                else
                    ModConsole.Error(string.Format("Mod <b>{0}</b> has been disabled by author, Reason: <i>No reason given...</i>", mod.ID));
                return;
            }
            if (mod.RemMetadata.type == 3 && !mod.hasUpdate)
            {
                if (mod.RemMetadata.sign != ManifestStuff.AzjatyckaMatematyka(mod.fileName))
                {
                    mod.isDisabled = true;
                    return;
                }
            }
            if (mod.metadata.type == 1 || mod.metadata.type == 4)
            {
                if (mod.metadata.icon.iconFileName != null && mod.metadata.icon.iconFileName != string.Empty)
                {
                    if (mod.metadata.icon.isIconRemote)
                    {
                        if (!File.Exists(Path.Combine(ManifestsFolder, @"Mod Icons\" + mod.metadata.icon.iconFileName)))
                        {
                            WebClient webClient = new WebClient();
                            webClient.Headers.Add("user-agent", string.Format("MSCLoader/{0} ({1})", MSCLoader_Ver, SystemInfo.operatingSystem));
                            webClient.DownloadFileCompleted += WebClient_DownloadFileCompleted;
                            webClient.DownloadFileAsync(new Uri(string.Format("{0}/images/modicons/{1}", metadataURL, mod.metadata.icon.iconFileName)), Path.Combine(ManifestsFolder, @"Mod Icons\" + mod.metadata.icon.iconFileName));
                        }
                    }
                }
                if (mod.metadata.minimumRequirements.MSCbuildID > 0)
                {
                    try
                    {
                        if (mod.metadata.minimumRequirements.MSCbuildID > Steamworks.SteamApps.GetAppBuildId())
                        {
                            if (mod.metadata.minimumRequirements.disableIfVer)
                            {
                                mod.isDisabled = true;
                                ModConsole.Error(string.Format("Mod <b>{0}</b> requires MSC build at least <b>{1}</b>, your current build is <b>{2}</b>. Author marked this as required!", mod.ID, mod.metadata.minimumRequirements.MSCbuildID, Steamworks.SteamApps.GetAppBuildId()));
                            }
                            else
                            {
                                ModConsole.Warning(string.Format("Mod <b>{0}</b> requires MSC build at least <b>{1}</b>, your current build is <b>{2}</b>. This may cause issues!", mod.ID, mod.metadata.minimumRequirements.MSCbuildID, Steamworks.SteamApps.GetAppBuildId()));
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        System.Console.WriteLine("Can't get buildID compare " + e);
                    }
                }
                if (mod.metadata.minimumRequirements.MSCLoaderVer != null && mod.metadata.minimumRequirements.MSCLoaderVer != string.Empty)
                {
                    Version v1 = new Version(mod.metadata.minimumRequirements.MSCLoaderVer);
                    Version v2 = new Version(MSCLoader_Ver);
                    if (v1.CompareTo(v2) == 1)
                    {
                        if (mod.metadata.minimumRequirements.disableIfVer)
                        {
                            mod.isDisabled = true;
                            ModConsole.Error(string.Format("Mod <b>{0}</b> requires MSCLoader at least <b>{1}</b>, your current version is <b>{2}</b>. Author marked this as required!", mod.ID, mod.metadata.minimumRequirements.MSCLoaderVer, MSCLoader_Ver));
                        }
                        else
                        {
                            ModConsole.Warning(string.Format("Mod <b>{0}</b> requires MSCLoader at least <b>{1}</b>, your current version is <b>{2}</b>. This may cause issues!", mod.ID, mod.metadata.minimumRequirements.MSCLoaderVer, MSCLoader_Ver));
                        }
                    }
                }
                if (mod.metadata.modConflicts.modIDs != null && mod.metadata.modConflicts.modIDs != string.Empty)
                {
                    string[] modIDs = mod.metadata.modConflicts.modIDs.Trim().Split(',');
                    for (int i = 0; i < modIDs.Length; i++)
                    {
                        if (LoadedMods.Select(s => s.ID).Where(x => x.Equals(modIDs[i])).Count() != 0)
                        {
                            if (mod.metadata.modConflicts.disableIfConflict)
                            {
                                mod.isDisabled = true;
                                if (mod.metadata.modConflicts.customMessage != null && mod.metadata.modConflicts.customMessage != string.Empty)
                                    ModConsole.Error(string.Format("Mod <color=orange><b>{0}</b></color> is marked as conflict with installed mod <color=orange><b>{1}</b></color>. Author's message: {2}", mod.ID, modIDs[i], mod.metadata.modConflicts.customMessage));
                                else
                                    ModConsole.Error(string.Format("Mod <color=orange><b>{0}</b></color> is marked as conflict with installed mod <color=orange><b>{1}</b></color>.", mod.ID, modIDs[i]));
                            }
                            else
                            {
                                if (mod.metadata.modConflicts.customMessage != null && mod.metadata.modConflicts.customMessage != string.Empty)
                                    ModConsole.Warning(string.Format("Mod <color=red><b>{0}</b></color> is marked as conflict with installed mod <color=red><b>{1}</b></color>. Author's message: {2}", mod.ID, modIDs[i], mod.metadata.modConflicts.customMessage));
                                else
                                    ModConsole.Warning(string.Format("Mod <color=red><b>{0}</b></color> is marked as conflict with installed mod <color=red><b>{1}</b></color>.", mod.ID, modIDs[i]));
                            }
                        }
                    }
                }
                if (mod.metadata.requiredMods.modID != null && mod.metadata.requiredMods.modID != string.Empty)
                {
                    string[] modIDs = mod.metadata.requiredMods.modID.Trim().Split(',');
                    string[] modIDvers = mod.metadata.requiredMods.minVer.Trim().Split(',');

                    for (int i = 0; i < modIDs.Length; i++)
                    {
                        string m = modIDs[i];
                        if (LoadedMods.Select(s => s.ID).Where(x => x.Equals(m)).Count() == 0)
                        {
                            mod.isDisabled = true;
                            if (mod.metadata.requiredMods.customMessage != null && mod.metadata.requiredMods.customMessage != string.Empty)
                                ModConsole.Error(string.Format("Mod <b>{0}</b> is missing required mod <b>{1}</b>. Author's message: {2}", mod.ID, m, mod.metadata.requiredMods.customMessage));
                            else
                                ModConsole.Error(string.Format("Mod <b>{0}</b> is missing required mod <b>{1}</b>.", mod.ID, m));
                        }
                        else
                        {
                            try
                            {
                                Version v1 = new Version(modIDvers[i]);
                                Version v2 = new Version(LoadedMods.Where(x => x.ID.Equals(m)).FirstOrDefault().Version);
                                if (v1.CompareTo(v2) == 1)
                                {
                                    if (mod.metadata.requiredMods.customMessage != null && mod.metadata.requiredMods.customMessage != string.Empty)
                                        ModConsole.Warning(string.Format("Mod <b>{0}</b> requires mod <b>{1}</b> to be at least version <b>{3}</b>. Author's message: {2}", mod.ID, m, mod.metadata.requiredMods.customMessage, v1));
                                    else
                                        ModConsole.Warning(string.Format("Mod <b>{0}</b> requires mod <b>{1}</b> to be at least version <b>{2}</b>.", mod.ID, m, v1));
                                }
                            }
                            catch (Exception e)
                            {
                                System.Console.WriteLine(e);
                            }
                        }
                    }
                }
            }
        }

        private void WebClient_DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            if (e.Error != null)
                System.Console.WriteLine(e.Error);
        }

        [Serializable]
        class SaveOtk
        {
            public string k1;
            public string k2;
        }

        private void sAuthCheckCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            try
            {
                if (e.Error != null)
                    throw new Exception(e.Error.Message);

                string result = e.Result;

                if (result != string.Empty)
                {
                    string[] ed = result.Split('|');
                    if (ed[0] == "error")
                    {
                        switch (ed[1])
                        {
                            case "0":
                                throw new Exception("Getting steamID failed.");
                            case "1":
                                throw new Exception("steamID rejected.");
                            default:
                                throw new Exception("Unknown error.");
                        }
                    }
                    else if (ed[0] == "ok")
                    {
                        SaveOtk s = new SaveOtk();
                        s.k1 = ed[1];
                        s.k2 = ed[2];
                        System.Runtime.Serialization.Formatters.Binary.BinaryFormatter f = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                        string sp = Path.Combine(SettingsFolder, @"MSCLoader_Settings\otk.bin");
                        FileStream st = new FileStream(sp, FileMode.Create);
                        f.Serialize(st, s);
                        st.Close();
                    }
                    else
                    {
                        System.Console.WriteLine("Unknown: " + ed[0]);
                        throw new Exception("Unknown server response.");
                    }
                }
                bool ret = Steamworks.SteamApps.GetCurrentBetaName(out string Name, 128);
                if (ret && (bool)ModSettings_menu.expWarning.GetValue())
                {
                    if (Name != "default_32bit") //32bit is NOT experimental branch
                        ModUI.ShowMessage(string.Format("<color=orange><b>Warning:</b></color>{1}You are using beta build: <color=orange><b>{0}</b></color>{1}{1}Remember that some mods may not work correctly on beta branches.", Name, Environment.NewLine), "Experimental build warning");
                }
                System.Console.WriteLine(string.Format("MSC buildID: <b>{0}</b>", Steamworks.SteamApps.GetAppBuildId()));
                if (Steamworks.SteamApps.GetAppBuildId() == 1)
                    throw new DivideByZeroException();
            }
            catch (Exception ex)
            {
                string sp = Path.Combine(SettingsFolder, @"MSCLoader_Settings\otk.bin");
                if (e.Error != null)
                {
                    if (File.Exists(sp))
                    {
                        System.Runtime.Serialization.Formatters.Binary.BinaryFormatter f = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                        FileStream st = new FileStream(sp, FileMode.Open);
                        SaveOtk s = f.Deserialize(st) as SaveOtk;
                        st.Close();
                        string murzyn = "otk_" + MurzynskaMatematyka(string.Format("{0}{1}", steamID, s.k1));
                        if (s.k2.CompareTo(murzyn) != 0)
                        {
                            File.Delete(sp);
                            steamID = null;
                            ModConsole.Error("SteamAPI failed with error: " + ex.Message);
                            if (CheckSteam())
                            {
                                System.Console.WriteLine(new AccessViolationException().Message);
                                Environment.Exit(0);
                            }
                        }
                        else
                        {
                            System.Console.WriteLine("offline");
                        }
                    }
                    else
                    {
                        steamID = null;
                        ModConsole.Error("SteamAPI failed with error: " + ex.Message);
                        if (CheckSteam())
                        {
                            System.Console.WriteLine(new AccessViolationException().Message);
                            Environment.Exit(0);
                        }
                    }
                }
                else
                {
                    if (File.Exists(sp))
                        File.Delete(sp);
                    steamID = null;
                    ModConsole.Error("SteamAPI failed with error: " + ex.Message);
                    if (devMode)
                        ModConsole.Error(ex.ToString());
                    if (CheckSteam())
                    {
                        System.Console.WriteLine(new AccessViolationException().Message);
                        Environment.Exit(0);
                    }
                }

                System.Console.WriteLine(ex);
            }
        }

        private void LoadReferences()
        {
            if (Directory.Exists(Path.Combine(ModsFolder, "References")))
            {
                string[] files = Directory.GetFiles(Path.Combine(ModsFolder, "References"), "*.dll");
                for (int i = 0; i < files.Length; i++)
                {
                    Assembly.LoadFrom(files[i]);
                }
            }
            else
            {
                Directory.CreateDirectory(Path.Combine(ModsFolder, "References"));
            }
        }

        private void LoadCoreAssets()
        {
            ModConsole.Print("Loading core assets...");
            AssetBundle ab = LoadAssets.LoadBundle("MSCLoader.CoreAssets.core.unity3d");
            guiskin = ab.LoadAsset<GUISkin>("MSCLoader.guiskin");
            ModUI.messageBox = ab.LoadAsset<GameObject>("MSCLoader MB.prefab");
            ModUI.messageBoxBtn = ab.LoadAsset<GameObject>("MB_Button.prefab");
            mainMenuInfo = ab.LoadAsset<GameObject>("MSCLoader Info.prefab");
            loading = ab.LoadAsset<GameObject>("LoadingMods.prefab");
            loadingMeta = ab.LoadAsset<GameObject>("MSCLoader pbar.prefab");
            loading = GameObject.Instantiate(loading);
            loading.SetActive(false);
            loading.name = "MSCLoader loading screen";
            loading.transform.SetParent(ModUI.GetCanvas().transform, false);
            loadingMeta = GameObject.Instantiate(loadingMeta);
            loadingMeta.SetActive(false);
            loadingMeta.name = "MSCLoader pbar";
            loadingMeta.transform.SetParent(ModUI.GetCanvas().transform, false);
            ModConsole.Print("Loading core assets completed!");
            ab.Unload(false); //freeup memory
        }

        /// <summary>
        /// Toggle main menu path via settings
        /// </summary>
        public static void MainMenuPath()
        {
            Instance.mainMenuInfo.transform.GetChild(1).gameObject.SetActive((bool)ModSettings_menu.modPath.GetValue());
        }
        Text modUpdates;
        private void MainMenuInfo()
        {
            Text info, mf;
            mainMenuInfo = Instantiate(mainMenuInfo);
            mainMenuInfo.name = "MSCLoader Info";
            menuInfoAnim = mainMenuInfo.GetComponent<Animation>();
            menuInfoAnim.Play("fade_in");
            info = mainMenuInfo.transform.GetChild(0).gameObject.GetComponent<Text>();
            mf = mainMenuInfo.transform.GetChild(1).gameObject.GetComponent<Text>();
            modUpdates = mainMenuInfo.transform.GetChild(2).gameObject.GetComponent<Text>();
            info.text = string.Format("Mod Loader MSCLoader <color=cyan>v{0}</color> is ready! (<color=orange>Checking for updates...</color>)", MSCLoader_Ver);
            WebClient client = new WebClient();
            client.Headers.Add("user-agent", string.Format("MSCLoader/{0} ({1})", MSCLoader_Ver, SystemInfo.operatingSystem));

            //client.Proxy = new WebProxy("127.0.0.1:8888"); //ONLY FOR TESTING
            client.DownloadStringCompleted += VersionCheckCompleted;
            string branch = "unknown";
            if (experimental)
                branch = "exp_build";
            else
                branch = "stable";
            client.DownloadStringAsync(new Uri(string.Format("{0}/ver.php?core={1}", serverURL, branch)));

            mf.text = string.Format("<color=orange>Mods folder:</color> {0}", ModsFolder);
            MainMenuPath();
            modUpdates.text = string.Empty;
            mainMenuInfo.transform.SetParent(ModUI.GetCanvas().transform, false);
        }

        private void VersionCheckCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            Text info = mainMenuInfo.transform.GetChild(0).gameObject.GetComponent<Text>();
            try
            {
                if (e.Error != null)
                    throw new Exception(e.Error.Message);

                string[] result = e.Result.Split('|');
                if (result[0] == "error")
                {
                    switch (result[1])
                    {
                        case "0":
                            throw new Exception("Unknown branch");
                        case "1":
                            throw new Exception("Database connection error");
                        default:
                            throw new Exception("Unknown error");
                    }

                }
                else if (result[0] == "ok")
                {
                    if (result[1].Trim().Length > 8)
                        throw new Exception("Parse Error, please report that problem!");
                    int i;
                    if (experimental)
                        i = expBuild.CompareTo(result[1].Trim());
                    else
                        i = MSCLoader_Ver.CompareTo(result[1].Trim());
                    if (i != 0)
                        if (experimental)
                            info.text = string.Format("MSCLoader <color=cyan>v{0}</color> is ready! [<color=magenta>Experimental</color> <color=lime>build {1}</color>] (<color=orange>New build available: <b>{2}</b></color>)", MSCLoader_Ver, expBuild, result[1]);
                        else
                            info.text = string.Format("MSCLoader <color=cyan>v{0}</color> is ready! (<color=orange>New version available: <b>v{1}</b></color>)", MSCLoader_Ver, result[1].Trim());
                    else if (i == 0)
                        if (experimental)
                            info.text = string.Format("MSCLoader <color=cyan>v{0}</color> is ready! [<color=magenta>Experimental</color> <color=lime>build {1}</color>]", MSCLoader_Ver, expBuild);
                        else
                            info.text = string.Format("MSCLoader <color=cyan>v{0}</color> is ready! (<color=lime>Up to date</color>)", MSCLoader_Ver);
                }
                else
                {
                    System.Console.WriteLine("Unknown: " + result[0]);
                    throw new Exception("Unknown server response.");
                }
            }
            catch (Exception ex)
            {
                ModConsole.Error(string.Format("Check for new version failed with error: {0}", ex.Message));
                if (devMode)
                    ModConsole.Error(ex.ToString());
                System.Console.WriteLine(ex);
                if (experimental)
                    info.text = string.Format("MSCLoader <color=cyan>v{0}</color> is ready! [<color=magenta>Experimental</color> <color=lime>build {1}</color>]", MSCLoader_Ver, expBuild);
                else
                    info.text = string.Format("MSCLoader <color=cyan>v{0}</color> is ready!", MSCLoader_Ver);

            }
            if (devMode)
                info.text += " [<color=red><b>Dev Mode!</b></color>]";
        }

        IEnumerator NewGameMods()
        {
            loading.transform.GetChild(2).GetComponent<Text>().text = string.Format("MSCLoader <color=green>v{0}</color>", MSCLoader_Ver);
            ModConsole.Print("Resetting mods...");
            loading.SetActive(true);
            loading.transform.GetChild(3).GetComponent<Slider>().minValue = 1;
            loading.transform.GetChild(3).GetComponent<Slider>().maxValue = LoadedMods.Count - 2;

            int i = 1;
            foreach (Mod mod in LoadedMods)
            {
                loading.transform.GetChild(0).GetComponent<Text>().text = string.Format("<color=red>Resetting mods: <color=orange><b>{0}</b></color> of <color=orange><b>{1}</b></color>. <b>Do not skip intro yet!...</b></color>", i, LoadedMods.Count - 2);
                loading.transform.GetChild(3).GetComponent<Slider>().value = i;
                loading.transform.GetChild(3).GetChild(1).GetChild(0).GetComponent<Image>().color = Color.red;
                if (mod.ID.StartsWith("MSCLoader_"))
                    continue;
                i++;
                loading.transform.GetChild(1).GetComponent<Text>().text = mod.Name;
                yield return null;
                try
                {
                    mod.OnNewGame();
                }
                catch (Exception e)
                {
                    StackFrame frame = new StackTrace(e, true).GetFrame(0);

                    string errorDetails = string.Format("{2}<b>Details: </b>{0} in <b>{1}</b>", e.Message, frame.GetMethod(), Environment.NewLine);
                    ModConsole.Error(string.Format("Mod <b>{0}</b> throw an error!{1}", mod.ID, errorDetails));
                    if (devMode)
                        ModConsole.Error(e.ToString());
                    System.Console.WriteLine(e);
                }

            }
            loading.transform.GetChild(0).GetComponent<Text>().text = string.Format("Resetting Done! You can skip intro now!");
            yield return new WaitForSeconds(1f);
            loading.SetActive(false);
            IsModsDoneResetting = true;
            ModConsole.Print("Resetting done!");
            IsModsResetting = false;
        }

        IEnumerator LoadMods()
        {
            Mod[] mods = LoadedMods.Where(x => !x.isDisabled).ToArray();
            loading.transform.GetChild(2).GetComponent<Text>().text = string.Format("MSCLoader <color=green>v{0}</color>", MSCLoader_Ver);
            loading.transform.GetChild(0).GetComponent<Text>().text = string.Format("Loading mods. Please wait...");
            loading.transform.GetChild(1).GetComponent<Text>().text = string.Empty;
            loading.transform.GetChild(3).gameObject.SetActive(false);
            loading.transform.SetAsLastSibling(); //Always on top

            while (GameObject.Find("PLAYER/Pivot/AnimPivot/Camera/FPSCamera") == null)
                yield return null;
            ModConsole.Print("Loading mods...");
            ModConsole.Print("<color=#505050ff>");
            loading.SetActive(true);
            yield return null;
            Stopwatch s = new Stopwatch();
            s.Start();
            for (int i = 0; i < mods.Length; i++)
            {
                try
                {
                    mods[i].OnLoad();
                }
                catch (Exception e)
                {
                    string errorDetails = string.Format("{2}<b>Details: </b>{0} in <b>{1}</b>", e.Message, new StackTrace(e, true).GetFrame(0).GetMethod(), Environment.NewLine);
                    ModConsole.Error(string.Format("Mod <b>{0}</b> throw an error!{1}", mods[i].ID, errorDetails));
                    if (devMode)
                        ModConsole.Error(e.ToString());
                    System.Console.WriteLine(e);
                }
            }
            ModSettings_menu.LoadBinds();
            if (SecondPassMods.Count() > 0)
            {
                ModConsole.Print("Loading mods (second pass)...");
                loading.transform.GetChild(0).GetComponent<Text>().text = string.Format("Loading mods (second pass). Please wait...");
                yield return null;
                for (int j = 0; j < SecondPassMods.Length; j++)
                {
                    if (SecondPassMods[j].isDisabled)
                        continue;
                    try
                    {
                        SecondPassMods[j].SecondPassOnLoad();
                    }
                    catch (Exception e)
                    {
                        string errorDetails = string.Format("{2}<b>Details: </b>{0} in <b>{1}</b>", e.Message, new StackTrace(e, true).GetFrame(0).GetMethod(), Environment.NewLine);
                        ModConsole.Error(string.Format("Mod <b>{0}</b> throw an error!{1}", SecondPassMods[j].ID, errorDetails));
                        if (devMode)
                            ModConsole.Error(e.ToString());
                        System.Console.WriteLine(e);
                    }
                }
            }
            s.Stop();
            GameObject.Find("ITEMS").FsmInject("Save game", SaveMods);
            ModConsole.Print("</color>");
            allModsLoaded = true;
            loading.SetActive(false);
            ModConsole.Print(string.Format("Loading mods completed in {0}ms!", s.ElapsedMilliseconds));
        }

        IEnumerator LoadModsAsync()
        {
            Mod[] mods = LoadedMods.Where(x => !x.isDisabled).ToArray();

            loading.transform.GetChild(3).gameObject.SetActive(true);
            loading.transform.SetAsLastSibling(); //Always on top
            Slider progressBar = loading.transform.GetChild(3).GetComponent<Slider>();
            while (GameObject.Find("PLAYER/Pivot/AnimPivot/Camera/FPSCamera") == null) 
                yield return null;
            loading.transform.GetChild(2).GetComponent<Text>().text = string.Format("MSCLoader <color=green>v{0}</color>", MSCLoader_Ver);
            ModConsole.Print("Loading mods...");

            ModConsole.Print("<color=#505050ff>");
            loading.SetActive(true);
            progressBar.minValue = 1;
            progressBar.maxValue = mods.Length - 2;
            loading.transform.GetChild(3).GetChild(1).GetChild(0).GetComponent<Image>().color = new Color32(0, 113, 0, 255);
            Stopwatch s = new Stopwatch();
            s.Start();
            for (int i = 0; i < mods.Length; i++)
            {
                if (mods[i].ID.StartsWith("MSCLoader_"))
                {
                    mods[i].OnLoad();
                    continue;
                }
                loading.transform.GetChild(0).GetComponent<Text>().text = string.Format("Loading mods: <color=orage><b>{0}</b></color> of <color=orage><b>{1}</b></color>. Please wait...", i - 1, mods.Length - 2);
                progressBar.value = i-1;
                loading.transform.GetChild(1).GetComponent<Text>().text = mods[i].Name;
                try
                {
                    mods[i].OnLoad();                  
                }
                catch (Exception e)
                {

                    string errorDetails = string.Format("{2}<b>Details: </b>{0} in <b>{1}</b>", e.Message, new StackTrace(e, true).GetFrame(0).GetMethod(), Environment.NewLine);
                    ModConsole.Error(string.Format("Mod <b>{0}</b> throw an error!{1}", mods[i].ID, errorDetails));
                    if (devMode)
                        ModConsole.Error(e.ToString());
                    System.Console.WriteLine(e);
                }
                yield return null;

            }
            ModSettings_menu.LoadBinds();
            if (SecondPassMods.Count() > 0)
            {
                progressBar.value = 1;
                ModConsole.Print("Loading mods (second pass)...");
                progressBar.maxValue = SecondPassMods.Count();                
                for (int j = 0; j < SecondPassMods.Length; j++)
                {
                    if (SecondPassMods[j].isDisabled)
                        continue;
                    loading.transform.GetChild(0).GetComponent<Text>().text = string.Format("Loading mods (second pass): <color=orage><b>{0}</b></color> of <color=orage><b>{1}</b></color>. Please wait...", j+1, SecondPassMods.Count());
                    progressBar.value = j+1;
                    loading.transform.GetChild(1).GetComponent<Text>().text = SecondPassMods[j].Name;
                    try
                    {
                        SecondPassMods[j].SecondPassOnLoad();
                    }
                    catch (Exception e)
                    {
                        string errorDetails = string.Format("{2}<b>Details: </b>{0} in <b>{1}</b>", e.Message, new StackTrace(e, true).GetFrame(0).GetMethod(), Environment.NewLine);
                        ModConsole.Error(string.Format("Mod <b>{0}</b> throw an error!{1}", SecondPassMods[j].ID, errorDetails));
                        if (devMode)
                            ModConsole.Error(e.ToString());
                        System.Console.WriteLine(e);
                    }
                    yield return null;

                }
            }
            s.Stop();
            GameObject.Find("ITEMS").FsmInject("Save game", SaveMods);
            ModConsole.Print("</color>");
            allModsLoaded = true;
            loading.SetActive(false);
            ModConsole.Print(string.Format("Loading mods completed in {0}ms!", s.ElapsedMilliseconds));

        }

        private static bool wasSaving = false;
        private void SaveMods()
        {
            saveErrors = new List<string>();
            wasSaving = true;
            for (int i = 0; i < OnSaveMods.Length; i++)
            {
                try
                {
                    if (!OnSaveMods[i].isDisabled)
                        OnSaveMods[i].OnSave();
                }
                catch (Exception e)
                {
                    string errorDetails = string.Format("{2}<b>Details: </b>{0} in <b>{1}</b>", e.Message, new StackTrace(e, true).GetFrame(0).GetMethod(), Environment.NewLine);
                    saveErrors.Add(string.Format("Mod <b>{0}</b> throw an error!{1}", OnSaveMods[i].ID, errorDetails));
                    if (devMode)
                        saveErrors.Add(e.ToString());
                    System.Console.WriteLine(e);
                }
            }
        }

        private void PreLoadMods()
        {
            // Load .dll files
            string[] files = Directory.GetFiles(ModsFolder);
            for (int i = 0; i < files.Length; i++)
            {
                if (files[i].EndsWith(".dll"))
                {
                    LoadDLL(files[i]);
                }
            }
            SecondPassMods = LoadedMods.Where(x => x.SecondPass).ToArray();
            OnGUImods = LoadedMods.Where(x => x.GetType().GetMethod("OnGUI").GetMethodBody().GetILAsByteArray().Length > 2).ToArray();
            UpdateMods = LoadedMods.Where(x => x.GetType().GetMethod("Update").GetMethodBody().GetILAsByteArray().Length > 2).ToArray();
            FixedUpdateMods = LoadedMods.Where(x => x.GetType().GetMethod("FixedUpdate").GetMethodBody().GetILAsByteArray().Length > 2).ToArray();
            OnSaveMods = LoadedMods.Where(x => x.GetType().GetMethod("OnSave").GetMethodBody().GetILAsByteArray().Length > 2).ToArray();
            
            //cleanup files if not in dev mode
            if (!devMode)
            {

                string cleanupLast = Path.Combine(SettingsFolder, @"MSCLoader_Settings\lastCleanupCheck");
                if (File.Exists(cleanupLast))
                {
                    string lastCheckS = File.ReadAllText(cleanupLast);
                    DateTime.TryParse(lastCheckS, out DateTime lastCheck);
                    if ((DateTime.Now - lastCheck).TotalDays >= 14 || (DateTime.Now - lastCheck).TotalDays < 0)
                    {
                        bool found = false;
                        List<string> cleanupList = new List<string>();
                        foreach (string dir in Directory.GetDirectories(AssetsFolder))
                        {
                            if (!LoadedMods.Exists(x => x.ID == new DirectoryInfo(dir).Name))
                            {
                                found = true;
                                cleanupList.Add(new DirectoryInfo(dir).Name);
                            }
                        }
                        if (found)
                            ModUI.ShowYesNoMessage($"There are unused mod files/assets that can be cleaned up.{Environment.NewLine}{Environment.NewLine}List of unused mod files:{Environment.NewLine}<color=aqua>{string.Join(", ", cleanupList.ToArray())}</color>{Environment.NewLine}Do you want to clean them up?", "Unused files found", CleanupFolders);
                        File.WriteAllText(cleanupLast, DateTime.Now.ToString());
                    }
                }
                else
                {
                    File.WriteAllText(cleanupLast, DateTime.Now.ToString());
                }

            }

        }
        void CleanupFolders()
        {
            foreach (string dir in Directory.GetDirectories(SettingsFolder))
            {
                if (!LoadedMods.Exists(x => x.ID == new DirectoryInfo(dir).Name))
                {
                    try
                    {
                        Directory.Delete(dir, true);
                    }
                    catch (Exception ex)
                    {
                        ModConsole.Error(string.Format("{0} (corrupted file?)", ex.Message));
                    }
                }
            }
            foreach (string dir in Directory.GetDirectories(AssetsFolder))
            {
                if (!LoadedMods.Exists(x => x.ID == new DirectoryInfo(dir).Name))
                {
                    try
                    {
                        Directory.Delete(dir, true);
                    }
                    catch (Exception ex)
                    {
                        ModConsole.Error(string.Format("{0} (corrupted file?)", ex.Message));
                    }
                }
            }
        }
        private void LoadModsSettings()
        {
            for (int i = 0; i < LoadedMods.Count; i++)
            {
                if (LoadedMods[i].ID.StartsWith("MSCLoader_"))
                    continue;
                try
                {
                    LoadedMods[i].ModSettings();
                }
                catch (Exception e)
                {
                    ModConsole.Error(string.Format("Settings error for mod <b>{0}</b>{2}<b>Details:</b> {1}", LoadedMods[i].ID, e.Message, Environment.NewLine));
                    if (devMode)
                        ModConsole.Error(e.ToString());
                    System.Console.WriteLine(e);
                }
            }
            ModSettings_menu.LoadSettings();
        }

        private void LoadDLL(string file)
        {
            try
            {
                Assembly asm = Assembly.LoadFrom(file);
                bool isMod = false;

                AssemblyName[] list = asm.GetReferencedAssemblies();
                if (File.ReadAllText(file).Contains("RegistryKey") || File.ReadAllText(file).Contains("Steamworks"))
                    throw new FileLoadException();

                //Warn about wrong .net target, source of some mod crashes.
                if (!asm.ImageRuntimeVersion.Equals(Assembly.GetExecutingAssembly().ImageRuntimeVersion))
                    ModConsole.Warning(string.Format("File <b>{0}</b> is targeting runtime version <b>{1}</b> which is different that current running version <b>{2}</b>. This may cause unexpected behaviours, check your target assembly.", Path.GetFileName(file), asm.ImageRuntimeVersion, Assembly.GetExecutingAssembly().ImageRuntimeVersion));

                // Look through all public classes                
                foreach (Type type in asm.GetTypes())
                {
                    string msVer = null;
                    if (typeof(Mod).IsAssignableFrom(type))
                    {
                        for (int i = 0; i < list.Length; i++)
                        {
                            if (list[i].Name == "Assembly-CSharp-firstpass")
                            {
                                if (File.ReadAllText(file).Contains("Steamworks") || File.ReadAllText(file).Contains("GetSteamID"))
                                    throw new Exception("Targeting forbidden reference");
                            }
                            if (list[i].Name == "MSCLoader")
                            {
                                string[] verparse = list[i].Version.ToString().Split('.');
                                if (list[i].Version.ToString() == "1.0.0.0")
                                    msVer = "0.1";
                                else
                                {
                                    if (verparse[2] == "0")
                                        msVer = string.Format("{0}.{1}", verparse[0], verparse[1]);
                                    else
                                        msVer = string.Format("{0}.{1}.{2}", verparse[0], verparse[1], verparse[2]);
                                }
                            }
                        }
                        isMod = true;
                        LoadMod((Mod)Activator.CreateInstance(type), msVer, file);
                        break;
                    }
                    else
                    {
                        isMod = false;
                    }
                }
                if (!isMod)
                {
                    ModConsole.Error($"<b>{Path.GetFileName(file)}</b> - doesn't look like a mod or missing Mod subclass!{Environment.NewLine}<b>Details:</b> File loaded correctly, but failed to find Mod methods.{Environment.NewLine}If this is a reference put this file into \"<b>References</b>\" folder.");
                    InvalidMods.Add(Path.GetFileName(file));
                }
            }
            catch (Exception e)
            {
                ModConsole.Error($"<b>{Path.GetFileName(file)}</b> - doesn't look like a mod, remove this file from mods folder!{Environment.NewLine}<b>Details:</b> {e.GetFullMessage()}{Environment.NewLine}");
                
                if (devMode)
                    ModConsole.Error(e.ToString());
                System.Console.WriteLine(e);
                InvalidMods.Add(Path.GetFileName(file));
            }

        }

        private void LoadMod(Mod mod, string msver, string fname = null)
        {
            // Check if mod already exists
            if (!LoadedMods.Contains(mod))
            {
                // Create config folder
                if (!Directory.Exists(Path.Combine(SettingsFolder, mod.ID)))
                {
                    Directory.CreateDirectory(Path.Combine(SettingsFolder, mod.ID));
                }
                if (mod.UseAssetsFolder)
                {
                    if (!Directory.Exists(Path.Combine(AssetsFolder, mod.ID)))
                    {
                        Directory.CreateDirectory(Path.Combine(AssetsFolder, mod.ID));
                    }
                }
                mod.compiledVersion = msver;
                mod.fileName = fname;
                LoadedMods.Add(mod);
                try
                {
                    if (mod.LoadInMenu && mod.fileName == null)
                    {
                        mod.OnMenuLoad();
                    }
                }
                catch (Exception e)
                {
                    StackFrame frame = new StackTrace(e, true).GetFrame(0);

                    string errorDetails = string.Format("{2}<b>Details: </b>{0} in <b>{1}</b>", e.Message, frame.GetMethod(), Environment.NewLine);
                    ModConsole.Error(string.Format("Mod <b>{0}</b> throw an error!{1}", mod.ID, errorDetails));
                    if (devMode)
                        ModConsole.Error(e.ToString());
                    System.Console.WriteLine(e);
                }
                if (File.Exists(GetMetadataFolder(string.Format("{0}.json", mod.ID))))
                {
                    string serializedData = File.ReadAllText(GetMetadataFolder(string.Format("{0}.json", mod.ID)));
                    mod.metadata = JsonConvert.DeserializeObject<ModsManifest>(serializedData);
                }
            }
            else
            {
                ModConsole.Error(string.Format("<color=orange><b>Mod already loaded (or duplicated ID):</b></color><color=red><b>{0}</b></color>", mod.ID));
            }
        }


        internal void Mod_OnGUI()
        {
            GUI.skin = guiskin;
            for (int i = 0; i < OnGUImods.Length; i++)
            {
                if (OnGUImods[i].isDisabled)
                    continue;
                try
                {
                    if (allModsLoaded || OnGUImods[i].LoadInMenu)
                        OnGUImods[i].OnGUI();
                }
                catch (Exception e)
                {
                    ModExceptionHandler(e, OnGUImods[i]);
                }
            }
        }


        internal void Mod_Update()
        {
            for (int i = 0; i < UpdateMods.Length; i++)
            {
                if (UpdateMods[i].isDisabled)
                    continue;
                try
                {
                    if (allModsLoaded || UpdateMods[i].LoadInMenu)
                        UpdateMods[i].Update();
                }
                catch (Exception e)
                {
                    ModExceptionHandler(e, UpdateMods[i]);
                }
            }
        }

        internal void Mod_FixedUpdate()
        {
            for (int i = 0; i < FixedUpdateMods.Length; i++)
            {
                if (FixedUpdateMods[i].isDisabled)
                    continue;
                try
                {
                    if (allModsLoaded || FixedUpdateMods[i].LoadInMenu)
                        FixedUpdateMods[i].FixedUpdate();
                }
                catch (Exception e)
                {
                    ModExceptionHandler(e, FixedUpdateMods[i]);
                }
            }
        }

        void ModExceptionHandler(Exception e, Mod mod)
        {
            if (LogAllErrors)
            {
                string errorDetails = string.Format("{2}<b>Details: </b>{0} in <b>{1}</b>", e.Message, new StackTrace(e, true).GetFrame(0).GetMethod(), Environment.NewLine);
                ModConsole.Error(string.Format("Mod <b>{0}</b> throw an error!{1}", mod.ID, errorDetails));
            }
            System.Console.WriteLine(e);
            if (allModsLoaded && fullyLoaded)
                mod.modErrors++;
            if (devMode)
            {
                if (mod.modErrors == 30)
                {
                    ModConsole.Error(string.Format("Mod <b>{0}</b> throws <b>too many errors</b>! Last error: ", mod.ID));
                    ModConsole.Error(e.ToString());
                    if ((bool)ModSettings_menu.dm_disabler.GetValue())
                    {
                        mod.isDisabled = true;
                        ModConsole.Warning(string.Format("[DevMode] Mod <b>{0}</b> has been disabled!", mod.ID));
                    }
                    else
                    {
                        ModConsole.Warning(string.Format("[DevMode] Mod <b>{0}</b> is still running!", mod.ID));
                    }
                }

            }
            else
            {
                if (mod.modErrors >= 30)
                {
                    mod.isDisabled = true;
                    ModConsole.Error(string.Format("Mod <b>{0}</b> has been <b>disabled!</b> Because it thrown too many errors!{1}Report this problem to mod author.", mod.ID, Environment.NewLine));
                }
            }
        }

        internal static string MurzynskaMatematyka(string rawData)
        {
            using (System.Security.Cryptography.SHA1 sha256 = System.Security.Cryptography.SHA1.Create())
            {
                byte[] bytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(rawData));
                System.Text.StringBuilder builder = new System.Text.StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }
        void StopAskingForMscoSupport()
        {
            if (File.Exists(Path.Combine("", @"mysummercar_Data\Managed\MSCOClient.dll")))
            {
                System.Console.WriteLine($"MSCOClient.dll - {new AccessViolationException().Message}");
                File.Delete(Path.Combine("", @"mysummercar_Data\Managed\MSCOClient.dll"));
                Application.Quit();
            }
        }
    }

    /// <summary>
    /// Exception extension
    /// </summary>
    public static class ExceptionExtensions
    {
        /// <summary>
        /// Get Full Exception messages (including inner exceptions) but without stack trace.
        /// </summary>
        /// <param name="ex">Exception</param>
        /// <returns></returns>
        public static string GetFullMessage(this Exception ex)
        {
            return ex.InnerException == null
                 ? ex.Message
                 : ex.Message + " --> " + ex.InnerException.GetFullMessage();
        }
    }


}
