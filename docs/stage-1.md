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
| `API.ORIENTATION_WRITE` | `Screen.orientation`, `Screen.autorotateToPortrait`, `Screen.autorotateToPortraitUpsideDown`, `Screen.autorotateToLandscapeLeft`, `Screen.autorotateToLandscapeRight` Write | Warning | ориентация задаётся темплейтом; на YouTube Playables лок запрещён |

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

### Шаг 4a

- **Восстановление после незакрытых литералов — по строке, не до конца файла** (исправляет текст шага 4). Лексер разбирает и неактивные ветки `#if`, где `don't` в прозе или лишняя кавычка законны, поэтому одна незакрытая строка не должна прятать остаток файла:
  - обычная строка, текстовая часть не-verbatim интерполяции и спецификатор формата в ней заканчиваются переводом строки, выдают токен до конца строки и `UnterminatedString`; стек интерполяций при этом снимается до ближайшего verbatim-кадра, потому что все не-verbatim кадры вокруг тоже не переживают перенос;
  - символьный литерал без закрывающей `'` до конца строки: `'` выдаётся как `Punct` плюс `UnterminatedCharLiteral`, разбор продолжается со следующего символа;
  - verbatim-строки и `/* */` по-прежнему тянутся до конца файла: они и должны переносить строки.
- Конец файла внутри открытой дырки даёт `UnterminatedString` на каждый оставшийся кадр.
- `Token` несёт `Start` и `Length`. `LexerAssert` в каждом тесте проверяет инварианты: `source.Substring(Start, Length)` — сырой текст токена (равен `Text`, кроме verbatim-идентификатора, где `Text` без `@`), `Line`/`Column` пересчитываются из `Start` независимо от лексера, токены идут по возрастанию `Start` без перекрытий. Проверки «конкатенация текстов восстанавливает исходник» нет: пробелы и комментарии токенами не становятся.
- `Directive` — только при пустом стеке интерполяций: внутри дырки `#` в начале строки остаётся обычным `Punct`.
- Идентификаторы: старт `char.IsLetter` или `_`, продолжение `char.IsLetterOrDigit` или `_`, так что кириллические имена работают. Колонки считаются в UTF-16 code units, таб — одна колонка, ведущий U+FEFF в первую строку не входит.
- Токены интерполяции: открывающий делимитер склеен с первым текстовым куском, закрывающая кавычка — с последним, пустые внутренние куски не выдаются, `{` и `}` дырки — `Punct`, спецификатор формата после `:` верхнего уровня — один `String`.
- `:` верхнего уровня дырки — всегда формат, в том числе в `$"{f ? a : b}"`: так же ведёт себя настоящий компилятор. Внутри `()` и `[]` двоеточие остаётся оператором, `::` проверяется раньше одиночного `:`.
- Число поглощает `.` только перед цифрой, поэтому `1..2` — это `Number(1)`, `Punct(.)`, `Number(.2)`. `<<` и `>>` в список операторов не входят (их нет в таблице шага 4), поэтому `List<List<int>>` не склеивается в один токен.
- `LexDiagnostic` — только код и позиция, без текста: формулировки появятся там, где диагностика станет находкой.
- В главном цикле есть страховка от «нулевого шага»: если за итерацию не изменились ни позиция, ни списки токенов и диагностик, ни стек, позиция сдвигается на символ. По построению это недостижимо, но зависший редактор хуже лишней проверки.
- **Разделение таблицы тестов шага 4.** Строки с ожиданиями Write/Read, `API.*`, Confidence и editor-only относятся к шагам 4b и 5. На 4a они переведены в проверку последовательности токенов: например, для `var u = "https://x.com"; Time.timeScale = 0;` проверяется, что `//` внутри строки не начал комментарий, а `Time` — `Identifier` с колонкой 26. Сами ожидания Write/Read остаются тестами шага 5.
- `SkeletonSmokeTests` удалён: своё дело сделал, тесты лексера появились.

### Шаг 4b, условная компиляция

