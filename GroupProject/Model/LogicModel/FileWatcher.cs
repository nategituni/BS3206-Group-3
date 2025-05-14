using System;
using System.IO;

namespace GroupProject.Model.LogicModel
{
    public class FileWatcher
    {
        private FileSystemWatcher _watcher;
        private static string _lastWrittenFile;
        private static DateTime _lastWrittenTime;


        public void StartWatching(string folderPath)
        {
            _watcher = new FileSystemWatcher(folderPath)
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName,
                Filter = "*.xml",
                EnableRaisingEvents = true
            };

            _watcher.Changed += OnChanged;
            _watcher.Created += OnChanged;
        }

        private void OnChanged(object sender, FileSystemEventArgs e)
        {
            var filePath = e.FullPath;

            if (filePath == _lastWrittenFile && 
                File.Exists(filePath) &&
                File.GetLastWriteTime(filePath) == _lastWrittenTime)
            {
                Console.WriteLine("Change detected, but it was just written by the validator. Ignoring.");
                return;
            }

            Console.WriteLine($"Detected change: {filePath}");
            Validator.RunValidation(filePath, this);
        }


        public void MarkFileAsWritten(string filePath)
        {
            _lastWrittenFile = filePath;
            _lastWrittenTime = File.GetLastWriteTime(filePath);
        }
    }
}