using System.Diagnostics;
using System.Runtime.Serialization;

namespace LumenX.GameProject
{
    [DataContract]
    class World : ViewModel
    {
        private string _worldName;
        [DataMember]
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

        [DataMember]
        public Project _project { get; private set;}

        private bool _active;
        [DataMember]
        public bool Active
        {
            get => _active;
            set
            {
                if (_active != value)
                {
                    _active = value;
                    OnPropertyChanged(nameof(Active));
                }
            }
        }

        public World(string name, Project project)
        {
            Debug.Assert(project != null, "Project cannot be null");
            _project = project;
            WorldName = name;
        }
    }
}