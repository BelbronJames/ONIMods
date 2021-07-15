﻿/*
 * Copyright 2021 Peter Han
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software
 * and associated documentation files (the "Software"), to deal in the Software without
 * restriction, including without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all copies or
 * substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING
 * BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
 * DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

using HarmonyLib;
using PeterHan.PLib.Actions;
using PeterHan.PLib.AVC;
using PeterHan.PLib.Core;
using PeterHan.PLib.Database;
using PeterHan.PLib.PatchManager;
using System.Collections.Generic;
using UnityEngine;

namespace PeterHan.BulkSettingsChange {
	/// <summary>
	/// Patches which will be applied via annotations for Bulk Settings Change.
	/// 
	/// This code took inspiration from https://github.com/0Mayall/ONIBlueprints/
	/// </summary>
	public sealed class BulkChangePatches : KMod.UserMod2 {
		/// <summary>
		/// The action to bring up the bulk change tool.
		/// </summary>
		internal static PAction BulkChangeAction { get; private set; }

		[PLibMethod(RunAt.BeforeDbInit)]
		internal static void BeforeDbInit() {
			var icon = SpriteRegistry.GetToolIcon();
			Assets.Sprites.Add(icon.name, icon);
		}

		/// <summary>
		/// Logs when the mod is loaded.
		/// </summary>
		public override void OnLoad(Harmony harmony) {
			base.OnLoad(harmony);
			PUtil.InitLibrary();
			new PLocalization().Register();
			new PPatchManager(harmony).RegisterPatchClass(typeof(BulkChangePatches));
			BulkChangeAction = new PActionManager().CreateAction(BulkChangeStrings.ACTION_KEY,
				BulkChangeStrings.ACTION_TITLE, new PKeyBinding(KKeyCode.Q));
			new PVersionCheck().Register(this, new SteamVersionChecker());
		}

		/// <summary>
		/// Applied to PlayerController to load the change settings tool into the available
		/// tool list.
		/// </summary>
		[HarmonyPatch(typeof(PlayerController), "OnPrefabInit")]
		public static class PlayerController_OnPrefabInit_Patch {
			/// <summary>
			/// Applied after OnPrefabInit runs.
			/// </summary>
			/// <param name="__instance">The current instance.</param>
			internal static void Postfix(PlayerController __instance) {
				// Create list so that new tool can be appended at the end
				var interfaceTools = new List<InterfaceTool>(__instance.tools);
				var bulkChangeTool = new GameObject(typeof(BulkChangeTool).Name);
				bulkChangeTool.AddComponent<BulkChangeTool>();
				// Reparent tool to the player controller, then enable/disable to load it
				bulkChangeTool.transform.SetParent(__instance.gameObject.transform);
				bulkChangeTool.SetActive(true);
				bulkChangeTool.SetActive(false);
				PUtil.LogDebug("Created BulkChangeTool");
				// Add tool to tool list
				interfaceTools.Add(bulkChangeTool.GetComponent<InterfaceTool>());
				__instance.tools = interfaceTools.ToArray();
			}
		}

		/// <summary>
		/// Applied to ToolMenu to add the settings change tool to the tool list.
		/// </summary>
		[HarmonyPatch(typeof(ToolMenu), "CreateBasicTools")]
		public static class ToolMenu_CreateBasicTools_Patch {
			/// <summary>
			/// Applied after CreateBasicTools runs.
			/// </summary>
			/// <param name="__instance">The basic tool list.</param>
			internal static void Postfix(ToolMenu __instance) {
				PUtil.LogDebug("Adding BulkChangeTool to basic tools");
				__instance.basicTools.Add(ToolMenu.CreateToolCollection(BulkChangeStrings.
					TOOL_TITLE, BulkChangeStrings.TOOL_ICON_NAME, BulkChangeAction.GetKAction(),
					typeof(BulkChangeTool).Name, BulkChangeStrings.TOOL_DESCRIPTION, false));
			}
		}
	}
}
