using UnityEngine;

namespace Mycelia.App
{
    /// <summary>
    /// Target frame rate per scene (performance budget): 60 fps in gameplay, 30 fps everywhere else to save battery
    /// and heat. The loader applies it as each scene opens; a scene may lower it while idle, e.g. when paused.
    /// </summary>
    public static class FrameRatePolicy
    {
        public const int Gameplay = 60;
        public const int Menus = 30;

        public static int TargetFor(SceneId scene) => scene switch
        {
            SceneId.Game => Gameplay,
            _ => Menus,
        };

        public static void Apply(SceneId scene) => Application.targetFrameRate = TargetFor(scene);
    }
}
