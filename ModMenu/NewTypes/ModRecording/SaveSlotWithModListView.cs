using System.Reflection.Emit;
using Kingmaker.PubSubSystem;
using Kingmaker.Code.UI.MVVM.View.SaveLoad.Console;
using Kingmaker.Code.UI.MVVM.View.SaveLoad.PC;
using Kingmaker.Code.UI.MVVM.VM.SaveLoad;
using Kingmaker.Code.UI.MVVM.View.SaveLoad.Base;
using Owlcat.Runtime.UI.ConsoleTools.ClickHandlers;
using Owlcat.Runtime.UI.VirtualListSystem;
using Kingmaker.PubSubSystem.Core;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using static ModMenu.NewTypes.ModRecording.SaveSlotWithModListVM;
using static ModMenu.NewTypes.ModRecording.StringsAndIcons;
using static ModMenu.NewTypes.ModRecording.TooltipTemplateModRecord;
using Kingmaker.Utility.UnityExtensions;

namespace ModMenu.NewTypes.ModRecording
{
  internal static class ExtensionSaveSlotWithModListView
  {
    public const string modGreenMarkName = "ModsGreenMark";
    public const string modOrangeMarkName = "modOrangeMark";
    public const string modRedMarkName = "modRedMark";

    static UnityEngine.GameObject? originalPrefab = null;
    static SaveSlotWithModListPCView? m_config_PC = null!;
    static SaveSlotWithModListConsoleView? m_config_Console = null!;

