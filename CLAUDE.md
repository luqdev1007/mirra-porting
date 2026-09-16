# CLAUDE.md — mirra-porting

## Что это

Репозиторий — Unity-проект, в котором разрабатывается пакет `com.luqmus.mirra-porting`: Editor-инструменты для портирования чужих Unity-игр под MirraSDK5 (студия Mirra Games публикует их на Яндекс Игры, YouTube Playables, CrazyGames и другие веб-площадки).

- Сам пакет: `Packages/com.luqmus.mirra-porting/` (embedded-пакет).
- Всё остальное в репозитории (`Assets/Sandbox/` и т. д.) — песочница для проверки пакета. В порты не переносится.
- В портируемый проект пакет копируется, используется (вручную или агентом через Unity MCP) и удаляется. **В репозиторий порта он не коммитится.**

Инструменты:
1. **Сканер проекта** — отчёт «что мешает пройти модерацию»: окно + запуск без UI с отчётом в `Library/MirraPorting/`.
2. **Профили площадок** — после того как сканер готов. Не начинать без отдельной задачи.

Сканер находит механические проблемы. Смысловые решения (где показывать рекламу, когда `GameIsReady`, когда сохранять) и проверка в браузере остаются за человеком — инструмент их не заменяет и не должен делать вид, что заменяет.

## Как работать

- Отвечать на русском. Код, идентификаторы и комментарии в коде — на английском.
- Порядок: **разведка → план → подтверждение → реализация.** Текущая задача лежит в `docs/stage-N.md`. Перед написанием кода показать план по файлам и дождаться подтверждения.
- **В начале сессии** прочитать этот файл и раздел «Решения по ходу этапа» в текущем `docs/stage-N.md`: там всё согласованное, что не следует из текста задачи, и где остановилась работа. Каждое новое согласованное решение дописывать туда же и коммитить вместе с кодом.
- После каждого логического шага: дождаться компиляции, прочитать консоль Unity через MCP, прогнать EditMode-тесты. Шаг не закончен, пока в консоли есть ошибки или предупреждения от сборок `Luqmus.*`.
- Коммиты небольшие, по одному на шаг.
- **Не выдумывать API** — ни Unity, ни MirraSDK. Не уверен в сигнатуре или в том, что API есть в 2022.3 — сказать об этом и спросить, а не угадывать. Формат значений (пути, регистр, порядок) проверять на реальном проекте, reflection показывает только сигнатуры.
- **Тексты находок** (Message, Suggestion) — только факты из этого файла и документации SDK. Не приписывать SDK поведение, которое нигде не описано: отчёт читают коллеги и принимают написанное за правду.
- Удалять мёртвый код, а не комментировать.

## Жёсткие ограничения пакета

1. **Совместимость:** Unity 2022.3.x, 6.0.x, 6.3.x. Проект разработки — на 2022.3 (самая старая поддерживаемая), активная платформа — WebGL.
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
    Core/          модель находок и правил, область сканирования, SourceFile, ScanContext, ScanRunner
    Code/          лексер C#, условная компиляция, using-контекст, объявленные имена (без Unity API)
    Checks/Code/   проверки исходников (ForbiddenApiCheck и т. д.)
    Checks/Assets/ проверки ассетов и сцен (этап 3)
    Checks/Build/  Player Settings, Build Profiles, темплейт (этап 4)
    Fixes/         окна массовых исправлений (этап 3)
    Report/        Markdown и JSON (DTO для JsonUtility — отдельно от модели Core)
    UI/            ScannerWindow
  Editor.Sdk/
    Luqmus.MirraPorting.Editor.Sdk.asmdef
  Tests/Editor/
    Luqmus.MirraPorting.Editor.Tests.asmdef
    Core/  Code/  Checks/           тесты по зеркальной структуре; исходники-примеры — строками в тестах
