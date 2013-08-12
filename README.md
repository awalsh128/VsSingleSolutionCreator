# Visual Studio Single Solution Creator

Given a root directory, this program will rewrite all projects to reference each other as project references (instead of file references) and put them into a single solution. This program's narrow purpose is to consolidate large source tree projects that may be referencing each other by file, and make all the code visible in a single solution for easy refactoring across many projects.

## Basic Usage

Get help:

	> VsSingleSolutionCreator.exe --help
	VsSingleSolutionCreator 1.0.0.0
	Copyright Andrew Walsh 2013

	  -r, --root                    Required. Root path of code base

	  -w, --writeFiles              If set files will be modified, otherwise it
	                                will just be a test run

	  -m, --writeFilesAsModified    If set new project files will be generated in
	                                parallel leaving the original files unmodified

	  -e, --exemptDirectories       Directories to exclude from processing

	  --help                        Display this help screen.

Create single solution:

	> VsSingleSolutionCreator.exe -r "C:\Projects" -w