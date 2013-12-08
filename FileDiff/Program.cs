using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Ionic.Zip;

namespace FileDiff
{
    static class Program
    {
        private static bool _quietMode;
        private static string _logFilePath = "";

        private static void Main(string[] args)
        {
            try
            {
                if (args == null)
                    throw new ArgumentNullException("args");

                string firstDirectory;
                string secondDirectory;
                List<string> excludeDirectoryList;
                List<string> excludeFileList;
                bool showDiff;
                string copyDirectory;
                bool skipCopy;
                string zipName;
                bool enableLogging;
                getArgumentValues(
                    args,
                    out firstDirectory,
                    out secondDirectory,
                    out excludeDirectoryList,
                    out excludeFileList,
                    out showDiff,
                    out copyDirectory,
                    out skipCopy,
                    out zipName,
                    out _quietMode,
                    out enableLogging);

                if(enableLogging)
                    CreateLogFile(Directory.GetCurrentDirectory());

                //Get the first list of files
                var topDirectory = new DirectoryInfo(firstDirectory);
                var firstFileList = GetListOfFilesForDirectory(topDirectory, excludeDirectoryList, excludeFileList);
                WriteFilesToScreen(firstFileList);

                WriteToScreen("---------------------------");

                //Get the second list of files
                topDirectory = new DirectoryInfo(secondDirectory);
                var secondFileList = GetListOfFilesForDirectory(topDirectory, excludeDirectoryList, excludeFileList);
                WriteFilesToScreen(secondFileList);

                //Perform Comparison
                var fileDiffList = PerformFileCompare(firstFileList, secondFileList);

                WriteToScreen("---------------------------");

                var fileList = fileDiffList as IList<FileInformation> ?? fileDiffList.ToList();

                if (showDiff)
                {
                    if (fileList.All(f => f.CompareStatus == CompareStatus.Identical))
                        WriteToScreen("No differences between directories found.");

                    WriteFilesToScreen(fileList, true, false);
                                   
                }

                WriteToScreen("---------------------------");

                if (!skipCopy)
                {
                    bool filesCopied = CopyFilesWithChangesToDirectory(copyDirectory, fileList, zipName);
                    if (filesCopied)
                    {
                        WriteToScreen("---------------------------");
                        ZipCopiedFolder(copyDirectory, zipName);
                        Directory.SetCurrentDirectory(copyDirectory);
                        Directory.Delete(copyDirectory + "\\" + zipName, true);
                        WriteToScreen("Temporary folder deleted.");
                    }
                }
            }
            catch (Exception ex)
            {
                WriteToScreen(string.Format("{0}", ex.Message), true);
            }

            WriteToScreen("Exiting.");

        }

        private static void CreateLogFile(string currentDirectory)
        {
            _logFilePath = @"" + currentDirectory + "\\FileDiffLog - " + DateTime.Now.ToString(@"yyyy-MM-dd hh.mm.ss tt", new CultureInfo("en-US")) + ".txt";
            File.CreateText(_logFilePath).Close();
        }

        private static void WriteToScreen(string message, bool overrideSilentMode = false)
        {
            //If we have a log file
            if (!string.IsNullOrEmpty(_logFilePath))
            {
                using (var outfile = File.AppendText(_logFilePath))
                {
                    outfile.WriteLine(message);
                }
            }

            if(!_quietMode || overrideSilentMode)
                Console.WriteLine(message);
        }

        private static void ZipCopiedFolder(string copyDirectory, string zipName)
        {
            using (var zip = new ZipFile())
            {
                WriteToScreen("Zipping contents of temporary folder");
                zip.AddDirectory(copyDirectory + "\\" + zipName);
                zip.Comment = "This zip was created at " + DateTime.Now.ToString(CultureInfo.InvariantCulture);
                zip.Save(copyDirectory + "\\" + zipName + ".zip");
                WriteToScreen("Zip Created.");
            }
        }