Assets/Sandbox/    скрипты с намеренными нарушениями и случаями классификации + EXPECTED.md
tools/             install.ps1, uninstall.ps1
docs/              задачи этапов с разделом «Решения по ходу этапа»
```

## Проверенные факты Unity

Выяснено на реальном проекте, повторно не проверять:

- `CompilationPipeline.Assembly.sourceFiles` — пути относительно проекта, прямые слэши. Для пакетов любого источника (Embedded, Local, Git, Registry) префикс виртуальный `Packages/<name>/…`, не `resolvedPath`.
- `AssembliesType.Editor` содержит **все** сборки, компилируемые для редактора, включая рантаймовые. Признак editor-only — разность с `AssembliesType.Player`. Набор Player-сборок зависит от активной платформы.
- asmdef без ограничений платформ сильнее специальной папки `Editor`: файл в `RuntimeAsmdef/Editor/` попадает в игровую сборку.
- Тесты embedded-пакета видны в Test Runner без `testables` в `manifest.json`.
- Unity записывает embedded-пакет в `Packages/packages-lock.json`.
- NUnit: public-метод теста не может принимать internal-тип в параметрах (CS0051) — через `TestCase` передавать только `string`/`bool`/числа.

## Установка в порт и удаление

Пакет переносится копированием папки, без правки `manifest.json` порта:

1. `tools/install.ps1 -Project <путь к порту>` копирует `Packages/com.luqmus.mirra-porting` в `<порт>/Packages/` и добавляет строку `Packages/com.luqmus.mirra-porting/` в `<порт>/.git/info/exclude` (локальный игнор, в репозиторий порта не попадает).
2. Unity подхватывает embedded-пакет сам. Если в порте есть ошибки компиляции, Unity может не загрузить пакет — сначала проект должен компилироваться.
3. `tools/uninstall.ps1 -Project <путь к порту>` удаляет папку и выводит `git status --short` порта.

В этом репозитории запись пакета в `packages-lock.json` коммитится. В порте — нет: перед коммитом в порт проверить `git status`.

## MirraSDK5

- Пакет: `com.romanlee17.mirrasdk5`, актуальная версия в main — 5.1.31. Репозиторий: `https://github.com/MirraSDK/SDK5.git`. Документация: `https://romanlee17.gitbook.io/mirrasdk`.
- Сборки: `MirraGames.SDK`, `MirraGames.SDK.Common`, `MirraGames.SDK.Editor`, `MirraGames.SDK.MirraWeb`, `MirraGames.SDK.Addressables`, `MirraGames.SDK.Fallback`, `MirraGames.SDK.UnityEngine`, `MirraGames.SDK.System`, `MirraGames.SDK.Prototype`, `MirraGames.SDK.Android`, `MirraGames.SDK.iOS`.
- Точка входа: класс `MirraSDK` в namespace `MirraGames.SDK` (подтверждено компилятором). Интерфейсы, типы и события — `MirraGames.SDK.Common`.
- Из `Editor.Sdk` ссылаться только на `MirraGames.SDK` и `MirraGames.SDK.Common`.
- Версию SDK в проекте определять через `UnityEditor.PackageManager.PackageInfo`, без ссылок на сборки SDK.
- **Источник истины по API** — исходники пакета (`Library/PackageCache/com.romanlee17.mirrasdk5@<хеш>/`), а не документация: в её примерах есть ошибки (пропущенное имя переменной в примере rewarded, висячая запятая в `Payments.Purchase`, устаревшее `MirraSDK.Remote.ReleaseAddressable` — правильно `MirraSDK.Assets.ReleaseAddressable`, `.ogg` с `AudioType.MPEG`).
- Для WebGL используется конфигурация `MirraWeb` в окне Toolkit (меню `MirraSDK5`): фреймворк `mirraSDK.js` сам определяет площадку в рантайме. По README распознаются CoolMath, CrazyGames, GameDistribution, GamePix, Lagged, OK, PlayDeck, Poki, VK, Y8, YandexGames, WG Playground, YouTube Playables. Playgama — через отдельный Playgama Bridge.
- Перед установкой SDK проект должен компилироваться. Если в порте другой платформенный SDK (PluginYG2 и т. п.): сначала установить MirraSDK, перевести вызовы, потом удалить старый SDK.
- `MirraSDK.Time.Scale` и `MirraSDK.Audio.Volume/Pause` по документации кэшируют значение игры и восстанавливают его после паузы на рекламу (например, замедление времени). Прямая запись в Unity API это значение теряет — это и есть причина правил замены.
- При импорте SDK создаёт `Assets/Resources/MirraSDK5/`: `Preferences.json` и `.asset` с UI Toolkit-разметкой окон SDK. В порте это часть интеграции и коммитится. `Preferences.json` — вероятное хранилище настроек Toolkit, смотреть при проектировании инструмента 2. После временной установки SDK в этом репозитории папку удалять.
- Проверки ассетов (этапы 3–4) должны исключать `Assets/Resources/MirraSDK5/` — asmdef там нет, правило исключения SDK по asmdef эту папку не покрывает.

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

SDK сам управляет паузой, звуком и курсором во время рекламы. Прямая запись в эти поля ломает паузу.

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

## Настройки сборки по площадкам (для этапа 4 и инструмента 2)

Выжимка из матрицы студии — только то, что проверяется в редакторе или влияет на код.

| Площадка | Decompression Fallback | IAP | Внешние ссылки | Ограничения |
|---|---|---|---|---|
| Яндекс Игры | не указано | да, с консумированием | — | ≤ 100 МБ распакованно |
| YouTube Playables | выкл | — | запрещены | сжатие выкл, Name Files As Hashes, любой файл ≤ 30 МБ; лок ориентации запрещён; Esc закрывает модалки |
| CrazyGames | выкл | убрать → rewarded | — | Brotli; ≤ 50 МБ десктоп / 20 МБ мобайл в архиве |
| Playgama | выкл | — | — | нужен Playgama Bridge; нельзя rewarded «+1 жизнь» при каждой смерти |
| GamePix | вкл | убрать → rewarded | запрещены (включая «Больше игр», «Оцените нас», политику конфиденциальности) | — |
| GameDistribution | вкл | убрать → rewarded/interstitial | — | GameID в Toolkit |
| Y8 | вкл | — | запрещены | interstitial не чаще раза в 2 мин |
| Lagged | вкл | — | — | interstitial не чаще раза в минуту |
| WG Playground | — | убрать → rewarded | — | — |
| VK / OK | — | — | нет ссылок на сторонние сервисы | interstitial ≥ 30 с, не на старте |

