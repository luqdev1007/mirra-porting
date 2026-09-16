# Ожидаемый результат сканирования песочницы

Эталон для ручной сверки с «Tools / Mirra Porting / Scan and Export».

Два раздела: классификация editor-only (шаг 3) и находки запрещённого API (шаг 5). Проверки
этапов 3–4 добавят сюда свои разделы.

## EditorOnly

| Файл | EditorOnly | Почему |
|---|---|---|
| `Assets/Sandbox/Plain/PlainScript.cs` | false | Обычный код игры, `Assembly-CSharp` |
| `Assets/Sandbox/Editor/SpecialFolderEditorScript.cs` | true | Специальная папка `Editor`, `Assembly-CSharp-Editor` |
| `Assets/Sandbox/EditorAsmdef/EditorAsmdefScript.cs` | true | `Sandbox.EditorAsmdef` с `includePlatforms: ["Editor"]`, в пути слова `Editor` нет |
| `Assets/Sandbox/RuntimeAsmdef/Editor/NestedEditorFolderScript.cs` | false | `Sandbox.RuntimeAsmdef` без ограничений платформ: asmdef сильнее папки `Editor` |

Последняя строка — главная: запасной вариант «в пути есть сегмент `Editor`» дал бы здесь
`true`, поэтому сначала спрашиваются сборки и только потом путь.

## ForbiddenApi

Таблица построена из пометок `// EXPECT` в самих скриптах: файл, строка, правило, severity,
confidence, editor-only. Строки без пометки находок давать не должны.

