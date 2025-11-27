using Kingmaker;
using Kingmaker.Blueprints.Root;
using Kingmaker.Blueprints.Root.Strings;
using Kingmaker.Localization;
using Kingmaker.Settings;
using Kingmaker.UI.Common;
using Kingmaker.UI.MVVM;
using Kingmaker.Code.UI.MVVM.View.ServiceWindows.Journal;
using Kingmaker.Code.UI.MVVM.View.Settings.PC;
using Kingmaker.Code.UI.MVVM.View.Settings.PC.Entities;
using Kingmaker.Code.UI.MVVM.View.Settings.PC.Entities.Decorative;
using Kingmaker.Code.UI.MVVM.VM.Settings;
using Kingmaker.Code.UI.MVVM.VM.Settings.Entities;
using Kingmaker.Code.UI.MVVM.VM.Settings.Entities.Decorative;
using Kingmaker.Code.UI.MVVM.VM.Settings.Entities.Difficulty;
using Kingmaker.UI.Models.SettingsUI;
using Kingmaker.Utility;
using ModMenu.Settings;
using Owlcat.Runtime.UI.Controls.Button;
using Owlcat.Runtime.UI.MVVM;
using Owlcat.Runtime.UI.VirtualListSystem;
using Owlcat.Runtime.UI.VirtualListSystem.ElementSettings;
using System.Reflection.Emit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Kingmaker.UI.Models.SettingsUI.UISettingsManager;
using Object = UnityEngine.Object;
using Kingmaker.Code.UI.MVVM.View.Common.Dropdown;
using Kingmaker.UI.Models.SettingsUI.SettingAssets;
using Kingmaker.Code.UI.MVVM;
using static UnityModManagerNet.UnityModManager;
using Kingmaker.Utility.DotNetExtensions;
using UniRx;

namespace ModMenu.NewTypes
{
  internal class SettingsEntityPatches
  {
    internal static readonly FieldInfo OverrideType =
      AccessTools.Field(typeof(VirtualListLayoutElementSettings), "m_OverrideType");

    /// <summary>
    /// Patch to prevent exceptions on deserializing settings
    /// </summary>
    [HarmonyPatch]
    static class DictionarySettingsProviderPatcher
      {
        [HarmonyTargetMethod]
        static MethodInfo TargetMethod()
        {
          return typeof(DictionarySettingsProvider)
                .GetMethod(nameof(DictionarySettingsProvider.GetValue))
                .MakeGenericMethod(typeof(ModsMenuEntry));
        }

        [HarmonyPrefix]
        public static bool DeserializeSettingEntry(string key, ref ModsMenuEntry __result)
        {
          if (key.Equals(SettingsEntityModMenuEntry.instance.Key))
          {
            __result = ModsMenuEntry.EmptyInstance;
            return false;
          }
          return true;
        }
      }


    /// <summary>
    /// Patch to return the correct view model for <see cref="UISettingsEntityImage"/>
    /// </summary>
    [HarmonyPatch(typeof(SettingsVM))]
    static class SettingsVM_Patch
    {
      [HarmonyPatch(nameof(SettingsVM.GetVMForSettingsItem)), HarmonyPrefix]
      static bool Prefix(
        UISettingsEntityBase uiSettingsEntity, ref VirtualListElementVMBase __result)
      {
        try
        {
          if (uiSettingsEntity is UISettingsEntityImage imageEntity)
          {
            Main.Logger.NativeLog("Returning SettingsEntityImageVM.");
            __result = new SettingsEntityImageVM(imageEntity);
            return false;
          }
          if (uiSettingsEntity is UISettingsEntityButton buttonEntity)
          {
            __result = new SettingsEntityButtonVM(buttonEntity);
            Main.Logger.NativeLog($"Returning SettingsEntityButtonVM. Is null? {__result == null}");
            return false;
          }
          if (uiSettingsEntity is UISettingsEntitySubHeader subHeaderEntity)
          {
            Main.Logger.NativeLog("Returning SettingsEntitySubHeaderVM.");
            __result = new SettingsEntitySubHeaderVM(subHeaderEntity);
            return false;
          }
          if (uiSettingsEntity is UISettingsEntityDropdownButton dropdownButton)
          {
            Main.Logger.NativeLog("Returning SettingsEntityDropdownButtonVM.");
            __result = new SettingsEntityDropdownButtonVM(dropdownButton);
            return false;
          }
          if (uiSettingsEntity is UISettingsEntityDropdownModMenuEntry modMenuDropdown)
          {
            Main.Logger.NativeLog("Returning SettingsEntityDropdownButtonVM.");
            __result = new SettingsEntityDropdownVM(modMenuDropdown, (SettingsEntityDropdownVM.DropdownType)5);
            return false;
          }
        }
        catch (Exception e)
        {
          
          Main.Logger.LogException("SettingsVM.GetVMForSettingsItem", e);
        }
        return true;
      }