Для всех: Run in Background включён; `index.html` в корне zip. Неверный Decompression Fallback обычно проявляется так: локально работает, на площадке не грузится.

Открытые вопросы (не предполагать ответ):
- переключает ли Toolkit Decompression Fallback и сжатие под площадку сам (кроме YouTube, где это делает конфигурация YouTube);
- возвращает ли `Payments.IsAvailable` false там, где IAP надо убрать — из C#-исходников не решается, определяет `mirraSDK.js` в рантайме.

## MirraGamesTemplate — факты (для этапа 4)

Исходник: `github.com/mirrasdk/MirraGamesTemplate`, main, коммит `e68491e` (04.07.2025). В порте лежит в `Assets/WebGLTemplates/MirraGamesTemplate/`, в Player Settings выбирается как `PROJECT:MirraGamesTemplate`.

**Источник истины — код темплейта, документация студии устарела:** там сказано, что `"1.777 "` выбросит исключение (на деле значение обрезается `trim` и работает), что Mobile aspect ratio одно поле (их два), а Worker filename и Show diagnostics описаны как поля темплейта (в Unity 2022+ это встроенные переменные).

Пользовательские переменные (все в `unityApp.js`, значения читаются `PlayerSettings.GetTemplateCustomValue(name)`):

| Переменная | Разбор | Поведение и ловушки |
|---|---|---|
| `PORTRAIT_ONLY` | `toBoolean` | Истина только для `"true"`, `"True"`, `"1"`. `"TRUE"`, `"true "`, `"yes"` — ложь без предупреждения |
| `LANDSCAPE_ONLY` | `toBoolean` | То же. Если оба истинны — `throw` до `startLoading()`: **игра не загружается вообще** |
| `MOBILE_PORTRAIT_ASPECT_RATIO` | `toNumber` | Пусто = без лока. Строка `trim`-ится, поэтому `"1.777 "` работает. `"16/9"` и `"16 / 9"` работают. `"16:9"` → `parseFloat` → **16**. `"1,777"` → **1** |
| `MOBILE_LANDSCAPE_ASPECT_RATIO` | `toNumber` | То же |
| `DESKTOP_ASPECT_RATIO` | `toNumber` | То же |
| `MATCH_WEBGL_TO_CANVAS_SIZE` | `toBoolean` | Рекомендация: пусто |
| `AUTO_SYNC_PERSISTENT_DATA_PATH` | `toBoolean` | Рекомендация: пусто — по данным студии в некоторых проектах вызывает вылеты |
| `DEVICE_PIXEL_RATIO` | `toNumber` | Применяется, если не 0. `"0,8"` → 0 → **молча игнорируется**. Рекомендация: пусто; 0.6–0.9 — рычаг FPS ценой чёткости |

Лок ориентации — только оверлей с картинкой на мобильных, не системный лок. При ошибке загрузки темплейт показывает игроку `alert(message)`.

`SHOW_DIAGNOSTICS` включается галочкой Show diagnostics overlay в Publishing Settings (Unity 2022+).

Встроенные переменные Unity, которые использует темплейт: `PRODUCT_NAME`, `COMPANY_NAME`, `PRODUCT_VERSION`, `LOADER_FILENAME`, `DATA_FILENAME`, `FRAMEWORK_FILENAME`, `CODE_FILENAME`, `WORKER_FILENAME`, `SYMBOLS_FILENAME`, `USE_THREADS`, `USE_WASM`, `SHOW_DIAGNOSTICS`, `BACKGROUND_COLOR`, `SPLASH_SCREEN_STYLE`, `PRODUCT_DESCRIPTION` (последнюю — проверить, встроенная ли; если в Player Settings появляется поле Product Description, значит пользовательская).

Картинки: `gameBackground.png` (рекомендуется 1280×720) и `gameIcon.png` (256×256 или 512×512) в корне темплейта, PNG, имена не менять — на них ссылается `TemplateData/style.css`.

## Этапы

1. `docs/stage-1.md` — ядро: лексер, запрещённые API, отчёт MD/JSON, окно, запуск без UI.
2. Хоткеи (Escape, Ctrl/Cmd), следы чужих SDK (PluginYG/YG2, GamePush, CrazySDK, Playgama и т. п.), ранняя инициализация до `WaitForProviders`.
3. AudioClip Load Type с массовым исправлением, UnityEvent → методы с `Quit`/`OpenURL`.
4. WebGL Player Settings, Build Profiles (Unity 6), проверки темплейта, выбор целевых площадок.

После этапа 4 — инструмент 2 (профили площадок).
