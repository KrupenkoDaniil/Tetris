using System.Windows;
using System.Windows.Media;
using Tetris.Core.Gameplay;

namespace Tetris.Desktop.Presentation;

public sealed class CellSurface : FrameworkElement
{
    private static readonly Brush Field = CreateBrush("#121824");
    private static readonly Brush GridLine = CreateBrush("#232E40");
    private static readonly Brush Silver = CreateBrush("#B9C5D3");
    private static readonly Brush[][] Palettes =
    [
        MakePalette("#25BC68", "#4ADE80", "#86EFAC", "#A3E635"),
        MakePalette("#60A5FA", "#38BDF8", "#A5D8FF", "#F3F8FF"),
        MakePalette("#FFD38E", "#FFAA48", "#FF8738", "#FF6A5A"),
        MakePalette("#AF96FF", "#D8ADFF", "#F77EC9", "#FFB9E4"),
        MakePalette("#08090B", "#B9C5D3", "#EDF2F7", "#F6C34E")
    ];

    private GameSnapshot? _board;
    private PieceSnapshot? _preview;
    private int _level = 1;

    public void ShowBoard(GameSnapshot snapshot, int level)
    {
        _board = snapshot;
        _preview = null;
        _level = level;
        InvalidateVisual();
    }

    public void ShowPiece(PieceSnapshot? piece, int level)
    {
        _board = null;
        _preview = piece;
        _level = level;
        InvalidateVisual();
    }

    protected override void OnRender(DrawingContext drawing)
    {
        base.OnRender(drawing);
        if (_board is { } snapshot)
        {
            RenderBoard(drawing, snapshot);
        }
        else if (_preview is { } piece)
        {
            var size = Math.Floor(Math.Min(ActualWidth / 5, ActualHeight / 5));
            if (size >= 2)
            {
                var width = piece.Mask.Max(row => row.Length);
                var x = Math.Floor((ActualWidth - width * size) / 2);
                var y = Math.Floor((ActualHeight - piece.Mask.Count * size) / 2);
                DrawPiece(drawing, piece, size, x, y);
            }
        }
    }

    private void RenderBoard(DrawingContext drawing, GameSnapshot snapshot)
    {
        const int columns = GameSnapshot.BoardColumns;
        const int rows = GameSnapshot.BoardRows;
        var size = Math.Floor(Math.Min((ActualWidth - 2) / columns, (ActualHeight - 2) / rows));
        if (size < 2)
            return;

        var width = columns * size;
        var height = rows * size;
        var x = Math.Floor((ActualWidth - width) / 2);
        var y = Math.Floor((ActualHeight - height) / 2);
        drawing.DrawRectangle(GridLine, null, new Rect(x - 1, y - 1, width + 2, height + 2));
        drawing.DrawRectangle(Field, null, new Rect(x, y, width, height));

        for (var col = 1; col < columns; col++)
            drawing.DrawRectangle(GridLine, null, new Rect(x + col * size, y, 1, height));
        for (var row = 1; row < rows; row++)
            drawing.DrawRectangle(GridLine, null, new Rect(x, y + row * size, width, 1));

        foreach (var cell in snapshot.LockedCells)
            DrawCell(drawing, x + cell.Column * size, y + cell.Row * size, size, cell.PieceType);

        if (snapshot.ActivePiece is { } active)
            DrawPiece(drawing, active, size, x + active.Column * size, y + active.Row * size);
    }

    private void DrawPiece(DrawingContext drawing, PieceSnapshot piece, double size, double x, double y)
    {
        for (var row = 0; row < piece.Mask.Count; row++)
        {
            for (var col = 0; col < piece.Mask[row].Length; col++)
            {
                if (piece.Mask[row][col] == '#')
                    DrawCell(drawing, x + col * size, y + row * size, size, piece.Type);
            }
        }
    }

    private void DrawCell(DrawingContext drawing, double x, double y, double size, int pieceType)
    {
        var slot = (pieceType - 1) % 4;
        var fill = Palettes[_level - 1][slot];
        var rect = new Rect(x + 1, y + 1, size - 1, size - 1);
        if (_level == 5 && slot == 0 && size >= 4)
        {
            drawing.DrawRectangle(Silver, null, rect);
            rect.Inflate(-1, -1);
        }

        drawing.DrawRectangle(fill, null, rect);
    }

    private static Brush[] MakePalette(params string[] colors) =>
        Array.ConvertAll(colors, CreateBrush);

    private static Brush CreateBrush(string hex)
    {
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
        brush.Freeze();
        return brush;
    }
}
