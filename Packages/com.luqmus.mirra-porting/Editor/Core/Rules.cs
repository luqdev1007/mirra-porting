using System;
using System.Collections.Generic;

namespace Luqmus.MirraPorting.Core
{
    /// <summary>
    /// Catalog of every rule the scanner can report. The window and the report read their text
    /// from here; checks reference the fields directly.
    ///
    /// The wording says what breaks and what to use instead, without claiming more about the SDK
    /// than the porting notes state.
    /// </summary>
    internal static class Rules
    {
        /// <summary>
        /// A check threw. The scan keeps going, so this is a warning about incomplete coverage
        /// rather than an error in the project. The exception text goes into the finding message.
        /// </summary>
        internal static readonly Rule ScanInternalError = new Rule(
            "SCAN.INTERNAL_ERROR",
            Categories.Scan,
            Severity.Warning,
            "Проверка завершилась с ошибкой, часть проекта могла остаться непросканированной",
            "Сообщите об ошибке разработчику пакета вместе с текстом исключения из сообщения находки");

        private const string TimeSuggestion = "MirraSDK.Time.Scale";
        private const string VolumeSuggestion = "MirraSDK.Audio.Volume";
        private const string AudioPauseSuggestion = "MirraSDK.Audio.Pause";
        private const string CursorVisibleSuggestion = "MirraSDK.Device.CursorVisible";
        private const string CursorLockSuggestion = "MirraSDK.Device.CursorLock";
        private const string DataSuggestion = "Сейвы — через MirraSDK.Data.*; логи и кэш можно оставить";

        internal static readonly Rule TimeScaleWrite = Write("API.TIMESCALE_WRITE", "Time.timeScale", TimeSuggestion);
        internal static readonly Rule TimeScaleRead = Read("API.TIMESCALE_READ", "Time.timeScale", TimeSuggestion);

        internal static readonly Rule AudioVolumeWrite =
            Write("API.AUDIO_VOLUME_WRITE", "AudioListener.volume", VolumeSuggestion);

        internal static readonly Rule AudioVolumeRead =
            Read("API.AUDIO_VOLUME_READ", "AudioListener.volume", VolumeSuggestion);

        internal static readonly Rule AudioPauseWrite =
            Write("API.AUDIO_PAUSE_WRITE", "AudioListener.pause", AudioPauseSuggestion);

        internal static readonly Rule AudioPauseRead =
            Read("API.AUDIO_PAUSE_READ", "AudioListener.pause", AudioPauseSuggestion);

        internal static readonly Rule CursorVisibleWrite =
            Write("API.CURSOR_VISIBLE_WRITE", "Cursor.visible", CursorVisibleSuggestion);

        internal static readonly Rule CursorVisibleRead =
            Read("API.CURSOR_VISIBLE_READ", "Cursor.visible", CursorVisibleSuggestion);

        internal static readonly Rule CursorLockWrite =
            Write("API.CURSOR_LOCK_WRITE", "Cursor.lockState", CursorLockSuggestion);

        internal static readonly Rule CursorLockRead =
            Read("API.CURSOR_LOCK_READ", "Cursor.lockState", CursorLockSuggestion);

        internal static readonly Rule PlayerPrefs = new Rule(
            "API.PLAYERPREFS",
            Categories.ForbiddenApi,
            Severity.Error,
            "PlayerPrefs не переживает смену браузера и не попадает в облачные сейвы площадки",
            "MirraSDK.Data.*");

        internal static readonly Rule OpenUrl = new Rule(
            "API.OPEN_URL",
            Categories.ForbiddenApi,
            Severity.Error,
            "Application.OpenURL на части площадок блокируется, а внешние ссылки бывают запрещены правилами",
            "MirraSDK.Device.OpenURL, и проверить, разрешены ли внешние ссылки на целевой площадке");

        internal static readonly Rule Quit = new Rule(
            "API.QUIT",
            Categories.ForbiddenApi,
            Severity.Error,
            "Веб-игра не может закрыть вкладку, а кнопка «Выход» не проходит модерацию",
            "Удалить вызов вместе с кнопкой «Выход»");