| Файл | Строка | RuleId | Severity | Confidence | EditorOnly |
|---|---|---|---|---|---|
| `Assets/Sandbox/ForbiddenApi/AliasUsage.cs` | 14 | `API.TIMESCALE_WRITE` | Error | High | false |
| `Assets/Sandbox/ForbiddenApi/EditorOnlyUsage.cs` | 14 | `API.TIMESCALE_WRITE` | Info | High | true |
| `Assets/Sandbox/ForbiddenApi/EditorOnlyUsage.cs` | 16 | `API.CURSOR_VISIBLE_WRITE` | Error | High | false |
| `Assets/Sandbox/ForbiddenApi/FileIoUsage.cs` | 14 | `API.PERSISTENT_DATA_PATH` | Warning | High | false |
| `Assets/Sandbox/ForbiddenApi/FileIoUsage.cs` | 15 | `API.FILE_WRITE` | Warning | High | false |
| `Assets/Sandbox/ForbiddenApi/FileIoUsage.cs` | 16 | `API.FILE_WRITE` | Warning | High | false |
| `Assets/Sandbox/ForbiddenApi/FileIoUsage.cs` | 17 | `API.FILE_WRITE` | Warning | High | false |
| `Assets/Sandbox/ForbiddenApi/HighConfidenceUsage.cs` | 26 | `API.TIMESCALE_WRITE` | Error | High | false |
| `Assets/Sandbox/ForbiddenApi/HighConfidenceUsage.cs` | 27 | `API.TIMESCALE_READ` | Warning | High | false |
| `Assets/Sandbox/ForbiddenApi/HighConfidenceUsage.cs` | 28 | `API.AUDIO_VOLUME_WRITE` | Error | High | false |
| `Assets/Sandbox/ForbiddenApi/HighConfidenceUsage.cs` | 29 | `API.AUDIO_VOLUME_READ` | Warning | High | false |
| `Assets/Sandbox/ForbiddenApi/HighConfidenceUsage.cs` | 30 | `API.AUDIO_PAUSE_WRITE` | Error | High | false |
| `Assets/Sandbox/ForbiddenApi/HighConfidenceUsage.cs` | 31 | `API.AUDIO_PAUSE_READ` | Warning | High | false |
| `Assets/Sandbox/ForbiddenApi/HighConfidenceUsage.cs` | 36 | `API.CURSOR_VISIBLE_WRITE` | Error | High | false |
| `Assets/Sandbox/ForbiddenApi/HighConfidenceUsage.cs` | 37 | `API.CURSOR_VISIBLE_READ` | Warning | High | false |
| `Assets/Sandbox/ForbiddenApi/HighConfidenceUsage.cs` | 38 | `API.CURSOR_LOCK_WRITE` | Error | High | false |
| `Assets/Sandbox/ForbiddenApi/HighConfidenceUsage.cs` | 39 | `API.CURSOR_LOCK_READ` | Warning | High | false |
| `Assets/Sandbox/ForbiddenApi/HighConfidenceUsage.cs` | 40 | `API.ORIENTATION_WRITE` | Warning | High | false |
| `Assets/Sandbox/ForbiddenApi/HighConfidenceUsage.cs` | 41 | `API.ORIENTATION_WRITE` | Warning | High | false |
| `Assets/Sandbox/ForbiddenApi/HighConfidenceUsage.cs` | 42 | `API.ORIENTATION_WRITE` | Warning | High | false |
| `Assets/Sandbox/ForbiddenApi/HighConfidenceUsage.cs` | 43 | `API.ORIENTATION_WRITE` | Warning | High | false |
| `Assets/Sandbox/ForbiddenApi/HighConfidenceUsage.cs` | 44 | `API.ORIENTATION_WRITE` | Warning | High | false |
| `Assets/Sandbox/ForbiddenApi/HighConfidenceUsage.cs` | 50 | `API.PLAYERPREFS` | Error | High | false |
| `Assets/Sandbox/ForbiddenApi/HighConfidenceUsage.cs` | 51 | `API.PLAYERPREFS` | Error | High | false |
| `Assets/Sandbox/ForbiddenApi/HighConfidenceUsage.cs` | 52 | `API.PERSISTENT_DATA_PATH` | Warning | High | false |
| `Assets/Sandbox/ForbiddenApi/HighConfidenceUsage.cs` | 57 | `API.OPEN_URL` | Error | High | false |
| `Assets/Sandbox/ForbiddenApi/HighConfidenceUsage.cs` | 58 | `API.QUIT` | Error | High | false |
| `Assets/Sandbox/ForbiddenApi/HighConfidenceUsage.cs` | 59 | `API.QUIT` | Error | High | false |
| `Assets/Sandbox/ForbiddenApi/HighConfidenceUsage.cs` | 61 | `API.SYSTEM_LANGUAGE` | Error | High | false |
| `Assets/Sandbox/ForbiddenApi/HighConfidenceUsage.cs` | 62 | `API.IS_MOBILE` | Error | High | false |
| `Assets/Sandbox/ForbiddenApi/HighConfidenceUsage.cs` | 63 | `API.IS_MOBILE` | Error | High | false |
| `Assets/Sandbox/ForbiddenApi/HighConfidenceUsage.cs` | 64 | `API.RUN_IN_BACKGROUND_WRITE` | Warning | High | false |
| `Assets/Sandbox/ForbiddenApi/QualifiedUsage.cs` | 13 | `API.TIMESCALE_WRITE` | Error | High | false |
| `Assets/Sandbox/ForbiddenApi/QualifiedUsage.cs` | 14 | `API.QUIT` | Error | High | false |
| `Assets/Sandbox/ForbiddenApi/ShadowedUsage.cs` | 12 | `API.TIMESCALE_WRITE` | Error | Low | false |
| `Assets/Sandbox/ForbiddenApi/ShadowedUsage.cs` | 13 | `API.CURSOR_VISIBLE_WRITE` | Error | Low | false |
| `Assets/Sandbox/ForbiddenApi/UsingStaticUsage.cs` | 15 | `API.TIMESCALE_READ` | Warning | Low | false |
| `Assets/Sandbox/ForbiddenApi/UsingStaticUsage.cs` | 20 | `API.QUIT` | Error | Low | false |

Всего: **38** находок.

Случай, которого здесь нет и быть не может: голое `File` без `using System.IO;` (Low) — такой
файл не компилируется, поэтому он проверяется только тестами.
