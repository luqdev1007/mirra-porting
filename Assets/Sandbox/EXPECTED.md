# Ожидаемый результат сканирования песочницы

Эталон для ручной сверки с «Tools / Mirra Porting / Scan and Export».

Пока здесь только классификация editor-only (шаг 3). Колонки правил — RuleId, Severity,
Confidence, строка — добавятся на шаге 8, когда в песочнице появятся намеренные нарушения.

## EditorOnly

| Файл | EditorOnly | Почему |
|---|---|---|
| `Assets/Sandbox/Plain/PlainScript.cs` | false | Обычный код игры, `Assembly-CSharp` |
| `Assets/Sandbox/Editor/SpecialFolderEditorScript.cs` | true | Специальная папка `Editor`, `Assembly-CSharp-Editor` |
| `Assets/Sandbox/EditorAsmdef/EditorAsmdefScript.cs` | true | `Sandbox.EditorAsmdef` с `includePlatforms: ["Editor"]`, в пути слова `Editor` нет |
| `Assets/Sandbox/RuntimeAsmdef/Editor/NestedEditorFolderScript.cs` | false | `Sandbox.RuntimeAsmdef` без ограничений платформ: asmdef сильнее папки `Editor` |

Последняя строка — главная: запасной вариант «в пути есть сегмент `Editor`» дал бы здесь
`true`, поэтому сначала спрашиваются сборки и только потом путь.
