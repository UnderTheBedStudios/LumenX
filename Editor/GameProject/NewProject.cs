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
                    ProjectPathValidation();
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
                    ProjectPathValidation();
                    OnPropertyChanged(nameof(ProjectPath));
                }
            }
        }

        private ObservableCollection<ProjectTemplate> _templates = new ObservableCollection<ProjectTemplate>();
        public ReadOnlyObservableCollection<ProjectTemplate> Templates { get ;}

        private bool ProjectPathValidation()
        {
            var path = ProjectPath;

            if (!Path.EndsInDirectorySeparator(path)) path += @"\";
            path += $@"{ProjectName}\";

            ValidProj = false;

            if (string.IsNullOrWhiteSpace(ProjectName.Trim()))
                ErrorCode = "Project name cannot be empty.";
            else if (ProjectName.IndexOfAny(Path.GetInvalidFileNameChars()) != -1)
                ErrorCode = "Project name contains invalid characters.";
            else if (string.IsNullOrWhiteSpace(ProjectPath.Trim()))
                ErrorCode = "Project path cannot be empty.";
            else if (ProjectPath.IndexOfAny(Path.GetInvalidPathChars()) != -1)
                ErrorCode = "Project path contains invalid characters.";
            else if (Directory.Exists(path) && Directory.EnumerateFileSystemEntries(path).Any())
                ErrorCode = "A non-empty directory with the same name already exists.";
            else
            {
                ErrorCode = string.Empty;
                ValidProj = true;
            }

            return ValidProj;
        }

        private bool _validProj;
        public bool ValidProj
        {
            get => _validProj;
            set
            {
                if (_validProj != value)
                {
                    _validProj = value;
                    OnPropertyChanged(nameof(ValidProj));
                }
            }
        }

        private string _errorCode;
        public string ErrorCode
        {
            get => _errorCode;
            set
            {
                if (_errorCode != value)
                {
                    _errorCode = value;
                    OnPropertyChanged(nameof(ErrorCode));
                }
            }
        }   

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

                ProjectPathValidation();
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