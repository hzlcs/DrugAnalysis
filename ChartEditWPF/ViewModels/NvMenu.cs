using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChartEditWPF.ViewModels
{
    public class NvMenu
    {
        private readonly List<string> menuItems = new List<string>();

        public IReadOnlyList<string> MenuItems => menuItems;

        public NvMenu()
        {
            AddMenuItem("File");
            AddMenuItem("Edit");
            AddMenuItem("View");
            AddMenuItem("Help");
        }

        public void AddMenuItem(string menuItem)
        {
            if (menuItems.Contains(menuItem))
            {
                throw new ArgumentException($"Menu item {menuItem} already exists", nameof(menuItem));
            }
            menuItems.Add(menuItem);
        }
    }

    public class MenuItem
    {
        public string Name { get; }
        public MenuItem(string name)
        {
            Name = name;
        }
    }
}
