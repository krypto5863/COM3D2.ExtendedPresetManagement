using COM3D2.PropMyItem.Plugin;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using UnityEngine;

namespace ExtendedPresetManagement
{
	class PMIPatch
	{
		[HarmonyPatch(typeof(PropMyItem), "Update")]
		[HarmonyTranspiler]
		static IEnumerable<CodeInstruction> CodeTranspiler(IEnumerable<CodeInstruction> instructions)
		{
			var custominstruc = new CodeMatcher(instructions)
			.MatchForward(true,
			new CodeMatch(OpCodes.Ldarg_0),
			new CodeMatch(OpCodes.Ldarg_0),
			new CodeMatch(OpCodes.Ldfld),
			new CodeMatch(OpCodes.Ldc_I4_0),
			new CodeMatch(OpCodes.Ceq),
			new CodeMatch(OpCodes.Stfld),
			new CodeMatch(OpCodes.Ldarg_0),
			new CodeMatch(OpCodes.Ldarg_0),
			new CodeMatch(OpCodes.Ldfld),
			new CodeMatch(OpCodes.Ldc_I4_0),
			new CodeMatch(OpCodes.Ceq),
			new CodeMatch(OpCodes.Stfld),
			new CodeMatch(OpCodes.Br)
			)
			.Insert(
			new CodeInstruction(OpCodes.Ldarg_0),
			new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(PropMyItem), "_isVisible")),
			Transpilers.EmitDelegate<Action<bool>>((vis) => {
				Main.PMIUIStatus = vis;	
			})
			)
			.InstructionEnumeration();

			return custominstruc;
		}
		/*
		[HarmonyPatch(typeof(PropMyItem), "guiSelectedCategory")]
		[HarmonyTranspiler]
		static IEnumerable<CodeInstruction> CodeTranspiler1(IEnumerable<CodeInstruction> instructions, ILGenerator il)
		{

			var custominstruc = new CodeMatcher(instructions, il)
			.MatchForward(true,
			new CodeMatch(OpCodes.Ldarg_0),
			new CodeMatch(OpCodes.Ldfld),
			new CodeMatch(OpCodes.Ldarg_0),
			new CodeMatch(OpCodes.Ldfld),
			new CodeMatch(OpCodes.Callvirt),
			new CodeMatch(OpCodes.Ldfld),
			new CodeMatch(OpCodes.Ldloc_S),
			new CodeMatch(OpCodes.Ldelem_Ref),
			new CodeMatch(OpCodes.Call),
			new CodeMatch(OpCodes.Call),
			new CodeMatch(OpCodes.Brfalse)
			)
			.Advance(1)
			.Insert(
			new CodeInstruction(OpCodes.Ldarg_0),
			Transpilers.EmitDelegate<Action>(() =>
			{
				Debug.Log("Label comes next.");
			}))
			.CreateLabel(out var target)
			.Start()
			//var custominstruc = new CodeMatcher(instructions)
			.MatchForward(false,
			new CodeMatch(OpCodes.Ldarg_0),
			new CodeMatch(OpCodes.Ldfld),
			new CodeMatch(OpCodes.Ldarg_0),
			new CodeMatch(OpCodes.Ldfld),
			new CodeMatch(OpCodes.Callvirt),
			new CodeMatch(OpCodes.Ldfld),
			new CodeMatch(OpCodes.Ldloc_S),
			new CodeMatch(OpCodes.Ldelem_Ref),
			new CodeMatch(OpCodes.Call),
			new CodeMatch(OpCodes.Call),
			new CodeMatch(OpCodes.Brfalse)
			)
			.Insert(
			new CodeInstruction(OpCodes.Ldarg_0),
			Transpilers.EmitDelegate<Action>(() =>
			{

				Debug.Log("Delegate is about to be emitted");

			}),
			new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Main), "RunOnce")),
			new CodeInstruction(OpCodes.Brtrue, target)
			)
			.InstructionEnumeration();

			return custominstruc;
		}*/
	}
}
