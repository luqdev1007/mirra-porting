# Этап 1 — ядро сканера

Цель: сканер находит запрещённые Unity API в исходниках, показывает их в окне и пишет отчёт в Markdown и JSON — вручную из окна и без UI из пункта меню, который вызывает агент.

Перед кодом: прочитать `CLAUDE.md`, составить план по шагам ниже (файлы, классы, публичные методы) и дождаться подтверждения. Шаги выполнять по порядку, после каждого — компиляция, консоль, тесты, коммит.

---

## Шаг 1. Скелет

**`package.json`**
```json
{
  "name": "com.luqmus.mirra-porting",
  "version": "0.1.0",
  "displayName": "Mirra Porting Tools",
  "description": "Editor tools for porting Unity games to MirraSDK5.",
  "unity": "2022.3",
  "author": { "name": "luqmus", "url": "https://github.com/luqdev1007" }
}
```

**`Editor/Luqmus.MirraPorting.Editor.asmdef`**
```json
{
  "name": "Luqmus.MirraPorting.Editor",
  "rootNamespace": "Luqmus.MirraPorting",
  "references": [],
  "includePlatforms": ["Editor"],
  "autoReferenced": false
}
```

**`Editor.Sdk/Luqmus.MirraPorting.Editor.Sdk.asmdef`**
```json
{
  "name": "Luqmus.MirraPorting.Editor.Sdk",
  "rootNamespace": "Luqmus.MirraPorting.Sdk",
  "references": ["Luqmus.MirraPorting.Editor", "MirraGames.SDK", "MirraGames.SDK.Common"],
  "includePlatforms": ["Editor"],
  "autoReferenced": false,
  "defineConstraints": ["MIRRA_SDK5"],
  "versionDefines": [
    { "name": "com.romanlee17.mirrasdk5", "expression": "5.0.0", "define": "MIRRA_SDK5" }
  ]
}
```
Внутри один файл-заглушка: `internal static class SdkAssemblyMarker` с полем `typeof(MirraGames.SDK.MirraSDK)`. Нужен только чтобы проверить, что сборка компилируется при наличии SDK.

**`Tests/Editor/Luqmus.MirraPorting.Editor.Tests.asmdef`**
```json
{
  "name": "Luqmus.MirraPorting.Editor.Tests",
  "references": ["Luqmus.MirraPorting.Editor", "UnityEngine.TestRunner", "UnityEditor.TestRunner"],
  "includePlatforms": ["Editor"],
  "overrideReferences": true,
  "precompiledReferences": ["nunit.framework.dll"],
  "autoReferenced": false,
  "defineConstraints": ["UNITY_INCLUDE_TESTS"]
}
```
`Editor/AssemblyInfo.cs`: `[assembly: InternalsVisibleTo("Luqmus.MirraPorting.Editor.Tests")]`.

Путь к фикстурам в тестах — через `PackageInfo.FindForAssembly(...).resolvedPath`, не через жёсткий `Packages/...`. Если тесты пакета не видны в Test Runner — сообщить, не править `manifest.json` без согласования.

**Корень репозитория:** стандартный Unity `.gitignore` (`Library/`, `Temp/`, `Logs/`, `UserSettings/`, `obj/`, `*.csproj`, `*.sln`).

**`tools/install.ps1` и `tools/uninstall.ps1`** — по описанию в `CLAUDE.md`, раздел «Установка в порт». Параметр `-Project` обязательный. Проверять, что по пути есть `Packages/manifest.json`. Строку в `.git/info/exclude` не дублировать. Если `.git` нет — предупредить и продолжить.

Готово, когда: проект компилируется без MirraSDK, 0 предупреждений от `Luqmus.*`.

---

## Шаг 2. Модель находок

