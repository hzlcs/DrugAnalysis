using ChartEditLibrary.Entitys;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ChartEditWPF.ViewModels
{
    public partial class ColorPropertyEditVM : ObservableObject
    {
        public ColorPropertyEditVM()
        {
            Colors = [.. CommonConfig.Instance.ColorEditItems.Select(v => new EditItem(v, Remove, Add))];
        }

        public ObservableCollection<EditItem> Colors { get; }

        private void Remove(EditItem item)
        {
            Colors.Remove(item);
            CommonConfig.Instance.ColorEditItems.Remove(item.ColorItem);
        }

        [RelayCommand]
        private void Add(EditItem? item)
        {
            int name = 0, index = Colors.Count;
            if(Colors.Count == 0)
            {
                name = 0;
                index = -1;
            }
            else
            {
                index = item is null ? Colors.Count - 1 : Colors.IndexOf(item);
                name = Colors.Count;
                while (CommonConfig.Instance.ColorEditItems.Any(v => v.Name == $"color{name}"))
                {
                    name++;
                }
            }
            ColorEditItem ci = new ColorEditItem() { Name = "color" + name };
            CommonConfig.Instance.ColorEditItems.Insert(index + 1, ci);
            Colors.Insert(index + 1, new EditItem(ci, Remove, Add));
            Colors[index + 1].EditColorCommand.Execute(null);
        }

        public partial class EditItem(ColorEditItem colorEditItem, Action<EditItem> removeAction, Action<EditItem> addAction) : ObservableObject
        {
            public ColorEditItem ColorItem { get; } = colorEditItem;

            [ObservableProperty]
            private Visibility editVisibility = Visibility.Collapsed;

            [ObservableProperty]
            private string buttonContent = ">";

            [RelayCommand]
            private void EditColor()
            {
                if (ButtonContent == ">")
                {
                    EditVisibility = Visibility.Visible;
                    ButtonContent = "<";
                }
                else
                {
                    EditVisibility = Visibility.Collapsed;
                    ButtonContent = ">";
                }
            }

            [RelayCommand]
            private void Remove()
            {
                removeAction?.Invoke(this);
            }

            [RelayCommand]
            private void Add()
            {
                addAction?.Invoke(this);
            }
        }
    }
}
