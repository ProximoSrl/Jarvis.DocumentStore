using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using System.IO;
using MongoDB.Bson.Serialization;

namespace Jarvis.Framework.CommitBackup.Core
{
    /// <summary>
    /// Not finished. 
    /// </summary>
    public class PlainTextFileCommitWriter : ICommitWriter
    {
        public String FileName { get; private set; }

        private Int64 _maxFileLength;

        private Boolean _multiFile;

        private Int64 _currentFileSize;

        private String _currentFileName;

        StreamWriter _fileStreamWriter;

        private Info _info;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="maxFileLength">Max individual file length in byte</param>
        public PlainTextFileCommitWriter(String fileName, Int64 maxFileLength)
        {
            _currentFileName = FileName = fileName;
            _maxFileLength = maxFileLength;
            _multiFile = maxFileLength < Int64.MaxValue;

            _info = GetInfo();
        }

    

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fileName"></param>
        public PlainTextFileCommitWriter(String fileName) :
            this(fileName, Int64.MaxValue)
        {

        }

        public void Append(long commitId, BsonDocument commit)
        {
            if (_info.LastCommitWritten >= commitId)
                return; //commit already saved.

            if (_fileStreamWriter == null)
            {
                if (_multiFile)
                {
                    //first of all find all files 
                    GetCurrentFileNameAndSize();
                    if (_currentFileSize > _maxFileLength)
                        _info.CurrentFileCounter += 1;
                    GetCurrentFileNameAndSize();
                }
                _fileStreamWriter = CreateFileStreamForWrite();
            }

            var stringData = commit.ToString();
            _fileStreamWriter.WriteLine(stringData);
            if (CheckCurrentSizeExceeding(stringData))
            {
                Close();
            }
            _info.LastCommitWritten = commitId;
        }

        private bool CheckCurrentSizeExceeding(String dataWritten)
        {
            if (!_multiFile) return false;
            _currentFileSize += dataWritten.Length;

            return _currentFileSize > _maxFileLength && _multiFile;
        }

        private StreamWriter CreateFileStreamForWrite()
        {
            Stream fileStream = new FileStream(_currentFileName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite);
            fileStream.Seek(0, SeekOrigin.End);

            return new StreamWriter(fileStream);
        }

        private Stream CreateFileStreamForRead(String fileName)
        {
            var fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            return fileStream;
        }

        public long GetLastCommitAppended()
        {
            return _info.LastCommitWritten;
        }

        public IEnumerable<BsonDocument> GetCommits(long commitStart)
        {
            Int64 lineNumber = 1;
            var allFiles = GetFiles();
            foreach (var file in allFiles)
            {
                using (var fs = CreateFileStreamForRead(file))
                using (StreamReader reader = new StreamReader(fs))
                {
                    String line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (lineNumber++ >= commitStart)
                        {
                            yield return BsonDocument.Parse(line);
                        }
                    }
                }
            }
        }

        public void Close()
        {
            if (_fileStreamWriter == null) return;
            try
            {
                _fileStreamWriter.Flush();
                _fileStreamWriter.Dispose();
            }
            finally
            {
                _fileStreamWriter = null;
            }
            PersistInfo();
        }

        private void PersistInfo()
        {
            File.WriteAllText(GetInfoFileName(), _info.ToBsonDocument().ToString());
        }

        private Info GetInfo()
        {
            var fileName = GetInfoFileName();
            if (File.Exists(fileName))
            {
                var data = File.ReadAllText(fileName);
                return BsonSerializer.Deserialize<Info>(data);
            }
            return new Info();
        }

        private string GetInfoFileName()
        {
            return FileName + ".info";
        }

        private IEnumerable<String> GetAllPartialFiles()
        {
            var fn = Path.GetFileName(FileName);
            var files = Directory.GetFiles(Path.GetDirectoryName(FileName), fn + ".*");
            return files;
        }

        private IEnumerable<String> GetFiles()
        {
            if (_multiFile)
            {
                var files = GetAllPartialFiles();
                foreach (var fileInfo in files
                    .Select(f => new { file = f, counter = GetCounterFromFileName(f) })
                    .Where(f => f.counter != -1)
                    .OrderBy(f => f.counter))
                {
                    yield return fileInfo.file;
                }
            }
            else
            {
                yield return _currentFileName;
            }
        }

        private int GetCounterFromFileName(string file)
        {
            if (file.Length <= FileName.Length + 1) return -1;
            var suffix = file.Substring(FileName.Length + 1);
            Int32 counter;
            if (Int32.TryParse(suffix, out counter))
                return counter;

            return -1;
        }

        private void GetCurrentFileNameAndSize()
        {
            _currentFileSize = 0;
            _currentFileName = GetCurrentFileName();
            if (File.Exists(_currentFileName))
            {
                _currentFileSize = new FileInfo(_currentFileName).Length;
            }
        }

        private String GetCurrentFileName()
        {
            return FileName + "." + _info.CurrentFileCounter.ToString();
        }

        private class Info
        {
            public Int64 LastCommitWritten { get; set; }

            public Int64 CurrentFileCounter { get; set; }
        }
    }
}