```
Severity:   Error | Warning | Info
Confidence: High | Medium | Low

Finding
  RuleId       string   "API.TIMESCALE_WRITE"
  Category     string   "ForbiddenApi"
  Severity
  Confidence
  EditorOnly   bool     файл из Editor-сборки или код внутри #if UNITY_EDITOR
  Path         string   путь от корня проекта с прямыми слэшами: "Assets/…" или "Packages/…"
  Line, Column int      1-based, позиция первого токена цепочки (Time в Time.timeScale)
  Snippet      string   строка исходника, trim, не длиннее 160 символов
  Message      string   что не так, по-русски
  Suggestion   string   на что заменить, по-русски
```

Правила (`Rule`: `Id`, `Category`, `Severity`, `Message`, `Suggestion`) — в одном статическом каталоге, чтобы окно и отчёт брали текст оттуда.

`IScanCheck` получает `ScanContext` (список исходников с токенами, объявленные в проекте имена типов) и добавляет находки. `SCAN.INTERNAL_ERROR` — правило для сбоев проверок.

---

## Шаг 3. Область сканирования

Файлы `.cs`:
- в `Assets/**`;
- в пакетах с источником `Embedded` или `Local` (`PackageInfo.GetAllRegisteredPackages()`), путь для отчёта — `Packages/<name>/…`.

Исключить:
- пакеты `com.luqmus.mirra-porting` и `com.romanlee17.mirrasdk5`;
- папку с `.asmdef`, имя сборки в котором начинается с `MirraGames.SDK`, и все её подпапки (SDK, лежащий в `Assets`);
- папки, имя которых оканчивается на `~` или начинается с `.` (Unity их не импортирует).

`EditorOnly = true`, если:
- файл входит в сборки `CompilationPipeline.GetAssemblies(AssembliesType.Editor)` и не входит в `AssembliesType.Player`;
- запасной вариант, если файла нет ни в одной сборке, — в пути есть сегмент `Editor`;
- или токен внутри активной ветки `#if`, условие которой содержит `UNITY_EDITOR` без отрицания (см. шаг 4).

Файлы читать как UTF-8 с учётом BOM. Битые байты не мешают: искомые идентификаторы ASCII.

---

## Шаг 4. Лексер (`Editor/Code/`, без Unity API)

Вход — текст файла, выход — список токенов с `Kind`, `Text`, `Line`, `Column`.

`Kind`: `Identifier`, `Number`, `String`, `Char`, `Punct`, `Directive`.

Требования:
- Комментарии `//` и `/* */` пропускаются, но номера строк после них правильные.
- Строки `"…"` с escape-последовательностями, verbatim `@"…"` (`""` внутри, многострочные).
- Интерполяция `$"…"`, `$@"…"`, `@$"…"`: текстовые части — токены `String`, выражения в `{…}` — обычные токены. `{{` и `}}` — экранирование. Внутри дырки `:` и `,` верхнего уровня начинают формат/выравнивание, но не внутри `()`, `[]`, вложенных строк. Вложенные строки внутри дырок.
- Символьные литералы: `'"'`, `'\''`, `'\\'`.
- Verbatim-идентификаторы `@class` → `Identifier` с текстом `class`.
- Многосимвольные операторы как один `Punct`: `== != <= >= => += -= *= /= %= &= |= ^= <<= >>= ??= ++ -- && || ?? ?. ::`. `?.` только если после точки не цифра (`x ? .5f : 1f`).
- `Directive` — `#` первым непробельным символом строки, токен до конца строки.
- Лексер **никогда не бросает исключения**. Незакрытая строка или комментарий — токен до конца файла плюс диагностика.

Поверх токенов:
- **Условная компиляция:** стек `#if/#elif/#else/#endif`. Ветка editor-only, если условие содержит `UNITY_EDITOR` без `!` перед ним; `#else` у такой ветки — не editor-only. Остальные условия не вычисляются: код во всех ветках сканируется.
- **Using-контекст файла:** алиасы `using T = UnityEngine.Time;` и `using static UnityEngine.Time;`, наличие `using System.IO;`.
- **Объявленные типы:** имя после `class`, `struct`, `enum`, `interface` во всех сканируемых файлах → общий набор для `ScanContext`.

