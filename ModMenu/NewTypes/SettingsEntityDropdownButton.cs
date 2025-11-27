using Kingmaker.Localization;
using Kingmaker.PubSubSystem;
using Kingmaker.Code.UI.MVVM.View.Settings.PC.Entities;
using Kingmaker.Code.UI.MVVM.VM.Settings.Entities;
using Kingmaker.UI.Models.SettingsUI;
using Kingmaker.UI.Models.SettingsUI.SettingAssets.Dropdowns;
using Kingmaker.PubSubSystem.Core;
using Owlcat.Runtime.UI.Controls.Button;
using Owlcat.Runtime.UI.VirtualListSystem.ElementSettings;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UniRx.Triggers;
using Owlcat.Runtime.UniRx;
using Owlcat.Runtime.UI.Controls.Other;
using Kingmaker.Utility.UnityExtensions;

namespace ModMenu.NewTypes
{
  public class UISettingsEntityDropdownButton : UISettingsEntityDropdownInt
  {
    internal LocalizedString ButtonText;
    internal Sprite ButtonImage;
    internal Action<int> OnClick;

    internal static UISettingsEntityDropdownButton Create(
      LocalizedString description, LocalizedString longDescription, LocalizedString buttonText, Action<int> onClick, Sprite buttonImage = null)
    {
      var button = ScriptableObject.CreateInstance<UISettingsEntityDropdownButton>();
      button.m_Description = description;
      button.m_TooltipDescription = longDescription;

      button.ButtonText = buttonText;
      button.OnClick = onClick;
      button.ButtonImage = buttonImage;
      button.m_EncyclopediaDescription = new();
      return button;
    }

    public override SettingsListItemType? Type => SettingsListItemType.Custom;
  }

  internal class SettingsEntityDropdownButtonVM : SettingsEntityDropdownVM
  {
    private readonly UISettingsEntityDropdownButton buttonEntity;

    public string Text => buttonEntity.ButtonText;
    public Sprite ButtonImage => buttonEntity.ButtonImage;

    internal SettingsEntityDropdownButtonVM(UISettingsEntityDropdownButton buttonEntity) : base(buttonEntity)
    {
      this.buttonEntity = buttonEntity;
    }

    public void PerformClick(int selectedIndex)
    {
      buttonEntity.OnClick?.Invoke(selectedIndex);
    }
  }

  internal class SettingsEntityDropdownButtonView
    : SettingsEntityDropdownPCView
  {
    private SettingsEntityDropdownButtonVM VM => ViewModel as SettingsEntityDropdownButtonVM;

    public override VirtualListLayoutElementSettings LayoutSettings
    {
      get
      {
        bool set_mOverrideType = m_LayoutSettings == null;
        m_LayoutSettings ??= new();
        if (set_mOverrideType)
        {
          SettingsEntityPatches.OverrideType.SetValue(
            m_LayoutSettings, VirtualListLayoutElementSettings.LayoutOverrideType.UnityLayout);
        }

        return m_LayoutSettings;
      }
    }

    private VirtualListLayoutElementSettings m_LayoutSettings;

    public override void BindViewImplementation()
    {
      base.BindViewImplementation();
      Title.text = VM.Title;
      ButtonLabel.text = VM.Text;
      ButtonLabel.gameObject.SetActive(!ButtonLabel.text.IsNullOrEmpty());
      ButtonImage.sprite = VM.ButtonImage;
      ButtonImage.gameObject.SetActive(ButtonImage.sprite != null);
      AddDisposable(Button.OnLeftClickAsObservable().Subscribe(_OnLeftClick));
      AddDisposable(Button.OnMouseEnterAsObservable().Subscribe(_OnPointerEnter));
      AddDisposable(Button.OnMouseExitAsObservable().Subscribe(_OnPointerExit));


      SetupColor(isHighlighted: false);
    }

    private Color NormalColor = Color.clear;
    private Color HighlightedColor = new(0.52f, 0.52f, 0.52f, 0.29f);

    // These must be public or they'll be null
    public Image HighlightedImage;
    public TextMeshProUGUI Title;
    public OwlcatMultiButton Button;
    public TextMeshProUGUI ButtonLabel;
    public Image ButtonImage;

    private void SetupColor(bool isHighlighted)
    {
      if (HighlightedImage != null)
      {
        HighlightedImage.color = isHighlighted ? HighlightedColor : NormalColor;
      }
    }

    public void _OnPointerEnter()
    {
      EventBus.RaiseEvent(delegate (ISettingsDescriptionUIHandler h)
      {
        h.HandleShowSettingsDescription(ViewModel.UISettingsEntity, ViewModel.Title, ViewModel.Description);
      },
      true);
      SetupColor(isHighlighted: true);
    }

    public void _OnPointerExit()
    {
      EventBus.RaiseEvent(delegate (ISettingsDescriptionUIHandler h)
      {
        h.HandleHideSettingsDescription();
      },
      true);
      SetupColor(isHighlighted: false);
    }

    public void _OnLeftClick()
    {
      VM.PerformClick(VM.GetTempValue());
    }
  }
}
