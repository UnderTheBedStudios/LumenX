using System.Diagnostics;

namespace LumenX.GameProject
{
    class World : ViewModel
    {
        private string _worldName;
        public string WorldName
        {
            get => _worldName;
            set
            {
                if (_worldName != value)
                {
                    _worldName = value;
                    OnPropertyChanged(nameof(WorldName));
                }
            }
        }

        public Project _project { get; private set;}

        public World(string name, Project project)
        {
            Debug.Assert(project != null, "Project cannot be null");
            _project = project;
            WorldName = name;
        }
    }
}