### Обязательные тесты лексера и сопоставления

Каждый случай — фикстура или строка в тесте с ожидаемым результатом.

| Код | Ожидание |
|---|---|
| `// Time.timeScale = 0;` | ничего |
| `/* строка1\n Time.timeScale = 0; */ int a;` | ничего; `a` на строке 2 |
| `var s = "Time.timeScale = 0";` | ничего |
| `var u = "https://x.com"; Time.timeScale = 0;` | Write, правильная колонка |
| `var p = @"C:\dir\""x"""; Time.timeScale = 0;` | Write |
| `$"scale {Time.timeScale:F2}"` | Read |
| `$"{{Time.timeScale}}"` | ничего |
| `$@"a\n{Time.timeScale}"` и `@$"…"` | Read, строка 2 |
| `$"{(f ? Time.timeScale : 1f)}"` | Read |
| `$"{ "a}" + Time.timeScale }"` | Read |
| `char c = '"'; Time.timeScale = 0;` | Write |
| `Time\n    .timeScale\n    = 0f;` | Write, строка 1 |
| `UnityEngine.Time.timeScale = 1;`, `global::UnityEngine.Time.timeScale = 1;` | Write |
| `MyGame.Time.timeScale = 1;` | ничего |
| `if (Time.timeScale == 0)` | Read |
| `Time.timeScale += 1;`, `Time.timeScale++;`, `--Time.timeScale;` | Write |
| `Time.timeScale.ToString()` | Read |
| `nameof(Time.timeScale)` | ничего |
| `onClick.AddListener(Application.Quit);` | `API.QUIT` |
| `using T = UnityEngine.Time; … T.timeScale = 0;` | Write |
| `using static UnityEngine.Time; … timeScale = 0;` | Write, Confidence Low |
| в проекте объявлен `class Time`, и `Time.timeScale = 0;` | Write, Confidence Low |
| `cursor.visible = false;` (переменная) | ничего |
| `#if UNITY_EDITOR\nTime.timeScale = 0;\n#endif` | Write, EditorOnly |
| `#if !UNITY_EDITOR\nTime.timeScale = 0;\n#endif` | Write, не EditorOnly |
| `#if UNITY_EDITOR … #else\nTime.timeScale = 0;\n#endif` | Write, не EditorOnly |
| незакрытая строка в конце файла | без исключения, диагностика |
| файл с BOM и кириллицей в комментариях | позиции верные |

---

## Шаг 5. `ForbiddenApiCheck`

### Сопоставление

Цепочка: `[global ::] [UnityEngine .] Type . Member`, где `Type` — имя из таблицы или алиас из using-контекста. Если перед `Type` стоит `.` с другим квалификатором, кроме `UnityEngine`, это не совпадение. Регистр учитывается.

При `using static UnityEngine.<Type>` голое `Member` без `.` перед ним — совпадение с Confidence Low.

Если `Type` объявлен в сканируемых исходниках — Confidence Low.

Вид обращения по соседним токенам:
- следующий токен `=`, `+=`, `-=`, `*=`, `/=`, `%=`, `&=`, `|=`, `^=`, `<<=`, `>>=`, `??=`, `++`, `--`, или предыдущий `++`/`--` → **Write**;
- следующий `(` → **Call**;
- иначе → **Read** (для методов это method group, считается как Call).

Цепочка внутри `nameof(…)` пропускается.

Известный пропуск (записать в комментарий, не чинить): запись через деконструкцию `(Time.timeScale, x) = …` определяется как Read.

### Правила

