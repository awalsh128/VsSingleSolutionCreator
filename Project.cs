using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

namespace VsSingleSolutionCreator
{
	public class Project
	{
		private const string cProjectTypeGuid = "{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}";
		private const string vbProjectTypeGuid = "{F184B08F-C81C-45F6-A57F-5ABD9991F28F}";
		private const string referenceFormat = "<ProjectReference Include=\"{0}\">\n      <Project>{1}</Project>\n      <Name>{2}</Name>\n    </ProjectReference>";

		public const string ModifiedSuffix = "_Mod";

		public static string RootFilepath { get; set; }

		public string AssemblyName { get; private set; }
		public string FileBasename { get; private set; }
		public string FilePath { get; set; }
		public List<FileReference> FileReferences { get; set; }
		public string GuidText { get; private set; }
		public string ModifiedFilePath { get; private set; }
		public string ModifiedFileBasename { get; private set; }
		public string SubRootDirectory { get; private set; }
		public List<string> TypeGuidText { get; private set; }

		public Project(string filepath)
		{				
			this.FileBasename = Path.GetFileNameWithoutExtension(filepath);
			this.FileReferences = new List<FileReference>();
			this.FilePath = filepath;
			this.SetModifiedNames(filepath);
			this.SubRootDirectory = filepath.Replace(Project.RootFilepath, "").Split(new char[] { '\\' }, StringSplitOptions.RemoveEmptyEntries).First();

			List<string> typeGuidText = null;

			using (var reader = XmlReader.Create(filepath))
			{
				reader.MoveToContent();
				while (reader.Read() && (this.AssemblyName == null || this.FileBasename == null || typeGuidText == null))
				{
					if (reader.NodeType == XmlNodeType.Element)
					{
						if (reader.Name == "ProjectGuid")
						{
							reader.Read();	// move cursor to inner text
							this.GuidText = reader.Value;
						}
						else if (reader.Name == "AssemblyName")
						{
							reader.Read();
							this.AssemblyName = reader.Value;
						}
						else if (reader.Name == "ProjectTypeGuids")
						{
							reader.Read();
							typeGuidText = reader.Value.ToUpper().Split(new char[] { ';' }).ToList();
						}
					}
				}
			}
															
			if (typeGuidText == null)
			{
				if (filepath.EndsWith("vbproj"))
				{
					this.TypeGuidText = new List<string> { vbProjectTypeGuid };
				}
				else // assume C# project
				{
					this.TypeGuidText = new List<string> { cProjectTypeGuid };
				}
			}
			else
			{
				this.TypeGuidText = typeGuidText;
			}
		}		

		public override int GetHashCode()
		{
			return this.GuidText.GetHashCode();
		}		

		public string GetModifiedProjectReferenceText(string relativeToPath)
		{
			var filepath = GetRelativePath(this.ModifiedFilePath, relativeToPath);
			return String.Format(referenceFormat, filepath, this.GuidText, this.FileBasename);
		}

		public string GetPreferedTypeGuidText()
		{
			if (this.TypeGuidText.Count > 0)
			{
				if (this.TypeGuidText.Contains(cProjectTypeGuid))
				{
					return cProjectTypeGuid;
				}
				else if (this.TypeGuidText.Contains(vbProjectTypeGuid))
				{
					return vbProjectTypeGuid;
				}
				else
				{
					return this.TypeGuidText.First();
				}
			}
			else
			{
				return this.TypeGuidText.First();
			}
		}

		public string GetProjectReferenceText(string relativeToPath)
		{
			var filepath = GetRelativePath(this.FilePath, relativeToPath);
			return String.Format(referenceFormat, filepath, this.GuidText, this.FileBasename);
		}

		public static string GetRelativePath(string absolutePath, string relativeToPath)
		{			
			var absoluteDirectories = absolutePath.Split(new char[] { '\\' });
			var relativeToDirectories = relativeToPath.Split(new char[] { '\\' });

			int i = 0;
			while (i < absoluteDirectories.Length && absoluteDirectories[i] == relativeToDirectories[i]) i++;

			int escapeCount = relativeToDirectories.Length - i - 1;	// Ignore # of matching directories and file itself.

			var relativeDirectories = new List<string>();
			for (int j = 0; j < escapeCount; j++) relativeDirectories.Add("..");
			relativeDirectories.AddRange(absoluteDirectories.Skip(i));

			return Path.Combine(relativeDirectories.ToArray());
		}

		private void SetModifiedNames(string filePath)
		{
			var file = new FileInfo(filePath);
			var basename = Path.GetFileNameWithoutExtension(file.Name);
			if (basename.EndsWith("."))
			{
				basename = basename.Substring(0, basename.Length - 1);	// remove dot
			}
			this.ModifiedFileBasename = basename + ModifiedSuffix + file.Extension;
			this.ModifiedFilePath = Path.Combine(file.DirectoryName, this.ModifiedFileBasename).ToString();
		}

		public override string ToString()
		{
			return this.FilePath;
		}
	}
}