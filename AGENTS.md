# Работа с проектом

Инструкции действуют для всего репозитория. Работай из каталога с `Tetris.slnx`. Перед изменением прочитай участки кода, которых касается задача, и соответствующие разделы [спецификации](docs/spec.md) и [архитектуры](docs/architecture.md). Сохраняй пользовательские изменения в рабочей копии. Если поведение меняется по запросу, обнови связанные требования и критерии приёмки в той же правке.

## Куда вносить изменения

| Задача | Где искать |
|---|---|
| Изменить фигуры или их выпадение | [`PieceCatalog.cs`](src/Tetris.Core/Gameplay/PieceCatalog.cs) хранит маски. Вложенный `WeightedFigureSource` в [`GameSession.cs`](src/Tetris.Core/Gameplay/GameSession.cs) задаёт веса и повторный выбор. |
| Изменить игровые правила | [`GameSession.cs`](src/Tetris.Core/Gameplay/GameSession.cs) отвечает за команды, время и очки. [`Board.cs`](src/Tetris.Core/Gameplay/Board.cs) проверяет позиции, закрепляет фигуру и очищает строки. |
| Изменить управление или переходы между экранами | [`KeyboardControls.cs`](src/Tetris.Desktop/Presentation/KeyboardControls.cs) сопоставляет физические клавиши с командами. [`MainWindow.xaml.cs`](src/Tetris.Desktop/MainWindow.xaml.cs) передаёт ввод и время в [`GameController.cs`](src/Tetris.Desktop/Application/GameController.cs). |
| Изменить экран, палитру или звук | Разметка и ресурсы находятся в `src/Tetris.Desktop`. `CellSurface` рисует клетки по маскам и типам. [`GameAudio.cs`](src/Tetris.Desktop/Presentation/GameAudio.cs) подключает дорожки из [`assets/music`](assets/music/). |
| Изменить рекорды | [`RecordBook.cs`](src/Tetris.Core/Records/RecordBook.cs) обновляет списки и сортирует Топ-5. [`FileRecordStore.cs`](src/Tetris.Desktop/Storage/FileRecordStore.cs) определяет путь и сохраняет файлы. |

Храни игровые правила в `Tetris.Core`. Ядро принимает команды и интервалы времени, поэтому его можно проверять без окна. Держи WPF, MP3 и файловые операции в `Tetris.Desktop`. Используй `GameSnapshot` для отображения состояния и `LinesCleared` для реакции на очистку при одном закреплении. Обновляй таблицу рекордов по переходу в `Lost` через контроллер.

Скрипт `switch_theme.py` копирует из выбранной темы `App.xaml`, `MainWindow.xaml` и `Presentation/CellSurface.cs`. При правке одного из этих файлов перенеси общие изменения в обе копии в [`assets`](assets/). Сохраняй различия оформления и имена элементов и обработчиков, которые использует код окна. При правке звука сверяй имена MP3 с файлами на диске, включая два пробела в `game  loop.mp3`.

## Проверка изменений

- Собери решение командой `dotnet build Tetris.slnx`. Для изменений интерфейса, ввода или звука запусти `dotnet run --project src/Tetris.Desktop/Tetris.Desktop.csproj` на Windows и проверь затронутый сценарий из [критериев приёмки](docs/spec.md#критерии-приёмки).
- Для сценариев с заданной последовательностью фигур передай `GameSession` предсказуемый `IFigureSource` и продвигай время через `Advance`.
- При изменениях в рекордах проверь восстановление обоих файлов после перезапуска, повтор сохранения и порядок при равном счёте. Используй временный каталог через `FileRecordStore(string directory)` или отдельную копию программы вне дерева проекта. Файлы `recent.json`, `top.json`, `pending.json` и `*.json.tmp` исключены правилами `db/.gitignore`.
- По итогам укажи, что изменено, какие проверки выполнены и где результат требует дополнительной проверки.
