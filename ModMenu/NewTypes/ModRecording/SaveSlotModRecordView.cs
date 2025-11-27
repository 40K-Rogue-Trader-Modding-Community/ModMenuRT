using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using JetBrains.Annotations;
using Kingmaker.Modding;
using Kingmaker.PubSubSystem.Core;
using Kingmaker.UI.Workarounds;
using Kingmaker.UI.Common;
using Kingmaker.UI.MVVM.View.SaveLoad.Base;
using Kingmaker.Utility.NewtonsoftJson;
using Kingmaker.Utility.DotNetExtensions;
using Kingmaker.Utility.Serialization;
using Owlcat.Runtime.UI.ConsoleTools;
using Owlcat.Runtime.UI.ConsoleTools.HintTool;
using Owlcat.Runtime.UI.Controls.Button;
using Owlcat.Runtime.UI.Controls.Selectable;
using Owlcat.Runtime.UI.MVVM;
using Kingmaker.Code.UI.MVVM.View.SaveLoad.PC;
using Kingmaker.Code.UI.MVVM.View.SaveLoad.Console;
using Kingmaker.Code.UI.MVVM.VM.MessageBox;
using Kingmaker.Code.UI.MVVM.VM.SaveLoad;
using Kingmaker.Code.UI.MVVM.VM.Tooltip.Utils;
using Owlcat.Runtime.UniRx;
using Rewired;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using UnityModManagerNet;
using static ModMenu.NewTypes.ModRecording.StringsAndIcons;
using static ModMenu.NewTypes.ModRecording.TooltipTemplateModRecord;
using Owlcat.Runtime.UI.Controls;
using Kingmaker.Code.UI.MVVM.VM.Common;
using Kingmaker.Code.UI.MVVM.View.Common.PC;

namespace ModMenu.NewTypes.ModRecording
{
  //AAAAAAAAAAa
  [HarmonyPatch]
  internal partial class SaveSlotModRecordView : ViewBase<SaveSlotVM>
  {
    const string nameContainer = "ModMenuContainerForModRecordView";
    const string nameRecordView = "ModMenuModRecordView";

    SaveInfoWithModList Record;

    TextMeshProUGUI m_ModStuff;
    Image SpriteModStuff;
    TextMeshProUGUI m_NoDep;
    Image SpriteNoDep;
    OwlcatButton ButtonEnable;
    OwlcatButton ButtonDisable;
    OwlcatSelectable TooltipModStuff;
    OwlcatSelectable TooltipNoDep;

    public override void BindViewImplementation()
    {
      if (ViewModel?.Reference is not SaveInfoWithModList modInfo)
      {
        if (ViewModel is null)
          Main.Logger.Error("Trying to bind SaveSlotModRecordView with a null VM!");
        else if (ViewModel.Reference is null)
          Main.Logger.Error("Trying to bind SaveSlotModRecordView with a null ModInfo!");
        else
          Main.Logger.Error($"SaveInfo {ViewModel.Reference.Name} is not a SaveInfoWithModList! Can not bind SaveSlotModRecordView");
        return;
      };
      Record = modInfo;
      (ViewModel as SaveSlotWithModListVM).BoundModRecordView = this;
      Refresh();
      AddDisposable(TooltipModStuff.SetTooltip(new TooltipTemplateModRecord(TooltipTemplateModRecordEnum.WithDependency, this)));
      AddDisposable(TooltipNoDep.SetTooltip(new TooltipTemplateModRecord(TooltipTemplateModRecordEnum.NoDependency, this)));

      var maybeCommonView = transform.root.GetComponent<ViewBase<CommonVM>>();
      Transform infoWindow;
      if (maybeCommonView == null)
      {
        Debug.LogWarning("Did not find CommonView. Will be no tooltips");
        return;
      }
      if (maybeCommonView is CommonPCView pCView)
        infoWindow = pCView.transform.Find("CommonCanvas/InfoWindowPCView");
      else
        infoWindow = maybeCommonView.transform.Find("CommonCanvas/InfoWindowConsoleView");

      if (infoWindow == null)
      {
        Debug.LogWarning("Did not find CommonView. Will be no tooltips");
        return;
      }

      infoWindow.SetAsLastSibling();
    }

