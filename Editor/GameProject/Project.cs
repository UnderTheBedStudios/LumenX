using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Windows.Input;
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

        public static UndoRedo undoRedo { get; } = new UndoRedo();

        public ICommand Undo { get; private set;}
        public ICommand Redo { get; private set; }

        public ICommand AddWorld { get; private set; }
        public ICommand RemoveWorld { get; private set; }

        private void AddWorldInternal(string worldName)
        {
            Debug.Assert(!string.IsNullOrEmpty(worldName.Trim()));
            _worlds.Add(new World(worldName, this));
        }
        private void RemoveWorldInternal(World world)
        {
            Debug.Assert(_worlds.Contains(world));
            _worlds.Remove(world);
        }
        
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

            AddWorld = new RelayCommand<object>(x =>
            {
                AddWorldInternal($"New World {_worlds.Count}");
                var newWorld = _worlds.Last();
                var worldIndex = _worlds.Count - 1;
                undoRedo.Add(new UndoRedoAction(
                    () => RemoveWorldInternal(newWorld),
                    () => _worlds.Insert(worldIndex, newWorld),
                    $"Add {newWorld.WorldName}"));
            });

            RemoveWorld = new RelayCommand<World>(x =>
            {
                var worldIndex = _worlds.IndexOf(x);
                RemoveWorldInternal(x);

                undoRedo.Add(new UndoRedoAction(
                    () => _worlds.Insert(worldIndex, x),
                    () => RemoveWorldInternal(x),
                    $"Remove {x.WorldName}"
                ));
            }, x => !x.Active);

            Undo = new RelayCommand<object>(x => undoRedo.Undo());
            Redo = new RelayCommand<object>(x => undoRedo.Redo());
        }
        
        public Project(string name, string path)
        {
            ProjectName = name;
            ProjectPath = path;

            OnDeserialized(new StreamingContext());
        }
    }
}