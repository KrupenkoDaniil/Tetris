from pathlib import Path
import shutil
import subprocess
from tempfile import NamedTemporaryFile


PROJECT = Path(__file__).resolve().parent
THEMES = {"1": "Dark Green", "2": "Night Archade"}
THEME_FILES = ("App.xaml", "MainWindow.xaml", "Presentation/CellSurface.cs")


def replace_file(path, contents):
    temporary = None
    try:
        with NamedTemporaryFile(dir=path.parent, delete=False) as output:
            temporary = Path(output.name)
            output.write(contents)
        temporary.replace(path)
    finally:
        if temporary is not None:
            temporary.unlink(missing_ok=True)


def apply_theme(theme):
    source = PROJECT / "assets" / theme
    destination = PROJECT / "src" / "Tetris.Desktop"
    changes = []
    for name in THEME_FILES:
        path = destination / name
        old_contents = path.read_bytes()
        new_contents = (source / name).read_bytes()
        if old_contents != new_contents:
            changes.append((path, old_contents, new_contents))

    applied = []
    try:
        for path, old_contents, new_contents in changes:
            replace_file(path, new_contents)
            applied.append((path, old_contents))
    except OSError:
        for path, old_contents in reversed(applied):
            replace_file(path, old_contents)
        raise


def main():
    print("Выбор оформления")
    for number, theme in THEMES.items():
        print(f"{number}. {theme}")
    print("0. Выход")

    while True:
        try:
            choice = input("Выбери 0, 1 или 2: ").strip()
        except (EOFError, KeyboardInterrupt):
            print()
            return 0
        if choice == "0":
            return 0
        if choice in THEMES:
            break
        print("Неверный номер. Попробуй ещё раз.")

    theme = THEMES[choice]
    try:
        apply_theme(theme)
    except OSError as error:
        print(f"Не удалось заменить файлы темы: {error}")
        return 1

    dotnet = shutil.which("dotnet")
    if dotnet is None:
        print(f"Файлы темы {theme} применены. Для пересборки нужен .NET SDK.")
        return 1

    print(f"Тема {theme} выбрана. Собираю игру…", flush=True)
    result = subprocess.run(
        [dotnet, "build", "Tetris.slnx", "--no-incremental", "--nologo"],
        cwd=PROJECT,
        check=False,
    )
    if result.returncode:
        print("Файлы темы заменены. Если игра запущена, закрой её и выбери тему снова.")
        return result.returncode

    print(f"Готово. При следующем запуске будет применена тема {theme}.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