    //AAAAAAAAAAAAAaaaa
    //[HarmonyPatch(typeof(SaveLoadConsoleView), nameof(SaveLoadConsoleView.CreateInput))]
    //[HarmonyPostfix]
    static void AddModMenuRecordWidgetsForSaveLoadScreen(SaveLoadConsoleView __instance)
    {
      var collection = __instance.SlotCollectionView;
      //collection.AddDisposable(__instance.m_CommonHintsWidget.BindHint(__instance.m_InputLayer.AddButton(
      //  delegate (InputActionEventData _) { },
      //  11,
      //  collection.m_HasSlot.And(collection.NavigationBehaviour.DeepestFocusAsObservable.Select(HasMods)).ToReactiveProperty(), 
      //  InputActionEventType.ButtonJustPressed),
      //  ShowModListTooltipHintName,
      //  ConsoleHintsWidget.HintPosition.Right));
    }

    static bool HasMods(IConsoleEntity entity)
      {
        if (entity is not ISaveSlotWithModListView slotWithMods)
          return false;
        if (slotWithMods.saveSlotWithModListVM is not SaveSlotWithModListVM ViewModelWithMods)
        {
          Main.Logger.Error("VM for SaveSlotWithModList is null");
          return false;
        }
        return ViewModelWithMods.AllMods.Any();
      }

    public override void DestroyViewImplementation()
    {
      if (ViewModel is not SaveSlotWithModListVM modInfo)
        return;
      modInfo.BoundModRecordView = null;
    }

    internal void Refresh()
    {
#if DEBUG
      Main.Logger.Log($"SaveSlotModRecordView run Refresh"); 
#endif
      var saveSlot = ViewModel as SaveSlotWithModListVM;
      var totalMods = saveSlot.OwlMods.Count + saveSlot.UMMMods.Count + saveSlot.OtherMods.Count;
      bool Console = ButtonEnable == null || ButtonDisable == null;
      if (totalMods == 0)
      {
        m_ModStuff.text = NoMods;
        SpriteModStuff.sprite = null;
        m_NoDep.text = "";
        SpriteNoDep.sprite = null;
        m_NoDep.transform.parent.transform.gameObject.SetActive(false);
        m_ModStuff.transform.parent.gameObject.SetActive(true);
        if (!Console)
        {
          var text = ButtonDisable.GetComponentInChildren<TextMeshProUGUI>();
          if (!object.ReferenceEquals(text.text, ButtonDisableExtraDeactivated))
            ButtonDisable.GetComponentInChildren<TextMeshProUGUI>().text = ButtonDisableExtraDeactivated;
          text = ButtonEnable.GetComponentInChildren<TextMeshProUGUI>();
          if (!object.ReferenceEquals(text.text, ButtonEnableMissingDeactivated))
            ButtonEnable.GetComponentInChildren<TextMeshProUGUI>().text = ButtonEnableMissingDeactivated;
          ButtonEnable.SetInteractable(false);
          ButtonDisable.SetInteractable(false);
        }
        return;
      }

      var enabled = saveSlot.UMMMods.Where(m => m.state > ModState.Outdated).Count() + saveSlot.OwlMods.Where(m => m.state > ModState.Outdated).Count();
      var disabled = saveSlot.UMMMods.Count + saveSlot.OwlMods.Count - enabled;

      if (saveSlot.OtherMods.Count > 0)
      {
        m_ModStuff.text = string.Format(WithOtherMods,
          totalMods,
          enabled,
          disabled,
          saveSlot.OtherMods.Count);
        SpriteModStuff.sprite = IconDislike;
        m_ModStuff.transform.parent.gameObject.SetActive(true);

      }
      else if (disabled > 0)
      {
        m_ModStuff.text = string.Format(WithDisabledMods,
          totalMods,
          enabled,
          disabled);
        SpriteModStuff.sprite = IconDislike;
        m_ModStuff.transform.parent.gameObject.SetActive(true);
      }
      else
      {
        m_ModStuff.text = string.Format(WithoutDisabledMods, totalMods);
        SpriteModStuff.sprite = IconLike;
        m_ModStuff.transform.parent.gameObject.SetActive(true);
      }

      if (saveSlot.Exclusions.Count == 0)
      {
        m_NoDep.text = "";
        SpriteNoDep.sprite = null;
        m_NoDep.transform.parent.gameObject.SetActive(false);
      }
      else
      {
        m_NoDep.text = totalMods == 0 ? string.Format(NoDep, saveSlot.Exclusions.Count)
          : string.Format(NoDepAdd, saveSlot.Exclusions.Count);
        SpriteNoDep.sprite = saveSlot.Exclusions.Any(mod => mod.state < ModState.Good) ? IconNew : IconLike;
        m_NoDep.transform.parent.gameObject.SetActive(true);
        if (!Console)
        {
          ButtonEnable.gameObject.SetActive(true);
          ButtonDisable.gameObject.SetActive(true);
        }
      }
      if (!Console)
      {
        if (saveSlot.DisabledMods is 0)
        {
          //Main.Logger.Log($"SaveSlotModRecordView - disabled the ButtonEnable");

          ButtonEnable.Interactable = false;
          ButtonEnable.GetComponentInChildren<TextMeshProUGUI>().text = ButtonEnableMissingDeactivated;
        }
        else
        {
          //Main.Logger.Log($"SaveSlotModRecordView - enabled the ButtonEnable");
          ButtonEnable.Interactable = true;
          ButtonEnable.GetComponentInChildren<TextMeshProUGUI>().text = string.Format(ButtonEnableMissing, saveSlot.DisabledMods);
        }

        if (UnityModManager.ModEntries.Where(mod => mod.Enabled).Cast<object>().Concat(OwlcatModificationsManager.Instance.AppliedModifications.Cast<object>())
          .Any(entry => !saveSlot.AllMods.Any(mod => mod.mod == entry)))
        {
          //Main.Logger.Log($"SaveSlotModRecordView - enabled the ButtonDisable");
          ButtonDisable.Interactable = true;
          ButtonDisable.GetComponentInChildren<TextMeshProUGUI>().text = ButtonDisableExtraDeactivated;
        }
        else
        {
          //Main.Logger.Log($"SaveSlotModRecordView - disabled the ButtonDisable");
          ButtonDisable.Interactable = false;
          ButtonDisable.GetComponentInChildren<TextMeshProUGUI>().text = ButtonDisableExtraDeactivated;
        }
      }
    }