| RuleId | Совпадение | Severity | Suggestion |
|---|---|---|---|
| `API.TIMESCALE_WRITE` | `Time.timeScale` Write | Error | `MirraSDK.Time.Scale` |
| `API.TIMESCALE_READ` | `Time.timeScale` Read | Warning | `MirraSDK.Time.Scale`: во время рекламы SDK ставит 0, игра может запомнить это значение |
| `API.AUDIO_VOLUME_WRITE` / `_READ` | `AudioListener.volume` | Error / Warning | `MirraSDK.Audio.Volume` |
| `API.AUDIO_PAUSE_WRITE` / `_READ` | `AudioListener.pause` | Error / Warning | `MirraSDK.Audio.Pause` |
| `API.CURSOR_VISIBLE_WRITE` / `_READ` | `Cursor.visible` | Error / Warning | `MirraSDK.Device.CursorVisible` |
| `API.CURSOR_LOCK_WRITE` / `_READ` | `Cursor.lockState` | Error / Warning | `MirraSDK.Device.CursorLock` |
| `API.PLAYERPREFS` | `PlayerPrefs.<любой член>` | Error | `MirraSDK.Data.*` |
| `API.OPEN_URL` | `Application.OpenURL` | Error | `MirraSDK.Device.OpenURL`; проверить, разрешены ли внешние ссылки на площадке |
| `API.QUIT` | `Application.Quit` | Error | удалить вместе с кнопкой «Выход» |
| `API.SYSTEM_LANGUAGE` | `Application.systemLanguage` | Error | `MirraSDK.Language.Current` |
| `API.IS_MOBILE` | `Application.isMobilePlatform`, `SystemInfo.deviceType` | Error | `MirraSDK.Device.IsMobile` |
| `API.PERSISTENT_DATA_PATH` | `Application.persistentDataPath` | Warning | файловые сейвы → `MirraSDK.Data.*`; логи и кэш можно оставить |
| `API.FILE_WRITE` | `File.WriteAllText/WriteAllBytes/WriteAllLines/AppendAllText/AppendAllLines` | Warning | то же. `System.IO.File` или `File` при `using System.IO` → High, иначе Low |
| `API.RUN_IN_BACKGROUND_WRITE` | `Application.runInBackground` Write | Warning | Run in Background должен оставаться включённым |
| `API.ORIENTATION_WRITE` | `Screen.orientation`, `Screen.autoRotateToPortrait`, `Screen.autoRotateToPortraitUpsideDown`, `Screen.autoRotateToLandscapeLeft`, `Screen.autoRotateToLandscapeRight` Write | Warning | ориентация задаётся темплейтом; на YouTube Playables лок запрещён |

Находки с `EditorOnly = true` получают Severity Info независимо от правила.

---

## Шаг 6. `ScanRunner` и отчёты

- `ScanRunner.Run(...)` — синхронный запуск всех проверок, прогресс через callback, поддержка отмены.
- Пункт меню **`Tools/Mirra Porting/Scan and Export`**: запуск без отмены и диалогов, запись в `Library/MirraPorting/report.json` и `report.md`. Запись атомарная: во временный файл, потом замена. Прогресс-бар закрывается в `finally`. В конце одна строка в консоль:
  `[MirraPorting] Scan finished: 12 errors, 30 warnings, 5 info, 840 files, 1234 ms → Library/MirraPorting/report.json`
- Пункт меню **`Tools/Mirra Porting/Scanner`** — открыть окно.

### JSON

`JsonUtility`: enum и даты писать строками, отсутствующие значения — пустой строкой.

