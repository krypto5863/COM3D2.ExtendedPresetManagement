using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Windows.Forms;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ExtendedPresetManagement
{
	[BepInPlugin("ExtendedPresetManagement", "ExtendedPresetManagement", "1.5.1")]
	[BepInDependency("org.bepinex.plugins.unityinjectorloader", BepInDependency.DependencyFlags.SoftDependency)]
	public class Main : BaseUnityPlugin
	{
		public static Harmony harmony;
		public static Main @this2;
		public static PresetMgr @this3;
		public static CharacterMgr @this4;
		public static PresetButtonCtrl @this5;
		//private static RenamePrompt SavePrompt = new RenamePrompt();
		private static SaveFileDialog SavePrompt = new SaveFileDialog();
		public static string CustomPresetDirectory = null;
		public static string OriginalPresetDirectory = null;
		public static string PreviousPresetDirectory = null;
		public static bool PresetPanelOpen = false;
		public static bool RunOnce = true;
		public static string[] PresetFolders;

		public static bool PMIUIStatus = false;
		private static bool ViewMode;

		private static bool IsAutoSaving;

		internal static ManualLogSource BepLogger;

		private static ConfigEntry<bool> SaveAsDefault;
		private static ConfigEntry<KeyboardShortcut> SaveModifier;
		private static ConfigEntry<bool> AutoSave;
		private static ConfigEntry<bool> AutoSaveText;
		private static ConfigEntry<float> AutoSaveInterval;
		private static ConfigEntry<int> MaximumAutoSaves;

		private static IEnumerator AutoSaver;
		private static CountDownText ScreenText;

		void Awake()
		{
			//We set our patcher so we can call it back and patch dynamically as needed.
			harmony = Harmony.CreateAndPatchAll(typeof(Main));

			BepLogger = Logger;

			try
			{
				harmony.PatchAll(typeof(PMIPatch));
			}
			catch
			{
				Debug.LogWarning("PMI was not patched! Might not be loaded...");
			}

			try
			{
				harmony.PatchAll(typeof(ExPresetPatch));
			}
			catch
			{
				Debug.LogWarning("ExternalPreset was not patched! Might not be loaded...");
			}

			@this2 = this;

			SavePrompt.AddExtension = true;
			SavePrompt.DefaultExt = ".preset";
			SavePrompt.OverwritePrompt = true;
			SavePrompt.ValidateNames = true;
			SavePrompt.Filter = "Preset Files |*.preset";

			SaveAsDefault = Config.Bind("General", "Save As By Default", true, "This denotes whether the save as prompt (save file dialog) is opened by default when you save a preset. Setting it to false will save presets normally unless you hold the Save Modifer key.");

			SaveModifier = Config.Bind("General", "Save Modifier", new KeyboardShortcut(KeyCode.LeftControl), "Holding this key while saving will cause the game to save normally if Save as By Default is true. Otherwise, it will do the opposite.");

			AutoSave = Config.Bind("Autosave", "Use Autosaves", true, "Whether or not the system will autosave.");

			AutoSave.SettingChanged += (obj, arg) =>
			{
				if (!AutoSave.Value)
				{
					if (AutoSaver != null)
					{
						StopCoroutine(AutoSaver);
						AutoSaver = null;
					}

					if (ScreenText != null)
					{
						ScreenText.SetState(false);
					}
				}
				else
				{
					if (SceneManager.GetActiveScene().name.Equals("SceneEdit"))
					{
						AutoSaver = AutoSaveCo();
						StartCoroutine(AutoSaver);

						if (ScreenText == null)
						{
							ScreenText = new CountDownText();
						}
					}
				}
			};

			AutoSaveInterval = Config.Bind("Autosave", "Save Interval", 300f, "The time between auto saves in seconds! The lowest possible value is 60, values below 60 will be ignored and 60 will be used. The save interval is only used after the current save wait interval has finished.");

			MaximumAutoSaves = Config.Bind("Autosave", "Max Number of Autosaves Kept", 50, "When the number of autosaves breaches this limit, we'll delete the oldest presets until we are once again under the limit.");

			AutoSaveText = Config.Bind("Autosave", "Show Autosave Text", true, "Whether the autosave countdown text will appear on screen.");

			SceneManager.sceneLoaded += (s, e) =>
			{
				if (s.name.Equals("SceneEdit"))
				{
					if (AutoSave.Value)
					{
						AutoSaver = AutoSaveCo();
						StartCoroutine(AutoSaver);

						if (ScreenText == null)
						{
							ScreenText = new CountDownText();
						}
					}
				}
				else
				{
					if (AutoSave.Value)
					{

						if (AutoSaver != null)
						{
							StopCoroutine(AutoSaver);
							AutoSaver = null;
						}

						if (ScreenText != null)
						{
							ScreenText.SetState(false);
						}
					}
				}
			};
		}

		void OnGUI()
		{
			if (PresetPanelOpen == true && ViewMode == false)
			{
				if (RunOnce)
				{

					if (Main.OriginalPresetDirectory == null)
					{
						_ = this4.PresetDirectory;
					}

					PresetFolders = Directory.GetDirectories(Main.OriginalPresetDirectory, "*", SearchOption.AllDirectories);

					RunOnce = false;
				}

				MyUI.Start();
			}
			else if (PMIUIStatus)
			{
				if (RunOnce)
				{

					if (Main.OriginalPresetDirectory == null)
					{
						_ = this4.PresetDirectory;
					}
					PresetFolders = Directory.GetDirectories(Main.OriginalPresetDirectory, "*", SearchOption.AllDirectories);

					//RunOnce = false;

					//Debug.Log("Runonce triggered!");
				}

				MyUI.Start(true);
			}
			else
			{
				if (!RunOnce)
				{
					RunOnce = true;
				}
			}
		}

		private static IEnumerator AutoSaveCo()
		{
#if DEBUG
			BepLogger.LogDebug("CoRoutine Engaged...");
#endif

			while (true)
			{
				yield return new WaitForSecondsRealtime(Math.Max(AutoSaveInterval.Value, 60) - 6);

				if (AutoSaveText.Value)
				{
					ScreenText.SetState(true);

					int i = 6;

					while (i-- != 0)
					{
						ScreenText.Text = $"Autosaving in {i + 1} seconds...";

						if (i + 1 > 4)
						{
							ScreenText.Color = Color.green;
						}
						else if (i + 1 > 2)
						{
							ScreenText.Color = Color.yellow;
						}
						else
						{
							ScreenText.Color = Color.red;
						}

						yield return new WaitForSecondsRealtime(1);
					}
				}
				else 
				{
					yield return new WaitForSecondsRealtime(6);
				}

				IsAutoSaving = true;
				this4.PresetSave(this5.m_maid, CharacterMgr.PresetType.All);
				IsAutoSaving = false;

				ScreenText.SetState(false);
			}
		}

		[HarmonyPatch(typeof(PresetMgr), "OpenPresetPanel")]
		[HarmonyPatch(typeof(PresetMgr), "ClosePresetPanel")]
		[HarmonyPostfix]
		static void PresetPanelStatusChanged(PresetMgr __instance)
		{
			PresetPanelOpen = (AccessTools.Field(typeof(PresetMgr), "m_goPresetPanel").GetValue(__instance) as GameObject).activeSelf;

			this3 = __instance;

			//Debug.Log("Preset panel active changed to "+ PresetPanelOpen);
		}

		[HarmonyPatch(typeof(SceneEdit), "FromView")]
		[HarmonyPostfix]
		static void FromView()
		{
			ViewMode = false;
		}
		[HarmonyPatch(typeof(SceneEdit), "ToView")]
		[HarmonyPostfix]
		static void ToView()
		{
			ViewMode = true;
		}

		[HarmonyPatch(typeof(SceneEdit), "OnDestroy")]
		[HarmonyPrefix]
		static void ExitingEditMode()
		{
			PresetPanelOpen = false;
			ViewMode = false;
		}
		[HarmonyPatch(typeof(CharacterMgr), "Awake")]
		[HarmonyPostfix]
		static void InstanceSaver(CharacterMgr __instance)
		{
			this4 = __instance;
		}
		[HarmonyPatch(typeof(PresetButtonCtrl), "Init")]
		[HarmonyPostfix]
		static void InstanceSaver2(PresetButtonCtrl __instance)
		{
			this5 = __instance;
		}

		[HarmonyPatch(typeof(CharacterMgr), "PresetDirectory", MethodType.Getter)]
		[HarmonyPrefix]
		static bool PatchPresetDirectoryGetter(CharacterMgr __instance, ref string __result)
		{
			this4 = __instance;

			if (OriginalPresetDirectory == null)
			{
				OriginalPresetDirectory = Path.Combine(GameMain.Instance.SerializeStorageManager.StoreDirectoryPath, "Preset");
			}

			if (CustomPresetDirectory == null || CustomPresetDirectory == "")
			{
				return true;
			}

			__result = CustomPresetDirectory;
			return false;
		}

		[HarmonyPatch(typeof(CharacterMgr), "PresetSave")]
		[HarmonyTranspiler]
		static IEnumerable<CodeInstruction> CodeTranspiler(IEnumerable<CodeInstruction> instructions)
		{
			var custominstruc = new CodeMatcher(instructions)
			.MatchForward(false,
			new CodeMatch(OpCodes.Ldarg_0),
			new CodeMatch(OpCodes.Call, AccessTools.PropertyGetter(typeof(CharacterMgr), "get_PresetDirectory")),
			new CodeMatch(OpCodes.Stloc_S)
			)
			.Insert(
			new CodeInstruction(OpCodes.Ldloc_S, 5),
			new CodeInstruction(OpCodes.Ldarg_0),
			Transpilers.EmitDelegate<Func<String, CharacterMgr, String>>((str, lthis) =>
			{

				if ((SaveAsDefault.Value ^ SaveModifier.Value.IsPressed()) || IsAutoSaving)
				{
					string FileName = "";

					if (!IsAutoSaving)
					{
						SavePrompt.InitialDirectory = lthis.PresetDirectory;
						SavePrompt.FileName = str;
						SavePrompt.ShowDialog();

						FileName = SavePrompt.FileName;
					}
					else
					{
						if (OriginalPresetDirectory.IsNullOrWhiteSpace())
						{
							FileName = lthis.PresetDirectory + "\\Autosave" + "\\" + str;
						}
						else
						{
							FileName = OriginalPresetDirectory + "\\Autosave" + "\\" + str;
						}

						if (Directory.Exists(Path.GetDirectoryName(FileName)))
						{
							var autosavedFiles = Directory.GetFiles(Path.GetDirectoryName(FileName), "*.preset").ToList();

#if DEBUG
							BepLogger.LogDebug("Fetched files in autosaves");
#endif

							if (autosavedFiles.Count() > MaximumAutoSaves.Value)
							{
#if DEBUG
								BepLogger.LogDebug("Count was higher");
#endif

								autosavedFiles = autosavedFiles.OrderByDescending(file => File.GetCreationTimeUtc(file)).ToList();

#if DEBUG
								BepLogger.LogDebug("done ordering");
#endif

								while (autosavedFiles.Count() > MaximumAutoSaves.Value)
								{
#if DEBUG
									BepLogger.LogDebug("removing " + autosavedFiles.Last());
#endif
									File.Delete(autosavedFiles.Last());

									File.Delete(autosavedFiles.Last() + ".expreset.xml");

									autosavedFiles.Remove(autosavedFiles.Last());
								}
							}
						}
					}

					//BepLogger.LogDebug("Finished Showing Dialog..");

					if (Path.GetDirectoryName(FileName) == "")
					{
						PreviousPresetDirectory = lthis.PresetDirectory;
						//CustomPresetDirectory = "null";
						return null;
					}

					PreviousPresetDirectory = lthis.PresetDirectory;
					CustomPresetDirectory = Path.GetDirectoryName(FileName);

					//BepLogger.LogDebug($"Set path of {lthis.PresetDirectory}, returning result: {Path.GetFileName(SavePrompt.FileName)}");

					return Path.GetFileName(FileName);
				}
				return str;
			}),
			new CodeInstruction(OpCodes.Stloc_S, 5)
			)
			.InstructionEnumeration();

			var custominstruc2 = new CodeMatcher(custominstruc)
			.MatchForward(false,
			new CodeMatch(OpCodes.Ldloc_0),
			new CodeMatch(OpCodes.Ret)
			)
			.Insert(
			Transpilers.EmitDelegate<Action>(() =>
				{
					//Debug.Log("Setting the preset directory back from the save directory.");

					if (PreviousPresetDirectory != OriginalPresetDirectory && PreviousPresetDirectory != null)
					{
						CustomPresetDirectory = PreviousPresetDirectory;
						PreviousPresetDirectory = null;
					}
					else if (PreviousPresetDirectory == OriginalPresetDirectory)
					{
						CustomPresetDirectory = null;
						PreviousPresetDirectory = null;
					}
				})
			)
			.InstructionEnumeration();

			return custominstruc2;
		}
	}
}