    [HarmonyPatch(typeof(SaveLoadBaseView), nameof(SaveLoadBaseView.BindViewImplementation))]
    [HarmonyPostfix]
    static void SaveSlotView_BindViewImplementation_PatchToBindModRecordList(SaveLoadBaseView __instance)
    {
      var go = __instance?.m_DetailedSaveSlotView.GetComponent<SaveSlotModRecordView>();
      if (go == null)
      {
        Main.Logger.Error("SaveSlotModRecordView - failed to find the game object with the view! Will not bind.");
        return;
      }
      var view = go.GetComponent<SaveSlotModRecordView>();
      if (view == null)
      {
        Main.Logger.Error("SaveSlotModRecordView - failed to find the view! Will not bind.");
        return;
      }
      __instance.AddDisposable(__instance.ViewModel.SelectedSaveSlot.Subscribe(view.Bind));
    }

    [HarmonyPatch(typeof(SaveLoadBaseView), nameof(SaveLoadBaseView.DestroyViewImplementation))]
    [HarmonyPostfix]
    static void SaveSlotView_BindViewImplementation_PatchToUnbindModRecordList(SaveLoadBaseView __instance)
    {
      var go = __instance?.m_DetailedSaveSlotView.GetComponent<SaveSlotModRecordView>();
      if (go == null)
      {
        Main.Logger.Error("SaveSlotModRecordView - failed to find the game object with the view! Will not unbind.");
        return;
      }
      var view = go.GetComponent<SaveSlotModRecordView>();
      if (view == null)
      {
        Main.Logger.Error("SaveSlotModRecordView - failed to find the view! Will not unbind.");
        return;
      }
      view.Record = null;
      view.Unbind();
    }

