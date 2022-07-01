using MelonLoader;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SpeedrunTools
{
  class PluginResetSave : Plugin
  {
    private const int SCENE_IDX_MENU = 1;

    public readonly Hotkey HotkeyReset = new Hotkey()
    {
      Predicate = (cl, cr) => cl.GetAButton() && cl.GetBButton(),
      Handler = () =>
      {
        MelonLogger.Msg("Resetting save and returning to menu");
        // Load main menu
        SceneManager.SetActiveScene(SceneManager.GetSceneAt(SCENE_IDX_MENU));
      }
    };
  }
}