      [HarmonyPatch(nameof(SettingsVM.SwitchSettingsScreen)), HarmonyPrefix]
      static bool Prefix(UISettingsManager.SettingsScreen settingsScreen, SettingsVM __instance)
      {
        if (settingsScreen != ModsMenuEntity.SettingsScreenId) return true;
        try
        {
        Main.Logger.NativeLog("Collecting setting entities.");

        __instance.m_SettingEntities.Clear();
        __instance.m_SettingEntities.Add(__instance.AddDisposableAndReturn(SettingsVM.GetVMForSettingsItem(UISettingsEntityDropdownModMenuEntry.instance)));
          if (UISettingsEntityDropdownModMenuEntry.instance.Setting.GetTempValue() == ModsMenuEntry.EmptyInstance)
            return false;
        //__instance.m_SettingEntities.Add(__instance.AddDisposableAndReturn(SettingsVM.GetVMForSettingsItem(separator)));

            //Here should be a toggle for mod disabling, but do we need it?
          SettingsEntitySubHeaderVM? subheader;
          foreach (var uisettingsGroup in ModsMenuEntity.CollectSettingGroups)
          {
            __instance.m_SettingEntities.Add(__instance.AddDisposableAndReturn(new SettingsEntityHeaderVM(uisettingsGroup.Title)));
            subheader = null;
            foreach (UISettingsEntityBase uisettingsEntityBase in uisettingsGroup.VisibleSettingsList)
            {
              if (uisettingsEntityBase is UISettingsEntitySubHeader sub)
              {
                subheader = new SettingsEntitySubHeaderVM(sub);
                __instance.m_SettingEntities.Add(__instance.AddDisposableAndReturn(subheader));
                continue;
              }
              VirtualListElementVMBase element = __instance.AddDisposableAndReturn(SettingsVM.GetVMForSettingsItem(uisettingsEntityBase));
              __instance.m_SettingEntities.Add(element);
              subheader?.SettingsInGroup.Add(element);
            }
          }

        }
        catch (Exception e)
        {
          Main.Logger.LogException("SettingsVM.SwitchSettingsScreen", e);
        }
        return false;
      }

      /*[HarmonyPatch(nameof(SettingsVM.SwitchSettingsScreen)), HarmonyPostfix]
      static void Postfix(UISettingsManager.SettingsScreen settingsScreen, SettingsVM __instance)
      {
        try
        {
          if (settingsScreen != ModsMenuEntity.SettingsScreenId) { return; }
          Main.Logger.NativeLog("Configuring header buttons.");

          // Add all settings in each group to the corresponding expand/collapse button
          SettingsEntityCollapsibleHeaderVM headerVM = null;
          SettingsEntitySubHeaderVM subHeaderVM = null;
          for (int i = 0; i < __instance.m_SettingEntities.Count; i++)
          {
            var entity = __instance.m_SettingEntities[i];
            if (entity is SettingsEntitySubHeaderVM subHeader)
            {
              subHeaderVM = subHeader;
              if (headerVM is not null)
                headerVM.SettingsInGroup.Add(subHeaderVM); // Sub headers are nested in headers
              continue;
            }
            else if (entity is SettingsEntityHeaderVM header)
            {
              headerVM = new SettingsEntityCollapsibleHeaderVM(header.Tittle);
              __instance.m_SettingEntities[i] = headerVM;
              subHeaderVM = null; // Make sure we stop counting sub header entries
              continue;
            }

            if (headerVM is not null)
              headerVM.SettingsInGroup.Add(entity);
            if (subHeaderVM is not null)
              subHeaderVM.SettingsInGroup.Add(entity);
          }
        }
        catch (Exception e)
        {
          Main.Logger.LogException("SettingsVM.SwitchSettingsScreen", e);
        }
      }*/
    }

