using MelonLoader;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace SpeedrunTools {
public static class BuildInfo {
  public const string Name = "SpeedrunTools";
  public const string Author = "jakzo";
  public const string Company = null;
  public const string Version = AppVersion.Value;
  public const string DownloadLink =
      "https://boneworks.thunderstore.io/package/jakzo/SpeedrunTools/";

  public const string Developer = "Stress Level Zero";
  public const string GameName = "BONEWORKS";
}

public class SpeedrunTools : MelonMod {
  private static readonly Feature[] features = {
    new FeatureSpeedrun(), new FeatureRemoveBossClawRng(),
    new FeatureTeleport(), new FeatureBlindfold(),
    // new FeatureReplay(),
    // new FeatureControlTesting(),
    // new FeatureFps(),
    // new FeatureTas(),
    // new FeatureFixPhysicsRate(),
  };

  private static List<Feature> enabledFeatures = new List<Feature>();

  private static Dictionary<Feature, Pref<bool>> featureEnabledPrefs =
      new Dictionary<Feature, Pref<bool>>();

  private static Hotkeys s_hotkeys = new Hotkeys();

  public static bool s_isRunActive = false;

  private static IEnumerable<Hotkey> GetHotkeys(Feature feature) {
    foreach (var field in feature.GetType().GetFields()) {
      var type = Nullable.GetUnderlyingType(field.FieldType) ?? field.FieldType;
      if (type == typeof(Hotkey)) {
        var hotkey = field.GetValue(feature) as Hotkey;
        yield return hotkey;
      }
    }
  }

  private static void EnableFeature(Feature feature) {
    if (enabledFeatures.Contains(feature))
      return;
    MelonLogger.Msg($"Enabling feature: {feature.GetType().Name}");
    enabledFeatures.Add(feature);
    foreach (var hotkey in GetHotkeys(feature))
      s_hotkeys.AddHotkey(feature, hotkey);
    feature.OnEnabled();
  }

  private static void DisableFeature(Feature feature) {
    if (!enabledFeatures.Contains(feature))
      return;
    MelonLogger.Msg($"Disabling feature: {feature.GetType().Name}");
    enabledFeatures.Remove(feature);
    foreach (var hotkey in GetHotkeys(feature))
      s_hotkeys.RemoveHotkey(hotkey);
    feature.OnDisabled();
  }

  private static void OnFeatureCallback(Action<Feature> action) {
    foreach (var feature in enabledFeatures) {
      if (s_isRunActive && !feature.isAllowedInRuns)
        continue;
      try {
        action(feature);
      } catch (Exception ex) {
        MelonLogger.Error(ex);
      }
    }
  }

  public override void OnApplicationStart() {
    Directory.CreateDirectory(Utils.DIR);

    Utils.s_prefCategory = MelonPreferences.CreateCategory(Utils.PREF_CATEGORY);
    Utils.PrefDebug.Create();
    foreach (var feature in features) {
      var name = feature.GetType().Name;
      var enabledPref =
          new Pref<bool>() { Id = $"enable{name}", Name = $"Enable {name}",
                             DefaultValue = true };
      enabledPref.Create();
      featureEnabledPrefs[feature] = enabledPref;

      foreach (var field in feature.GetType().GetFields()) {
        var type =
            Nullable.GetUnderlyingType(field.FieldType) ?? field.FieldType;
        if (type.IsGenericType &&
            type.GetGenericTypeDefinition() == typeof(Pref<>)) {
          var pref = field.GetValue(feature) as IPref;
          pref.Create();
        }
      }

      if (enabledPref.Read())
        EnableFeature(feature);
    }

    Utils.LogDebug("OnApplicationStart");
    OnFeatureCallback(feature => feature.OnApplicationStart());
  }

  public override void OnSceneWasLoaded(int buildIndex, string sceneName) {
    Utils.LogDebug("OnSceneWasLoaded");
    foreach (var feature in features) {
      if (featureEnabledPrefs[feature].Read()) {
        EnableFeature(feature);
      } else {
        DisableFeature(feature);
      }
    }
    OnFeatureCallback(feature =>
                          feature.OnSceneWasLoaded(buildIndex, sceneName));
  }

  public override void OnSceneWasInitialized(int buildIndex, string sceneName) {
    Utils.LogDebug("OnSceneWasInitialized");
    s_hotkeys.Init();
    OnFeatureCallback(feature =>
                          feature.OnSceneWasInitialized(buildIndex, sceneName));
  }

  public override void OnUpdate() {
    s_hotkeys.OnUpdate();
    OnFeatureCallback(feature => feature.OnUpdate());
  }

  public override void OnFixedUpdate() {
    OnFeatureCallback(feature => feature.OnFixedUpdate());
  }
}
}