- Правило editor-only из шага 4 («условие содержит `UNITY_EDITOR` без `!`») заменено точным: условие вычисляется **в предположении сборки игрока** в трёхзначной логике. Символы с префиксом `UNITY_EDITOR` (`UNITY_EDITOR`, `_WIN`, `_OSX`, `_LINUX`, `_64`) — `False`, `true`/`false` — как есть, остальные — `Unknown`; операторы `!`, `&&`, `||`, `==`, `!=`, скобки. Ветка editor-only тогда и только тогда, когда её итоговое условие точно `False`. Старая формулировка ошибалась на `UNITY_EDITOR || UNITY_WEBGL` (не editor-only) и на `UNITY_EDITOR == false` (не editor-only).
- Итоговое условие ветки: `#if c` → `c`; `#elif c` → `c ∧ ¬(∨ предыдущих)`; `#else` → `¬(∨ предыдущих)`. Ветка внутри editor-only-ветки editor-only всегда, независимо от своего условия.
- **`#if false` — editor-only.** Это не редактор, а мёртвый код, но признак у нас один: «не попадает в сборку игрока», и находки оттуда — такой же шум. Если шагу 5 понадобится различать, нужен будет второй признак.
- Условие токенизируется повторным вызовом `CSharpLexer.Lex`: он уже умеет идентификаторы, `&&`, `==` и скобки и заодно отбрасывает хвостовой `//`-комментарий, так что отдельного разбора текста директивы нет.
- Ошибка разбора условия → `Unknown` (ветка не editor-only) + `InvalidConditionExpression`: лучше лишняя находка, чем потерянная.
- Непарный `#endif`, `#elif`/`#else` без `#if`, второй `#else` → `UnbalancedDirective`; незакрытый `#if` → `UnclosedConditional`, его ветка тянется до конца файла. Исключений наружу нет.
- `#define`/`#undef` в файле игнорируются — известное ограничение: условия вычисляются только по именам символов.
- Новые коды добавлены в существующий `LexDiagnosticCode`, тип `LexDiagnostic` не переименован: разбор директив — часть той же лексической фазы, а один список диагностик на файл проще и шагу 6, и окну.
- `TextSpan` — полуинтервал `[Start, End)`; соседние и перекрывающиеся диапазоны склеиваются, поэтому вложенная editor-only-ветка растворяется в родительской. Тело ветки — от конца строки директивы до начала следующей директивы: первый символ строки `#endif` уже не editor-only, а перевод строки после `#if` — ещё editor-only.
- Запрос — `IsEditorOnlyAt(int start)` по смещению, бинарным поиском: шагу 5 удобнее спрашивать про позицию найденной цепочки, и это не ломается при перенумерации токенов.
- **Тесты:** public-метод NUnit не может принимать internal-тип в параметрах (CS0051), поэтому таблицы истинности и ожидаемые `ConditionValue`/`DirectiveKind` живут внутри тестов, в приватных хелперах, а не в `TestCase`. Через `TestCase` передаются только `string` и `bool`.

### Шаг 4b, using-контекст, объявленные имена, интеграция