    /// <summary>
    /// Patch to add new setting type prefabs.
    /// </summary>
    [HarmonyPatch(typeof(SettingsPCView.SettingsViews))]
    static class SettingsViews_Patch
    {
      static readonly MethodInfo CallToInitialize = AccessTools.DeclaredMethod(typeof(VirtualListComponent), nameof(VirtualListComponent.Initialize));

      [HarmonyPatch(nameof(SettingsPCView.SettingsViews.InitializeVirtualList)), HarmonyTranspiler]
      static IEnumerable<CodeInstruction> TranspilerToInitializeNewTemplates(IEnumerable<CodeInstruction> instructions)
      {
        foreach (var instr in instructions)
          if (!instr.Calls(CallToInitialize))
            yield return instr;
          else
          {
            yield return new CodeInstruction(OpCodes.Ldarg_0);
            yield return CodeInstruction.Call((IVirtualListElementTemplate[] original, SettingsPCView.SettingsViews __instance) => InitializeNewVirtualElementTemplates(original, __instance));
            yield return instr;
          }
      }
      static IVirtualListElementTemplate[] InitializeNewVirtualElementTemplates(IVirtualListElementTemplate[]  original, SettingsPCView.SettingsViews __instance)
      {
        try
        {
          Main.Logger.NativeLog("Adding new type prefabs.");

          // Copy the bool settings
          var copyFrom = __instance.m_SettingsEntityBoolViewPrefab.gameObject;
          var imageTemplate = CreateImageTemplate(Object.Instantiate(copyFrom));
          var buttonTemplate =
            CreateButtonTemplate(Object.Instantiate(copyFrom),
            __instance.m_SettingsEntityStatisticsOptOutViewPrefab?.m_GoToStatisticsButton);

          var headerTemplate =
            CreateCollapsibleHeaderTemplate(
              Object.Instantiate(__instance.m_SettingsEntityHeaderViewPrefab.gameObject));
          var subHeaderTemplate = CreateSubHeaderTemplate(Object.Instantiate(headerTemplate.gameObject));

          // Copy dropdown since you know, it seems like close to dropdown button right?
          var dropdownButtonTemplate =
            CreateDropdownButtonTemplate(
              Object.Instantiate(__instance.m_SettingsEntityDropdownViewPrefab.gameObject),
              __instance.m_SettingsEntityStatisticsOptOutViewPrefab?.m_GoToStatisticsButton);

          var dropdownModMenuView = Object.Instantiate(__instance.m_SettingsEntityDropdownViewPrefab.gameObject).GetComponent<SettingsEntityDropdownPCView>();

          original = original.Concat(
            //new VirtualListElementTemplate<SettingsEntityHeaderVM>(__instance.m_SettingsEntityHeaderViewPrefab),
            //new VirtualListElementTemplate<SettingsEntityBoolVM>(__instance.m_SettingsEntityBoolViewPrefab),
            //new VirtualListElementTemplate<SettingsEntityDropdownVM>(__instance.m_SettingsEntityDropdownViewPrefab, 0),
            //new VirtualListElementTemplate<SettingsEntityDropdownVM>(__instance.m_SettingsEntityDropdownViewPrefab, 1),
            //new VirtualListElementTemplate<SettingsEntitySliderVM>(__instance.m_SettingsEntitySliderViewPrefab, 0),
            //new VirtualListElementTemplate<SettingsEntitySliderVM>(__instance.m_SettingsEntitySliderGammaCorrectionViewPrefab, 1),
            //new VirtualListElementTemplate<SettingsEntitySliderVM>(__instance.m_SettingEntityFontSizeViewPrefab, 2),
            //new VirtualListElementTemplate<SettingEntityKeyBindingVM>(__instance.m_SettingEntityKeyBindingViewPrefab),
            //new VirtualListElementTemplate<SettingsEntityDropdownGameDifficultyVM>(__instance.m_SettingsEntityDropdownGameDifficultyViewPrefab, 0),
            //new VirtualListElementTemplate<SettingsEntityStatisticsOptOutVM>(__instance.m_SettingsEntityStatisticsOptOutViewPrefab),
            new VirtualListElementTemplate<SettingsEntityImageVM>(imageTemplate),
            new VirtualListElementTemplate<SettingsEntityButtonVM>(buttonTemplate),
            new VirtualListElementTemplate<SettingsEntityCollapsibleHeaderVM>(headerTemplate),
            new VirtualListElementTemplate<SettingsEntitySubHeaderVM>(subHeaderTemplate),
            new VirtualListElementTemplate<SettingsEntityDropdownButtonVM>(dropdownButtonTemplate, 0),
            new VirtualListElementTemplate<SettingsEntityDropdownVM>(dropdownModMenuView, 5)
            ).ToArray();
          return original;
        }
        catch (Exception e)
        {
          Main.Logger.LogException("SettingsViews_Patch", e);
          return original;
        }
      }

