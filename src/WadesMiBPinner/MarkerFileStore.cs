using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace WadesMiBPinner
{
    public sealed class MarkerFileStore
    {
        public const string MarkerFileName = "Wade's Map Markers - MiB's.xml";
        public const string PackName = "Wade's Map Markers - MiB's";
        public const string DefaultIcon = "TREASURE";
        public const int DefaultFacet = 0;

        private XDocument _document;
        private readonly List<MarkerBinding> _bindings = new List<MarkerBinding>();
        private FileFingerprint _loadedFingerprint;

        public MarkerFileStore(string directoryPath)
        {
            if (String.IsNullOrWhiteSpace(directoryPath))
            {
                throw new ArgumentException("A marker folder is required.", "directoryPath");
            }

            DirectoryPath = Path.GetFullPath(directoryPath);
            Load();
        }

        public string DirectoryPath { get; private set; }

        public string FilePath
        {
            get { return Path.Combine(DirectoryPath, MarkerFileName); }
        }

        public string BackupPath
        {
            get { return FilePath + ".backup"; }
        }

        public bool FileExists
        {
            get { return File.Exists(FilePath); }
        }

        public bool BackupExists
        {
            get { return File.Exists(BackupPath); }
        }

        public IList<MapMarker> Markers
        {
            get { return _bindings.Select(binding => binding.Marker).ToList().AsReadOnly(); }
        }

        public void Load()
        {
            _bindings.Clear();

            if (!File.Exists(FilePath))
            {
                _document = CreateEmptyDocument();
                _loadedFingerprint = FileFingerprint.Missing;
                return;
            }

            XmlReaderSettings settings = new XmlReaderSettings();
            settings.DtdProcessing = DtdProcessing.Prohibit;
            settings.XmlResolver = null;
            settings.IgnoreWhitespace = false;

            using (FileStream stream = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (XmlReader reader = XmlReader.Create(stream, settings))
            {
                _document = XDocument.Load(reader, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);
            }

            if (_document.Root == null || _document.Root.Name.LocalName != "Pack")
            {
                throw new InvalidDataException("The marker file does not contain a Pack root element.");
            }

            foreach (XElement element in _document.Root.Elements().Where(item => item.Name.LocalName == "Marker"))
            {
                string name = ReadRequiredAttribute(element, "Name");
                int x = ReadIntegerAttribute(element, "X");
                int y = ReadIntegerAttribute(element, "Y");
                string icon = ReadRequiredAttribute(element, "Icon");
                int facet = ReadIntegerAttribute(element, "Facet");
                _bindings.Add(new MarkerBinding(new MapMarker(name, x, y, icon, facet), element));
            }

            _loadedFingerprint = FileFingerprint.Capture(FilePath);
        }

        public bool ContainsCoordinates(int x, int y)
        {
            return _bindings.Any(binding => binding.Marker.X == x && binding.Marker.Y == y);
        }

        public MapMarker Add(int x, int y)
        {
            ValidateCoordinate(x, "X");
            ValidateCoordinate(y, "Y");

            if (ContainsCoordinates(x, y))
            {
                throw new InvalidOperationException("A marker already exists at " + x + ", " + y + ".");
            }

            string name = NextMarkerName();
            XElement element = new XElement(
                "Marker",
                new XAttribute("Name", name),
                new XAttribute("X", x.ToString(CultureInfo.InvariantCulture)),
                new XAttribute("Y", y.ToString(CultureInfo.InvariantCulture)),
                new XAttribute("Icon", DefaultIcon),
                new XAttribute("Facet", DefaultFacet.ToString(CultureInfo.InvariantCulture)));

            _document.Root.Add(element);
            MapMarker marker = new MapMarker(name, x, y, DefaultIcon, DefaultFacet);
            _bindings.Add(new MarkerBinding(marker, element));

            try
            {
                Save();
                return marker;
            }
            catch
            {
                element.Remove();
                _bindings.RemoveAt(_bindings.Count - 1);
                throw;
            }
        }

        public void Remove(IEnumerable<MapMarker> markers)
        {
            if (markers == null)
            {
                throw new ArgumentNullException("markers");
            }

            List<MarkerBinding> toRemove = new List<MarkerBinding>();
            foreach (MapMarker marker in markers)
            {
                MarkerBinding binding = _bindings.FirstOrDefault(item => Object.ReferenceEquals(item.Marker, marker));
                if (binding != null)
                {
                    toRemove.Add(binding);
                }
            }

            if (toRemove.Count == 0)
            {
                return;
            }

            List<int> originalIndexes = toRemove.Select(item => _bindings.IndexOf(item)).ToList();
            foreach (MarkerBinding binding in toRemove)
            {
                binding.Element.Remove();
                _bindings.Remove(binding);
            }

            try
            {
                Save();
            }
            catch
            {
                for (int index = 0; index < toRemove.Count; index++)
                {
                    MarkerBinding binding = toRemove[index];
                    int originalIndex = Math.Min(originalIndexes[index], _bindings.Count);
                    _bindings.Insert(originalIndex, binding);
                    InsertMarkerElementAtBindingPosition(binding, originalIndex);
                }

                throw;
            }
        }

        public void RestoreBackup()
        {
            if (!File.Exists(BackupPath))
            {
                throw new FileNotFoundException("No backup is available to restore.", BackupPath);
            }

            string restoreSource = BackupPath;
            string restoreTemporary = FilePath + ".restore-" + Guid.NewGuid().ToString("N") + ".tmp";
            File.Copy(restoreSource, restoreTemporary, true);

            try
            {
                EnsureFileHasNotChanged();
                if (File.Exists(FilePath))
                {
                    File.Replace(restoreTemporary, FilePath, null, true);
                }
                else
                {
                    File.Move(restoreTemporary, FilePath);
                }
            }
            catch (PlatformNotSupportedException)
            {
                File.Copy(restoreTemporary, FilePath, true);
                File.Delete(restoreTemporary);
            }
            catch
            {
                SafeDelete(restoreTemporary);
                throw;
            }

            SafeDelete(BackupPath);
            Load();
        }

        public static bool CanWriteToDirectory(string directoryPath)
        {
            if (String.IsNullOrWhiteSpace(directoryPath) || !Directory.Exists(directoryPath))
            {
                return false;
            }

            string probePath = Path.Combine(directoryPath, ".mib-pinner-write-test-" + Guid.NewGuid().ToString("N") + ".tmp");
            try
            {
                using (FileStream stream = new FileStream(probePath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    stream.WriteByte(1);
                    stream.Flush(true);
                }

                return true;
            }
            catch
            {
                return false;
            }
            finally
            {
                SafeDelete(probePath);
            }
        }

        private void Save()
        {
            Directory.CreateDirectory(DirectoryPath);
            string temporaryPath = FilePath + ".write-" + Guid.NewGuid().ToString("N") + ".tmp";

            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Encoding = new UTF8Encoding(false);
            settings.Indent = true;
            settings.IndentChars = "  ";
            settings.NewLineChars = Environment.NewLine;
            settings.NewLineHandling = NewLineHandling.Replace;
            settings.OmitXmlDeclaration = false;

            try
            {
                using (FileStream stream = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                using (XmlWriter writer = XmlWriter.Create(stream, settings))
                {
                    _document.Save(writer);
                    writer.Flush();
                    stream.Flush(true);
                }

                ValidateSavedDocument(temporaryPath);
                EnsureFileHasNotChanged();

                if (File.Exists(FilePath))
                {
                    try
                    {
                        File.Replace(temporaryPath, FilePath, BackupPath, true);
                    }
                    catch (PlatformNotSupportedException)
                    {
                        FallbackReplace(temporaryPath);
                    }
                    catch (IOException)
                    {
                        FallbackReplace(temporaryPath);
                    }
                }
                else
                {
                    File.Move(temporaryPath, FilePath);
                }

                _loadedFingerprint = FileFingerprint.Capture(FilePath);
            }
            catch
            {
                SafeDelete(temporaryPath);
                throw;
            }
        }

        private void FallbackReplace(string temporaryPath)
        {
            File.Copy(FilePath, BackupPath, true);
            File.Copy(temporaryPath, FilePath, true);
            File.Delete(temporaryPath);
        }

        private void EnsureFileHasNotChanged()
        {
            FileFingerprint current = FileFingerprint.Capture(FilePath);
            if (!_loadedFingerprint.Equals(current))
            {
                throw new IOException("The marker file changed outside this app. Reload it before trying again so no one else's markers are overwritten.");
            }
        }

        private static void ValidateSavedDocument(string path)
        {
            using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (XmlReader reader = XmlReader.Create(stream, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null }))
            {
                XDocument document = XDocument.Load(reader);
                if (document.Root == null || document.Root.Name.LocalName != "Pack")
                {
                    throw new InvalidDataException("The temporary marker file did not contain a Pack root and was not installed.");
                }
            }
        }

        private string NextMarkerName()
        {
            int largest = 0;
            foreach (MarkerBinding binding in _bindings)
            {
                string name = binding.Marker.Name ?? String.Empty;
                if (!name.StartsWith("MiB ", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                int parsed;
                if (Int32.TryParse(name.Substring(4).Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out parsed))
                {
                    largest = Math.Max(largest, parsed);
                }
            }

            return "MiB " + (largest + 1).ToString("000", CultureInfo.InvariantCulture);
        }

        private void InsertMarkerElementAtBindingPosition(MarkerBinding binding, int index)
        {
            if (index < _bindings.Count - 1)
            {
                MarkerBinding following = _bindings[index + 1];
                following.Element.AddBeforeSelf(binding.Element);
            }
            else
            {
                _document.Root.Add(binding.Element);
            }
        }

        private static XDocument CreateEmptyDocument()
        {
            return new XDocument(
                new XDeclaration("1.0", "UTF-8", null),
                new XElement(
                    "Pack",
                    new XAttribute("Name", PackName),
                    new XAttribute("Revision", "0")));
        }

        private static string ReadRequiredAttribute(XElement element, string attributeName)
        {
            XAttribute attribute = element.Attribute(attributeName);
            if (attribute == null)
            {
                throw CreateMarkerFormatException(element, "is missing the " + attributeName + " attribute");
            }

            return attribute.Value;
        }

        private static int ReadIntegerAttribute(XElement element, string attributeName)
        {
            string value = ReadRequiredAttribute(element, attributeName);
            int parsed;
            if (!Int32.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed))
            {
                throw CreateMarkerFormatException(element, "has an invalid " + attributeName + " value");
            }

            return parsed;
        }

        private static InvalidDataException CreateMarkerFormatException(XElement element, string message)
        {
            IXmlLineInfo lineInfo = element as IXmlLineInfo;
            string line = lineInfo != null && lineInfo.HasLineInfo() ? " on line " + lineInfo.LineNumber : String.Empty;
            return new InvalidDataException("A Marker" + line + " " + message + ". The file was not changed.");
        }

        private static void ValidateCoordinate(int value, string name)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(name, "Coordinates cannot be negative.");
            }
        }

        private static void SafeDelete(string path)
        {
            try
            {
                if (!String.IsNullOrWhiteSpace(path) && File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch
            {
                // A temporary probe or interrupted write can be cleaned up later.
            }
        }

        private sealed class MarkerBinding
        {
            public MarkerBinding(MapMarker marker, XElement element)
            {
                Marker = marker;
                Element = element;
            }

            public MapMarker Marker { get; private set; }
            public XElement Element { get; private set; }
        }

        private sealed class FileFingerprint
        {
            public static readonly FileFingerprint Missing = new FileFingerprint(false, 0, 0, String.Empty);

            private FileFingerprint(bool exists, long length, long lastWriteTicks, string hash)
            {
                Exists = exists;
                Length = length;
                LastWriteTicks = lastWriteTicks;
                Hash = hash;
            }

            private bool Exists { get; set; }
            private long Length { get; set; }
            private long LastWriteTicks { get; set; }
            private string Hash { get; set; }

            public static FileFingerprint Capture(string path)
            {
                if (!File.Exists(path))
                {
                    return Missing;
                }

                FileInfo info = new FileInfo(path);
                string hash;
                using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                using (SHA256 algorithm = SHA256.Create())
                {
                    hash = Convert.ToBase64String(algorithm.ComputeHash(stream));
                }

                return new FileFingerprint(true, info.Length, info.LastWriteTimeUtc.Ticks, hash);
            }

            public override bool Equals(object obj)
            {
                FileFingerprint other = obj as FileFingerprint;
                return other != null &&
                    Exists == other.Exists &&
                    Length == other.Length &&
                    LastWriteTicks == other.LastWriteTicks &&
                    String.Equals(Hash, other.Hash, StringComparison.Ordinal);
            }

            public override int GetHashCode()
            {
                return Hash == null ? 0 : Hash.GetHashCode();
            }
        }
    }
}
