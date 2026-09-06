using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using VuonNho.Core;

namespace VuonNho.Infrastructure
{
    /// <summary>
    /// Ghi file tam day du, doc lai de kiem tra, roi thay the file chinh va giu ban tot truoc do lam backup.
    /// Snapshot nho nen bat dau bang ghi dong bo tuan tu; chi them hang doi async neu do duoc giat hinh.
    /// </summary>
    public sealed class FileSaveRepository : ISaveRepository
    {
        public const string MainFileName = "vuon-nho-save.json";
        public const string BackupFileName = "vuon-nho-save.backup.json";
        public const string TempFileName = "vuon-nho-save.tmp";

        readonly string _directory;
        static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(false);

        public string MainPath { get { return Path.Combine(_directory, MainFileName); } }
        public string BackupPath { get { return Path.Combine(_directory, BackupFileName); } }
        public string TempPath { get { return Path.Combine(_directory, TempFileName); } }

        public FileSaveRepository(string directory)
        {
            if (string.IsNullOrEmpty(directory)) throw new ArgumentException("Thieu thu muc save.");
            _directory = directory;
            if (!Directory.Exists(_directory)) Directory.CreateDirectory(_directory);
        }

        public bool HasSave
        {
            get { return File.Exists(MainPath) || File.Exists(BackupPath); }
        }

        public IList<SaveCandidate> LoadCandidates()
        {
            var candidates = new List<SaveCandidate>(2);
            candidates.Add(ReadFile("main", MainPath));
            candidates.Add(ReadFile("backup", BackupPath));
            return candidates;
        }

        static SaveCandidate ReadFile(string name, string path)
        {
            var candidate = new SaveCandidate { Name = name };
            try
            {
                if (File.Exists(path)) candidate.Json = File.ReadAllText(path, Utf8NoBom);
            }
            catch (Exception error)
            {
                candidate.ReadError = name + ": " + error.Message;
            }
            return candidate;
        }

        public void Save(string json)
        {
            if (json == null) throw new ArgumentNullException("json");
            if (!Directory.Exists(_directory)) Directory.CreateDirectory(_directory);

            File.WriteAllText(TempPath, json, Utf8NoBom);

            // Kiem tra file tam doc lai duoc truoc khi cho no thay file chinh.
            string readBack = File.ReadAllText(TempPath, Utf8NoBom);
            if (!string.Equals(readBack, json, StringComparison.Ordinal))
                throw new IOException("File save tam doc lai khong khop noi dung vua ghi.");

            if (File.Exists(MainPath))
            {
                // File.Replace giu lai ban truoc do lam backup trong mot buoc.
                File.Replace(TempPath, MainPath, BackupPath, true);
            }
            else
            {
                File.Move(TempPath, MainPath);
            }
        }

        public void DeleteAll()
        {
            SafeDelete(MainPath);
            SafeDelete(BackupPath);
            SafeDelete(TempPath);
        }

        static void SafeDelete(string path)
        {
            try
            {
                if (File.Exists(path)) File.Delete(path);
            }
            catch (Exception)
            {
                // Xoa that bai khong duoc lam hong luong choi; lan luu sau se ghi de.
            }
        }
    }
}