    [HarmonyPatch(typeof(SaveLoadBaseView), nameof(SaveLoadBaseView.Initialize))]
    [HarmonyPrefix]
    static void SaveLoadView_Initialize_PatchToInjectModRecordView(SaveLoadBaseView __instance)
    {
      //Stopwatch watch = Stopwatch.StartNew();
      if (__instance?.m_DetailedSaveSlotView.GetComponent<SaveSlotModRecordView>())
        return; //already made changes

      var material =
        TMP_Settings.instance?.m_defaultFontAsset?.material;
      var fontAsset = TMP_Settings.instance?.m_defaultFontAsset;

      var _SaveSlotPCView = __instance.m_DetailedSaveSlotView as SaveSlotPCView;
      bool isPcView = _SaveSlotPCView != null;
      RectTransform rectTransform;

      var may = _SaveSlotPCView.transform.parent.GetComponentInParent<VerticalLayoutGroup>(true);
      if (may)
      {
        may.childForceExpandWidth = false;
        may.childAlignment = TextAnchor.UpperCenter;
      }
      else
      {
#if DEBUG
        Main.Logger.Warning("NO group"); 
#endif
      }

      rectTransform = __instance.m_DetailedSaveSlotView.gameObject.transform as RectTransform;
      var modRecordView = rectTransform.gameObject.AddComponent<SaveSlotModRecordView>();

      var ModStuffHolder = new GameObject("ModStuffHolder", typeof(RectTransform));
      var ModStuffHolderRectTrans = ModStuffHolder.transform as RectTransform;
      ModStuffHolderRectTrans.SetParent(rectTransform, false);
      ModStuffHolderRectTrans.anchorMin = new Vector2(0, 0);
      ModStuffHolderRectTrans.anchorMin = new Vector2(1, 1);
      var lay = ModStuffHolder.AddComponent<LayoutElementExtended>();
      lay.PreferredWidthExtended = new()
      {
        ReferenceType = LayoutElementExtendedValue.ReferenceTypes.Width,
        Reference = (RectTransform)ModStuffHolderRectTrans.parent,
        ReferenceDelta = -10,
        Enabled = true
      };
      var esfFitter1 = ModStuffHolder.AddComponent<ContentSizeFitterExtended>();
      esfFitter1.m_HorizontalFit = ContentSizeFitterExtended.FitMode.PreferredSize;
      esfFitter1.m_VerticalFit = ContentSizeFitterExtended.FitMode.Unconstrained;

      var ModStuff = new GameObject("ModStuff", typeof(RectTransform));
      var ModStuffRect = ModStuff.transform as RectTransform;
      ModStuffRect.SetParent(ModStuffHolderRectTrans, false);
      ContentSizeFitterExtended esfFitter  = ModStuff.AddComponent<ContentSizeFitterExtended>();
      esfFitter.m_HorizontalFit = ContentSizeFitterExtended.FitMode.Clamp;
      esfFitter.m_VerticalFit = ContentSizeFitterExtended.FitMode.Unconstrained;
      var tmp = ModStuff.AddComponent<TextMeshProUGUI>();
      tmp.m_fontAsset = fontAsset;
      tmp.fontSharedMaterial = material;
      tmp.color = new Color(0.1961f, 0.2078f, 0.2706f, 1);
      tmp.fontSizeMax = 18;
      tmp.fontSizeMin = 10;
      //tmp.m_fontSizeBase = 18;
      tmp.autoSizeTextContainer = true;
      tmp.enableAutoSizing = false;
      tmp.textWrappingMode = TextWrappingModes.Normal;
      tmp.margin = new(0, 0, 47, 0);
      tmp.alignment = TextAlignmentOptions.MidlineJustified;
      modRecordView.m_ModStuff = tmp;
      ModStuff.SetActive(true);

      var ModStuffSprite = new GameObject("ModStuffSprite", typeof(RectTransform));
      var ModStuffSpriteRectTrans = ModStuffSprite.GetComponent<RectTransform>();
      modRecordView.SpriteModStuff = ModStuffSprite.AddComponent<Image>();
      esfFitter = ModStuffSprite.AddComponent<ContentSizeFitterExtended>();
      esfFitter.m_HorizontalFit = ContentSizeFitterExtended.FitMode.PreferredSize;
      esfFitter.m_VerticalFit = ContentSizeFitterExtended.FitMode.PreferredSize;
      ModStuffSpriteRectTrans.SetParent(ModStuffHolderRectTrans, false);
      ModStuffSpriteRectTrans.anchorMin = new(1f, 0.5f);
      ModStuffSpriteRectTrans.anchorMax = new(1f, 0.5f);
      ModStuffSpriteRectTrans.offsetMin = new(-42, 0);
      ModStuffSpriteRectTrans.offsetMax = new(0, 0);
      ModStuffSprite.SetActive(true);

      modRecordView.TooltipModStuff = ModStuffSprite.AddComponent<OwlcatSelectable>();

      var ExclusionsHolder = GameObject.Instantiate(ModStuffHolder, ModStuffHolder.transform.parent);
      ExclusionsHolder.name = "ExclusionsHolder";
      var Exclusions = ExclusionsHolder.transform.GetChild(0);
      Exclusions.name = "Exclusions";
      var ExclusionsSprite = ExclusionsHolder.transform.GetChild(1);
      ExclusionsSprite.name = "ExclusionsSprite";
      modRecordView.m_NoDep = Exclusions.GetComponent<TextMeshProUGUI>();
      modRecordView.SpriteNoDep = ExclusionsSprite.GetComponent<Image>();
      modRecordView.TooltipNoDep = ExclusionsSprite.GetComponent<OwlcatSelectable>();

      if (!isPcView)
        goto AfterButtons;

      var ModRecordButtons = new GameObject("ModRecordButtons", typeof(RectTransform));
      var ModRecordButtonsRectTrans = ModRecordButtons.transform as RectTransform;
      ModRecordButtonsRectTrans.SetParent(rectTransform, false);
      var ButtonsHorGroup = ModRecordButtons.AddComponent<HorizontalLayoutGroupWorkaround>();
      ButtonsHorGroup.childAlignment = TextAnchor.LowerCenter;
      ButtonsHorGroup.spacing = 10; 
      lay = ModRecordButtons.AddComponent<LayoutElementExtended>();
      lay.PreferredWidthExtended = new()
      {
        ReferenceType = LayoutElementExtendedValue.ReferenceTypes.Width,
        Reference = (RectTransform)ModStuffHolderRectTrans.parent,
        ReferenceDelta = -10,
        Enabled = true
      };
      esfFitter = ModRecordButtons.AddComponent<ContentSizeFitterExtended>();
      esfFitter.m_HorizontalFit = ContentSizeFitterExtended.FitMode.PreferredSize;
      esfFitter.m_VerticalFit = ContentSizeFitterExtended.FitMode.Clamp;


      var ButtonPrototype = _SaveSlotPCView.m_DeleteButton;
      var Button = Instantiate(ButtonPrototype);
      Button.name = "ModRecordButtonEnable";
      rectTransform = (RectTransform)Button.transform;
      rectTransform.anchorMin = new(0, 0);
      rectTransform.anchorMax = new(0.5f, 1);
      var unnecessaryImage = Button.transform.Find("TextBlock/Image");
      if (unnecessaryImage)
        { GameObject.Destroy(unnecessaryImage.gameObject); }
      var text = Button.GetComponentInChildren<TextMeshProUGUI>();
      if (text != null)
      {
        text.text = ButtonEnableMissingDeactivated;
        text.enableAutoSizing = true;
        text.autoSizeTextContainer = true;
        text.fontSizeMin = 10;
        text.fontSizeMax = 18;
        text.margin = new Vector4(5, 5, 5, 5);
        text.extraPadding = false;
        text.m_padding = 0;
      }
      Button.transform.SetParent(ModRecordButtonsRectTrans, false);
      Button.m_OnLeftClick = new();
      Button.OnLeftClick.AddListener(() => modRecordView.StartDialogToProceedOrCancel(true));
      modRecordView.ButtonEnable = Button;

      Button = Instantiate(Button, Button.transform.parent);
      rectTransform = (RectTransform)Button.transform;
      rectTransform.anchorMin = new(0.5f, 0);
      rectTransform.anchorMax = new(1, 1);
      rectTransform.offsetMin = default;
      rectTransform.offsetMax = default;
      Button.name = "ModRecordButtonDisable";
      Button.m_OnLeftClick = new();
      Button.OnLeftClick.AddListener(() => modRecordView.StartDialogToProceedOrCancel(false));
      modRecordView.ButtonDisable = Button;

    AfterButtons:;
      modRecordView.gameObject.SetActive(true);
    }