        internal static readonly Rule SystemLanguage = new Rule(
            "API.SYSTEM_LANGUAGE",
            Categories.ForbiddenApi,
            Severity.Error,
            "Application.systemLanguage даёт язык браузера или системы, а не язык, выбранный на площадке",
            "MirraSDK.Language.Current");

        internal static readonly Rule IsMobile = new Rule(
            "API.IS_MOBILE",
            Categories.ForbiddenApi,
            Severity.Error,
            "Unity определяет тип устройства в WebGL по-своему, результат может разойтись с площадкой",
            "MirraSDK.Device.IsMobile");

        internal static readonly Rule PersistentDataPath = new Rule(
            "API.PERSISTENT_DATA_PATH",
            Categories.ForbiddenApi,
            Severity.Warning,
            "Application.persistentDataPath: файловые сейвы в вебе не переживают смену браузера",
            DataSuggestion);

        internal static readonly Rule FileWrite = new Rule(
            "API.FILE_WRITE",
            Categories.ForbiddenApi,
            Severity.Warning,
            "Запись файлов в вебе не переживает смену браузера и не попадает в облачные сейвы",
            DataSuggestion);

        internal static readonly Rule RunInBackgroundWrite = new Rule(
            "API.RUN_IN_BACKGROUND_WRITE",
            Categories.ForbiddenApi,
            Severity.Warning,
            "Run in Background должен оставаться включённым: без него игра замирает, когда фокус уходит со страницы, в том числе на рекламу",
            "Убрать запись Application.runInBackground");

        internal static readonly Rule OrientationWrite = new Rule(
            "API.ORIENTATION_WRITE",
            Categories.ForbiddenApi,
            Severity.Warning,
            "Ориентацию задаёт WebGL-темплейт; на YouTube Playables лок ориентации запрещён",
            "Убрать управление ориентацией из кода, настроить темплейт");

        private static readonly Rule[] AllRules =
        {
            ScanInternalError,
            TimeScaleWrite,
            TimeScaleRead,
            AudioVolumeWrite,
            AudioVolumeRead,
            AudioPauseWrite,
            AudioPauseRead,
            CursorVisibleWrite,
            CursorVisibleRead,
            CursorLockWrite,
            CursorLockRead,
            PlayerPrefs,
            OpenUrl,
            Quit,
            SystemLanguage,
            IsMobile,
            PersistentDataPath,
            FileWrite,
            RunInBackgroundWrite,
            OrientationWrite,
        };

        private static readonly Dictionary<string, Rule> ById = BuildIndex(AllRules);

        /// <summary>Every known rule, in declaration order.</summary>
        internal static IReadOnlyList<Rule> All
        {
            get { return AllRules; }
        }

        /// <summary>Rule by id, or null when the id is unknown.</summary>
        internal static Rule Find(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }

            Rule rule;
            return ById.TryGetValue(id, out rule) ? rule : null;
        }

        /// <summary>
        /// Writing a value the SDK owns. The wording is the same for every such member, so that
        /// the report reads as one voice.
        /// </summary>
        private static Rule Write(string id, string member, string suggestion)
        {
            return new Rule(
                id,
                Categories.ForbiddenApi,
                Severity.Error,
                "Прямая запись " + member +
                " в обход SDK: SDK сам управляет этим значением во время рекламы, запись игры с ним конфликтует",
                suggestion);
        }

        /// <summary>Reading a value the SDK owns, which during an ad is the SDK's value.</summary>
        private static Rule Read(string id, string member, string suggestion)
        {
            return new Rule(
                id,
                Categories.ForbiddenApi,
                Severity.Warning,
                "Чтение " + member +
                " в обход SDK: во время рекламы значение выставляет SDK, и игра может принять его за своё",
                suggestion);
        }

        private static Dictionary<string, Rule> BuildIndex(Rule[] rules)
        {
            var index = new Dictionary<string, Rule>(rules.Length, StringComparer.Ordinal);
            foreach (Rule rule in rules)
            {
                index.Add(rule.Id, rule);
            }

            return index;
        }
    }
}
