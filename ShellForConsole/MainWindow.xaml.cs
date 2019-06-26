#region usings

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Xml.Linq;
using Microsoft.Win32;

#endregion

namespace ShellForConsole
{
    public partial class MainWindow
    {
        public static AnimationTimeline TransparencyAnimation;
        public MainWindow()
        {
            InitializeComponent();
            TransparencyAnimation = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromSeconds(0.5)
            };
        }

        private void ConsoleSelect_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter= "Исполняемыe файлы|*.exe|Все файлы|*.*",
                InitialDirectory= AppDomain.CurrentDomain.BaseDirectory,
                Title="Выберите исполняемый файл"
            };
            bool? showDialog = openFileDialog.ShowDialog();
            if (showDialog != true) return;
            PathToConsole.Text = openFileDialog.FileName.CheckCurrentPath();
            if(!string.IsNullOrWhiteSpace(PathToPattern.Text))
            {
                BottomGrid.Visibility = Visibility.Visible;
            }
        }

        private void InitComboBox(XContainer tabs)
        {
            ComboBox.DisplayMemberPath = "Name";
            ComboBox.SelectedValuePath = "Tab";
            List<ComboData> comboDataList = tabs.Elements().Select(tab => new ComboData(tab)).ToList();
            ComboBox.ItemsSource = comboDataList;
        }

        private void SetInfoFromXml(XContainer info)
        {
            XElement firstOrDefault = info.Elements().FirstOrDefault(a => a.Name.LocalName.ToLower() == "windowlabel");
            if (firstOrDefault != null)
                Title = firstOrDefault.Value;
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Tab tab = (Tab) ComboBox.SelectedValue;
            if (tab != null) GenerateForm(tab);
        }

        private void GenerateForm(Tab tab)
        {
            MainGrid.Children.Clear();
            MainGrid.RowDefinitions.Clear();


            tab.Fields.OrderBy(a => a.Order).ToList().ForEach(GenerateRow);

            MainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto, MinHeight=25 });
            Button btn = new Button {Content = "Выполнить"};
            btn.Click += SendCommand;
            Grid.SetColumnSpan(btn, 3);
            Grid.SetColumn(btn, 0);
            Grid.SetRow(btn, tab.Fields.Count);
            MainGrid.Children.Add(btn);
        }

        private void GenerateRow(Field field)
        {
            TransparencyAnimation.BeginTime = TimeSpan.FromSeconds(field.Order * 0.1);
            MainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto, MinHeight = 25 });
            AddLabel(field);
            AddControl(field);
        }

        private void AddControl(Field field)
        {
            switch (field.Type.ToLower())
            {
                case "text":
                {
                    GenerateTextControl(field);
                        break;
                }
                case "number":
                {
                    GenerateNumberControl(field);
                    break;
                }
                case "inputfile":
                {
                    GenerateInputfileControl(field);
                    break;
                }
                case "outputfile":
                {
                    GenerateOutputFileControl(field);
                        break;
                }
                case "combobox":
                {
                    GenerateComboBoxcontrol(field);
                        break;
                }
                case "inputfilemultiple":
                {
                    GenerateMultipseSelectFileControl(field);
                        break;
                }
            }
        }

        private void GenerateMultipseSelectFileControl(Field field)
        {

            TextBox control = new TextBox { Width = 0 };
            Button button = new Button { Content = "Выбрать файлы" };
            button.Click += (sender, e) =>
            {
                OpenFileDialog dialog = new OpenFileDialog
                {
                    Multiselect = true,
                    InitialDirectory = AppDomain.CurrentDomain.BaseDirectory,
                };
                bool? showDialog = dialog.ShowDialog();
                if (showDialog != true) return;
                control.Text = string.Empty;
                ((Button) sender).ToolTip = string.Empty;
                dialog.FileNames.ToList().ForEach(a => control.Text += a.CheckCurrentPath() + "||y||");
                control.Text = control.Text.Remove(control.Text.Length - 4, 3) + "||n";
                dialog.SafeFileNames.ToList().ForEach(a => ((Button)sender).ToolTip += a + "\n");
            };

            Grid.SetRow(button, field.Order - 1);
            Grid.SetColumn(button, 1);
            Grid.SetColumnSpan(button, 2);
            button.AddAnimation();
            MainGrid.Children.Add(button);

            Grid.SetColumn(control, 1);
            Grid.SetRow(control, field.Order - 1);
            control.AddAnimation();
            MainGrid.Children.Add(control);
        }

        private void GenerateComboBoxcontrol(Field field)
        {
            TextBox control = new TextBox { Width = 0 };
            ComboBox combobox = new ComboBox
            {
                SelectedValuePath = "Key",
                DisplayMemberPath = "Value",
                ItemsSource = field.DropDownItems,
                SelectedIndex = 0
            };

            combobox.SelectionChanged += (o, e) =>
            {
                control.Text = ((ComboBox)o).SelectedValue.ToString();
            };

            Grid.SetColumn(combobox, 1);
            Grid.SetRow(combobox, field.Order - 1);
            Grid.SetColumnSpan(combobox, 2);
            combobox.AddAnimation();
            MainGrid.Children.Add(combobox);

            Grid.SetColumn(control, 1);
            Grid.SetRow(control, field.Order - 1);
            control.AddAnimation();
            MainGrid.Children.Add(control);
        }

        private void GenerateOutputFileControl(Field field)
        {
            TextBox control = new TextBox();
            Button btn = new Button { Content = "Выбрать файл", Width = 100 };
            btn.Click += (o, e) =>
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Title = "Выберете файл для сохранения",
                    Filter = "Все файлы|*.*",
                    OverwritePrompt = false,
                    InitialDirectory = AppDomain.CurrentDomain.BaseDirectory,
                };
                bool? showDialog = saveFileDialog.ShowDialog();
                if (showDialog == true)
                    control.Text = saveFileDialog.FileName.CheckCurrentPath();
            };

            Grid.SetColumn(control, 1);
            Grid.SetRow(control, field.Order - 1);
            control.AddAnimation();
            MainGrid.Children.Add(control);

            Grid.SetColumn(btn, 2);
            Grid.SetRow(btn, field.Order - 1);
            btn.AddAnimation();
            MainGrid.Children.Add(btn);
        }

        private void GenerateInputfileControl(Field field)
        {
            TextBox control = new TextBox();
            Button btn = new Button { Content = "Выбрать файл", Width = 100 };
            btn.Click += (o, e) =>
            {
                OpenFileDialog openFileDialog = new OpenFileDialog
                {
                    InitialDirectory = AppDomain.CurrentDomain.BaseDirectory,
                };
                bool? showDialog = openFileDialog.ShowDialog();
                if (showDialog == true)
                {
                    control.Text = openFileDialog.FileName.CheckCurrentPath();
                }
            };
            
            Grid.SetColumn(control, 1);
            Grid.SetRow(control, field.Order - 1);
            control.AddAnimation();
            MainGrid.Children.Add(control);

            Grid.SetColumn(btn, 2);
            Grid.SetRow(btn, field.Order-1);
            btn.AddAnimation();
            MainGrid.Children.Add(btn);
        }

        private void GenerateNumberControl(Field field)
        {
            TextBox control = new TextBox();
            control.AddAnimation();
            control.TextChanged += (sender, e) =>
            {
                Regex reg = new Regex("[^0-9.,]");
                ((TextBox)sender).Text = reg.Replace(((TextBox)sender).Text, "");
            };
            Grid.SetColumnSpan(control, 2);
            Grid.SetColumn(control, 1);
            Grid.SetRow(control, field.Order - 1);
            MainGrid.Children.Add(control);
        }

        private void GenerateTextControl(Field field)
        {
            TextBox control = new TextBox();
            control.AddAnimation();
            Grid.SetColumnSpan(control, 2);
            Grid.SetColumn(control, 1);
            Grid.SetRow(control, field.Order-1);
            MainGrid.Children.Add(control);
        }

        private void AddLabel(Field field)
        {
            TextBlock label = new TextBlock
            {
                Text = field.Label,
                MaxWidth = 200,
                TextWrapping = TextWrapping.WrapWithOverflow,
                Padding = new Thickness(0, 0, 10, 0),
                ToolTip = field.Label
            };
            label.AddAnimation();
            Grid.SetColumn(label, 0);
            Grid.SetRow(label, field.Order - 1);
            MainGrid.Children.Add(label);
        }

        internal void SendCommand(object sender, EventArgs e)
        {
            string consoleParameter = ((Tab) ComboBox.SelectedValue).Key;
            List<string> commands = MainGrid
                .Children
                .OfType<TextBox>()
                .Where(a => Grid.GetColumn(a) == 1)
                .OrderBy(Grid.GetRow)
                .SelectMany(textBox => textBox.Text.Split(new[] {"||"}, StringSplitOptions.RemoveEmptyEntries))
                .ToList();

            if (commands.All(string.IsNullOrWhiteSpace) && commands.Count > 0)
            {
                MessageBox.Show("Одно из полей не содержит значения, выполнение приостановлено", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            ConsoleOut.Text = "Подождите";
            Process p = new Process
            {
                StartInfo =
                {
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    FileName = PathToConsole.Text,
                    Arguments = consoleParameter,
                    StandardOutputEncoding = Encoding.GetEncoding(866)
                }
            };
            p.Start();
            StreamWriter writer = p.StandardInput;
            StreamReader reader = p.StandardOutput;
            foreach (string s in commands)
            {
                writer.WriteLine(s);
            }
            p.WaitForExit();
            ConsoleOut.Text = reader.ReadToEnd() + @"Готово";
        }

        private void PatternSelect_Click(object sender, RoutedEventArgs e)
        {
            BottomGrid.Visibility = Visibility.Collapsed;
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                InitialDirectory = AppDomain.CurrentDomain.BaseDirectory,
                Title = "Выберите файл разметки",
                Filter= "Файлы разметки для оболочки|*.sfck|Все файлы|*.*"
            };
            bool? showDialog = openFileDialog.ShowDialog();
            if (showDialog != true) return;

            PathToPattern.Text = openFileDialog.FileName.CheckCurrentPath();
            StreamReader reader = new StreamReader(PathToPattern.Text, Encoding.UTF8);
            XDocument xDoc;
            try
            {
                xDoc = XDocument.Parse(reader.ReadToEnd());
            }
            catch
            {
                MessageBox.Show("Некорректный файл разметки", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                PathToPattern.Text = "";
                return;
            }
            reader.Close();
            XElement root = xDoc.Elements().FirstOrDefault(b => b.Name.LocalName.ToLower() == "root");
            if (root == null)
            {
                MessageBox.Show("Отсутствует объект \"root\"", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                PathToPattern.Text = string.Empty;
                return;
            }
            XElement info = root.Elements().FirstOrDefault(a => a.Name.LocalName.ToLower() == "info");
            XElement tabs = root.Elements().FirstOrDefault(a => a.Name.LocalName.ToLower() == "tabs");
            SetInfoFromXml(info);
            InitComboBox(tabs);
            if (!string.IsNullOrWhiteSpace(PathToConsole.Text))
            {
                BottomGrid.Visibility = Visibility.Visible;
            }
        }
    }

    internal static class Extentions
    {
        public static string CheckCurrentPath(this string file)
        {
            return file.Contains(AppDomain.CurrentDomain.BaseDirectory) ? file.Split('\\').LastOrDefault() : file;
        }

        public static void AddAnimation(this UIElement control)
        {
            control.Opacity = 0;
            control.BeginAnimation(UIElement.OpacityProperty, MainWindow.TransparencyAnimation);
        }
    }


    internal class ComboData
    {
        public ComboData(XElement tab)
        {
            Tab = new Tab(tab);
            Name = Tab.Name;
        }

        public Tab Tab { get; set; }
        public string Name { get; set; }
    }

    internal class Tab
    {
        internal List<Field> Fields = new List<Field>();

        internal string Key { get; set; }
        internal string Name { get; set; }

        public Tab()
        {
        }

        public Tab(XElement tab)
        {
            XAttribute xAttributeName = tab.Attribute(XName.Get("name"));
            if (xAttributeName != null) Name = xAttributeName.Value;

            XAttribute xAttributeKey = tab.Attribute(XName.Get("key"));
            if (xAttributeKey != null) Key = xAttributeKey.Value;

            XElement xFields = tab.Element(XName.Get("fields"));
            if (xFields == null) return;
            foreach (XElement field in xFields.Elements())
            {
                Fields.Add(new Field(field));
            }
                
        }
        
    }

    internal class Field
    {
        internal List<KeyValuePair<string, string>> DropDownItems = new List<KeyValuePair<string, string>>();
        internal string Label;
        internal int Order;
        internal string Type;

        public Field()
        {
        }

        public Field(XElement field)
        {
            XAttribute xAttributeOrder = field.Attribute(XName.Get("order"));
            if (xAttributeOrder != null)
                Order = int.Parse(xAttributeOrder.Value);
            XAttribute xAttributeType = field.Attribute(XName.Get("type"));
            if (xAttributeType != null) Type = xAttributeType.Value;
            XAttribute xAttributeLabel = field.Attribute(XName.Get("label"));
            if (xAttributeLabel != null) Label = xAttributeLabel.Value;
            foreach (XElement element in field.Elements())
            {
                if (element.Name.LocalName.ToLower() == "comboboxitem")
                {
                    XAttribute xAttributeValue = element.Attribute(XName.Get("value"));
                    if (xAttributeValue == null) continue;
                    XAttribute xAttributeKey = element.Attribute(XName.Get("key"));
                    if (xAttributeKey == null) continue;
                    DropDownItems.Add(new KeyValuePair<string, string>(xAttributeKey.Value, xAttributeValue.Value));
                }
            }
               
        }
    }
}