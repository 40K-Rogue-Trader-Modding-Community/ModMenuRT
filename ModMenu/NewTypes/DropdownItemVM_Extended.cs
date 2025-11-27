using Kingmaker.Code.UI.MVVM.View.Common.Dropdown;
using Kingmaker.Code.UI.MVVM.VM.Common.Dropdown;
using Kingmaker.Code.UI.MVVM.VM.Settings.Entities;
using Kingmaker.Utility.DotNetExtensions;
using ModMenu.Settings;
using System.Reflection.Emit;
using UnityEngine;

namespace ModMenu.NewTypes
{
  [HarmonyPatch]
  internal abstract class DropdownItemVM_Extended : DropdownItemVM
  {
    public DropdownItemVM_Extended(string text, Sprite icon = null) : base(text, icon)
    {

    }

    public abstract void ExtendedActions(DropdownItemView view);

    static DropdownItemVM MakeCorrectVM(string text, Sprite sprite, SettingsEntityDropdownVM instance)
    {
      if (instance.UISettingsEntity is UISettingsEntityDropdownModMenuEntry menuDropdown)
      {
        if (ModsMenuEntity.ModEntries.TryFind(m => m.ModInfo.ModName.Text == text, out var mod))
          return new SettingsModMenuDropdownItemVM(text, mod, sprite);

      }
      return new DropdownItemVM(text, sprite);

    }

    [HarmonyPatch(typeof(DropdownItemView), nameof(DropdownItemView.BindViewImplementation)), HarmonyPostfix]
    static void AddACallToExtendedDropdownActions(DropdownItemView __instance)
    {
      if (__instance.ViewModel is DropdownItemVM_Extended extended)
        extended.ExtendedActions(__instance);
    }

    static ConstructorInfo OldVmConstructor = AccessTools.Constructor(typeof(DropdownItemVM), new Type[] { typeof(string), typeof(Sprite) });
    [HarmonyPatch(typeof(SettingsEntityDropdownVM), nameof(SettingsEntityDropdownVM.GetSorterDropDownVM)), HarmonyTranspiler]
    static IEnumerable<CodeInstruction> TranspilerToInsertExtendedDropdownVMs(IEnumerable<CodeInstruction> __instructions, ILGenerator ilGen)
    {
      foreach (var inst in __instructions)
        if (inst.opcode == OpCodes.Newobj && inst.operand as ConstructorInfo == OldVmConstructor)
        {
          yield return new CodeInstruction(OpCodes.Ldarg_0);
          yield return CodeInstruction.Call((string text, Sprite sprite, SettingsEntityDropdownVM instance) => MakeCorrectVM(text, sprite, instance));
        }
        else
          yield return inst;
    }
  }
}