using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace VsSingleSolutionCreator
{
	class Program
	{
		static void Main(string[] args)
		{
			// customize the behavior of the program here
			Project.RootFilepath = @"e:\code\trunk";
			bool writeFiles = false;					// Set to false if you want to debug and test run.
			bool writeFilesAsModified = true;		// Set to true if you want new project files to be written outside of SCC.
			var exemptDirectories = new string[] { "ExemptProject" };

			Console.WriteLine("Getting projects in " + Project.RootFilepath);
			// get all projects as name to project map			
			var assemblyNameToProject = GetProjects(Project.RootFilepath, exemptDirectories);

			Console.WriteLine("Mapping assemblies to projects that reference them.");
			// get assembly name to referencing projects map if needed
			// eg. assemblyNameFileReferencesToProjects["Xignite.Library.Core"] -> all projects that reference that assembly
			var assemblyNameFileReferencesToProjects = new Dictionary<string, List<Project>>();

			Console.WriteLine("Getting file references for each project.");
			// for each project get its file references and hang it on project
			foreach (var project in assemblyNameToProject.Values)
			{
				project.FileReferences = GetFileReferences(project, assemblyNameToProject);
				AddReferencingProjects(project, assemblyNameFileReferencesToProjects);
			}			

			if (writeFiles)
			{
				var solutionFilepath = Path.Combine(Project.RootFilepath, "Xignite.sln");
				if (writeFilesAsModified)
				{
					Console.WriteLine("Writing new modified project files and their new project references.");
					WriteModifiedProjectReferences(assemblyNameToProject);
					Console.WriteLine("Writing new solution file to " + solutionFilepath);
					WriteModifiedSolutionText(solutionFilepath, assemblyNameToProject);
				}
				else
				{
					// for each project, for each file reference, replace with project reference from map
					Console.WriteLine("Rewriting project files and their new project references.");
					RewriteProjectReferences(assemblyNameToProject);
					Console.WriteLine("Writing new solution file to " + solutionFilepath);
					WriteSolutionText(solutionFilepath, assemblyNameToProject);
				}
				Console.WriteLine(String.Format("{0} projects added to {1}.", assemblyNameToProject.Values.Count, solutionFilepath));
			}			

			Console.WriteLine("\nFinished, press any key to exit...");
			Console.ReadKey();
		}

		static void AddReferencingProjects(Project project, Dictionary<string, List<Project>> assemblyNameFileReferencesToProjects)
		{
			foreach (var fileReference in project.FileReferences)
			{
				List<Project> projects;
				if (!assemblyNameFileReferencesToProjects.ContainsKey(fileReference.AssemblyName))
				{
					projects = new List<Project>();
					assemblyNameFileReferencesToProjects.Add(fileReference.AssemblyName, projects);
				}
				else
				{
					projects = assemblyNameFileReferencesToProjects[fileReference.AssemblyName];
				}

				if (!projects.Contains(project))
				{
					projects.Add(project);
				}
			}
		}

		static List<FileReference> GetFileReferences(Project project, Dictionary<string, Project> assemblyNameToProject)
		{
			var results = new List<FileReference>();
			using (var reader = XmlReader.Create(project.FilePath))
			{
				reader.MoveToContent();
				while (reader.Read())
				{
					if (reader.NodeType == XmlNodeType.Element)
					{
						if (reader.Name == "Reference")
						{
							string includeText = reader.GetAttribute("Include");
							string xmlText = reader.ReadOuterXml().Replace("\n", "\r\n").Replace(" xmlns=\"http://schemas.microsoft.com/developer/msbuild/2003\"", "");
							if (includeText != null)
							{
								var assemblyName = includeText.Split(new char[] { ',' }).First();
								if (assemblyNameToProject.ContainsKey(assemblyName))
								{
									results.Add(new FileReference(assemblyName, project.FilePath, xmlText));
								}
							}
						}
					}
				}
			}
			return results;
		}		

		static IEnumerable<string> GetProjectFiles(string path, IEnumerable<string> exemptDirectories)
		{
			return GetProjectFiles(path, exemptDirectories, "*.??proj");
		}

		static IEnumerable<string> GetProjectFiles(string path, IEnumerable<string> exemptDirectories, string filter)
		{
			var modifiedFilePattern = Project.ModifiedSuffix + ".";
			foreach (string directoryPath in Directory.GetDirectories(path))
			{
				if (exemptDirectories != null && exemptDirectories.Contains((new DirectoryInfo(directoryPath)).Name)) continue;

				foreach (string filepath in Directory.GetFiles(directoryPath, filter))
				{
					if ((!filepath.EndsWith("csproj") && !filepath.EndsWith("vbproj")) || filepath.Contains(modifiedFilePattern)) continue;
					yield return filepath;
				}
				foreach (var filepath in GetProjectFiles(directoryPath, exemptDirectories, filter))
				{
					if ((!filepath.EndsWith("csproj") && !filepath.EndsWith("vbproj")) || filepath.Contains(modifiedFilePattern)) continue;
					yield return filepath;
				}
			}
		}

		static Dictionary<string, Project> GetProjects(string path, IEnumerable<string> exemptDirectories)
		{
			var result = new Dictionary<string, Project>();
			foreach (var projectFilepath in GetProjectFiles(path, exemptDirectories))
			{				
				var project = new Project(projectFilepath);
				if (!result.ContainsKey(project.AssemblyName))
				{
					result.Add(project.AssemblyName, project);
				}
			}

			return result;
		}

		static void RewriteProjectReferences(Dictionary<string, Project> assemblyNameToProject)
		{		
			foreach (var project in assemblyNameToProject.Values)
			{
				if (project.FileReferences.Count > 0)
				{
					string text = File.ReadAllText(project.FilePath);
					foreach (var reference in project.FileReferences)
					{
						text = text.Replace(reference.Text, assemblyNameToProject[reference.AssemblyName].GetProjectReferenceText(project.FilePath));
					}
					File.WriteAllText(project.FilePath, text);
				}
			}
		}

		static void WriteModifiedProjectReferences(Dictionary<string, Project> assemblyNameToProject)
		{
			foreach (var project in assemblyNameToProject.Values)
			{
				if (project.FileReferences.Count > 0)
				{
					string text = File.ReadAllText(project.FilePath);
					foreach (var reference in project.FileReferences)
					{
						text = text.Replace(reference.Text, assemblyNameToProject[reference.AssemblyName].GetModifiedProjectReferenceText(project.FilePath));
					}
					File.WriteAllText(project.ModifiedFilePath, text);
				}
				else
				{
					File.Copy(project.FilePath, project.ModifiedFilePath, true);
				}
			}
		}

		static void WriteModifiedSolutionText(string filepath, Dictionary<string, Project> nameToProject)
		{
			WriteSolutionText(filepath, nameToProject, p => p.ModifiedFileBasename, p => p.ModifiedFilePath);
		}

		static void WriteSolutionText(string filepath, Dictionary<string, Project> nameToProject)
		{
			WriteSolutionText(filepath, nameToProject, p => p.FileBasename, p => p.FilePath);
		}

		static void WriteSolutionText(string filepath, Dictionary<string, Project> nameToProject, Func<Project, string> getFileBasename, Func<Project, string> getFilepath)
		{
			var solutionFoldersToGuids = new Dictionary<string, string>();
			var solutionBuilder = new StringBuilder();

			solutionBuilder.AppendLine("Microsoft Visual Studio Solution File, Format Version 12.00\n# Visual Studio 2012");

			// emit projects
			foreach (var project in nameToProject.Values)
			{				
				if (!solutionFoldersToGuids.ContainsKey(project.SubRootDirectory))
				{
					solutionFoldersToGuids.Add(project.SubRootDirectory, "{" + Guid.NewGuid().ToString() + "}");
				}
				solutionBuilder.AppendLine(String.Format("Project(\"{0}\") = \"{1}\", \"{2}\", \"{3}\"\nEndProject",
					project.GetPreferedTypeGuidText(), getFileBasename(project), Project.GetRelativePath(getFilepath(project), filepath), project.GuidText));
			}

			// emit solution folders
			foreach (var pair in solutionFoldersToGuids)
			{
				solutionBuilder.AppendLine(String.Format("Project(\"{{2150E333-8FDC-42A3-9474-1A3956D46DE8}}\") = \"{0}\", \"{0}\", \"{1}\"\nEndProject", pair.Key, pair.Value));
			}

			// emit project to solution folder mappings
			solutionBuilder.AppendLine("Global\n\tGlobalSection(NestedProjects) = preSolution\n");
			foreach (var project in nameToProject.Values)
			{
				// project GUID to solution folder GUID
				solutionBuilder.AppendLine(String.Format("\t\t{0} = {1}", project.GuidText, solutionFoldersToGuids[project.SubRootDirectory]));					
			}
			solutionBuilder.AppendLine("\tEndGlobalSection\nEndGlobal");

			File.WriteAllText(filepath, solutionBuilder.ToString());
		}
	}
}