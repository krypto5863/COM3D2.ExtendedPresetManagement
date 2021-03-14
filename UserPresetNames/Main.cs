using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
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
	[BepInPlugin("ExtendedPresetManagement", "ExtendedPresetManagement", "1.0.0.0")]
	[BepInDependency("org.bepinex.plugins.unityinjectorloader", BepInDependency.DependencyFlags.SoftDependency)]
	public class Main : BaseUnityPlugin
	{
		public static Harmony harmony;
		public static Main @this2;
		public static PresetMgr @this3;
		public static CharacterMgr @this4;
		//private static RenamePrompt SavePrompt = new RenamePrompt();
		private static SaveFileDialog SavePrompt = new SaveFileDialog();
		public static string CustomPresetDirectory = null;
		public static string OriginalPresetDirectory = null;
		public static string PreviousPresetDirectory = null;
		public static bool PresetPanelOpen = false;
		public static bool RunOnce = true;
		public static string[] PresetFolders; 
		private static ConfigEntry<bool> SaveAsDefault;
		public static bool PMIUIStatus = false;

		void Awake()
		{
			//We set our patcher so we can call it back and patch dynamically as needed.
			harmony = Harmony.CreateAndPatchAll(typeof(Main));

			try 
			{
				harmony.PatchAll(typeof(PMIPatch));
			}
			catch {
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

			SaveAsDefault = Config.Bind("General", "Save As By Default", true, "This denotes whether the save as prompt (save file dialog) is opened by default when you save a preset. Setting it to false will save presets normally unless you hold CTRL while saving.");
		}

		void OnGUI() 
		{
			if (PresetPanelOpen == true)
			{
				if (RunOnce)
				{

					if (Main.OriginalPresetDirectory == null)
					{
						_ = this4.PresetDirectory;
					}
					PresetFolders = Directory.GetDirectories(Main.OriginalPresetDirectory);

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
					PresetFolders = Directory.GetDirectories(Main.OriginalPresetDirectory);

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

		[HarmonyPatch(typeof(PresetMgr), "OpenPresetPanel")]
		[HarmonyPatch(typeof(PresetMgr), "ClosePresetPanel")]
		[HarmonyPostfix]
		static void PresetPanelStatusChanged(ref PresetMgr __instance)
		{
			PresetPanelOpen = (AccessTools.Field(typeof(PresetMgr), "m_goPresetPanel").GetValue(__instance) as GameObject).activeSelf;

			this3 = __instance;

			//Debug.Log("Preset panel active changed to "+ PresetPanelOpen);
		}

		[HarmonyPatch(typeof(SceneEdit), "OnDestroy")]
		[HarmonyPostfix]
		static void ExitingEditMode()
		{
			PresetPanelOpen = false;
		}
		[HarmonyPatch(typeof(CharacterMgr), "Awake")]
		[HarmonyPostfix]
		static void InstanceSaver(ref CharacterMgr __instance) 
		{
			this4 = __instance;
		}

[HarmonyPatch(typeof(CharacterMgr), "PresetDirectory", MethodType.Getter)]
		[HarmonyPrefix]
		static bool PatchPresetDirectoryGetter (ref CharacterMgr __instance, ref string __result)
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
			Transpilers.EmitDelegate<Func<String,CharacterMgr, String>>((str, lthis) => {

				if (SaveAsDefault.Value || Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
				{


					SavePrompt.InitialDirectory = lthis.PresetDirectory;
					SavePrompt.FileName = str;
					SavePrompt.ShowDialog();

					//Debug.Log("Finished Showing Dialog..");

					if (Path.GetDirectoryName(SavePrompt.FileName) == "")
					{
						PreviousPresetDirectory = lthis.PresetDirectory;
						//CustomPresetDirectory = "null";
						return null;
					}

					PreviousPresetDirectory = lthis.PresetDirectory;
					CustomPresetDirectory = Path.GetDirectoryName(SavePrompt.FileName);

					//Debug.Log($"Set path of {lthis.PresetDirectory}, returning result: {Path.GetFileName(SavePrompt.FileName)}");

					return Path.GetFileName(SavePrompt.FileName);
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
