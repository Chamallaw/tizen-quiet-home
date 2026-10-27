using System.Collections.Generic;

namespace QuietHome.Core
{
    public enum Direction { Left, Right, Up, Down }

    /// <summary>Focus par rangées horizontales ; mémorise la colonne de chaque rangée.</summary>
    public sealed class FocusModel
    {
        readonly Dictionary<string, int> columns = new Dictionary<string, int>();
        IList<Row> rows = new List<Row>();

        public int RowIndex { get; private set; }

        /// <summary>Rangée prise au démarrage et après Reset (sinon la première non vide).</summary>
        public string HomeRowId;

        public int Column(int row) =>
            row >= 0 && row < rows.Count && columns.TryGetValue(rows[row].Id, out var c) ? c : 0;

        public Tile Current =>
            RowIndex < rows.Count && rows[RowIndex].Tiles.Count > 0 ? rows[RowIndex].Tiles[Column(RowIndex)] : null;

        /// <summary>Remplace les rangées en conservant la position (par identifiant de rangée).</summary>
        public void SetRows(IList<Row> newRows)
        {
            string currentId = RowIndex < rows.Count ? rows[RowIndex].Id : null;
            rows = newRows;
            foreach (var r in rows)
            {
                if (columns.TryGetValue(r.Id, out var c))
                    columns[r.Id] = r.Tiles.Count == 0 ? 0 : Clamp(c, 0, r.Tiles.Count - 1);
            }
            int idx = -1;
            for (int i = 0; i < rows.Count; i++) if (rows[i].Id == currentId) idx = i;
            RowIndex = idx >= 0 && rows[idx].Tiles.Count > 0 ? idx : HomeRow();
        }

        public bool Move(Direction dir)
        {
            if (rows.Count == 0 || rows[RowIndex].Tiles.Count == 0) return false;
            var row = rows[RowIndex];
            int col = Column(RowIndex);
            switch (dir)
            {
                case Direction.Left:
                    if (col == 0) return false;
                    columns[row.Id] = col - 1; return true;
                case Direction.Right:
                    if (col >= row.Tiles.Count - 1) return false;
                    columns[row.Id] = col + 1; return true;
                case Direction.Up:
                case Direction.Down:
                    int step = dir == Direction.Up ? -1 : 1;
                    int next = FirstNonEmpty(RowIndex + step, step);
                    if (next < 0 || next == RowIndex) return false;
                    RowIndex = next; return true;
            }
            return false;
        }

        public void Reset()
        {
            columns.Clear();
            RowIndex = HomeRow();
        }

        public void FocusRow(string rowId)
        {
            for (int i = 0; i < rows.Count; i++)
                if (rows[i].Id == rowId && rows[i].Tiles.Count > 0) RowIndex = i;
        }

        int HomeRow()
        {
            for (int i = 0; i < rows.Count; i++)
                if (rows[i].Id == HomeRowId && rows[i].Tiles.Count > 0) return i;
            return FirstNonEmpty(0, 1);
        }

        int FirstNonEmpty(int start, int step)
        {
            for (int i = start; i >= 0 && i < rows.Count; i += step)
                if (rows[i].Tiles.Count > 0) return i;
            return step > 0 && start == 0 ? 0 : -1;
        }

        static int Clamp(int v, int min, int max) => v < min ? min : v > max ? max : v;
    }
}
