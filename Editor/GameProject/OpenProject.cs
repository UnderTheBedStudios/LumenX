using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.Serialization;
using LumenX.Utils;

namespace LumenX.GameProject
{
    [DataContract]
    public class ProjectData
    {
        [DataMember]
        public string ProjectName { get; set; }
        [DataMember]
        public string ProjectPath { get; set; }
        [DataMember]
        public DateTime Date { get; set; }
        public string FullPath { get => $"{ProjectPath}{ProjectName}/{ProjectName}{Project.Extension}"; }
        public byte[] Icon { get; set; }
        public byte[] Screenshot { get; set; }
    }
    [DataContract]
    public class ProjectDataList
    {
        [DataMember]
        public required List<ProjectData> Projects { get; set; }
    }
    class OpenProject
    {
        private static readonly string _appDataPath = $@"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}/LumenX/";
        private static readonly string _projectDataPath;
        private static readonly ObservableCollection<ProjectData> _projects = new ObservableCollection<ProjectData>();
        private static readonly ReadOnlyObservableCollection<ProjectData> _readOnlyProjects = new ReadOnlyObservableCollection<ProjectData>(_projects);
        public ReadOnlyObservableCollection<ProjectData> Projects => _readOnlyProjects;

        private static void ReadProjectData()
        {
            if (File.Exists(_projectDataPath))
            {
                var data = Serializer.FromFile<ProjectDataList>(_projectDataPath);
                if (data == null) return;

                var projects = data.Projects.OrderByDescending(x=>x.Date);
                _projects.Clear();
                foreach (var project in projects)
                {
                    if (File.Exists(project.FullPath))
                    {
                        try
                        {
                            var projectDir = $@"{project.ProjectPath}{project.ProjectName}/";
                            project.Icon = File.ReadAllBytes($@"{projectDir}.LumenX/Icon.png");
                            project.Screenshot = File.ReadAllBytes($@"{projectDir}.LumenX/Screenshot.png");
                            _projects.Add(project);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Failed to load project '{project.ProjectName}', Error: {ex.Message}");
                        }
                    }
                }
            }
        }

        private static void WriteProjectData()
        {
            var projects = _projects.OrderBy(x=>x.Date).ToList();
            Serializer.ToFile(new ProjectDataList() { Projects = projects }, _projectDataPath);
        }

        public static Project Open(ProjectData projectData)
        {
            ReadProjectData();
            var project = _projects.FirstOrDefault(x=>x.FullPath == projectData.FullPath);
            if (project != null)
            {
                project.Date = DateTime.Now;
            }
            else
            {
                project = projectData;
                project.Date = DateTime.Now;
                _projects.Add(project);
            }
            WriteProjectData();

            Console.WriteLine($"[DEBUG] Active Project: {project.ProjectName}");
            Console.WriteLine($"[DEBUG] Full Path: {project.FullPath}");
            var loaded = Project.Load(project.FullPath);
            Console.WriteLine($"[DEBUG] LightDir after load: {loaded.ActiveWorld?.LightDir}");
            return loaded;
        }

        static OpenProject()
        {
            try
            {
                if (!Directory.Exists(_appDataPath)) Directory.CreateDirectory(_appDataPath);
                _projectDataPath = $@"{_appDataPath}ProjectData.xml";
                ReadProjectData();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                //TODO: Log errors
            }
        }
    }
}