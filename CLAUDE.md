# CLAUDE.md — mirra-porting

## Что это

Репозиторий — Unity-проект, в котором разрабатывается пакет `com.luqmus.mirra-porting`: Editor-инструменты для портирования чужих Unity-игр под MirraSDK5 (студия Mirra Games публикует их на Яндекс Игры, YouTube Playables, CrazyGames и другие веб-площадки).

- Сам пакет: `Packages/com.luqmus.mirra-porting/` (embedded-пакет).
- Всё остальное в репозитории (`Assets/Sandbox/` и т. д.) — песочница для проверки пакета. В порты не переносится.
- В портируемый проект пакет копируется, используется (вручную или агентом через Unity MCP) и удаляется. **В репозиторий порта он не коммитится.**

Инструменты:
1. **Сканер проекта** — отчёт «что мешает пройти модерацию»: окно + запуск без UI с отчётом в `Library/MirraPorting/`.
2. **Профили площадок** — после того как сканер готов. Не начинать без отдельной задачи.

## Как работать

- Отвечать на русском. Код, идентификаторы и комментарии в коде — на английском.
- Порядок: **разведка → план → подтверждение → реализация.** Текущая задача лежит в `docs/stage-N.md`. Перед написанием кода показать план по файлам и дождаться подтверждения.
- После каждого логического шага: дождаться компиляции, прочитать консоль Unity через MCP, прогнать EditMode-тесты. Шаг не закончен, пока в консоли есть ошибки или предупреждения от сборок `Luqmus.*`.
- Коммиты небольшие, по одному на шаг.
- **Не выдумывать API** — ни Unity, ни MirraSDK. Не уверен в сигнатуре или в том, что API есть в 2022.3 — сказать об этом и спросить, а не угадывать.
- Удалять мёртвый код, а не комментировать.

## Жёсткие ограничения пакета

1. **Совместимость:** Unity 2022.3.x, 6.0.x, 6.3.x. Проект разработки — на 2022.3 (самая старая поддерживаемая).
2. **C# 9.** Нельзя: file-scoped namespace, `global using`, raw string literals, `required`, `record` (нет `IsExternalInit` в 2022.3).
3. **Только публичный, не obsolete API 2022.3.** Если в Unity 6 API другой — изолировать под `#if UNITY_6000_0_OR_NEWER` и пометить комментарием `// VERIFY-6000`, чтобы проверить в 6.x вручную.
4. **Ноль зависимостей.** В `package.json` нет `dependencies`. Не использовать Newtonsoft.Json, Roslyn, System.Text.Json, сторонние DLL. JSON — только `JsonUtility`.
5. **Работа без MirraSDK.** Сборка `Luqmus.MirraPorting.Editor` компилируется и работает на проекте без SDK. Всё, что ссылается на типы SDK, — только в `Luqmus.MirraPorting.Editor.Sdk`, которая включается через Version Defines.
6. **Все asmdef с `"autoReferenced": false`** — код игры не должен иметь возможности сослаться на пакет, иначе его нельзя будет удалить.
7. **Ничего не писать в проект.** Пакет не создаёт и не меняет файлы в `Assets`, `ProjectSettings`, `Packages/manifest.json`, пока пользователь явно не нажал кнопку исправления. Отчёты — в `Library/MirraPorting/`. Настройки окна — `EditorPrefs` с ключом, включающим путь проекта.
8. **Путь, который вызывает агент (пункт меню «Scan and Export»), без модальных диалогов** и без ожидания ввода.
9. **Сканер не бросает исключений наружу.** Сбой на одном файле → находка `SCAN.INTERNAL_ERROR` с текстом исключения, сканирование продолжается.
10. **Ядро разбора кода (`Editor/Code/`) без `UnityEngine`/`UnityEditor`**, чтобы тестироваться на строках и потом при необходимости стать консольной утилитой.

## Структура

