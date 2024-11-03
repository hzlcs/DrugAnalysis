using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChartEditWPF.Models
{
    public partial class DataTableList : ObservableObject
    {
        private readonly object? defaultCellValue = 1;
        private readonly List<string> columns = [];
        private readonly List<string> rows = [];

        private readonly List<List<object?>> data = [];
        private readonly List<ColumnInfo> columnInfos = [];
        private readonly List<RowInfo> rowInfos = [];

        private readonly Dictionary<string, int> colIndex = [];
        private readonly Dictionary<string, int> rowIndex = [];

        public IReadOnlyList<ColumnInfo> Columns => columnInfos;
        public IReadOnlyList<RowInfo> Rows => rowInfos;

        public IReadOnlyList<object?> this[int rowIndex] => data[rowIndex];
        public object? this[int rowIndex, int colIndex] => data[rowIndex][colIndex];
        public IReadOnlyDictionary<string, object?> this[string rowName] => new ColCollection(colIndex, data[rowIndex[rowName]]);
        public object? this[string rowName, string columnName] => data[rowIndex[rowName]][colIndex[columnName]];

        public DataTableList() 
        {
            AddColumn("-");
        }
        public DataTableList(IEnumerable<string> columns, IEnumerable<string> rows) : this()
        {
            foreach (var col in columns)
            {
                AddColumn(col);
            }
            foreach (var row in rows)
            {
                AddRow(row);
            }

        }

        public void AddColumn(string columnName)
        {
            if (colIndex.ContainsKey(columnName))
            {
                throw new ArgumentException($"Column {columnName} already exists", nameof(columnName));
            }
            columns.Add(columnName);
            var index = columns.Count - 1;
            colIndex.Add(columnName, index);
            columnInfos.Add(new ColumnInfo(columnName, data.Select(v => v[index])));
            foreach (var row in data)
            {
                if (row.Count >= columns.Count)
                    break;
                row.Add(defaultCellValue);

            }
        }

        public void AddRow(string rowName)
        {
            if (rowIndex.ContainsKey(rowName))
            {
                throw new ArgumentException($"Row {rowName} already exists", nameof(rowName));
            }
            rows.Add(rowName);
            rowInfos.Add(new RowInfo(rowName));
            rowIndex.Add(rowName, rows.Count - 1);
            if (data.Count >= rows.Count)
                return;
            var newRow = Enumerable.Repeat(defaultCellValue, columns.Count).ToList();
            newRow[0] = rowName;
            data.Add(newRow);
        }

        readonly struct ColCollection : IReadOnlyDictionary<string, object?>
        {
            private readonly Dictionary<string, int> colIndex;
            private readonly List<object?> data;

            public ColCollection(Dictionary<string, int> colIndex, List<object?> data)
            {
                this.colIndex = colIndex;
                this.data = data;
            }

            public object? this[string key] => data[colIndex[key]];
            public IEnumerable<string> Keys => colIndex.Keys;
            public IEnumerable<object?> Values => data;
            public int Count => colIndex.Count;
            public bool ContainsKey(string key) => colIndex.ContainsKey(key);
            public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
            {
                List<object?> list = data;
                return colIndex.Select(v => new KeyValuePair<string, object?>(v.Key, list[v.Value])).GetEnumerator();
            }

            public bool TryGetValue(string key, out object? value)
            {
                if (colIndex.TryGetValue(key, out var index))
                {
                    value = data[index];
                    return true;
                }
                value = null;
                return false;
            }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        public record ColumnInfo(string Name, IEnumerable<object?> Data);

        public partial class RowInfo(string Name) : ObservableObject
        {
            public string Name { get; } = Name;
            [ObservableProperty]
            private bool _checked;
        }

        [RelayCommand]
        void SelectAll(bool isChecked)
        {
            foreach(var row in Rows)
            {
                row.Checked = isChecked;
            }
        }

    }
}