- `ScopedFile` не тронут: он про файловую систему и рождается до чтения. Разобранный файл — новый тип `SourceFile` (`Core/`) поверх него: `Lex`, `Regions`, `Usings`, `DeclaredNames`. Весь разбор делает `SourceFile.FromText(ScopedFile, string)`, поэтому тесты обходятся строкой, а диск остаётся только в `SourceFileLoader` (`File.ReadAllText` — UTF-8 с распознаванием и снятием BOM).
- Признак editor-only для позиции живёт в одном месте — `SourceFile.IsEditorOnlyAt(int start)` = `File.EditorOnly || Regions.IsEditorOnlyAt(start)`. Проверки шага 5 складывать эти два признака сами не должны.
- `ScanContext.Files` теперь `IReadOnlyList<SourceFile>`, плюс `DeclaredNames` по всему проекту. Собирает `ScanContextBuilder`; цикл по файлам тривиальный, на шаге 6 `ScanRunner` заменит его своим — с прогрессом и отменой, вызывая тот же `SourceFileLoader`.
- **Диагностики находками не становятся** — в неактивных ветках их закономерно много, и находка про чужой мёртвый код это шум. Исключение — `InternalError`: сбой лексера или разбора директив не попадает в счётчик, а превращается в `ScanError` и станет `SCAN.INTERNAL_ERROR` на шаге 6. Остальные коды — только счётчик `SourceFile.DiagnosticCount`.
- **Для шага 6:** диагностики, после которых файл перестал разбираться как код (`UnterminatedComment`, `UnterminatedVerbatimString`, `UnclosedConditional`), отчёт перечисляет поимённо — путь и строка, с которой файл разобран не полностью. Причина: в пропускаемой ветке `#if false` законен незакрытый `/*`, и тогда все находки ниже теряются молча. Первую такую диагностику отдаёт `SourceFile.FirstTruncatingDiagnostic` (или `null`).
- Чтобы это стало возможно, в лексер добавлен код `UnterminatedVerbatimString`: по одному `UnterminatedString` нельзя отличить строку, съевшую остаток файла, от строки, оборванной переводом строки, — а разница как раз в том, потеряно ли что-то ниже.
- `using` — директива, только если все охватывающие `{` открыл `namespace`; это отсекает `using (…)` и `using var x = …;` внутри методов. Плюс прямая проверка на `(` сразу после `using`.
- Using-контекст — на файл целиком, без областей namespace: `using` внутри `namespace A { }` действует во всём файле. Известное упрощение; два namespace с разными using в одном файле в играх почти не встречаются.
- **Известное ограничение:** стек фигурных скобок перекашивается, если `namespace X {` открыт в одной ветке `#if`, а закрыт в другой, и если дырка интерполяции оборвана переводом строки. Влияет только на `using`-директивы после такого места — они могут быть пропущены. Не чиним: цена — полноценный разбор ветвлений.
- Алиасы кладутся в словарь индексатором: один и тот же алиас в двух ветках `#if` не бросает исключение, побеждает последний.
- `global::` в начале цепочки отбрасывается, факт запоминается в `QualifiedName.HadGlobalQualifier`. Алиас обобщённого типа (`using L = …List<int>;`) обрезается по `<`. Поломанная `using`-директива молча пропускается, без диагностики: это не то, что чинит портировщик. Как трактовать цепочки — дело шага 5.
- Объявленные имена: имя после `class`/`struct`/`enum`/`interface`, если следующий токен — идентификатор и не `where`, а предыдущий — не `:` и не `,` (иначе это ограничение generic); плюс каждый сегмент имени namespace. Набор — `StringComparer.Ordinal`: идентификаторы C# регистрозависимы, правило `OrdinalIgnoreCase` из шага 3 касалось только путей.
- `record` и `delegate` в объявленных именах не учитываются: `record` в Unity недоступен (нет `IsExternalInit`), `delegate` с именем `Time` — теоретический случай. Известный пропуск, влияет только на Confidence.

### Шаг 5, правила и модель

