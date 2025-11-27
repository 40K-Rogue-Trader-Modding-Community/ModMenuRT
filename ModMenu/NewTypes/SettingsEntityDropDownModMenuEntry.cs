using Kingmaker.Code.UI.MVVM.View.Common.Dropdown;
using Kingmaker.Code.UI.MVVM.View.Settings.PC.Entities;
using Kingmaker.Code.UI.MVVM.VM.Common.Dropdown;
using Kingmaker.Code.UI.MVVM.VM.Settings;
using Kingmaker.Code.UI.MVVM.VM.Settings.Entities;
using Kingmaker.Code.UI.MVVM.VM.Tooltip.Bricks;
using Kingmaker.EntitySystem.Entities.Base;
using Kingmaker.Localization;
using Kingmaker.PubSubSystem;
using Kingmaker.PubSubSystem.Core;
using Kingmaker.Settings;
using Kingmaker.UI.Models.SettingsUI.SettingAssets;
using Kingmaker.UI.Models.SettingsUI.SettingAssets.Dropdowns;
using Kingmaker.UI.MVVM.VM.Tooltip.Templates;
using Kingmaker.Utility.DotNetExtensions;
using ModMenu.Settings;
using Owlcat.Runtime.UI.Controls.Button;
using Owlcat.Runtime.UI.Controls.Other;
using Owlcat.Runtime.UI.Controls.Toggles;
using Owlcat.Runtime.UI.Tooltips;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using static UnityModManagerNet.UnityModManager.Param;

namespace ModMenu.NewTypes
{
  internal class UISettingsEntityDropdownModMenuEntry : UISettingsEntityDropdown<ModsMenuEntry>
  {
    static UISettingsEntityDropdownModMenuEntry()
    {
      instance = CreateInstance<UISettingsEntityDropdownModMenuEntry>();
      instance.m_Description = Helpers.CreateString("UISettingsEntityDropdownModMenuEntry.Description", 
        enGB:"Select a mod", 
        ruRU: "Выберите мод", 
        zhCN: "选择一个模组",
        deDE: "Wähle einen Mod aus",
        frFR: "Choisir un mod");
      instance.m_TooltipDescription = Helpers.EmptyString;
      instance.m_EncyclopediaDescription = new();

      instance.LinkSetting(SettingsEntityModMenuEntry.instance);
      ((IUISettingsEntityDropdown)instance).OnTempIndexValueChanged += ChangeScreen;

      instance.m_EncyclopediaDescription = new();
    }
    static internal void ChangeScreen(int _)
    {
#if DEBUG
      Main.Logger.Log($"Switching settings screen from toggle");
#endif
      ModsMenuEntity.settingVM.SwitchSettingsScreen(ModsMenuEntity.SettingsScreenId);
      SettingsController.Instance.RemoveFromConfirmationList(instance.SettingsEntity, false);
      SettingsEntityModMenuEntry.instance.TempValueIsConfirmed = true;
    }

    internal static UISettingsEntityDropdownModMenuEntry instance;

    public override IReadOnlyList<string> LocalizedValues 
      => ModsMenuEntity.ModEntries.Select(entry => entry.ModInfo.ModName.Text).ToList();
    public override int GetIndexTempValue()
      => ModsMenuEntity.ModEntries.IndexOf(Setting.GetTempValue());
    

    public override void SetIndexTempValue(int value)
    {
      if (value is < 0 && value > ModsMenuEntity.ModEntries.Count())
      {
        Main.Logger.Error($"Value {value} is given to UISettingsEntityDropdownModMenuEntry when there're only {ModsMenuEntity.ModEntries.Count()} entries in the list");
        SetTempValue(ModsMenuEntity.ModEntries[0]);
      }

      SetTempValue(ModsMenuEntity.ModEntries[value]);
    }
    public override void SetIndexValueAndConfirm(int value)
    {
      SetIndexTempValue(value);
    }
  }

  [HarmonyPatch]
  internal class SettingsModMenuDropdownItemVM : DropdownItemVM_Extended
  {
    public SettingsModMenuDropdownItemVM(string text, ModsMenuEntry entry, Sprite? icon = null) : base(text, icon)
    {
      Entry = entry;
    }

    internal ModsMenuEntry? Entry;

    void HandleModDescription(ISettingsDescriptionUIHandler handler)
    {
      if (Entry != null)
      {
        handler.HandleShowSettingsDescription(UISettingsEntityDropdownModMenuEntry.instance, Entry.ModInfo.ModName.Text, Entry.ModInfo.GenerateDescription());
#if DEBUG
        Main.Logger.Log($"HandleModDescription {Entry.ModInfo.ModName.Text}");
#endif
      }
    }

    void HandleHover(bool hover)
    {
#if DEBUG
      Main.Logger.Log($"HandleHover");
#endif
      if (hover)
        EventBus.RaiseEvent<ISettingsDescriptionUIHandler>(HandleModDescription);
    }

    static FieldInfo titleField = AccessTools.DeclaredField(typeof(TooltipBrickTitle), nameof(TooltipBrickTitle.m_Title));
    static Action<TooltipBrickTitle, string> SetField = new ((brick, s) => titleField?.SetValue(brick, s));

    static FieldInfo textSizeField = AccessTools.DeclaredField(typeof(TooltipBrickTitle), nameof(TooltipBrickTitle.m_AdditionalTextSize));
    static Action<TooltipBrickTitle> SetTextSize = new((brick) => textSizeField?.SetValue(brick, (int)textSizeField.GetValue(brick) +6));

    [HarmonyPatch(typeof(TooltipTemplateSettingsEntityDescription), nameof(TooltipTemplateSettingsEntityDescription.GetHeader))]
    [HarmonyPostfix]
    static IEnumerable<ITooltipBrick> AddModImageToTooltip(
      IEnumerable<ITooltipBrick> __result,
      TooltipTemplateSettingsEntityDescription __instance)
    {
      if (__instance.m_SettingsEntity is UISettingsEntityDropdownModMenuEntry maybeInstance && maybeInstance == UISettingsEntityDropdownModMenuEntry.instance)
      {
#if DEBUG
        Main.Logger.Log($"AddModImageToTooltip inserting image {__instance.m_OwnTitle}");
#endif
        if (ModsMenuEntity.ModEntries.TryFind((ModsMenuEntry mod) => mod?.ModInfo.ModName.Text == __instance.m_OwnTitle, out var mod) 
          && mod?.ModInfo.ModImage is Sprite image) 
        {
#if DEBUG
          Main.Logger.Log($"AddModImageToTooltip Prepend");
#endif
          var list = __result.ToArray();
          var title = list.OfType<TooltipBrickTitle>().FirstOrDefault();
          if (title != null)
          {
            SetField(title, __instance.m_OwnTitle);
            SetTextSize(title);
          }

          return [list[0], new TooltipBrickPicture(image), .. list.Skip(1)];
        }
      }
      else
      {
#if DEBUG
        Main.Logger.Log($"AddModImageToTooltip returning. Was {__instance.m_SettingsEntity?.GetType().Name ?? "null type?"}");
#endif
      }

      return __result;
    }

    public override void ExtendedActions(DropdownItemView view) =>
      view.AddDisposable(view.Toggle?
        .m_MultiButton
        .OnHoverAsObservable()
        .Subscribe(HandleHover));
  }
}