```json
{
  "schemaVersion": 1,
  "tool": { "name": "com.luqmus.mirra-porting", "version": "0.1.0" },
  "project": { "path": "D:/ports/game", "unityVersion": "2022.3.62f1", "mirraSdkVersion": "" },
  "startedAt": "2026-09-15T12:00:00Z",
  "finishedAt": "2026-09-15T12:00:01Z",
  "durationMs": 1234,
  "summary": { "errors": 12, "warnings": 30, "info": 5, "filesScanned": 840 },
  "findings": [
    {
      "ruleId": "API.TIMESCALE_WRITE",
      "category": "ForbiddenApi",
      "severity": "Error",
      "confidence": "High",
      "editorOnly": false,
      "path": "Assets/Scripts/PauseMenu.cs",
      "line": 42,
      "column": 9,
      "snippet": "Time.timeScale = 0f;",
      "message": "Прямая запись Time.timeScale ломает паузу SDK во время рекламы",
      "suggestion": "MirraSDK.Time.Scale"
    }
  ]
}
```
Находки отсортированы: Severity, Category, RuleId, Path, Line. Тест: отчёт → JSON → обратно через `JsonUtility`, поля совпадают.

### Markdown

1. Заголовок, дата, версия Unity, версия MirraSDK (или «не установлен»), число файлов, время.
2. Сводная таблица: категория × Error/Warning/Info.
3. По категориям → по правилам (`### API.TIMESCALE_WRITE — 7`, сообщение и замена один раз) → список `- \`Assets/…/PauseMenu.cs:42\` — \`Time.timeScale = 0f;\``, с пометкой `(low confidence)` при необходимости.
4. Отдельный последний раздел «Только редактор».

---

## Шаг 7. `ScannerWindow`

UI Toolkit из кода, без UXML и USS-файлов.

- Панель: **Scan**, **Export Markdown…** (`SaveFilePanel`, по умолчанию папка над корнем проекта), **Copy Markdown**, переключатели Error / Warning / Info / Editor-only, счётчики.
- Дерево (`TreeView` из `UnityEngine.UIElements`): категория → правило (счётчик, сообщение, замена) → находка (`path:line`, snippet, confidence).
- Выбор находки открывает файл на строке: `AssetDatabase.OpenAsset` с номером строки.
- Скан с `DisplayCancelableProgressBar`.
- При `OnEnable` окно загружает последний `Library/MirraPorting/report.json`, если он есть. После domain reload результаты не должны пропадать.

---

## Шаг 8. Песочница `Assets/Sandbox/`

Компилирующиеся скрипты с намеренными нарушениями по всем правилам шага 5, включая:
- editor-only файл в `Assets/Sandbox/Editor/`;
- участок в `#if UNITY_EDITOR`;
- собственный тип `Time` в отдельном namespace с полем `timeScale`;
- `using static UnityEngine.Time;`;
- интерполяции и строки с `//`.

`Assets/Sandbox/EXPECTED.md` — таблица: файл, строка, RuleId, Severity, Confidence, EditorOnly. Сверка «Scan and Export» с этой таблицей — главный ручной тест этапа.

---

## Критерии приёмки

1. Проект без MirraSDK: компиляция без ошибок, 0 предупреждений от `Luqmus.*`.
2. После установки MirraSDK5 (`https://github.com/MirraSDK/SDK5.git`) сборка `Luqmus.MirraPorting.Editor.Sdk` компилируется. После удаления SDK всё снова компилируется.
3. Все EditMode-тесты зелёные.
4. «Scan and Export» на песочнице совпадает с `EXPECTED.md`, находок из самого пакета нет.
5. После скана `git status` чистый.
6. Окно: скан, фильтры, переход к строке, экспорт, восстановление после domain reload.
7. `install.ps1` в чистый проект 2022.3 → компилируется, пункты меню есть; `uninstall.ps1` → пакет удалён.

## Не входит в этап 1

Хоткеи, чужие SDK, ранняя инициализация, ассеты и сцены, UnityEvent, Player Settings и темплейт, выбор площадок, подавление находок, собственный MCP-инструмент, консольная утилита.

---

## Решения по ходу этапа

Сюда попадает всё согласованное, что не следует из текста задачи. Новое решение — дописывать в этот раздел и коммитить вместе с кодом.

### Шаг 1