- В `Finding` добавлено `ConfidenceReason` — почему уверенность ниже High, по-русски; для High всегда пустая строка, это следит `Finding.FromRule`. Правка модели шага 2; на шаге 6 поле должно попасть в JSON (`confidenceReason`) и в Markdown — после пометки о низкой уверенности.
- **Правка шага 4b:** `DeclaredNameCollector` возвращает пары «имя + namespace объявления» и список namespace, объявленных в файле; `ScanContext` хранит `DeclaredTypeIndex` (имя → список namespace) вместо плоского набора имён. Причина: плагин со своим `Screen` или `Cursor` в собственном namespace не должен понижать уверенность по всему проекту — затенение действует только там, где это объявление видно.
- Namespace объявления: вложенный тип берёт namespace внешнего типа (вложенность в тип ничего не меняет, меняет только `namespace`). Сегменты имени `namespace A.B.C` дают `C` в `A.B`, `B` в `A` и `A` в глобальном — каждый сегмент сам по себе имя, способное затенить тип Unity.
- Таблица запрещённого API — данные (`ForbiddenApiTable`): namespace, тип, член (или `*` для любого члена `PlayerPrefs`), вид члена и RuleId отдельно для Write, Read и Call. Отсутствие RuleId означает, что этот вид обращения не находка: `Screen.orientation` ловится только на запись, `Application.systemLanguage` — только на чтение.
- Для методов Read и Call дают один и тот же RuleId: `onClick.AddListener(Application.Quit)` — то же нарушение, что и вызов. У свойств `CallRuleId` нет. Это закреплено тестами на всю таблицу.
- Тексты правил не утверждают о механике SDK больше, чем написано в `CLAUDE.md`: запись — «в обход SDK: SDK сам управляет этим значением во время рекламы», чтение — «во время рекламы значение выставляет SDK, и игра может принять его за своё». Одна формулировка на все `*_WRITE` и одна на все `*_READ` — отчёт говорит одним голосом.
- Правило «предыдущий токен `=>` → не Write» решено **не вводить**: оно превратило бы `public void Pause() => Time.timeScale = 0;` (частый паттерн) из Error в Warning, а требуемый случай `float S => Time.timeScale;` и без него даёт Read — следующий за членом токен там `;`.

### Шаг 5, ForbiddenApiCheck

- Цепочка начинается только с токена, перед которым нет `.`, `::` и `?.`. Поэтому `UnityEngine.Time.timeScale = 0;` даёт ровно одну находку (сопоставление не стартует заново с середины разобранной цепочки), а `x?.Time.timeScale` не даёт ни одной.
- Квалификатор сверяется с `NamespaceOf(тип)`, а не с жёстким `UnityEngine`. Это же правило разрешает алиасы и `using static`, поэтому `using F = System.IO.File;` даёт High, как и `using T = UnityEngine.Time;`.
- Алиас с именем типа из таблицы, ведущий не в его namespace (`using Time = MyGame.Time;`), полностью выключает это имя в файле.
- Уверенность голого имени: High; Medium — если в файле нет `using UnityEngine;` или алиас без namespace; Low — если видно объявление своего типа с таким именем или это `File` без `using System.IO;`. При нескольких причинах побеждает самая низкая, её текст и попадает в `ConfidenceReason`.
- Затенение учитывает namespace: объявление видно файлу, если оно в глобальном namespace, в namespace из обычных `using` файла, или в namespace, которому файл принадлежит сам (файл в `A.B` видит объявления из `A`).
- Голый член при `using static` — только если перед ним нет `.` и предыдущий токен не идентификатор, кроме `return`, `case`, `in`, `is`, `as`, `else`, `throw`, `await`, `yield`, `when`. Confidence всегда Low. Строки `*` (PlayerPrefs) в этом сопоставлении не участвуют.
- **Известное ограничение:** голый член при `using static` после `>`, `]` или `?` даёт ложное совпадение с Low — `List<float> timeScale;` и `float? timeScale;` читаются как использование. Не чиним: отличить объявление от выражения здесь можно только разбором типов.
- Известный пропуск (в комментарии к коду): запись через деконструкцию `(Time.timeScale, x) = …` определяется как Read.
- Позиция находки — первый токен цепочки (`global`, namespace или сам тип). `EditorOnly` берётся только из `SourceFile.IsEditorOnlyAt(start первого токена)`.
- Сниппет — строки от первого токена цепочки до токена, определившего вид обращения: каждая `Trim`, склейка через пробел, обрезку до 160 делает `Finding.FromRule`. Поэтому цепочка на трёх строках показывается целиком.
- Каждый файл проверяется в `try/catch`: исключение даёт `SCAN.INTERNAL_ERROR` с путём файла и текстом исключения, остальные файлы проверяются. Файлы с диагностиками лексера проверяются как есть — строка, оборванная переводом строки, не прячет находки ниже.