    void StartDialogToProceedOrCancel(bool Enable)
    {
      UIUtility.ShowMessageBox(
        WarningText,
        DialogMessageBoxBase.BoxType.Dialog,
        Enable? new Action<DialogMessageBoxBase.BoxButton>(TryEnableMissingMods) : new Action<DialogMessageBoxBase.BoxButton>(TryDisableExtraMods),
        yesLabel: ButtonProсeed,
        noLabel: ButtonCancel);
    }

    void TryEnableMissingMods(DialogMessageBoxBase.BoxButton buttonType)
    {
      if (buttonType is not DialogMessageBoxBase.BoxButton.Yes)
        return;
      var vm = (ViewModel as SaveSlotWithModListVM);
      var m_owlmods = OwlcatModificationsManager.Instance.m_Settings.EnabledModifications;
      IEnumerable<string> OwlMods = m_owlmods;
      foreach (var mod in vm.AllMods.Where(m =>m.state == ModState.Disabled))
      {
#if DEBUG
        Main.Logger.Log($"TryEnableMissingMods - {mod.record.Id}"); 
#endif
        try
        {
          switch (mod.record.modType)
          {
            case SaveInfoWithModList.ModRecord.ModType.UmmMod:
              {
                var entry = mod.mod as UnityModManager.ModEntry;
                if (entry is not null)
                {
                  entry.Enabled = true;
                  entry.Active = true;
                }
                break;
              }
            case SaveInfoWithModList.ModRecord.ModType.OwlMod:
              {
                var entry = mod.mod as OwlcatModification;
                SetOwlModSetting(OwlcatModificationsManager.Instance.m_Settings.EnabledModifications.Concat(entry.Manifest.UniqueName).ToArray());
                entry.Apply();
                OwlMods = OwlMods.Concat(entry.Manifest.UniqueName);
                break;
              }
          }
        }
        catch (Exception ex)
        {
          Main.Logger.Error($"Failed to enable missing mod {mod?.record?.Id ?? "NULL?!"}!");
          Main.Logger.LogException(ex);
        }
      }
    }