      private static SettingsEntityButtonView CreateButtonTemplate(GameObject prefab, OwlcatMultiButton? buttonPrefab)
      {
        Main.Logger.NativeLog("Creating button template.");

        // Destroy the stuff we don't want from the source prefab
        Object.DestroyImmediate(prefab.GetComponent<SettingsEntityBoolPCView>());
        Object.DestroyImmediate(prefab.transform.Find("MultiButton").gameObject);
        Object.DontDestroyOnLoad(prefab);

        OwlcatMultiButton? buttonControl = null;
        TextMeshProUGUI? buttonLabel = null;

        // Add in our own button
        if (buttonPrefab != null)
        {
          var button = Object.Instantiate(buttonPrefab.gameObject, prefab.transform);
          button.name = "SettingsMultiButton";
          buttonControl = button.GetComponent<OwlcatMultiButton>();
          buttonLabel = button.GetComponentInChildren<TextMeshProUGUI>();

          var layout = button.AddComponent<LayoutElement>();
          layout.ignoreLayout = true;

          var rect = (RectTransform) button.transform;

          rect.anchorMin = new(1, 0.5f);
          rect.anchorMax = new(1, 0.5f);
          rect.pivot = new(0.5f, 0.5f);
          rect.offsetMin = new (-326, - 21);
          rect.offsetMax = new(-62, 19);

          rect.anchoredPosition = new(-194, - 1);
          rect.sizeDelta = new(264, 40);
        }

        // Add our own View (after destroying the Bool one)
        var templatePrefab = prefab.AddComponent<SettingsEntityButtonView>();

        // Wire up the fields that would have been deserialized if coming from a bundle
        templatePrefab.HighlightedImage =
          prefab.transform.Find("SettingsMultiButton/RaycastImage")?.gameObject.GetComponent<Image>();
        templatePrefab.Title =
          prefab.transform.GetComponentInChildren<TextMeshProUGUI>();
        templatePrefab.Button = buttonControl;
        templatePrefab.ButtonLabel = buttonLabel;

        templatePrefab.name = "SettingsEntityButtonView";

        return templatePrefab;
      }

      private static SettingsEntityImageView CreateImageTemplate(GameObject prefab)
      {
        Main.Logger.NativeLog("Creating image template.");

        //Destroy the stuff we don't want from the source prefab
        Object.DestroyImmediate(prefab.GetComponent<SettingsEntityBoolPCView>());
        Object.DestroyImmediate(prefab.transform.Find("MultiButton").gameObject);
        Object.DestroyImmediate(prefab.transform.Find("HorizontalLayoutGroup").gameObject);
        Object.DestroyImmediate(prefab.transform.Find("HighlightedImage").gameObject);
        Object.DontDestroyOnLoad(prefab);

        // Add our own View (after destroying the Bool one)
        var templatePrefab = prefab.AddComponent<SettingsEntityImageView>();

        // Create an imagePrefab as a child of the view so it can be scaled independently
        var imagePrefab = new GameObject("banner", typeof(RectTransform));
        imagePrefab.transform.SetParent(templatePrefab.transform, false);

        // Wire up the fields that would have been deserialized if coming from a bundle
        templatePrefab.Icon = imagePrefab.AddComponent<Image>();
        templatePrefab.Icon.preserveAspect = true;
        templatePrefab.TopBorder = prefab.transform.Find("TopBorderImage").gameObject;

        return templatePrefab;
      }

