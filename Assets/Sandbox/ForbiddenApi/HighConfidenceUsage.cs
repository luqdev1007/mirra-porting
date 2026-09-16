using UnityEngine;

namespace Sandbox.ForbiddenApi
{
    /// <summary>
    /// Every rule of the table, written the way a game usually writes it: bare names with
    /// using UnityEngine, so every finding here is High confidence.
    /// Read values go into public fields, otherwise the compiler warns about unused locals.
    /// </summary>
    public static class HighConfidenceUsage
    {
        public static float Scale;
        public static float Volume;
        public static bool AudioPaused;
        public static bool CursorShown;
        public static CursorLockMode CursorLock;
        public static SystemLanguage Language;
        public static bool Mobile;
        public static DeviceType Device;
        public static string DataPath;
        public static bool Background;
        public static ScreenOrientation Orientation;

        public static void TimeAndAudio()
        {
            Time.timeScale = 0f;                       // EXPECT API.TIMESCALE_WRITE Error High false
            Scale = Time.timeScale;                    // EXPECT API.TIMESCALE_READ Warning High false
            AudioListener.volume = 0f;                 // EXPECT API.AUDIO_VOLUME_WRITE Error High false
            Volume = AudioListener.volume;             // EXPECT API.AUDIO_VOLUME_READ Warning High false
            AudioListener.pause = true;                // EXPECT API.AUDIO_PAUSE_WRITE Error High false
            AudioPaused = AudioListener.pause;         // EXPECT API.AUDIO_PAUSE_READ Warning High false
        }

        public static void CursorAndScreen()
        {
            Cursor.visible = false;                    // EXPECT API.CURSOR_VISIBLE_WRITE Error High false
            CursorShown = Cursor.visible;              // EXPECT API.CURSOR_VISIBLE_READ Warning High false
            Cursor.lockState = CursorLockMode.Locked;  // EXPECT API.CURSOR_LOCK_WRITE Error High false
            CursorLock = Cursor.lockState;             // EXPECT API.CURSOR_LOCK_READ Warning High false
            Screen.orientation = ScreenOrientation.Portrait;   // EXPECT API.ORIENTATION_WRITE Warning High false
            Screen.autorotateToPortrait = true;                // EXPECT API.ORIENTATION_WRITE Warning High false
            Screen.autorotateToPortraitUpsideDown = false;     // EXPECT API.ORIENTATION_WRITE Warning High false
            Screen.autorotateToLandscapeLeft = true;           // EXPECT API.ORIENTATION_WRITE Warning High false
            Screen.autorotateToLandscapeRight = true;          // EXPECT API.ORIENTATION_WRITE Warning High false
            Orientation = Screen.orientation;          // reading the orientation is allowed
        }

        public static void Saves()
        {
            PlayerPrefs.SetInt("level", 1);            // EXPECT API.PLAYERPREFS Error High false
            PlayerPrefs.Save();                        // EXPECT API.PLAYERPREFS Error High false
            DataPath = Application.persistentDataPath; // EXPECT API.PERSISTENT_DATA_PATH Warning High false
        }

        public static void Platform()
        {
            Application.OpenURL("https://mirra.games"); // EXPECT API.OPEN_URL Error High false
            Application.Quit();                         // EXPECT API.QUIT Error High false
            System.Action exit = Application.Quit;      // EXPECT API.QUIT Error High false
            exit();
            Language = Application.systemLanguage;      // EXPECT API.SYSTEM_LANGUAGE Error High false
            Mobile = Application.isMobilePlatform;      // EXPECT API.IS_MOBILE Error High false
            Device = SystemInfo.deviceType;             // EXPECT API.IS_MOBILE Error High false
            Application.runInBackground = false;        // EXPECT API.RUN_IN_BACKGROUND_WRITE Warning High false
            Background = Application.runInBackground;   // reading it is allowed
        }
    }
}
