using CM3D2.ExternalPreset.Managed;
using CM3D2.ExternalSaveData.Managed;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace ExtendedPresetManagement
{
	class ExPresetPatch
	{

		[HarmonyPatch(typeof(ExPreset), "FindEXPresetFilePath")]
		[HarmonyPrefix]
		private static bool FindEXPresetFilePath(ref string __0, ref string __result) 
		{

			string path = Main.this4.PresetDirectory + "\\" + __0;

			if (File.Exists(path)) 
			{
				__result = path;
				return false;
			}

			__result = null;
			return false;
		}
		[HarmonyPatch(typeof(ExPreset), "Delete")]
		[HarmonyPrefix]
		private static bool Delete(ref CharacterMgr.Preset __0)
		{

			string path = Main.this4.PresetDirectory + "\\" + __0.strFileName + ".expreset.xml";

			if (File.Exists(path))
			{
				File.Delete(path);
			}

			return false;
		}
		[HarmonyPatch(typeof(ExPreset), "PluginsSave")]
		[HarmonyPrefix]
		static bool PluginsSave(ref Maid __0, ref string __1, ref CharacterMgr.PresetType __2)
		{
			if (__2 == CharacterMgr.PresetType.Wear) return false;
			var xml = new XmlDocument();
			bool nodeExist = false;
			var rootNode = xml.AppendChild(xml.CreateElement("plugins"));
			var exsaveNodeNameMap = AccessTools.Field(typeof(ExPreset), "exsaveNodeNameMap").GetValue(null) as HashSet<string>;
			foreach (string pluginName in exsaveNodeNameMap)
			{
				var node = xml.CreateElement("plugin");
				if (ExSaveData.TryGetXml(__0, pluginName, node))
				{
					rootNode.AppendChild(node);
					nodeExist = true;
				}
			}

			if (!nodeExist)
			{
				return false;
			}
			xml.Save(Main.this4.PresetDirectory + "\\" + __1 + ".expreset.xml");
			return false;
		}
	}
}