      private static SettingsEntityCollapsibleHeaderView CreateCollapsibleHeaderTemplate(GameObject prefab)
      {
        Main.Logger.NativeLog("Creating collapsible header template.");

        // Destroy the stuff we don't want from the source prefab
        Object.DestroyImmediate(prefab.GetComponent<SettingsEntityHeaderView>());
        Object.DontDestroyOnLoad(prefab);

        var buttonPC = prefab.GetComponentInChildren<ExpandableCollapseMultiButtonPC>();
        var buttonPrefab = buttonPC.gameObject;
        buttonPrefab.transform.Find("_CollapseArrowImage").gameObject.SetActive(true);
        var button = buttonPrefab.GetComponent<OwlcatMultiButton>();
        button.Interactable = true;

        // Add our own View
        var templatePrefab = prefab.AddComponent<SettingsEntityCollapsibleHeaderView>();
        templatePrefab.Title = prefab.transform.FindRecursive("Label").GetComponent<TextMeshProUGUI>();
        templatePrefab.Button = button;
        templatePrefab.ButtonPC = buttonPC;
        return templatePrefab;
      }

      // Prefab from the SettingsEntityCollapsibleHeaderView
      private static SettingsEntitySubHeaderView CreateSubHeaderTemplate(GameObject prefab)
      {
        Main.Logger.NativeLog("Creating sub header template.");

        // Destroy the stuff we don't want from the source prefab
        Object.DestroyImmediate(prefab.GetComponent<SettingsEntityCollapsibleHeaderView>());
        Object.DontDestroyOnLoad(prefab);

        // Add our own view
        var templatePrefab = prefab.AddComponent<SettingsEntitySubHeaderView>();
        templatePrefab.Title = prefab.transform.FindRecursive("Label").GetComponent<TextMeshProUGUI>();
        templatePrefab.Button = prefab.GetComponentInChildren<OwlcatMultiButton>();
        templatePrefab.ButtonPC = prefab.GetComponentInChildren<ExpandableCollapseMultiButtonPC>();
        return templatePrefab;
      }

      private static SettingsEntityDropdownButtonView CreateDropdownButtonTemplate(
        GameObject prefab, OwlcatMultiButton? buttonPrefab)
      {
        Main.Logger.NativeLog("Creating dropdown button template.");

        // Destroy the stuff we don't want from the source prefab
        Object.DestroyImmediate(prefab.GetComponent<SettingsEntityDropdownPCView>());
        Object.DestroyImmediate(prefab.transform.Find("SetConnectionMarkerIamSet")?.gameObject);
        Object.DontDestroyOnLoad(prefab);

        OwlcatMultiButton? buttonControl = null;
        TextMeshProUGUI? buttonLabel = null;
        Image? oldImage = null;
        // Add in our own button
        if (buttonPrefab != null)
        {
          var button = Object.Instantiate(buttonPrefab.gameObject, prefab.transform);
          buttonControl = button.GetComponent<OwlcatMultiButton>();
          buttonControl.name = "Button";
          buttonLabel = buttonControl.GetComponentInChildren<TextMeshProUGUI>();
          buttonLabel.name = "Text";
          buttonLabel.text = "";



          oldImage = button.GetComponent<Image>();
          if (oldImage != null)
          {
            oldImage.sprite = null;
          }

          var layout = button.AddComponent<LayoutElement>();
          layout.ignoreLayout = true;

          var rect = (RectTransform) button.transform;

          rect.anchorMin = new(1, 0.5f);
          rect.anchorMax = new(1, 0.5f);
          rect.pivot = new(1, 0.5f);

          rect.anchoredPosition = new(-510, 0);
          rect.sizeDelta = new(215, 45);
        }

        // Add our own View (after destroying the Bool one)
        var templatePrefab = prefab.AddComponent<SettingsEntityDropdownButtonView>();
        templatePrefab.name = "SettingsEntityDropdownButtonView";
        templatePrefab.m_MarkImage = prefab.transform.Find("HorizontalLayoutGroup/PointGroup/MarkImage")?.GetComponent<Image>();
        templatePrefab.m_PointImage = prefab.transform.Find("HorizontalLayoutGroup/PointGroup/PointImage")?.GetComponent<Image>();
        templatePrefab.m_SetConnector = prefab.transform.Find("HorizontalLayoutGroup/SetConnectionMarker")?.gameObject;
        templatePrefab.m_SetConnectorIAmSet = prefab.transform.Find("HorizontalLayoutGroup/SetConnectionMarker/IAmSetter")?.gameObject;

        // Wire up the fields that would have been deserialized if coming from a bundle
        templatePrefab.HighlightedImage =
          prefab.transform.Find("HighlightedImage").gameObject.GetComponent<Image>();
        templatePrefab.Title =
          prefab.transform.Find("HorizontalLayoutGroup/Text").gameObject.GetComponent<TextMeshProUGUI>();
        templatePrefab.m_Dropdown = prefab.GetComponentInChildren<OwlcatDropdown>();
        templatePrefab.Button = buttonControl!;
        templatePrefab.ButtonLabel = buttonLabel!;
        templatePrefab.ButtonImage = oldImage!;

        return templatePrefab;
      }

    }