    void TryDisableExtraMods(DialogMessageBoxBase.BoxButton buttonType)
    {
      if (buttonType is not DialogMessageBoxBase.BoxButton.Yes)
        return;
      var vm = (ViewModel as SaveSlotWithModListVM);
      foreach (var mod in UnityModManager.ModEntries)
      {
        bool inRecord = vm.UMMMods.Concat(vm.Exclusions.Where(m => m.record.modType is SaveInfoWithModList.ModRecord.ModType.UmmMod)).Any(m => m.record.Id == mod.Info.Id);
        if (mod.Enabled && !inRecord)
        {
          try
          {
            mod.Enabled = false;
            mod.Active = false;
          }
          catch (Exception ex)
          {
            Main.Logger.LogException(ex);
          }
        }
      }

      List<string> OwlMods = new();
      foreach (var mod in OwlcatModificationsManager.Instance.m_Settings.EnabledModifications)
      {
        bool inRecord = vm.OwlMods.Concat(vm.Exclusions.Where(m => m.record.modType is SaveInfoWithModList.ModRecord.ModType.OwlMod)).Any(m => m.record.Id == mod);
        if (!inRecord)
          OwlMods.Add(mod);
      }

      if (OwlMods.Count > 0)
      {
        var modifications = OwlcatModificationsManager.Instance.m_Settings.EnabledModifications.Where(m => !OwlMods.Contains(m)).ToArray();
        SetOwlModSetting(modifications);
        foreach (var mod in OwlMods)
          EventBus.RaiseEvent<ISubscriberToModStateChange>(subscriber => subscriber.OnOMMModStateChanged(mod, false));
      }
    }

    void SetOwlModSetting([NotNull]string[] ModsRenewed)
    {
      //Main.Logger.Log($"Trying to save OwlSettngs with following mods:\n {string.Join(",\n", ModsRenewed)}");
      var settingsOld = OwlcatModificationsManager.Instance.m_Settings;
      try
      {
        settingsOld.GetType().GetField(nameof(OwlcatModificationsManager.SettingsData.EnabledModifications)).SetValue(settingsOld, ModsRenewed);
      }
      catch (Exception)
      {
        Main.Logger.Error("Failed to assign OwlMod list when disabling mods");
      }
      try
      {
        
        JsonExtensions.SerializeToFile(NewtonsoftJsonHelper.Serializer, OwlcatModificationsManager.SettingsFilePath, settingsOld);
        
      }
      catch (Exception ex)
      {
        Main.Logger.Error($"Failed to save Owlcat MoManager Settings!");
        Main.Logger.LogException(ex);
      }
    }
  }
}