    internal static void UpdateModStateIndicator(this ISaveSlotWithModListView instance, ModRecordState state)
    {
      if (instance.RedMark == null || instance.OrangeMark == null || instance.GreenMark == null)
        return;

      if (state is ModRecordState.NoMods)
      {
        instance.RedMark.SetActive(false);
        instance.OrangeMark.SetActive(false);
        instance.GreenMark.SetActive(false);
      }
      if (state is ModRecordState.AllGood)
      {
        instance.RedMark.SetActive(false);
        instance.OrangeMark.SetActive(false);
        instance.GreenMark.SetActive(true);
      }
      if (state is ModRecordState.SomeProblems)
      {
        instance.RedMark.SetActive(false);
        instance.OrangeMark.SetActive(true);
        instance.GreenMark.SetActive(false);
      }
      if (state is ModRecordState.SomethingIsMissing)
      {
        instance.RedMark.SetActive(true);
        instance.OrangeMark.SetActive(false);
        instance.GreenMark.SetActive(false);
      }
    }
    internal static SaveSlotBaseView TryGetConfig(SaveSlotBaseView oldPrefab)
    {
      try
      {
        if (originalPrefab == null)
        {
          if (m_config_Console != null)
            UnityEngine.Object.Destroy(m_config_Console.gameObject);
          if (m_config_PC != null)
            UnityEngine.Object.Destroy(m_config_PC.gameObject);
          goto nullPrefab;
        }

        if (oldPrefab is SaveSlotConsoleView && m_config_Console != null)
          return m_config_Console; 
        else if (oldPrefab is SaveSlotPCView && m_config_PC != null)
          return m_config_PC; 

        nullPrefab:
        var a = GameObject.Instantiate(oldPrefab);
        SaveSlotBaseView newUntypedPrefab = oldPrefab is SaveSlotConsoleView ? a.gameObject.AddComponent<SaveSlotWithModListConsoleView>() : a.gameObject.AddComponent<SaveSlotWithModListPCView>();
        MemberWiseCloneView(newUntypedPrefab, a);
        newUntypedPrefab.gameObject.name = "SaveSlotWithModListView";
        UnityEngine.Object.DestroyImmediate(a);
        GameObject.DontDestroyOnLoad(newUntypedPrefab.gameObject);
        var newPrefab = (ISaveSlotWithModListView)newUntypedPrefab;
        var Pic = newUntypedPrefab.transform.Find("Picture");
        var QuickMark = Pic.Find("QuickSaveMark");
        var Mark = GameObject.Instantiate(QuickMark, Pic, false);
        Mark.name = modGreenMarkName;
        var newMarkTransform = (RectTransform)Mark;
        newMarkTransform.offsetMin = new Vector2(newMarkTransform.offsetMin.x + 138, newMarkTransform.offsetMin.y);
        newMarkTransform.offsetMax = new Vector2(newMarkTransform.offsetMax.x + 138, newMarkTransform.offsetMax.y);
        newMarkTransform.sizeDelta = ((RectTransform)QuickMark.transform).sizeDelta;
        Mark.GetComponent<Image>().sprite = IconOk;
        newPrefab.GreenMark = Mark.gameObject;

        Mark = GameObject.Instantiate(QuickMark, Pic, false);
        Mark.name = modOrangeMarkName;
        newMarkTransform = (RectTransform)Mark;
        newMarkTransform.SetParent(Pic);
        newMarkTransform.offsetMin = new Vector2(newMarkTransform.offsetMin.x + 138, newMarkTransform.offsetMin.y);
        newMarkTransform.offsetMax = new Vector2(newMarkTransform.offsetMax.x + 138, newMarkTransform.offsetMax.y);
        newMarkTransform.sizeDelta = ((RectTransform)QuickMark.transform).sizeDelta;
        Mark.GetComponent<Image>().sprite = IconNew;
        newPrefab.OrangeMark = Mark.gameObject;

        Mark = GameObject.Instantiate(QuickMark, Pic, false);
        Mark.name = modRedMarkName;
        newMarkTransform = (RectTransform)Mark;
        newMarkTransform.SetParent(Pic);
        newMarkTransform.offsetMin = new Vector2(newMarkTransform.offsetMin.x + 138, newMarkTransform.offsetMin.y);
        newMarkTransform.offsetMax = new Vector2(newMarkTransform.offsetMax.x + 138, newMarkTransform.offsetMax.y);
        newMarkTransform.sizeDelta = ((RectTransform)QuickMark.transform).sizeDelta;
        Mark.GetComponent<Image>().sprite = IconFailure;
        newPrefab.RedMark = Mark.gameObject;
        if (newUntypedPrefab is SaveSlotWithModListPCView pcView)
        { 
          m_config_PC = pcView;
#if DEBUG
          Main.Logger.Log("Generated SaveSlotWithModListPCView config");
#endif
        }
        else if (newUntypedPrefab is SaveSlotWithModListConsoleView consoleView)
        {
          m_config_Console = consoleView;
#if DEBUG
          Main.Logger.Log("Generated SaveSlotWithModListConsoleView config");
#endif
        }
        else
          throw new Exception("Generated a config which is neither PC nor Console!");

        return newUntypedPrefab;
      }
      catch (Exception ex)
      {
        Main.Logger.LogException(ex);
        originalPrefab = oldPrefab.gameObject;
        return oldPrefab;
      }

    }
    static void MemberWiseCloneView(SaveSlotBaseView newPrefab, SaveSlotBaseView oldPrefab)
    {
      foreach (var field in oldPrefab.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Concat(typeof(SaveSlotBaseView).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)))
      {
        field.SetValue(newPrefab, field.GetValue(oldPrefab));
      }
    }
    [HarmonyPatch]
    static class FixVirtualListfabricIndices
    {

      static bool FixVirtualListIndices(bool previousResult, Type t, VirtualListViewsFabric fabric)
      {
        if (t != typeof(SaveSlotWithModListVM))
          return previousResult;
        if (!previousResult)
        {
          var code = fabric.GetElementHashCode(t, 0);
          var anotherCode = fabric.GetElementHashCode(typeof(SaveSlotVM), 0);
          var Index = fabric.m_Prefabs.Length;
          fabric.m_Indices.Add(code, Index);
          var list = new IVirtualListElementView[Index + 1];
          for (var i = 0; i < Index; i++)
            list[i] = fabric.m_Prefabs[i];
          var oldPrefab = (SaveSlotBaseView) fabric.m_Prefabs[fabric.m_Indices[anotherCode]];
          SaveSlotBaseView newPrefab = UnityEngine.Object.Instantiate(TryGetConfig(oldPrefab));

          list[Index] = newPrefab;
          fabric.m_Prefabs = list;
          Index = fabric.m_Pools.Length;
          var pools = new Queue<IVirtualListElementView>[Index + 1];
          for (var i = 0; i < Index; i++)
            pools[i] = fabric.m_Pools[i];
          pools[Index] = new();
          fabric.m_Pools = pools;
        }
        return true;
      }

