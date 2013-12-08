FileDiff: A simple C# command line app to compare two directories and zip the difference to a file.

- Arguments -
-s:   Source Directory
-t:   Target Directory
-d:   skip Directories - list of directories to skip, each separated by * NOTE: you cannot have paths on skip directories, such as /a/b
-f:   skip Files - list of files to skip, each separated by * NOTE: you cannot have paths on skip files, such as a/b.txt
-cd:  Copy Directory - If copying the differences, this will be the folder to copy and zip to. 
-z:   Zip name - Changes the name of the output zip file, if not specified default is "Diff"
-h    Hide diff - hides the final output of differences between the two directories
-sc   Skip Copy - skips the copying of the files that are different. NOTE: This will also not create a zip file of the deltas between the directories.
-q    Quiet mode - hides out output to the screen, will still print errors
-l    Logging - If set, creates a log file at the current directory entitled "FileDiffLog - $currentDate $currentTime"  NOTE: This is not affected by quiet mode

- To Run -
Either unzip Release.zip or FileDiff.sln run through visual studio

When running through command line, FileDiff.exe and Ionic.Zip.dll need to be in the same folder

- Notes - 
This package contains the Assembly for DotNetZip, more info can be found here https://dotnetzip.codeplex.com/

Created By: Erik Hendrickson, erik33045@gmail.com