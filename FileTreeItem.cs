using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RepoToTxtDesktop
{
    public class FileTreeItem : INotifyPropertyChanged
    {
        public string Name { get; set; }
        public string FullPath { get; set; }
        public ObservableCollection<FileTreeItem> Children { get; set; }
        public bool IsDirectory { get; set; }

        private bool _isSelected = true;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                OnPropertyChanged(nameof(IsSelected));
                if (Children != null)
                {
                    foreach (var child in Children)
                        child.IsSelected = value;
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