- `SdkAssemblyMarker`: поле `internal static readonly`, не `private` — приватное неиспользуемое поле даёт CS0414 при установленном SDK.
- `SkeletonSmokeTests` — временный, доказывает, что сборка тестов собирается и видна в Test Runner. Удалить на шаге 4, когда появятся тесты лексера.
- `uninstall.ps1` убирает строку из `.git/info/exclude` только при точном совпадении после `trim`; остальные строки и их переводы строк сохраняются, файл пишется UTF-8 без BOM.
- В начале обоих `.ps1` — комментарий с примером запуска через `powershell -ExecutionPolicy Bypass -File …`.
- Скрипты проверяются на пустышке в `$env:TEMP` (каталог с `Packages/manifest.json` и `git init`), не внутри репозитория; после проверки пустышка удаляется.
- Перед коммитом шага 1 — временная установка MirraSDK5 для проверки, что `Luqmus.MirraPorting.Editor.Sdk` собирается, с последующим откатом `Packages/manifest.json` и `Packages/packages-lock.json`.
- Тесты embedded-пакета Unity подхватывает сама: запись `testables` в `Packages/manifest.json` не понадобилась.
- Запись `com.luqmus.mirra-porting` в `Packages/packages-lock.json` в этом репозитории коммитится: пакет здесь свой, embedded. Правило «lock не должен меняться из-за пакета» относится к репозиторию порта.
- После временной установки SDK удалять `Assets/Resources/MirraSDK5/` и, если в `Assets/Resources/` больше ничего не осталось, сам каталог вместе с `Assets/Resources.meta`.

### Шаг 2

- `Severity` и `Confidence` — с явными значениями (`Error = 0, Warning = 1, Info = 2`; `High = 0, Medium = 1, Low = 2`): порядок входит в контракт отчёта (по нему сортируются находки), а в JSON enum пишутся именами.
- `SCAN.INTERNAL_ERROR` — `Severity.Warning`, категория `Scan`: сбой проверки означает неполное покрытие сканера, а не проблему в проекте. Текст исключения подставляется через `messageOverride`, `Suggestion` берётся из правила.
- Понижение `Severity` до `Info` при `EditorOnly = true` живёт в `Finding.FromRule`, а не в каждой проверке: правило общее для всех категорий.
- Обрезка сниппета (`Trim`, не длиннее 160 символов, последний символ — `…`) — там же.
- Сортировка — LINQ `OrderBy/ThenBy` (`FindingOrder.Sort`), `List.Sort` не используется: он нестабилен. При полном совпадении ключей находки сохраняют порядок, в котором их выдали проверки, поэтому два скана неизменного проекта дают одинаковые отчёты. Отдельного `FindingComparer` нет.
- В ключах сортировки `Category` и `RuleId` сравниваются `StringComparer.Ordinal`, `Path` — `OrdinalIgnoreCase`, чтобы пути, отличающиеся только регистром, шли рядом.
- `Finding` и `Rule` неизменяемые и `internal`. Для JSON на шаге 6 будут отдельные `[Serializable]`-DTO в `Report/`: `JsonUtility` умеет только публичные поля, а модель ядра ради него менять не нужно.
- `Rules.Find` возвращает `null` на неизвестный id, а не бросает: чтение чужого `report.json` не должно падать.

### Шаг 3

