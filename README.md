# Visual Studio Single Solution Creator

Given a root directory, this program will rewrite all projects to reference each other as project references (instead of file references) and put them into a single solution. This program's narrow purpose is to consolidate large source tree projects that may be referencing each other by file, and make all the code visible in a single solution for easy refactoring across many projects.

## Basic Usage

This should be ran out of your Visual Studio IDE; if you want to change the arguments it can be done by reassigning the top variable values.

```C#
    // customize the behavior of the program here
    Project.RootFilepath = @"e:\code\trunk";
    bool writeFiles = false;            // Set to false if you want to debug and test run.
    bool writeFilesAsModified = true;   // Set to true if you want new project files to be written outside of SCC.
    var exemptDirectories = new string[] { "ExemptProjectDirectory" };
```
	
This program will only be ran once for most users. If you have a need to make this a command line tool, please let me know and I can update it.
