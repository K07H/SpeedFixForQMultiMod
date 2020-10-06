using HarmonyLib;
using QModManager.API;
using System;
using System.Globalization;
using System.Reflection;

namespace SpeedFixForQMultiMod
{
    public static class CyclopsMotorModeFixer
    {
        public static bool QMultiModIsPresent = false;
        public static float CyclopsForwardAccelMultiplier = 1.0f;
        public static float CyclopsVerticalAccelMultiplier = 1.0f;

        //public void ChangeCyclopsMotorMode(CyclopsMotorMode.CyclopsMotorModes newMode)
        public static void ChangeCyclopsMotorMode_Postfix(CyclopsMotorMode __instance)
        {
            if (QMultiModIsPresent)
            {
                float forward = __instance.subController.BaseForwardAccel;
                float vertical = __instance.subController.BaseVerticalAccel;
                __instance.subController.BaseForwardAccel *= CyclopsForwardAccelMultiplier;
                __instance.subController.BaseVerticalAccel *= CyclopsVerticalAccelMultiplier;
                if (QModManager.Utility.Logger.DebugLogsEnabled)
                    QPatch.Log(string.Format(CultureInfo.InvariantCulture, "INFO: Restored QMultiMod Cyclops speed multipliers: BaseForwardAccel=[{0}]*[{1}]=>[{2}] BaseVerticalAccel=[{3}]*[{4}]=>[{5}].",
                        forward,
                        CyclopsForwardAccelMultiplier,
                        __instance.subController.BaseForwardAccel,
                        vertical,
                        CyclopsVerticalAccelMultiplier,
                        __instance.subController.BaseVerticalAccel));
            }
        }
    }

    public class QPatch
    {
        private const string ModId = "SpeedFixForQMultiMod";
        private static bool _success = true;

        private static Harmony HarmonyInstance = null;

        internal static void Log(string str) => Console.WriteLine("[" + ModId + "] " + str);

        private static bool InitializeHarmony()
        {
            if (HarmonyInstance == null)
                if ((HarmonyInstance = new Harmony("com.osubmarin.cyclopsdockingmod")) == null)
                    Log("ERROR: Unable to initialize Harmony!");
            return HarmonyInstance != null;
        }
        
        public static void Patch()
        {
            Log("INFO: Initializing...");
            try
            {
                if (!InitializeHarmony())
                    return;
                IQMod qMultiMod = QModServices.Main.FindModById("qmultimod.mod");
                if (qMultiMod != null && qMultiMod.Enable && qMultiMod.LoadedAssembly != null)
                {
                    Type qMultiModSettings = qMultiMod.LoadedAssembly.GetType("QMultiMod.QMultiModSettings", false);
                    if (qMultiModSettings != null)
                    {
                        FieldInfo settingsInstanceField = qMultiModSettings.GetField("instance", BindingFlags.NonPublic | BindingFlags.Static);
                        FieldInfo forwardField = qMultiModSettings.GetField("CyclopsForwardAccelMultiplier", BindingFlags.Public | BindingFlags.Instance);
                        FieldInfo verticalField = qMultiModSettings.GetField("CyclopsVerticalAccelMultiplier", BindingFlags.Public | BindingFlags.Instance);
                        if (settingsInstanceField != null && forwardField != null && verticalField != null)
                        {
                            object settingsInstance = settingsInstanceField.GetValue(null);
                            if (settingsInstance != null)
                            {
                                MethodInfo changeCyclopsMotorModeMethod = typeof(CyclopsMotorMode).GetMethod("ChangeCyclopsMotorMode", BindingFlags.Public | BindingFlags.Instance);
                                MethodInfo changeCyclopsMotorModePostfix = typeof(CyclopsMotorModeFixer).GetMethod("ChangeCyclopsMotorMode_Postfix", BindingFlags.Public | BindingFlags.Static);
                                if (changeCyclopsMotorModeMethod != null && changeCyclopsMotorModePostfix != null)
                                {
                                    HarmonyInstance.Patch(changeCyclopsMotorModeMethod, null, new HarmonyMethod(changeCyclopsMotorModePostfix));
                                    CyclopsMotorModeFixer.CyclopsForwardAccelMultiplier = (float)forwardField.GetValue(settingsInstance);
                                    CyclopsMotorModeFixer.CyclopsVerticalAccelMultiplier = (float)verticalField.GetValue(settingsInstance);
                                    CyclopsMotorModeFixer.QMultiModIsPresent = true;
                                    Log(string.Format(CultureInfo.InvariantCulture, "INFO: Loaded QMultiMod Cyclops speed multiplier settings (forward=[{0}] vertical=[{1}]).", CyclopsMotorModeFixer.CyclopsForwardAccelMultiplier, CyclopsMotorModeFixer.CyclopsVerticalAccelMultiplier));
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                _success = false;
                Log(string.Format("ERROR: Exception caught! Message=[{0}] StackTrace=[{1}]", e.Message, e.StackTrace));
                if (e.InnerException != null)
                    Log(string.Format("ERROR: Inner exception => Message=[{0}] StackTrace=[{1}]", e.InnerException.Message, e.InnerException.StackTrace));
            }
            Log(_success ? "INFO: Initialized successfully." : "ERROR: Initialization failed.");
        }
    }
}