```
Packages/com.luqmus.mirra-porting/
  package.json
  Editor/
    Luqmus.MirraPorting.Editor.asmdef
    AssemblyInfo.cs                 // InternalsVisibleTo для тестов
    Core/          модель находок, область сканирования, ScanRunner, IScanCheck
    Code/          лексер C# и сопоставление обращений (без Unity API)
    Checks/Code/   проверки исходников
    Checks/Assets/ проверки ассетов и сцен (этап 3)
    Checks/Build/  Player Settings, Build Profiles, темплейт (этап 4)
    Fixes/         окна массовых исправлений (этап 3)
    Report/        Markdown и JSON
    UI/            ScannerWindow
  Editor.Sdk/
    Luqmus.MirraPorting.Editor.Sdk.asmdef
  Tests/Editor/
    Luqmus.MirraPorting.Editor.Tests.asmdef
    Fixtures/      *.txt — исходники-примеры, не компилируются
Assets/Sandbox/    скрипты и сцены с намеренными нарушениями + EXPECTED.md
tools/             install.ps1, uninstall.ps1
docs/              задачи этапов
```

## Установка в порт и удаление

Пакет переносится копированием папки, без правки `manifest.json` порта:

1. `tools/install.ps1 -Project <путь к порту>` копирует `Packages/com.luqmus.mirra-porting` в `<порт>/Packages/` и добавляет строку `Packages/com.luqmus.mirra-porting/` в `<порт>/.git/info/exclude` (локальный игнор, в репозиторий порта не попадает).
2. Unity подхватывает embedded-пакет сам.
3. `tools/uninstall.ps1 -Project <путь к порту>` удаляет папку и выводит `git status --short` порта.

Unity может записать embedded-пакет в `Packages/packages-lock.json` порта. Перед коммитом в порт проверить `git status`: этот файл не должен меняться из-за пакета.

## MirraSDK5

- Пакет: `com.romanlee17.mirrasdk5`, актуальная версия в main — 5.1.31. Репозиторий: `https://github.com/MirraSDK/SDK5.git`.
- Сборки: `MirraGames.SDK`, `MirraGames.SDK.Common`, `MirraGames.SDK.Editor`, `MirraGames.SDK.MirraWeb`, `MirraGames.SDK.Addressables`, `MirraGames.SDK.Fallback`, `MirraGames.SDK.UnityEngine`, `MirraGames.SDK.System`, `MirraGames.SDK.Prototype`, `MirraGames.SDK.Android`, `MirraGames.SDK.iOS`.
- Из `Editor.Sdk` ссылаться только на `MirraGames.SDK` и `MirraGames.SDK.Common`.
- Версию SDK в проекте определять через `UnityEditor.PackageManager.PackageInfo`, без ссылок на сборки SDK.

`PlatformType` (`MirraGames.SDK.Common`):
```csharp
public enum PlatformType
{
    Localhost, NintendoSwitch, Editor, Unknown, AmazonAppStore, AppleAppStore, Aptoide,
    HuaweiAppGallery, GooglePlay, MiAppMall, OneStore, RuStore, SamsungGalaxyStore,
    XiaomiGames, Steam, VKPlay, UniversalWeb, CrazyGames, GameDistribution, Poki, Y8,
    YandexGames, OK, VK, PlayDeck, Telegram, CoolMath, Lagged, Zygomatic, MSN, GamePix,
    MirraHub, PlaygamaBridge, LumixGames, WelwiseGames, Playgama, Facebook, QATool,
    Discord, GamePush, WgPlayground, YouTubePlayables,
}
```
`DeploymentType`: `Unknown, Editor, Web, Mobile, Standalone, Console`.

Чем отличаются `Playgama` и `PlaygamaBridge` — неизвестно, не предполагать.

## Таблица замен Unity API (правила модерации)

SDK сам управляет паузой и звуком во время рекламы. Прямая запись в эти поля ломает паузу.