        private static bool CopyFilesWithChangesToDirectory(string copyDirectory, IEnumerable<FileInformation> fileList, string zipName)
        {
            Directory.CreateDirectory(copyDirectory + "\\" + zipName);
            Directory.SetCurrentDirectory(copyDirectory + @"\\" + zipName);
            copyDirectory = Directory.GetCurrentDirectory();

            var hasCopiedFiles = false;
            foreach (var file in fileList.Where(f => f.CompareStatus == CompareStatus.Modified || f.CompareStatus == CompareStatus.Added).ToList())
            {
                //get the index of the last \\ which defines whether or not the file is in a sub directory
                var lastDirectoryIndex = file.FileName.LastIndexOf("\\", StringComparison.Ordinal);

                //Set the source and destination to the proper places
                var source = @"" + file.TopDirectory + file.FileName;
                var destination = @"" + copyDirectory + file.FileName;

                //If its in a directory, possibly create the directory
                if (lastDirectoryIndex >= 0)
                {
                    //Here we create the path if need be.
                    var path = file.FileName.Remove(lastDirectoryIndex);
                    if (!Directory.Exists(@"" + copyDirectory + path))
                    {
                        Directory.CreateDirectory(@"" + copyDirectory + path);
                    }                    
                }
                else
                    destination = @"" + copyDirectory;    
                
                
                File.Copy(source, destination, true);
                hasCopiedFiles = true;
                WriteToScreen(string.Format("{0} copied", source));
            }

            if(!hasCopiedFiles)
                WriteToScreen("No files were copied");

            return hasCopiedFiles;
        }

        private static void getArgumentValues(
            string[] args,
            out string firstDirectory,
            out string secondDirectory,
            out List<string> excludeDirectoryList,
            out List<string> excludeFileList,
            out bool showDiff,
            out string copyDirectory,
            out bool skipCopy,
            out string zipName,
            out bool enableQuietMode,
            out bool enableLogging)
        {
            if (args.Count(s => s.StartsWith("-s:")) != 1)
                throw new Exception("Must provide a single Source Directory with argument -s:");
            firstDirectory = @"" + args.FirstOrDefault(a => a.StartsWith("-s:"));
            firstDirectory = firstDirectory.Replace("-s:","");

            if (args.Count(s => s.StartsWith("-t:")) != 1)
                throw new Exception("Must provide a single Target Directory with argument -t:");
            secondDirectory = @"" + args.FirstOrDefault(a => a.StartsWith("-t:"));
            secondDirectory = secondDirectory.Replace("-t:", "");
            
            excludeDirectoryList = new List<string>();
            var directoryString = args.FirstOrDefault(a => a.StartsWith("-d:"));
            if (!string.IsNullOrEmpty(directoryString))
                excludeDirectoryList = directoryString.Replace("-d:", "").Split('*').ToList();
            if (args.Count(a => a.StartsWith("-d:")) > 1)
                throw new Exception("To provide a list of directories to skip, provide a single argument -d: with directories separated by *");

            excludeFileList = new List<string>();
            var fileString = args.FirstOrDefault(a => a.StartsWith("-f:"));
            if (!string.IsNullOrEmpty(fileString))
                excludeFileList = fileString.Replace("-f:", "").Split('*').ToList();
            if (args.Count(a => a.StartsWith("-f:")) > 1)
                throw new Exception("To provide a list of files to skip, provide a single argument -f: separated by *");

            showDiff = !args.Any(s => s.Equals("-h"));

            copyDirectory = "" + args.FirstOrDefault(a => a.StartsWith("-cd:"));
            copyDirectory = copyDirectory.Replace("-cd:", "");
            if (args.Count(a => a.StartsWith("-cd:")) > 1)
                throw new Exception("Only one Copy Directory argument \"-cd:\" may be specified");
            if (string.IsNullOrEmpty(copyDirectory))
                copyDirectory = Directory.GetCurrentDirectory();

            skipCopy = args.Any(s => s.Equals("-sc"));

            zipName = "" + args.FirstOrDefault(a => a.StartsWith("-z:"));
            zipName = zipName.Replace("-z:", "");
            if(args.Count(a => a.StartsWith("-z:")) > 1)
                throw new Exception("Only one Zip Name argument \"-z:\" may be specified");
            if (string.IsNullOrEmpty(zipName))
                zipName = "Diff";

            enableLogging = args.Any(s => s.Equals("-l"));

            enableQuietMode = args.Any(s => s.Equals("-q"));
        }