- Проверено на реальном проекте: `Assembly.sourceFiles` — проектно-относительные пути с прямыми слэшами, а для пакетов префикс виртуальный, `Packages/<name>/…` (`PackageInfo.assetPath`), не `resolvedPath`. Подтверждено на источниках Embedded, Local, Git и Registry: последние две группы лежат в `Library/PackageCache`, но в `sourceFiles` всё равно `Packages/<name>/…`. Поэтому `PathUtil.ToProjectRelative` переносит абсолютный путь на `ScanRoot.ProjectRelativePath`, и local-пакет вне проекта попадает в отчёт как `Packages/<name>/…`, совпадая с тем, что говорит `CompilationPipeline`.
- Все сравнения и множества путей — `StringComparer.OrdinalIgnoreCase`: списки исходников сборок, имена исключённых пакетов, сегмент `Editor` в запасном варианте, сортировка файлов.
- `IProjectEnvironment.ActiveBuildTarget` (в `UnityProjectEnvironment` — `EditorUserBuildSettings.activeBuildTarget.ToString()`) нужен отчёту на шаге 6: набор Player-сборок зависит от активной платформы, поэтому при цели, отличной от WebGL, отчёт должен предупреждать, что классификация editor-only может быть неточной. Насколько велика разница, видно по замеру на `StandaloneWindows64`, на котором репозиторий был во время шага 3: 32 Editor-сборки против 8 Player. Сейчас репозиторий на WebGL, замер повторён ниже, в разделе про проверку editor-only.
- `UnityProjectEnvironment` снимает состояние проекта (исходники сборок, корни сканирования) один раз в конструкторе: скан работает на одном снимке, даже если редактор успеет перекомпилироваться.
- Unity API в ядре не ограничен `UnityProjectEnvironment`: `AsmdefProbe` разбирает `.asmdef` через `JsonUtility`, и это допустимо — запрет касается сторонних JSON-библиотек. Полностью без Unity API остаётся только `Editor/Code/` (шаг 4).
- `ScanScope` не бросает исключений наружу: нечитаемая папка, отсутствующий корень и файл вне своего корня становятся `ScanError`. `ScanRunner` на шаге 6 превратит их в `SCAN.INTERNAL_ERROR`.
- Нечитаемый или битый `.asmdef` даёт пустое имя сборки, и папка не исключается: лучше просканировать лишнее, чем молча пропустить код игры.
- Имя SDK-сборки сверяется по префиксу `MirraGames.SDK` с учётом регистра (`StringComparison.Ordinal`).
- Файлы результата сортируются по `ProjectRelativePath`: порядок обхода каталогов файловой системой не гарантирован, а два скана неизменного проекта должны давать одинаковый отчёт.
- Файлы, имя которых начинается с точки, пропускаются так же, как скрытые папки: Unity их не импортирует.
- `ScanContext` пока содержит только окружение и список файлов, токены и объявленные типы добавит шаг 4.

### Проверка editor-only на реальных файлах (между шагами 3 и 4)

- Активная платформа проверки — **WebGL**: модуль установлен, а сам проект переключили на WebGL между шагом 3 и этой проверкой, так что переключать было нечего. На WebGL 36 Editor-сборок против 10 Player (в шаге 3, на `StandaloneWindows64`, было 32 против 8; ещё +4/+2 дала песочница).
- Четыре случая в `Assets/Sandbox/` совпали с ожиданием, расхождений нет: `Plain/PlainScript.cs` — false, `Editor/SpecialFolderEditorScript.cs` — true, `EditorAsmdef/EditorAsmdefScript.cs` — true (asmdef с `includePlatforms: ["Editor"]`, слова `Editor` в пути нет), `RuntimeAsmdef/Editor/NestedEditorFolderScript.cs` — false.
- Подтверждено: asmdef без ограничений платформ сильнее специальной папки `Editor`. Запасной вариант «в пути есть сегмент `Editor`» дал бы на последнем файле `true`, поэтому сборки спрашиваются первыми, а путь — только когда файл не принадлежит ни одной сборке.
- `CompilationPipeline.GetAssemblies(AssembliesType.Editor)` возвращает **все** сборки, собираемые для редактора, включая рантаймовые (`Assembly-CSharp`, `Sandbox.RuntimeAsmdef` есть и там, и в `Player`). Поэтому признак editor-only — именно разность с `AssembliesType.Player`, а не факт присутствия в списке Editor.
- Эталон случаев — `Assets/Sandbox/EXPECTED.md`, колонки правил добавятся на шаге 8.
