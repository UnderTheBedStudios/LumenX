using System.Collections.ObjectModel;
using System.Runtime.Serialization;

namespace LumenX.GameProject
{
    [DataContract(Name = "Game")]
    class Project : ViewModel
    {
        public static string Extension { get; } = ".lumenx";
        [DataMember]
        public string ProjectName { get; set; }
        [DataMember]
        public string ProjectPath { get; set; }
        [DataMember(Name = "Worlds")]
        private ObservableCollection<World> _worlds = new ObservableCollection<World>();

        public string FullPath => $"{ProjectPath}{ProjectName}{Extension}";
        public ObservableCollection<World> Worlds
        { get; }

        public Project(string name, string path)
        {
            ProjectName = name;
            ProjectPath = path;

            _worlds.Add(new World("Default World", this));
        }
    }
}