using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ExtendedPresetManagement
{
	internal class CountDownText
	{
		private UILabel ScreenText;
		internal string Text 
		{
			get => ScreenText.text;
			set 
			{
				ScreenText.text = value;
			}
		}

		internal Color Color
		{
			get => ScreenText.color;
			set
			{
				ScreenText.color = value;
			}
		}
		internal CountDownText() 
		{
			var MessageWindow = GameObject.Find("SystemUI Root").GetComponentsInChildren<Transform>(true).First(so => so && so.gameObject && so.name.Equals("SystemDialog")).gameObject;

			var MsgWindowFont = MessageWindow.GetComponentInChildren<UILabel>().trueTypeFont;

			var UIRoot = GameObject.Find("SystemUI Root").GetComponent<UIRoot>();

			ScreenText = NGUITools.AddChild<UILabel>(UIRoot.gameObject);

			var width = UIRoot.GetPixelSizeAdjustment(ScreenText.gameObject) * Screen.width;
			var height = UIRoot.GetPixelSizeAdjustment(ScreenText.gameObject) * Screen.height;

			ScreenText.trueTypeFont = MsgWindowFont;
			ScreenText.transform.localPosition = new Vector3(0, height * 0.45f, 0);
			ScreenText.width = (int)width;
			ScreenText.fontSize = 12;
			ScreenText.effectStyle = UILabel.Effect.Outline;

			ScreenText.gameObject.SetActive(false);
		}
		internal void SetState(bool active) 
		{
			ScreenText.gameObject.SetActive(active);
		}
	}
}
