using Kingmaker;
using Kingmaker.Localization;
using Kingmaker.Localization.Shared;
using Kingmaker.Code.UI.MVVM.View.Settings.PC.Entities;
using Kingmaker.Code.UI.MVVM.View.Settings.PC;
using Kingmaker.Code.UI.MVVM.VM.Settings.Entities;
using UnityEngine;
using Kingmaker.Utility;
using Kingmaker.Code.UI.MVVM.VM.Settings;
using UniRx;
using Kingmaker.UI.Common;
using static UnityModManagerNet.UnityModManager;
using UnityEngine.UI;
using Kingmaker.Localization.Enums;
using Newtonsoft.Json.Utilities;
using Kingmaker.Utility.UnityExtensions;
using Kingmaker.UI.Models.SettingsUI.SettingAssets;
using Kingmaker.PubSubSystem.Core.Interfaces;
using Kingmaker.PubSubSystem.Core;
using Kingmaker.PubSubSystem;
using Kingmaker.Code.UI.MVVM.View.Common.PC;

namespace ModMenu
{
  /// <summary>
  /// Generic utils for simple operations.
  /// </summary>
  public static class Helpers
  {
    private static readonly List<LocalString> Strings = new();
    internal static LocalizedString EmptyString = CreateString("", "");

    public static LocalizedString CreateString(string key, string enGB, string ruRU = null, string zhCN = null, string deDE = null, string frFR = null)
    {
      var localString = new LocalString(key, enGB, ruRU, zhCN, deDE, frFR);
      Strings.Add(localString);

      return localString.LocalizedString;
    }

    internal static Sprite CreateSprite(string embeddedImage)
    {
      var assembly = Assembly.GetExecutingAssembly();
      using var stream = assembly.GetManifestResourceStream(embeddedImage);
      byte[] bytes = new byte[stream.Length];
      stream.Read(bytes, 0, bytes.Length);
      var texture = new Texture2D(128, 128, TextureFormat.RGBA32, false);
      _ = texture.LoadImage(bytes);
      texture.name = embeddedImage + ".texture";
      //the default value for PixelsPerUnit is 1, meaning that Sprite's Prefered size becomes 100 times more. So must be set to 100% manually
      var sprite = Sprite.Create(texture, new(0, 0, texture.width, texture.height), Vector2.zero, 100);
      sprite.name = embeddedImage + ".sprite";
      return sprite;
    }

    private class LocalString
    {
      public readonly LocalizedString LocalizedString;
      private readonly string enGB;
      private readonly string ruRU;
      private readonly string zhCN;
      private readonly string deDE;
      private readonly string frFR;
      const string NullString = "<null>";

      static LocalString()
      {
        ((ILocalizationProvider)LocalizationManager.Instance).LocaleChanged += new((Locale _) 
          => { foreach (var locStr in Strings) locStr.Register(); });
      }

      public LocalString(string key, string enGB, string ruRU, string zhCN, string deDE, string frFR)
      {
        LocalizedString = new LocalizedString() { m_Key = key };
        this.enGB = enGB;
        this.ruRU = ruRU;
        this.zhCN = zhCN;
        this.deDE = deDE;
        this.frFR = frFR;
        if (LocalizationManager.Instance.CurrentPack != null)
          Register();
      }

      public void Register()
      {
        string localized;
        if (LocalizationManager.Instance.CurrentPack.Locale is Locale.enGB)
        {
          localized = enGB;
          goto putString;
        }

        localized = LocalizationManager.Instance.CurrentPack.Locale switch
        {
          Locale.ruRU => ruRU,
          Locale.zhCN => zhCN,
          Locale.deDE => deDE,
          Locale.frFR => frFR,
          _ => enGB
        };

        if (localized.IsNullOrEmpty() || localized == NullString)
          localized = enGB;

        ;putString:
        LocalizationManager.Instance.CurrentPack.PutString(LocalizedString.m_Key, localized);
      }
    }

    /// <summary>
    /// Updated the Description field of a setting that is set with WithLongDescription(). The update works by finding the Title of the setting.
    /// Titles that you wish to update must be unique inorder to update the correct setting.
    /// </summary>
    /// <typeparam name="T">
    /// This needs to be a class of the type that inherits from SettingsEntityWithValueVM that you wish to update. Such as if you are updating a slider the typeparam
    /// must be SettingsEntitySliderVM
    /// </typeparam>
    public class SettingsDescriptionUpdater<T>
        where T : SettingsEntityWithValueVM
    {
      private Transform? settingsUI;

      private List<SettingsEntityWithValueView<T>>? settingViews;


      private bool Ensure()
      {
        // UI tends to change frequently, ensure that eveything is up to date.

        settingsUI = (Game.Instance.RootUiContext.m_CommonView as CommonPCView)?.m_SettingsPCView.View.transform ;
        if (settingsUI == null)
          return false;

        settingViews = settingsUI.gameObject.GetComponentsInChildren<SettingsEntityWithValueView<T>>().ToList();

        if (settingViews == null || settingViews.Count == 0)
          return false;

        return true;
      }

      /// <summary>
      /// This is the method that updates the Description of the SettingsEntityWithValueVM
      /// </summary>
      /// <param name="title">
      /// This is the UNIQUE Title of the setting that you wish to edit. If the Title is
      /// not unique then the incorrect setting may be updated.
      /// </param>
      /// <param name="description">
      /// The text you wish to set the Description to.
      /// </param>
      /// <returns>
      /// Will return true if the update was successfull.
      /// </returns>
      
      public bool TryUpdate(string title, string description)
      {
        if (!Ensure()) return false;

        T? svm = null;

        foreach (var settingView in settingViews ?? [])
        {
          var test = (T)settingView.GetViewModel();
          if (test.Title.Text.Equals(title))
          {
            svm = test;
              break;
          }
        }

        if (svm == null)
          return false;

        svm.GetType().GetField("Description").SetValue(svm, description);

        EventBus.RaiseEvent<ISettingsDescriptionUIHandler>(handler => handler.HandleShowSettingsDescription(svm.UISettingsEntity, null, description));

        return true;
      }
    }

  }
}
