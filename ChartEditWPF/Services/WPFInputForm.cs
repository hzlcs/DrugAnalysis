﻿using ChartEditLibrary.Interfaces;
using ChartEditWPF.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChartEditWPF.Services
{
    internal class WPFInputForm : IInputForm
    {
        private string? result;
        private object value = null!;
        private object[]? values;

        public T ShowCombboxDialog<T>(string title, T[] options, bool canEdit = false)
        {
            new SelectOneWindow(title, options, canEdit, ValueCallback).ShowDialog();
            return (T)value;
        }

        private void ValueCallback(object obj)
        {
            value = obj;
        }

        public T[]? ShowListDialog<T>(string title, string itemName, T[] options)
        {
            new SelectMutiWindow(title, itemName, options, ValuesCallback).ShowDialog();
            if (values is null)
                return null;
            return values.Cast<T>().ToArray();
        }

        public object[]? ShowListDialog(string title, string itemName, SelectItem[] items)
        {
            new SelectMutiWindow(title, itemName, items, ValuesCallback).ShowDialog();
            return values;
        }



        private void ValuesCallback(object[] obj)
        {
            values = obj;
        }

        public bool TryGetInput(object? data, [MaybeNullWhen(false)] out string value)
        {
            var res = new InputWindow(data?.ToString(), InputWindow_Callback).ShowDialog();
            value = result;
            return res.GetValueOrDefault();
        }

        private void InputWindow_Callback(string obj)
        {
            result = obj;
        }

        
    }

    public partial class SelectItem : ObservableObject
    {
        [ObservableProperty]
        private bool isSelected;

        public SelectItem(object item)
        {
            Item = item;
        }

        public object Item { get; set; }
    }
}
