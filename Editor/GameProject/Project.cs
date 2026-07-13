using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.Serialization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using LumenX.Utils;

namespace LumenX.GameProject
{
    [DataContract(Name = "Game")]
    class Project : ViewModel
    {
        public static string Extension { get; } = ".lumenx";
        [DataMember]
        public string ProjectName { get; private set; } = "New Project";
        [DataMember]
        public string ProjectPath { get; set; }
        [DataMember(Name = "Worlds")]
        private ObservableCollection<World> _worlds = new ObservableCollection<World>();

        public string FullPath => $"{ProjectPath}{ProjectName}{Extension}";
        public ReadOnlyObservableCollection<World> Worlds
        { get; private set; }

        private World _activeWorld;
        public World ActiveWorld
        {
            get => _activeWorld;
            set
            {
                if (_activeWorld != value)
                {
                    _activeWorld = value;
                    OnPropertyChanged(nameof(ActiveWorld));
                }
            }
        }

        #region Horrible Yucky Icky Shit, but it works... Just don't look inside this region pls. I beg of you.
        public static Project Current => Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop ?
        desktop.MainWindow.DataContext as Project : null;
        #endregion
        
        public static Project Load(string file)
        {
            Debug.Assert(File.Exists(file));
            return Serializer.FromFile<Project>(file);

        }

        public void Unload()
        {
            
        }

        public static void Save(Project project)
        {
            Serializer.ToFile(project, project.FullPath);
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            if(_worlds != null)
            {
                Worlds = new ReadOnlyObservableCollection<World>(_worlds);
                OnPropertyChanged(nameof(Worlds));
            }
            ActiveWorld =  Worlds.FirstOrDefault(x=>x.Active);
            Console.WriteLine($"[DEBUG] Active World: {ActiveWorld.WorldName}");
        }
        
        public Project(string name, string path)
        {
            ProjectName = name;
            ProjectPath = path;

            OnDeserialized(new StreamingContext());
        }
    }
}