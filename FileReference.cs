namespace VsSingleSolutionCreator
{
	public class FileReference
	{
		public string AssemblyName { get; private set; }
		public string Filepath { get; private set; }		
		public string Text { get; private set; }

		public FileReference(string assemblyName, string filepath, string text)
		{
			this.AssemblyName = assemblyName;
			this.Filepath = filepath;			
			this.Text = text;
		}
	}
}
