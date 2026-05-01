using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.IO;
using LumenX.Utils;

namespace LumenX.GameProject
{
    [DataContract]
    public class ProjectTemplate
    {
        [DataMember]
        public string Type { get; set; }
        [DataMember]
        public string File { get; set; }
        [DataMember]
        public List<string> Folders { get; set; }

        public byte[] Icon { get; set; }
        public string IconPath { get; set; }

        public byte[] Preview { get; set; }
        public string PreviewPath { get; set; }
    }

    class NewProject : ViewModel
    {
        //TODO: Load the templates from installation location
        private readonly string _templateDir = @"../../../ProjectTemplates";
        private string _projectName = "New Project";
        public string ProjectName
        {
            get => _projectName;
            set
            {
                if (_projectName != value)
                {
                    _projectName = value;
                    OnPropertyChanged(nameof(ProjectName));
                }
            }
        }

        private string _projectPath = $@"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}/LumenXProjects";
        public string ProjectPath
        {
            get => _projectPath;
            set
            {
                if (_projectPath != value)
                {
                    _projectPath = value;
                    OnPropertyChanged(nameof(ProjectPath));
                }
            }
        }

        private ObservableCollection<ProjectTemplate> _templates = new ObservableCollection<ProjectTemplate>();
        private ReadOnlyObservableCollection<ProjectTemplate> Templates { get ;}

        public NewProject()
        {
            Templates = new ReadOnlyObservableCollection<ProjectTemplate>(_templates);

            try
            {
                var templateFiles = Directory.GetFiles(_templateDir, "template.xml", SearchOption.AllDirectories);
                Debug.Assert(templateFiles.Any());
                foreach (var file in templateFiles)
                {
                    var template = Serializer.FromFile<ProjectTemplate>(file);

                    template.IconPath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(file), "Icon.png"));
                    template.Icon = File.ReadAllBytes(template.IconPath);

                    template.PreviewPath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(file), "Screenshot.png"));
                    template.Preview = File.ReadAllBytes(template.PreviewPath);

                    template.File = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(file), template.File));

                    _templates.Add(template);
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions (e.g., log the error, show a message to the user)
                Console.WriteLine($"Error loading project templates: {ex.Message}");
                //TODO: Log error code
            }
        }
    }
}