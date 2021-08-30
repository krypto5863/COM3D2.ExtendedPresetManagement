using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ExtendedPresetManagement
{
	internal class PresetInfoPanel
	{
		GameObject InfoPanel;
		internal PresetInfoPanel() 
		{
			var MessageWindow = GameObject.Find("SystemUI Root").GetComponentsInChildren<Transform>(true).First(so => so && so.gameObject && so.name.Equals("SystemDialog")).gameObject;

			var MsgWindowFont = MessageWindow.GetComponentInChildren<UILabel>().trueTypeFont;

			var SystemUIRoot = GameObject.Find("SystemUI Root").GetComponent<UIRoot>();

			Main.BepLogger.LogDebug("Got UI Root");

			var CustomPartsWindow = GameObject.Find("CustomPartsWindow").GetComponentInChildren<UIPanel>();

			Main.BepLogger.LogDebug("Got Parts Window");

			InfoPanel = NGUITools.AddChild(SystemUIRoot.gameObject, CustomPartsWindow.gameObject);
			InfoPanel.name = "PresetInfoPanel";
			InfoPanel.transform.localPosition = new Vector3(0,0,0);

			Main.BepLogger.LogDebug("Made panel clone");

			Main.BepLogger.LogDebug("Changed label.");

			var width = UIRoot.GetPixelSizeAdjustment(InfoPanel) * Screen.width;
			var height = UIRoot.GetPixelSizeAdjustment(InfoPanel) * Screen.height;

			var ContentArea = InfoPanel.GetComponentsInChildren<Transform>(true).First(go => go.name.Equals("ContentParent"));

			Main.BepLogger.LogDebug("Got content area");

			foreach (Transform f in ContentArea.GetComponentsInChildren<Transform>(true)) 
			{
				if (!f.name.Equals("ContentParent")) 
				{
					UnityEngine.Object.Destroy(f.gameObject);
				}	
			}

			Main.BepLogger.LogDebug("Cleared");

			var headLabel = InfoPanel.GetComponentInChildren<UILabel>();
			headLabel.text = "Preset Info";

			InfoPanel.SetActive(true);
			InfoPanel.GetComponent<UIPanel>().alpha = 1;

		}
	}
}
