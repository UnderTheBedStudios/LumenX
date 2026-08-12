using System.Diagnostics;
using System.Numerics;
using System.Runtime.Serialization;

namespace LumenX.GameProject
{
    [DataContract]
    class World : ViewModel
    {
        private bool _active;
        [DataMember(Order = 0)]
        public bool Active
        {
            get => _active;
            set { if (_active != value) { _active = value; OnPropertyChanged(nameof(Active)); } }
        }

        private string _worldName;
        [DataMember(Order = 1)]
        public string WorldName
        {
            get => _worldName;
            set { if (_worldName != value) { _worldName = value; OnPropertyChanged(nameof(WorldName)); } }
        }

        private Vector3 _lightDir;
        [DataMember(Order = 2)]
        public Vector3 LightDir
        {
            get => _lightDir;
            set { if (_lightDir != value) { _lightDir = value; OnPropertyChanged(nameof(LightDir)); } }
        }

        private Vector3 _lightColor;
        [DataMember(Order = 3)]
        public Vector3 LightColor
        {
            get => _lightColor;
            set { if (_lightColor != value) { _lightColor = value; OnPropertyChanged(nameof(LightColor)); } }
        }

        [DataMember(Order = 4)]
        public Project _project { get; private set; }


        public World(string name, Project project, Vector3 lightDir, Vector3 lightColor)
        {
            Debug.Assert(project != null, "Project cannot be null");
            _project = project;
            WorldName = name;
            LightDir = lightDir;
            LightColor = lightColor;
        }
    }
}