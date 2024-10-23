using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;

namespace RepoToTxtDesktop
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<FileTreeItem> FileTree { get; set; }
        public ICommand SelectFolderCommand { get; }
        public ICommand GenerateTxtCommand { get; }

        private readonly RepoAnalyzer _repoAnalyzer;
        private string _currentFolderPath;

        public event PropertyChangedEventHandler PropertyChanged;

        public MainViewModel()
        {
            FileTree = new ObservableCollection<FileTreeItem>();
            SelectFolderCommand = new RelayCommand(SelectFolder);
            GenerateTxtCommand = new RelayCommand(GenerateTxt);
            _repoAnalyzer = new RepoAnalyzer();
        }
        private void SelectFolder()
        {
            var dialog = new FolderBrowserDialog();
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                _currentFolderPath = dialog.SelectedPath;
                FileTree.Clear();
                var rootItem = CreateFileTree(new DirectoryInfo(dialog.SelectedPath));
                FileTree.Add(rootItem);
            }
        }

        private FileTreeItem CreateFileTree(DirectoryInfo dir)
        {
            var item = new FileTreeItem
            {
                Name = dir.Name,
                FullPath = dir.FullName,
                IsDirectory = true,
                Children = new ObservableCollection<FileTreeItem>()
            };

            foreach (var subDir in dir.GetDirectories())
            {
                if (!subDir.Name.StartsWith("."))
                    item.Children.Add(CreateFileTree(subDir));
            }

            foreach (var file in dir.GetFiles())
            {
                item.Children.Add(new FileTreeItem
                {
                    Name = file.Name,
                    FullPath = file.FullName,
                    IsDirectory = false
                });
            }

            return item;
        }

        private void GenerateTxt()
        {
            if (string.IsNullOrEmpty(_currentFolderPath))
            {
                MessageBox.Show("Please select a folder first.");
                return;
            }

            try
            {
                var selectedFiles = GetSelectedFiles(FileTree);
                _repoAnalyzer.GenerateReport(_currentFolderPath, selectedFiles);
                MessageBox.Show("Report generated successfully!");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error generating report: {ex.Message}");
            }
        }

        private List<FileTreeItem> GetSelectedFiles(IEnumerable<FileTreeItem> items)
        {
            var selectedFiles = new List<FileTreeItem>();
            foreach (var item in items)
            {
                if (!item.IsDirectory && item.IsSelected)
                    selectedFiles.Add(item);

                if (item.Children != null)
                    selectedFiles.AddRange(GetSelectedFiles(item.Children));
            }
            return selectedFiles;
        }
    }
}
