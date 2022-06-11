using Blish_HUD.Content;
using Charr.Timers_BlishHUD.Pathing.Content;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace Charr.Timers_BlishHUD.IO
{
    public class TimerStream
    {
        public Stream Stream { get; set; }
        public PathableResourceManager ResourceManager { get; set; }

        public string FileName { get; set; }
        public bool IsFromZip { get; set; }
        public string ZipFile { get; set; }

        public TimerStream(Stream stream, PathableResourceManager resourceManager, string fileName, bool isFromZip = false,
            string zipFile = "") {
            Stream = stream;
            ResourceManager = resourceManager;
            FileName = fileName;
            IsFromZip = isFromZip;
            ZipFile = zipFile;
        }

    }

    public class TimerLoader : IDisposable
    {
        private HashSet<String> _normalTimerFiles;
        private Dictionary<string, HashSet<ZipArchiveEntry>> _zipTimerFileEntries;

        private DirectoryReader _directoryReader;
        private readonly Dictionary<string, SortedZipArchiveReader> _zipDataReaders;

        private PathableResourceManager _directoryResourceManager;
        private Dictionary<string, PathableResourceManager> _zipResourceManagers;

        public string TimerTimerDirectory { get; set; }

        public TimerLoader(string timerDirectory) {
            TimerTimerDirectory = timerDirectory;

            _normalTimerFiles = new HashSet<string>();
            _directoryReader = new DirectoryReader(timerDirectory);
            _directoryResourceManager = new PathableResourceManager(_directoryReader);

            _zipTimerFileEntries = new Dictionary<string, HashSet<ZipArchiveEntry>>();
            _zipDataReaders = new Dictionary<string, SortedZipArchiveReader>();
            _zipResourceManagers = new Dictionary<string, PathableResourceManager>();
        }

        public void LoadFiles(Action<TimerStream> loadFileFunc) {
            _normalTimerFiles.UnionWith(Directory.GetFiles(TimerTimerDirectory, "*.bhtimer", SearchOption.AllDirectories));
            foreach (var file in _normalTimerFiles) {
                loadFileFunc(new TimerStream(_directoryReader.GetFileStream(file), _directoryResourceManager, file));
            }

            foreach (var zipFile in Directory.GetFiles(TimerTimerDirectory, "*.zip", SearchOption.AllDirectories)) {
                if (!_zipDataReaders.TryGetValue(zipFile, out SortedZipArchiveReader zipDataReader)) {
                    zipDataReader = new SortedZipArchiveReader(zipFile);
                    _zipDataReaders.Add(zipFile, zipDataReader);
                    _zipResourceManagers.Add(zipFile, new PathableResourceManager(zipDataReader));
                    _zipTimerFileEntries.Add(zipFile, new HashSet<ZipArchiveEntry>());
                }

                _zipTimerFileEntries[zipFile].UnionWith(zipDataReader.GetValidFileEntries(".bhtimer"));

                foreach (var entry in _zipTimerFileEntries[zipFile]) {
                    loadFileFunc(new TimerStream(zipDataReader.GetFileStream(entry.Name), _zipResourceManagers[zipFile], entry.Name, true, zipFile));
                }
            }
        }

        public void ReloadFile(Action<TimerStream> loadFileFunc, string timerFileName) {
            if (!_normalTimerFiles.Contains(timerFileName)) {
                return;
            }
            loadFileFunc.Invoke(new TimerStream(_directoryReader.GetFileStream(timerFileName), _directoryResourceManager, timerFileName));
        }

        public void ReloadFile(Action<TimerStream> loadFileFunc, string zipFile, string timerFileName) {
            if (!_zipTimerFileEntries.ContainsKey(zipFile)) {
                return;
            }

            var zipDataReader = _zipDataReaders[zipFile];
            loadFileFunc.Invoke(new TimerStream(zipDataReader.GetFileStream(timerFileName), _zipResourceManagers[zipFile], timerFileName, true, zipFile));
        }

        public void Dispose() {
            _directoryResourceManager?.Dispose();
            foreach (var reader in _zipResourceManagers.Values) {
                reader.Dispose();
            }
        }
    }
}
