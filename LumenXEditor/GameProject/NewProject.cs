using LumenXEditor.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace LumenXEditor.GameProject
{
	[DataContract]
	public class ProjectTemplate
	{
		[DataMember]
		public string ProjectType { get; set; }
		[DataMember]
		public string ProjectFile { get; set; }
		[DataMember]
		public List<string> Folders { get; set; }
		
		public byte[] Icon { get; set; }
		public byte[] Screenshot { get; set; }
		public string IconFilePath { get; set; }
		public string ScreenshotFilePath { get; set; }
		public string ProjectFilePath { get; set; }
	}

	internal class NewProject : ViewModelBase
	{
		// TODO: get the path from the instalation location
		private readonly string _templatePath = @"..\..\ProjectTemplates";
		private string _projectProjectName = "NewLumenXProject";
		public string ProjectName
		{
			get => _projectProjectName;
			set
			{
				if (_projectProjectName != value)
				{
					_projectProjectName = value;
					ValidateProjectPath();
					OnPropertyChanged(nameof(ProjectName));
				}
			}
		}

		private string _projectPath = $@"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}\LumenXProject\";
		public string ProjectPath
		{
			get => _projectPath;
			set
			{
				if (_projectPath != value)
				{
					_projectPath = value;
					ValidateProjectPath();
					OnPropertyChanged(nameof(Path));
				}
			}
		}

		private bool _isValid;
		public bool IsValid
		{
			get => _isValid;
			set
			{
				if (_isValid != value)
				{
					_isValid = value;
					OnPropertyChanged(nameof(IsValid));
				}
			}
		}

		private string _errorMsg;

		public string ErrorMsg
		{
			get => _errorMsg;
			set
			{
				if (_errorMsg != value)
				{
					_errorMsg = value;
					OnPropertyChanged(nameof(ErrorMsg));
				}
			}
		}

		private ObservableCollection<ProjectTemplate> _projectTemplates = new ObservableCollection<ProjectTemplate>();
		public ReadOnlyObservableCollection<ProjectTemplate> ProjectTemplates
		{
			get;
		}

		private bool ValidateProjectPath()
		{
			var path = ProjectPath;
			if (!Path.EndsInDirectorySeparator(path)) path += @"\";
			path += $@"{ProjectName}\";

			IsValid = false;
			if (string.IsNullOrWhiteSpace(ProjectName.Trim()))
			{
				ErrorMsg = "Project name cannot be empty";
			}
			else if (ProjectName.IndexOfAny(Path.GetInvalidFileNameChars()) != -1)
			{
				ErrorMsg = "Project name cannot contain invalid characters";
			}
			else if (string.IsNullOrWhiteSpace((ProjectPath.Trim())))
			{
				ErrorMsg = "Project path cannot be empty";
			}
			else if (ProjectPath.IndexOfAny(Path.GetInvalidPathChars()) != -1)
			{
				ErrorMsg = "Project path cannot contain invalid characters";
			}
			else if (Path.Exists(path) && Directory.EnumerateFileSystemEntries(path).Any())
			{
				ErrorMsg = "Selected project folder already exists and is not empty";
			}
			else
			{
				ErrorMsg = string.Empty;
				IsValid = true;
			}

			return IsValid;
		}

		public NewProject()
		{
			ProjectTemplates = new ReadOnlyObservableCollection<ProjectTemplate>(_projectTemplates);
			try
			{
				var templatesFiles = Directory.GetFiles(_templatePath, "template.xml", SearchOption.AllDirectories);
				Debug.Assert(templatesFiles.Any());
				foreach (var file in templatesFiles)
				{
					var template = Serializer.FromFile<ProjectTemplate>(file);
					template.IconFilePath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(file), "Icon.png"));
					template.Icon = File.ReadAllBytes(template.IconFilePath);
					template.ScreenshotFilePath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(file), "Screenshot.png"));
					template.Screenshot = File.ReadAllBytes(template.ScreenshotFilePath);
					template.ProjectFilePath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(file), template.ProjectFile));
					
					_projectTemplates.Add(template);
				}

				ValidateProjectPath();
			}
			catch (Exception ex)
			{
				// Log the exception or show a message to the user
				Debug.WriteLine($"Error loading project templates: {ex.Message}");
			}
		}
	}
}
