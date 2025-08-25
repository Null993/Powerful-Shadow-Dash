using System.Reflection;
using Modding;
using Satchel.BetterMenus;
using Logger = Modding.Logger;

namespace ShadowDashMod
{
    // 可保存的全局设置
    public class GlobalSettings
    {
        public bool Enabled = true;
        public bool DebugLog = false;
    }

    public class ShadowDashMod
        : Mod
        , IGlobalSettings<GlobalSettings>
        , ICustomMenuMod
        , ITogglableMod
    {
        internal static ShadowDashMod Instance { get; private set; }
        internal static GlobalSettings GS { get; private set; } = new GlobalSettings();

        // 使用反射访问私有字段
        private static readonly FieldInfo shadowDashTimerField = typeof(HeroController).GetField("shadowDashTimer", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo dashCooldownTimerField = typeof(HeroController).GetField("dashCooldownTimer", BindingFlags.NonPublic | BindingFlags.Instance);

        public ShadowDashMod() : base("Powerful Shadow Dash")
        {
            Instance = this;
        }

        public override string GetVersion() => "1.0.0";

        // ===== IGlobalSettings 接口实现 =====
        public void OnLoadGlobal(GlobalSettings settings)
        {
            GS = settings ?? new GlobalSettings();
        }

        public GlobalSettings OnSaveGlobal()
        {
            return GS;
        }

        // ===== Mod 初始化 / 卸载 =====
        public override void Initialize()
        {
            Log("[PowerfulShadowDashMod] Initializing");

            // 使用 On.Hook 替代 Harmony
            On.HeroController.HeroDash += HeroController_HeroDash_Patch;

            Log("[PowerfulShadowDashMod] Initialized");
        }

        public void Unload()
        {
            On.HeroController.HeroDash -= HeroController_HeroDash_Patch;
            Log("[PowerfulShadowDashMod] Unloaded");
        }

        // ===== 修改 HeroDash 方法 =====
        private void HeroController_HeroDash_Patch(On.HeroController.orig_HeroDash orig, HeroController self)
        {
            if (!GS.Enabled)
            {
                orig(self);
                return;
            }

            Log("[PowerfulShadowDashMod] Applying zero cooldown shadow dash");

            // 保存原始值
            float originalShadowDashTimer = 0f;
            float originalDashCooldownTimer = 0f;

            if (shadowDashTimerField != null)
            {
                originalShadowDashTimer = (float)shadowDashTimerField.GetValue(self);
            }

            if (dashCooldownTimerField != null)
            {
                originalDashCooldownTimer = (float)dashCooldownTimerField.GetValue(self);
            }

            try
            {
                // 强制设置 shadowDashTimer 为 0.01f 以触发影冲
                if (shadowDashTimerField != null)
                {
                    shadowDashTimerField.SetValue(self, 0f);
                }

                // 可选：也可以设置普通冲刺的冷却时间为0
                //if (dashCooldownTimerField != null)
                //{
                //    dashCooldownTimerField.SetValue(self, 0f);
                //}

                // 调用原始方法
                orig(self);
            }
            finally
            {
                // 恢复原始值
                if (shadowDashTimerField != null)
                {
                    shadowDashTimerField.SetValue(self, originalShadowDashTimer);
                }

                if (dashCooldownTimerField != null)
                {
                    dashCooldownTimerField.SetValue(self, originalDashCooldownTimer);
                }
            }
        }

        // ===== 菜单 =====
        private Menu menuRef;
        public bool ToggleButtonInsideMenu => true;

        public MenuScreen GetMenuScreen(MenuScreen modListMenu, ModToggleDelegates? toggles)
        {
            if (menuRef == null)
            {
                menuRef = new Menu("Powerful Shadow Dash", new Element[]
                {
                    Blueprints.CreateToggle(
                        toggles.Value,
                        "Mod Enabled",
                        "Remove cooldown from shadow dash ability"
                    ),
                    new HorizontalOption(
                        "Debug Log", "",
                        new string[] {"OFF","ON"},
                        (i) => GS.DebugLog = (i == 1),
                        () => GS.DebugLog ? 1 : 0
                    )
                });
            }
            return menuRef.GetMenuScreen(modListMenu);
        }

        // ===== 日志工具 =====
        internal static void Log(string msg)
        {
            if (GS.DebugLog) Logger.Log(msg);
        }
    }
}