    [HarmonyPatch]
    internal static class DefaultButtonPatcher
    {

      //AAAAAAAAAAAAAAAAAAAAAAAAAAa

      /// <summary>
      /// Will make Default button affect the mod selected on the Mod tab
      /// </summary>
      /// <returns></returns>
      //[HarmonyPatch(typeof(SettingsController), nameof(SettingsController.ResetToDefault))]
      //[HarmonyTranspiler]
      //static IEnumerable<CodeInstruction> SettingsController_ResetToDefault_Transpiler_ToCollectModSettings(IEnumerable<CodeInstruction> instructions, ILGenerator gen)
      //{
      //  var _inst = instructions.ToList();
      //  int length = _inst.Count;
      //  int index = -1;
      //  MethodInfo gameGetter = typeof(Game).GetProperty(nameof(Game.Instance)).GetMethod;
      //  FieldInfo settingsManagerInfo = typeof(Game).GetField(nameof(Game.UISettingsManager));


      //  for (int i = 0; i < length; i++)
      //  {
      //    if (
      //      ((_inst[i + 0].opcode == OpCodes.Call || _inst[i + 0].opcode == OpCodes.Callvirt) && _inst[i + 0].operand is MethodInfo mi1 && mi1 == gameGetter) &&
      //      (_inst[i + 1].opcode == OpCodes.Ldfld && _inst[i + 1].operand is FieldInfo fi && fi == settingsManagerInfo) &&
      //      _inst[i + 2].opcode == OpCodes.Ldarg_1 &&
      //      _inst[i + 3].opcode == OpCodes.Newobj &&
      //      ((_inst[i + 4].opcode == OpCodes.Call || _inst[i + 4].opcode == OpCodes.Callvirt) && _inst[i + 4].operand is MethodInfo mi2 && mi2.Name.Contains("GetSettingsList")))
      //    {
      //      index = i;
      //      break;
      //    }
      //  }

      //  if (index == -1)
      //  {
      //    Main.Logger.Error("DefaultButtonPatcher - failed to find the index when transpile SettingsController.ResetToDefault. Default button will do nothing on the Mods tab.");
      //    return instructions;
      //  }

      //  Label labelNotMods = gen.DefineLabel();
      //  _inst[index].labels.Add(labelNotMods);

      //  Label labelIsMods = gen.DefineLabel();
      //  _inst[index+5].labels.Add(labelIsMods);

      //  MethodInfo mi = typeof(Enumerable).GetMethod(nameof(Enumerable.ToList)).MakeGenericMethod(typeof(UISettingsGroup));

      //  _inst.InsertRange(index, new CodeInstruction[] {
      //    new CodeInstruction(OpCodes.Ldarg_0),
      //    //CodeInstruction.Call((UISettingsManager.SettingsScreen e) => Convert.ToInt32(e)), //WHY DOES IT NOT WORK?!?!?!?!?!
      //    //new CodeInstruction(OpCodes.Ldc_I4, ModsMenuEntity.SettingsScreenValue),
      //    //new CodeInstruction(OpCodes.Ceq),
      //    CodeInstruction.Call((UISettingsManager.SettingsScreen e) => AnotherScreenCheck(e)),
      //    new CodeInstruction(OpCodes.Brfalse_S, labelNotMods),
      //    new CodeInstruction(OpCodes.Call, typeof(ModsMenuEntity).GetProperty(nameof(ModsMenuEntity.CollectSettingGroups), BindingFlags.Static | BindingFlags.NonPublic).GetMethod),
      //    new CodeInstruction(OpCodes.Callvirt, mi),
      //    new CodeInstruction(OpCodes.Br_S, labelIsMods)
      //  });;

      //  return _inst;
      //}

      static bool AnotherScreenCheck(UISettingsManager.SettingsScreen e) => e == (UISettingsManager.SettingsScreen)ModsMenuEntity.SettingsScreenValue;