### Шаг 5, песочница

- **Исправление таблицы шага 5.** В задаче члены `Screen` записаны как `autoRotateToPortrait`, `autoRotateToPortraitUpsideDown`, `autoRotateToLandscapeLeft`, `autoRotateToLandscapeRight`. В Unity они называются `autorotateToPortrait` и так далее — со строчной `r`. Имена членов сверяются с учётом регистра, поэтому четыре правила молча не находили бы ничего. В таблице теперь правильные имена, и **текст задачи в шаге 5 исправлен тоже**, чтобы ошибка не вернулась при следующем чтении. Поймала её компиляция песочницы.
- Чтобы такое ловилось тестами, а не компилятором чужого кода, добавлен `ForbiddenApiTableReflectionTests`: он сверяет всю таблицу с настоящим API через рефлексию — типы существуют и лежат в заявленных namespace, каждый член существует ровно с таким именем, свойства действительно свойства, методы — методы, у правил на запись член доступен на запись, у правил на чтение — на чтение. Тестовая сборка `UnityEngine` видит, ядру пакета рефлексия по-прежнему не нужна.
- Песочница `Assets/Sandbox/ForbiddenApi/` покрывает все правила таблицы и все случаи разрешения имени: квалифицированное обращение, `nameof`, строки и комментарии, свой `Time` и `Cursor` в отдельном namespace, алиас на Unity-тип и алиас, уводящий имя в сторону, `using static` вместе с одноимёнными объявлениями, ветка `#if UNITY_EDITOR`.
- Ожидания живут пометками `// EXPECT RuleId Severity Confidence EditorOnly` в самих скриптах, раздел `ForbiddenApi` в `EXPECTED.md` собран из них. Прогон `ScanScope` → `ScanContextBuilder` → `ForbiddenApiCheck` на реальном проекте дал **38 находок из 38**, без лишних и пропущенных.
- Голое `File` без `using System.IO;` (Low) в песочницу не попадает: такой файл не компилируется. Случай проверяется только тестами.

### Шаг 6, модель отчёта и DTO

- **Сводка без двойного счёта:** `summary.errors/warnings/info` считают только находки с `EditorOnly = false`, `summary.editorOnly` — все находки с `EditorOnly = true`, независимо от severity. Сумма четырёх равна числу находок, это закреплено тестом. Так же считает сводная таблица Markdown и строка в консоль.
- Модель отчёта (`ScanReport`, `ScanSummary`, `DiagnosticsSummary`, `TruncatedFile`, `ProjectInfo`) живёт в `Core` и не знает про форматы. DTO для `JsonUtility` — отдельно, в `Report/`: только они знают порядок и имена полей файла.
- `JsonReport.TryParse` нужен не только round-trip-тесту: на шаге 7 окно читает последний `report.json` при `OnEnable`. Неизвестный `severity` читается как `Info`, неизвестный `confidence` — как `Low`: отчёт из будущей версии должен открываться, а не падать.
- Даты — ISO 8601 UTC (`yyyy-MM-ddTHH:mm:ssZ`, `CultureInfo.InvariantCulture`), длительность — целое число миллисекунд.
- `IProjectEnvironment` дополнен `UnityVersion`, `MirraSdkVersion`, `ToolName`, `ToolVersion` — шапка отчёта берётся оттуда, поэтому фейковое окружение подставляет в тестах что угодно. Проверено на проекте: `PackageInfo.FindForAssembly` для сборки пакета отдаёт `com.luqmus.mirra-porting` и `0.1.0`, версия SDK пустая, когда пакета нет.
- **Профиль API проекта — `.NET Standard 2.0`** (проверено `PlayerSettings.GetApiCompatibilityLevel`). В нём есть `File.Replace` (3 и 4 аргумента) и **только** `File.Move(source, dest)`: перегрузки с флагом перезаписи нет, атомарная замена на шаге записи делается через `File.Replace` при существующей цели и `File.Move` иначе.

