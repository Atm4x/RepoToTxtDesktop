using ICSharpCode.AvalonEdit.Highlighting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace RepoToTxtDesktop
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new MainViewModel();
            DataContext = _viewModel;

            // Настраиваем редактор кода
            TextEditor.Options.EnableHyperlinks = false;
            TextEditor.Options.EnableEmailHyperlinks = false;
            TextEditor.Options.EnableTextDragDrop = false;
        }

        private void TreeItem_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var textBlock = sender as TextBlock;
                var fileItem = textBlock.DataContext as FileTreeItem;

                if (!fileItem.IsDirectory)
                {
                    try
                    {
                        string content = System.IO.File.ReadAllText(fileItem.FullPath);
                        TextEditor.Text = content;

                        // Определяем подсветку синтаксиса на основе расширения файла
                        string extension = System.IO.Path.GetExtension(fileItem.FullPath).ToLower();
                        TextEditor.SyntaxHighlighting = GetHighlightingForExtension(extension);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error reading file: {ex.Message}");
                    }
                }
            }
        }

        private IHighlightingDefinition GetHighlightingForExtension(string extension)
        {
            var highlightingManager = HighlightingManager.Instance;

            return extension switch
            {
                ".cs" => highlightingManager.GetDefinition("C#"),
                ".xml" => highlightingManager.GetDefinition("XML"),
                ".json" => highlightingManager.GetDefinition("JSON"),
                ".js" => highlightingManager.GetDefinition("JavaScript"),
                ".html" => highlightingManager.GetDefinition("HTML"),
                ".css" => highlightingManager.GetDefinition("CSS"),
                ".py" => highlightingManager.GetDefinition("Python"),
                _ => null
            };
        }
    }
}
