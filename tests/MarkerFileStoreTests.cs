using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using WadesMiBPinner;

internal static class MarkerFileStoreTests
{
    private static int _passed;

    private static int Main()
    {
        try
        {
            Run("creates the exact marker file and schema", CreatesExactMarkerFile);
            Run("rejects duplicate coordinates without changing XML", RejectsDuplicateCoordinates);
            Run("removes only the requested marker and keeps a backup", RemovesSelectedMarker);
            Run("preserves comments, text, and unknown XML content", PreservesUnknownContent);
            Run("rejects malformed existing files without overwriting", RejectsMalformedFile);
            Run("detects an outside edit before saving", DetectsOutsideEdit);
            Run("restores and consumes the latest backup", RestoresLatestBackup);
            Console.WriteLine("All " + _passed + " marker-store tests passed.");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
    }

    private static void CreatesExactMarkerFile()
    {
        WithTemporaryDirectory(directory =>
        {
            MarkerFileStore store = new MarkerFileStore(directory);
            MapMarker marker = store.Add(5450, 502);
            string path = Path.Combine(directory, MarkerFileStore.MarkerFileName);
            Assert(File.Exists(path), "Expected exact marker filename.");
            XDocument document = XDocument.Load(path);
            Assert(document.Root.Name.LocalName == "Pack", "Expected Pack root.");
            Assert((string)document.Root.Attribute("Name") == MarkerFileStore.PackName, "Unexpected pack name.");
            XElement element = document.Root.Elements("Marker").Single();
            Assert((string)element.Attribute("Name") == "MiB 001", "Unexpected generated name.");
            Assert((string)element.Attribute("X") == "5450", "Unexpected X.");
            Assert((string)element.Attribute("Y") == "502", "Unexpected Y.");
            Assert((string)element.Attribute("Icon") == "TREASURE", "Expected installed treasure icon.");
            Assert((string)element.Attribute("Facet") == "0", "Expected facet zero.");
            Assert(marker.Name == "MiB 001", "Returned marker should match saved marker.");
        });
    }

    private static void RejectsDuplicateCoordinates()
    {
        WithTemporaryDirectory(directory =>
        {
            MarkerFileStore store = new MarkerFileStore(directory);
            store.Add(100, 200);
            byte[] before = File.ReadAllBytes(store.FilePath);
            bool threw = false;
            try
            {
                store.Add(100, 200);
            }
            catch (InvalidOperationException)
            {
                threw = true;
            }

            Assert(threw, "Expected duplicate rejection.");
            Assert(before.SequenceEqual(File.ReadAllBytes(store.FilePath)), "Duplicate changed the file.");
        });
    }

    private static void RemovesSelectedMarker()
    {
        WithTemporaryDirectory(directory =>
        {
            MarkerFileStore store = new MarkerFileStore(directory);
            MapMarker first = store.Add(1, 2);
            MapMarker second = store.Add(3, 4);
            store.Remove(new[] { first });
            Assert(store.Markers.Count == 1, "Expected one marker after removal.");
            Assert(store.Markers[0].X == second.X && store.Markers[0].Y == second.Y, "Wrong marker remained.");
            Assert(File.Exists(store.BackupPath), "Expected recoverable backup.");
        });
    }

    private static void PreservesUnknownContent()
    {
        WithTemporaryDirectory(directory =>
        {
            string path = Path.Combine(directory, MarkerFileStore.MarkerFileName);
            string xml = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Pack Name=\"Custom\" Revision=\"7\" Extra=\"keep\">\n<!-- guild note -->\n#Ossuary Expansion\n<Unknown Value=\"keep\"/>\n<Marker Name=\"Old\" X=\"10\" Y=\"20\" Icon=\"TREASURE\" Facet=\"0\"/>\n</Pack>";
            File.WriteAllText(path, xml, new UTF8Encoding(false));
            MarkerFileStore store = new MarkerFileStore(directory);
            store.Add(30, 40);
            string saved = File.ReadAllText(path);
            Assert(saved.Contains("guild note"), "Comment was lost.");
            Assert(saved.Contains("#Ossuary Expansion"), "Text node was lost.");
            Assert(saved.Contains("<Unknown Value=\"keep\""), "Unknown element was lost.");
            Assert(saved.Contains("Extra=\"keep\""), "Unknown root attribute was lost.");
        });
    }

    private static void RejectsMalformedFile()
    {
        WithTemporaryDirectory(directory =>
        {
            string path = Path.Combine(directory, MarkerFileStore.MarkerFileName);
            string malformed = "<Pack><Marker X=\"1\"";
            File.WriteAllText(path, malformed, Encoding.UTF8);
            bool threw = false;
            try
            {
                new MarkerFileStore(directory);
            }
            catch
            {
                threw = true;
            }

            Assert(threw, "Expected malformed XML rejection.");
            Assert(File.ReadAllText(path) == malformed, "Malformed file was overwritten.");
        });
    }

    private static void DetectsOutsideEdit()
    {
        WithTemporaryDirectory(directory =>
        {
            MarkerFileStore store = new MarkerFileStore(directory);
            store.Add(10, 20);
            File.AppendAllText(store.FilePath, "\n<!-- external edit -->");
            bool threw = false;
            try
            {
                store.Add(30, 40);
            }
            catch (IOException)
            {
                threw = true;
            }

            Assert(threw, "Expected outside-edit conflict.");
            Assert(File.ReadAllText(store.FilePath).Contains("external edit"), "Outside edit was overwritten.");
        });
    }

    private static void RestoresLatestBackup()
    {
        WithTemporaryDirectory(directory =>
        {
            MarkerFileStore store = new MarkerFileStore(directory);
            store.Add(10, 20);
            store.Add(30, 40);
            Assert(store.BackupExists, "Expected backup after second write.");
            store.RestoreBackup();
            Assert(store.Markers.Count == 1, "Expected the first marker state after restore.");
            Assert(store.Markers[0].X == 10 && store.Markers[0].Y == 20, "Restored the wrong marker state.");
            Assert(!store.BackupExists, "Undo backup should be consumed after a successful restore.");
        });
    }

    private static void WithTemporaryDirectory(Action<string> action)
    {
        string directory = Path.Combine(Path.GetTempPath(), "WadesMiBPinnerTests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            action(directory);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    private static void Run(string name, Action test)
    {
        test();
        _passed++;
        Console.WriteLine("PASS  " + name);
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