### Шаг 6, ScanRunner и запись

- `ScanContextBuilder` и `ScanContextBuildResult` удалены: цикл по файлам переехал в `ScanRunner` вместе с прогрессом и отменой, их тесты — в `ScanRunnerTests`.
- Отмена спрашивается между файлами и между проверками. Отменённый скан возвращает `WasCancelled` без отчёта и **ничего не пишет**: прошлый `report.json` остаётся нетронутым.
- `ScanError` (нечитаемая папка, отсутствующий корень, нечитаемый файл) → находка `SCAN.INTERNAL_ERROR` с `line = 0, column = 0`: позиции у неё нет. `InternalErrorDiagnostic` лексера → `SCAN.INTERNAL_ERROR` **с его `Line`/`Column`**: место сбоя известно. Сбой самой проверки → находка без пути и позиции.
- `ScanScope` отдаёт `ExcludedSdkFolders` — папки, исключённые по asmdef `MirraGames.SDK*`. На каждую отчёт добавляет notice «MirraSDK лежит в …, версия не определена»: пакетом SDK не установлен, и версию прочитать неоткуда.
- Запись: `PrepareDirectory` создаёт папку и удаляет оставшиеся от прерванного прогона `*.tmp`; `WriteText` пишет во временный файл рядом и ставит его на место — `File.Replace` при существующей цели, `File.Move` иначе. UTF-8 без BOM.
- Ошибка записи наружу не бросается: `ScanAndExport` ловит исключение, пишет `Debug.LogError` с путём и причиной и возвращает `null`. Тест: на месте `report.json` лежит каталог с таким именем.
- Корень проекта — `Path.GetDirectoryName(Application.dataPath)`, не текущая директория процесса: у пакетного запуска она может быть любой.
- Единственный public-тип пакета — `Luqmus.MirraPorting.MirraPortingScanner` с `ScanAndExport()` и `ScanAndExport(outputDirectory)`, возвращающими путь к `report.json` или `null`. Прогресс-бар живёт в пункте меню, поэтому вызов из тестов и из `-executeMethod` не трогает UI.
- Список проверок — `ScanChecks.Create()`, одно место на весь сканер.
- Проверено тестом на реальном проекте: два скана неизменного проекта дают одинаковый JSON, кроме `startedAt`, `finishedAt`, `durationMs`.

### Шаг 6, Markdown

- Порядок разделов: шапка → notices → сводка → «Находки» → «Разобраны не полностью» → «Только редактор». Editor-only находки в сводную таблицу не попадают и живут только в своём разделе, под таблицей — строка «Ещё N находок в коде, который не попадает в сборку игры».
- Порядок записи файлов: `report.md`, затем `report.json` **последним** — появление JSON означает завершённый скан. Перед записью `PrepareDirectory` удаляет оставшиеся `*.tmp`.
- Находка без строки печатается без `:0`, находка без пути — как «весь проект». Если у находки нет сниппета (`SCAN.INTERNAL_ERROR`), вместо него печатается её собственное сообщение: у таких находок оно своё, а не из правила.
- Сниппет с обратной кавычкой оборачивается в `` `` `` с пробелами внутри, иначе Markdown ломается посреди строки кода.
- Пометка `(low confidence: причина)` ставится при любой уверенности ниже High — и Medium, и Low: для читателя это одно и то же «проверь глазами», а причина говорит, почему.
- Шапка про SDK: версия пакета, либо «не найден как пакет, лежит в `<путь>`», если `ScanScope` исключил папку по asmdef `MirraGames.SDK*`, либо «не установлен». Для этого `ScanReport` носит `SdkFoldersInAssets`; в JSON этот факт есть в `notices`, отдельным полем схема не раздувается.
- Числа в русском тексте склоняются (`1 находка`, `2 находки`, `5 находок`, `в 1 файле`): отчёт читают коллеги, «1 находок» выглядит как машинный текст. Поймано на живом отчёте, закреплено тестом.