| Unity API | Замена |
|---|---|
| `Time.timeScale` | `MirraSDK.Time.Scale` |
| `AudioListener.volume` | `MirraSDK.Audio.Volume` |
| `AudioListener.pause` | `MirraSDK.Audio.Pause` |
| `Cursor.visible` | `MirraSDK.Device.CursorVisible` |
| `Cursor.lockState` | `MirraSDK.Device.CursorLock` |
| `PlayerPrefs.*`, файловые сейвы | `MirraSDK.Data.*` |
| `Application.OpenURL` | `MirraSDK.Device.OpenURL` (и проверить, разрешены ли ссылки на площадке) |
| `Application.systemLanguage` | `MirraSDK.Language.Current` |
| `Application.isMobilePlatform`, `SystemInfo.deviceType` | `MirraSDK.Device.IsMobile` |
| `Application.Quit` / кнопка «Выход» | удалить |

## MirraGamesTemplate — факты (для этапа 4)

Исходник: `github.com/mirrasdk/MirraGamesTemplate`, main, коммит `e68491e` (04.07.2025). В порте лежит в `Assets/WebGLTemplates/MirraGamesTemplate/`, в Player Settings выбирается как `PROJECT:MirraGamesTemplate`.

Пользовательские переменные (все в `unityApp.js`, значения читаются `PlayerSettings.GetTemplateCustomValue(name)`):

| Переменная | Разбор | Поведение и ловушки |
|---|---|---|
| `PORTRAIT_ONLY` | `toBoolean` | Истина только для `"true"`, `"True"`, `"1"`. `"TRUE"`, `"true "`, `"yes"` — ложь без предупреждения |
| `LANDSCAPE_ONLY` | `toBoolean` | То же. Если оба истинны — `throw` до `startLoading()`: **игра не загружается вообще** |
| `MOBILE_PORTRAIT_ASPECT_RATIO` | `toNumber` | Пусто = без лока. Строка `trim`-ится, поэтому `"1.777 "` работает. `"16/9"` и `"16 / 9"` работают. `"16:9"` → `parseFloat` → **16**. `"1,777"` → **1** |
| `MOBILE_LANDSCAPE_ASPECT_RATIO` | `toNumber` | То же |
| `DESKTOP_ASPECT_RATIO` | `toNumber` | То же |
| `MATCH_WEBGL_TO_CANVAS_SIZE` | `toBoolean` | Рекомендация: пусто |
| `AUTO_SYNC_PERSISTENT_DATA_PATH` | `toBoolean` | Рекомендация: пусто |
| `DEVICE_PIXEL_RATIO` | `toNumber` | Применяется, если не 0. `"0,8"` → 0 → **молча игнорируется** |

Лок ориентации — только оверлей с картинкой на мобильных, не системный лок.

Встроенные переменные Unity, которые использует темплейт: `PRODUCT_NAME`, `COMPANY_NAME`, `PRODUCT_VERSION`, `LOADER_FILENAME`, `DATA_FILENAME`, `FRAMEWORK_FILENAME`, `CODE_FILENAME`, `WORKER_FILENAME`, `SYMBOLS_FILENAME`, `USE_THREADS`, `USE_WASM`, `SHOW_DIAGNOSTICS`, `BACKGROUND_COLOR`, `SPLASH_SCREEN_STYLE`, `PRODUCT_DESCRIPTION` (последнюю — проверить, встроенная ли; если в Player Settings появляется поле Product Description, значит пользовательская).

Картинки: `gameBackground.png` и `gameIcon.png` в корне темплейта, на них ссылается `TemplateData/style.css`.

## Этапы

1. `docs/stage-1.md` — ядро: лексер, запрещённые API, отчёт MD/JSON, окно, запуск без UI.
2. Хоткеи (Escape, Ctrl/Cmd), следы чужих SDK, ранняя инициализация до `WaitForProviders`.
3. AudioClip Load Type с массовым исправлением, UnityEvent → методы с `Quit`/`OpenURL`.
4. WebGL Player Settings, Build Profiles (Unity 6), проверки темплейта, выбор целевых площадок.

После этапа 4 — инструмент 2 (профили площадок).