      [HarmonyPatch(typeof(SettingsVM), nameof(SettingsVM.OpenDefaultSettingsDialog))]
      [HarmonyTranspiler]
      static internal IEnumerable<CodeInstruction> SettingsVM_OpenDefaultSettingsDialog_Transpiler_ToChangeDefaultDialogMessage(IEnumerable<CodeInstruction> instructions, ILGenerator gen)
      {
        var _inst = instructions.ToList();
        int length = _inst.Count;
        int indexStart = -1;
        int indexEnd = -1;
        newDefaultMessage = Helpers.CreateString(
          key: "ModsMenuNewDefaultButtonMessage",
          enGB: "Revert all settings of the mod {0} to their default values?",
          ruRU: "Вернуть все настройки для мода {0} к их значениям по-умолчанию?",
          zhCN: "还原所有{0}模组设置到默认值？",
          deDE: "Alle Einstellungen des Mods {0} auf ihre Standardwerte zurücksetzen?",
          frFR: "Rétablir les valeurs par défaut de tous les paramètres du mod {0}?");

        for (int i = 0; i < length; i++)
        {
          if (
            _inst[i].Calls(typeof(Kingmaker.Game).GetProperty(nameof(Kingmaker.Game.Instance)).GetMethod) &&
            _inst[i + 1].Calls(typeof(Kingmaker.Game).GetProperty(nameof(Kingmaker.Game.BlueprintRoot)).GetMethod) &&
            _inst[i + 2].opcode == OpCodes.Ldfld && _inst[i + 2].operand is FieldInfo fi1 && fi1 == AccessTools.Field(typeof(BlueprintRoot), nameof(BlueprintRoot.LocalizedTexts)) &&
            _inst[i + 3].opcode == OpCodes.Ldfld && _inst[i + 3].operand is FieldInfo fi2 && fi2 == AccessTools.Field(typeof(LocalizedTexts), nameof(LocalizedTexts.UserInterfacesText)) &&
            _inst[i + 4].opcode == OpCodes.Ldfld && _inst[i + 4].operand is FieldInfo fi3 && fi3 == AccessTools.Field(typeof(UIStrings), nameof(UIStrings.SettingsUI)) &&
            _inst[i + 5].opcode == OpCodes.Ldfld && _inst[i + 5].operand is FieldInfo fi4 && fi4 == AccessTools.Field(typeof(UITextSettingsUI), nameof(UITextSettingsUI.RestoreAllDefaultsMessage))
            )
          {
            indexStart = i;
            break;
          }
        }

        if (indexStart == -1)
        {
          Main.Logger.Error("DefaultButtonPatcher - failed to find the starting index when transpile SettingsVM.OpenDefaultSettingsDialog. Default button message will not be altered.");
          return instructions;
        }

        for (int i = indexStart + 6; i < length; i++)
        {
          if (
            _inst[i].opcode == OpCodes.Call && _inst[i].operand is MethodInfo { Name: nameof(string.Format)} &&
            _inst[i + 1].opcode == OpCodes.Stfld && _inst[i + 1].operand is FieldInfo { Name: "text" }
            )
          {
            indexEnd = i;
            break;
          }
        }

        if (indexEnd == -1)
        {
          Main.Logger.Error("DefaultButtonPatcher - failed to find the ending index when transpile SettingsVM.OpenDefaultSettingsDialog. Default button message will not be altered.");
          return instructions;
        }

        Label labelNotMod = gen.DefineLabel();
        _inst[indexStart].labels.Add(labelNotMod);

        Label labelIsMod = gen.DefineLabel();
        _inst[indexEnd +1].labels.Add(labelIsMod);

        _inst.InsertRange(indexStart, new CodeInstruction[]
        {
          CodeInstruction.Call(() => CheckForSelectedSettingsScreenType()),
          new CodeInstruction(OpCodes.Brfalse_S, labelNotMod),
          CodeInstruction.Call(() => MakeMeDefaultButtonMessage()),
          new CodeInstruction(OpCodes.Br_S, labelIsMod)
        });

        return _inst;
      }
      static LocalizedString? newDefaultMessage;
      static bool CheckForSelectedSettingsScreenType() =>  RootUIContext.Instance?.CommonVM.SettingsVM.Value?.SelectedMenuEntity.Value?.SettingsScreenType == (UISettingsManager.SettingsScreen)ModsMenuEntity.SettingsScreenValue;
      
      static string MakeMeDefaultButtonMessage()
      {
        return string.Format(newDefaultMessage, SettingsEntityModMenuEntry.instance.m_TempValue.ModInfo.ModName.Text);
      }
    }
  }
}