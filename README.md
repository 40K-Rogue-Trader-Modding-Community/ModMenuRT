# ModMenu RT

Adds a new page to the game options for mods. This allows mods to easily implement settings using native UI instead of UMM. This does not create a save dependency.

<p align="center"><img src="Img/ModMenuRT_Example.jpg?raw=true" alt="Test Settings Screenshot"/></p>

## Installation

1. Download the latest version of the mod from the [Releases](https://github.com/40K-Rogue-Trader-Modding-Community/ModMenuRT/releases/latest) section.
1. Extract the archive into the game's UMM mod folder - `%userprofile%\AppData\LocalLow\Owlcat Games\Warhammer 40000 Rogue Trader\UnityModManager\` on Windows.

Alternatively:

1. Download and install [ModFinder RT](https://www.nexusmods.com/warhammer40kroguetrader/mods/146).
1. Run ModFinder, find the ModMenu RT listing, and click the "Install" button.

***N.B.***: If you install a mod via ModFinder that has ModMenu as a requirement, ModFinder will automatically install ModMenu if you don't already have it.

## Problems or Suggestions

File an [issue on GitHub](https://github.com/40K-Rogue-Trader-Modding-Community/ModMenuRT/issues/new) or join the official [Owlcat Discord](https://discord.com/invite/owlcat) and ask for help in the #mod-user-general channel.

### Controller Support

**The mod does not support controllers at this time**. 

## Mod Developers

### Why should you use it?

* It looks nice!
* Automatically persists your settings
* Handles restoring defaults
* Automatically persists per-save settings
* Super easy to use

### How to use it

Refer to [TestSettings](https://github.com/40K-Rogue-Trader-Modding-Community/ModMenuRT/blob/main/ModMenu/Settings/TestSettings.cs). That exercises every function supported. The API is documented and generally self-explanatory.

In your mod's `Info.json` add `ModMenu` as a requirement:

```json
"Requirements": ["ModMenu"]
```

You can also specify a minimum version:

```json
"Requirements": ["ModMenu-2.3.2"]
```

It's safest to just specify the version you build against as the minimum version, but methods added after 1.0 do specify the version in their remarks.

Install ModMenu then in your mod's csproj add `$(RogueTraderData)\UnityModManager\ModMenu\ModMenu_RT.dll` as an assembly reference (for NuGet template projects).

### Basic Usage

Create a setting:

```C#
ModMenu.AddSettings(
  SettingsBuilder.New("mymod-settings, SettingsTitle)
    .AddToggle(Toggle.New("mymod-settings-toggle", defaultValue: true, MyToggleTitle)
      .OnValueChanged(OnToggle)));
      
private static void OnToggle(bool toggleValue) {
  // The user just changed the toggle, toggleValue is the new setting.
  // If you need to react to it changing then you can do that here.
  // If you don't need to do something whenever the value changes, you can skip OnValueChanged()
}
```

Get the setting value:

```C#
ModMenu.GetSettingValue<bool>("mymod-settings-toggle");
```

**The game handles the setting value for you.** You do not need to save the setting, or set the setting to a specific value. You *can* set it if necessary but most of the time it isn't necessary. This includes saving settings that you flag as per-save using `DependsOnSave()`.

For more examples see [TestSettings](https://github.com/40K-Rogue-Trader-Modding-Community/ModMenuRT/blob/main/ModMenu/Settings/TestSettings.cs).

### Best Practices

* **Do not add settings during mod load,** without additional handling you cannot create a `LocalizedString`. I recommend adding settings before, during, or after `BlueprintsCache.Init()`.
* Don't use `IsModificationAllowed` to enable/disable a setting based on another setting. This is checked when the page is opened so it won't apply immediately.
* Indicate settings which require reboot using `WithLongDescription()`. The game's setting boolean `RequireReboot` does nothing.

Define a "root" key unique to your mod to make sure there are no key conflicts:

```C#
private const string RootKey = "mymod-settings";
```

You can then prepend this to all of your settings keys:

```C#

// Results in a settings key "mymod-settings-key"
var toggle = Toggle.New(GetKey("toggle"), MyToggleTitle);

private static string GetKey(string key)
{
  return $"{RootKey}-{key}";
}
```

Just make sure you always get the key the same way when getting a setting value.

### Settings Behavior

* Settings with `DependsOnSave()` are associated with a save slot, but do not create save dependencies
    * You do not need to handle saving or restoring settings at all, though save dependent settings may be lost if the mod is disabled
* `OnValueChanged()` is called after the user clicks "Apply" and confirms
* `OnTempValueChanged()` is called immediately after the user changes the value, but before it is applied
* A setting's value can be checked at any time by calling [GetSettingValue()](https://github.com/40K-Rogue-Trader-Modding-Community/ModMenuRT/blob/main/ModMenu/ModMenu.cs#L85)

## Acknowledgements

* A shout out to Bubbles (factsubio) who essentially wrote the new image and button settings types when WittleWolfie was about to give up.
* The modding community on [Discord](https://discord.com/invite/owlcat), an invaluable and supportive resource for help modding.
* All the Owlcat modders who came before, wrote documents, and open sourced their code.

## Interested in Creating Mods?

* Check out the [OwlcatModdingWiki](https://github.com/WittleWolfie/OwlcatModdingWiki/wiki).
* Join us on [Discord](https://discord.com/invite/owlcat) in the #mod-dev-technical channel.