        private static IEnumerable<FileInformation> PerformFileCompare(List<FileInformation> firstFileList, List<FileInformation> secondFileList)
        {
            var fileDiffList = new List<FileInformation>();

            foreach (var file in firstFileList.ToList())
            {
                //If found in first, but not in second it was removed.
                if (secondFileList.All(f => f.FileName != file.FileName))
                {
                    file.CompareStatus = CompareStatus.Removed;
                    fileDiffList.Add(file);
                }
                    //Identical or Modified
                else
                {
                    var matchFile = secondFileList.FirstOrDefault(f => f.FileName == file.FileName);

                    if (matchFile != null)
                    {
                        if (matchFile.FileSize == file.FileSize && matchFile.LastWriteTo == file.LastWriteTo)
                        {
                            matchFile.CompareStatus = CompareStatus.Identical;
                        }
                        else
                        {
                            matchFile.CompareStatus = CompareStatus.Modified;
                        }
                        //Add to the diff list and remove from the second
                        fileDiffList.Add(matchFile);
                        secondFileList.Remove(matchFile);
                    }
                }

                //Remove From First List
                firstFileList.Remove(file);
            }

            //Any left, they have been added
            foreach (var file in secondFileList)
            {
                file.CompareStatus = CompareStatus.Added;
                fileDiffList.Add(file);
            }

            return fileDiffList;
        }

        private static void WriteFilesToScreen(IEnumerable<FileInformation> fileDiffList, bool showDiffColor = false, bool showIdentical = true)
        {
            foreach (var file in fileDiffList)
            {
                if(showDiffColor)
                {                    
                    if (file.CompareStatus == CompareStatus.Added)
                        Console.ForegroundColor = ConsoleColor.Green;
                    if (file.CompareStatus == CompareStatus.Removed)
                        Console.ForegroundColor = ConsoleColor.Red;
                    if (file.CompareStatus == CompareStatus.Modified)
                        Console.ForegroundColor = ConsoleColor.Yellow;
                }

                if(showIdentical || file.CompareStatus != CompareStatus.Identical)
                    WriteToScreen(string.Format("{0}{1} - {2} - {3}", file.TopDirectory, file.FileName, file.LastWriteTo, file.FileSize));

                Console.ResetColor();
            }
        }

        private static List<FileInformation> GetListOfFilesForDirectory(DirectoryInfo topDirectory, IEnumerable<string> excludeDirectoryList, IEnumerable<string> excludeFileList)
        {
            //Get Top Directory Files
            var fileList = topDirectory.EnumerateFiles().Where(f => excludeFileList.All(s => s != f.Name)).Select(enumeratedFile => new FileInformation(enumeratedFile, topDirectory.FullName)).ToList();

            //Get all sub-directory files
            fileList.AddRange(from di in topDirectory.EnumerateDirectories("*").Where(d => excludeDirectoryList.All(s => s != d.Name)) from fi in di.EnumerateFiles("*", SearchOption.AllDirectories).Where(f => excludeFileList.All(s => s != f.Name)).ToList() select new FileInformation(fi, topDirectory.FullName));

            return fileList;
        }
    }

    class FileInformation
    {
        public string TopDirectory { get; private set; }
        public string FileName { get; private set; }
        public DateTime LastWriteTo { get; private set; }
        public long FileSize { get; private set; }       
        public CompareStatus CompareStatus { get; set; }

        public FileInformation(FileInfo file, string topDirectory)
        {
            TopDirectory = topDirectory;
            FileName = file.FullName.Replace(TopDirectory, "");
            LastWriteTo = file.LastWriteTimeUtc;
            FileSize = file.Length;
        }


    }

    public enum CompareStatus
    {
        Identical = 1,
        Removed = 2,
        Added = 3,
        Modified = 4
    }
}



