using BepInEx;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Windows.Forms;
using UnityEngine;

namespace ExtendedPresetManagement
{
	[BepInPlugin("ExtendedPresetManagement", "ExtendedPresetManagement", "1.0.0.0")]
	public class Main : BaseUnityPlugin
	{
		public static Harmony harmony;
		public static Main @this2;
		private static RenamePrompt SavePrompt = new RenamePrompt();

		void Awake()
		{
			//We set our patcher so we can call it back and patch dynamically as needed.
			harmony = Harmony.CreateAndPatchAll(typeof(Main));
			@this2 = this;
		}

		[HarmonyPatch(typeof(CharacterMgr), "PresetSave")]
		[HarmonyTranspiler]
		static IEnumerable<CodeInstruction> CodeTranspiler(IEnumerable<CodeInstruction> instructions)
		{

			var custominstruc = new CodeMatcher(instructions)
			.MatchForward(false,
			new CodeMatch(OpCodes.Ldloc_S),
			new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(UTY), "FileNameEscape")),
			new CodeMatch(OpCodes.Stloc_S))
			.Insert(
			new CodeInstruction(OpCodes.Ldloc_S, 5),
			new CodeInstruction(OpCodes.Ldarg_0),
			Transpilers.EmitDelegate<Func<String,CharacterMgr, String>>((str, lthis) => {

				//Debug.Log("Provided Dir is " + lthis.PresetDirectory);

				SavePrompt.textBox1.Text = str;

				string result = null;

				do
				{
					SavePrompt.ShowDialog();

					if (SavePrompt.Result != null)
					{
						if (File.Exists(lthis.PresetDirectory + "//" + SavePrompt.Result + ".preset"))
						{
							DialogResult res = MessageBox.Show("We have found that a preset by this name already exists. Pressing OK will overwrite the file.", "A file by this name exists!", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning);

							if (res == DialogResult.Cancel)
							{
								continue;
							}
						}

						result = SavePrompt.Result;
					}
					else 
					{
						result = str;
					}
				} while (result == null || result == "");

				return result;
			}),
			new CodeInstruction(OpCodes.Stloc_S, 5)
			)
			.InstructionEnumeration();

			return custominstruc;
		}
	}
}
