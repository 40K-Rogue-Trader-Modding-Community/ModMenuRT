using Kingmaker.Settings;
using Kingmaker.Settings.Entities;
using Kingmaker.Settings.Interfaces;
using ModMenu.Settings;

namespace ModMenu.NewTypes
{
  internal class SettingsEntityModMenuEntry : SettingsEntity<ModsMenuEntry>
  {
    internal static SettingsEntityModMenuEntry instance = new(SettingsController.Instance, "modsmenu.entrystaticinstance", ModsMenuEntry.EmptyInstance);
    private SettingsEntityModMenuEntry(ISettingsController settingsController, string key, ModsMenuEntry defaultValue) : base(settingsController, key, defaultValue, false, false) {} 
  }
}