      [HarmonyPatch(typeof(VirtualListViewsFabric), nameof(VirtualListViewsFabric.GetIndex))]
      [HarmonyTranspiler]
      static IEnumerable<CodeInstruction> VirtualListViewsFabric_GetIndex_PatchToAddNewView(IEnumerable<CodeInstruction> instructions)
      {
        var _instr = instructions.ToList();

        var index = -1;
        var info = typeof(VirtualListViewsFabric).GetField(nameof(VirtualListViewsFabric.m_Indices), BindingFlags.NonPublic | BindingFlags.Instance);

        for (var i = 0; i < _instr.Count; i++)
          if (_instr[i].opcode == OpCodes.Ldarg_0 &&
              _instr[i + 1].opcode == OpCodes.Ldfld &&
              _instr[i + 1].operand as FieldInfo == info &&
              _instr[i + 2].opcode == OpCodes.Ldloc_3 &&
              _instr[i + 3].opcode == OpCodes.Callvirt &&
              ((_instr[i + 3].operand as MethodInfo)?.Name.StartsWith("ContainsKey") ?? false)
              )
          {
            index = i;
            break;
          }

        if (index == -1)
        {
          Main.Logger.Error("VirtualListViewsFabric_GetIndex_PatchToAddNewView - FAILED TO FIND TRANSPILER INDEX!");
          return instructions;
        }

        var toInsert = new CodeInstruction[]
        {
        new(OpCodes.Ldloc_0),
        new(OpCodes.Ldarg_0),
        CodeInstruction.Call((bool a, Type t, VirtualListViewsFabric f) => FixVirtualListIndices(a, t, f))
        };

        _instr.InsertRange(index + 4, toInsert);
        return _instr;
      }

    }
  }
  internal interface ISaveSlotWithModListView
  {
    [SerializeField]
    GameObject? GreenMark { get; set; }
    [SerializeField]
    GameObject? OrangeMark { get; set; }
    [SerializeField]
    GameObject? RedMark { get; set; }

    internal SaveSlotWithModListVM saveSlotWithModListVM { get; set; }
    internal abstract void UpdateModStateIndicator(ModRecordState state);
  }
  internal class SaveSlotWithModListPCView : SaveSlotPCView, ISaveSlotWithModListView
  {
    public GameObject? GreenMark { get { return _greenMark; } set { _greenMark = value; } }
    [SerializeField]
    GameObject? _greenMark; 
    public GameObject? OrangeMark { get { return _orangeMark; } set { _orangeMark = value; } }
    [SerializeField]
    GameObject? _orangeMark;
    public GameObject? RedMark { get { return _redMark; } set { _redMark = value; } }
    [SerializeField]
    GameObject? _redMark;
    public SaveSlotWithModListVM saveSlotWithModListVM
    {
      get
      {
        return (SaveSlotWithModListVM) ViewModel;
      }
      set
      {
        ViewModel = value;
      }
    }

    public override void BindViewImplementation()
    {
      try
      {
        base.BindViewImplementation();
      }
      catch (Exception ex)
      {
        Main.Logger.LogException(ex);
      }
      if (saveSlotWithModListVM is null)
      {
        Main.Logger.Warning($"SaveSlotWithModListView BindViewImplementation - save slot {ViewModel?.Reference?.Name ?? "NULL"} is trying to bind to bind to something that's not a saveSlotWithModListVM");
        return;
      }
      saveSlotWithModListVM.StateOfMods.Value = ModRecordState.Undefined;
      AddDisposable(saveSlotWithModListVM.StateOfMods.Subscribe(UpdateModStateIndicator));
      saveSlotWithModListVM.Refresh();
      AddDisposable(EventBus.Subscribe(saveSlotWithModListVM));
    }

    public override void DestroyViewImplementation()
    {
      EventBus.Unsubscribe(saveSlotWithModListVM);
      base.DestroyViewImplementation();
    }

    public void UpdateModStateIndicator (ModRecordState state)
    {
      try
      {
        ExtensionSaveSlotWithModListView.UpdateModStateIndicator(this, state);
      }
      catch (Exception ex)
      {
        Main.Logger.LogException(ex);
        Main.Logger.Log($"Slot is {ViewModel?.Reference?.Name ?? "NULL"}. Red is null? {RedMark == null}. Orange is null? {OrangeMark == null}. Green is null? {GreenMark == null}");
      }
    }
  }
  
  [HarmonyPatch(typeof(SaveSlotConsoleView))]
  internal class SaveSlotWithModListConsoleView : SaveSlotConsoleView, ISaveSlotWithModListView, IFunc02ClickHandler
  {
    public GameObject? GreenMark { get { return _greenMark; } set { _greenMark = value; } }
    [SerializeField]
    GameObject? _greenMark;
    public GameObject? OrangeMark { get { return _orangeMark; } set { _orangeMark = value; } }
    [SerializeField]
    GameObject? _orangeMark;
    public GameObject? RedMark { get { return _redMark; } set { _redMark = value; } }
    [SerializeField]
    GameObject? _redMark;
    public SaveSlotWithModListVM saveSlotWithModListVM
    {
      get
      {
        return (SaveSlotWithModListVM)ViewModel;
      }
      set
      {
        ViewModel = value;
      }
    }

    public override void BindViewImplementation()
    {
      try
      {
        base.BindViewImplementation();
      }
      catch (Exception ex)
      {
        Main.Logger.LogException(ex);
      }
      if (saveSlotWithModListVM is null)
      {
        Main.Logger.Warning($"SaveSlotWithModListView BindViewImplementation - save slot {ViewModel?.Reference?.Name ?? "NULL"} is trying to bind to bind to something that's not a saveSlotWithModListVM");
        return;
      }
      saveSlotWithModListVM.StateOfMods.Value = ModRecordState.Undefined;
      AddDisposable(saveSlotWithModListVM.StateOfMods.Subscribe(UpdateModStateIndicator));
      saveSlotWithModListVM.Refresh();
      AddDisposable(EventBus.Subscribe(saveSlotWithModListVM));
    }

    public override void DestroyViewImplementation()
    {
      EventBus.Unsubscribe(saveSlotWithModListVM);
      base.DestroyViewImplementation();
    }

    public void UpdateModStateIndicator(ModRecordState state)
    {
      try
      {
        ExtensionSaveSlotWithModListView.UpdateModStateIndicator(this, state);
      }
      catch (Exception ex)
      {
        Main.Logger.LogException(ex);
        Main.Logger.Log($"Slot is {ViewModel?.Reference?.Name ?? "NULL"}. Red is null? {RedMark == null}. Orange is null? {OrangeMark == null}. Green is null? {GreenMark == null}");
      }
    }

    [HarmonyPatch(nameof(SaveSlotConsoleView.CanFunc02Click))]
    [HarmonyPostfix]
    public static bool CanFunc02Click(bool __result, SaveSlotConsoleView __instance)
    {
      if (__instance is SaveSlotWithModListConsoleView view)
        return view.saveSlotWithModListVM != null && view.saveSlotWithModListVM.AllMods.Any();

      return __result;
    }

    [HarmonyPatch(nameof(SaveSlotConsoleView.GetFunc02ClickHint))]
    [HarmonyPostfix]
    public static string GetFunc02ClickHint(string __result, SaveSlotConsoleView __instance)
    {
      if (__instance is SaveSlotWithModListConsoleView)
        return "GetFunc02ClickHintTest";

      return __result;
    }

    [HarmonyPatch(nameof(SaveSlotConsoleView.OnFunc02Click))]
    [HarmonyPostfix]
    public static void OnFunc02Click(SaveSlotConsoleView __instance)
    {
      if (__instance is SaveSlotWithModListConsoleView @this)
        EventBus.RaiseEvent((ITooltipHandler h) => h.HandleInfoRequest(new TooltipTemplateModRecord(TooltipTemplateModRecordEnum.All, @this)), true);
    }

  }
}
