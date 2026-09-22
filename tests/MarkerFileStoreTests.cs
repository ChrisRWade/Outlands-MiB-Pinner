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
            Run("removes reloaded markers in arbitrary order", RemovesReloadedMarkersInArbitraryOrder);
            Run("removes the exact stale marker when coordinates are duplicated", RemovesExactStaleMarkerWithDuplicateCoordinates);
            Run("renumbers MiB labels without moving marker data", RenumbersLabelsWithoutMovingMarkers);
            Run("rolls back renumbering when the file changed outside the app", RejectsOutsideEditDuringRenumber);
            Run("marks a chart done and active by changing only its icon", TogglesCompletedIconOnly);
            Run("undo restores the previous icon state", UndoRestoresCompletedIcon);
            Run("rolls back completion when the file changed outside the app", RejectsOutsideEditDuringCompletion);
            Run("leaves custom marker icons unchanged", LeavesCustomIconsUnchanged);
            Run("mixed completion changes only eligible active markers", MixedCompletionChangesEligibleMarkersOnly);
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
            string xml = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Pack Name=\"Custom\" Revision=\"7\" Extra=\"keep\">\n<!-- preserved note -->\n#Ossuary Expansion\n<Unknown Value=\"keep\"/>\n<Marker Name=\"Old\" X=\"10\" Y=\"20\" Icon=\"TREASURE\" Facet=\"0\"/>\n</Pack>";
            File.WriteAllText(path, xml, new UTF8Encoding(false));
            MarkerFileStore store = new MarkerFileStore(directory);
            store.Add(30, 40);
            string saved = File.ReadAllText(path);
            Assert(saved.Contains("preserved note"), "Comment was lost.");
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

    private static void RemovesReloadedMarkersInArbitraryOrder()
    {
        WithTemporaryDirectory(directory =>
        {
            MarkerFileStore store = new MarkerFileStore(directory);
            MapMarker first = store.Add(10, 11);
            MapMarker second = store.Add(20, 21);
            MapMarker third = store.Add(30, 31);
            MapMarker fourth = store.Add(40, 41);

            store.Load();
            int removed = store.Remove(new[] { fourth, second });
            Assert(removed == 2, "Expected two markers removed from a reloaded store.");
            Assert(store.Markers.Select(marker => marker.X).SequenceEqual(new[] { 10, 30 }), "Random-order removal kept the wrong markers.");
            Assert(store.Markers.Select(marker => marker.Name).SequenceEqual(new[] { "MiB 001", "MiB 003" }), "Removal unexpectedly changed remaining labels.");

            int removedFirst = store.Remove(new[] { first });
            Assert(removedFirst == 1, "A stale marker identity could not be removed by its stable coordinates.");
            Assert(store.Markers.Count == 1 && store.Markers[0].X == third.X, "The wrong marker remained after stale-identity removal.");
        });
    }

    private static void RemovesExactStaleMarkerWithDuplicateCoordinates()
    {
        WithTemporaryDirectory(directory =>
        {
            string path = Path.Combine(directory, MarkerFileStore.MarkerFileName);
            string xml = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Pack Name=\"Test\" Revision=\"0\">" +
                "<Marker Name=\"MiB 001\" X=\"10\" Y=\"20\" Icon=\"TREASURE\" Facet=\"0\"/>" +
                "<Marker Name=\"MiB 002\" X=\"10\" Y=\"20\" Icon=\"TREASURE\" Facet=\"0\"/>" +
                "</Pack>";
            File.WriteAllText(path, xml, new UTF8Encoding(false));

            MarkerFileStore store = new MarkerFileStore(directory);
            MapMarker staleSecond = store.Markers[1];
            store.Load();
            int removed = store.Remove(new[] { staleSecond });

            Assert(removed == 1, "Expected one exact stale marker removal.");
            Assert(store.Markers.Count == 1 && store.Markers[0].Name == "MiB 001", "Duplicate coordinates caused the wrong marker to be removed.");
            XElement remaining = XDocument.Load(path).Root.Elements("Marker").Single();
            Assert((string)remaining.Attribute("Name") == "MiB 001", "The XML removed the wrong duplicate-coordinate record.");
        });
    }

    private static void RenumbersLabelsWithoutMovingMarkers()
    {
        WithTemporaryDirectory(directory =>
        {
            string path = Path.Combine(directory, MarkerFileStore.MarkerFileName);
            string xml = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Pack Name=\"Custom\" Revision=\"7\" Extra=\"keep\">\n" +
                "<!-- preserved note -->\n" +
                "<Marker Name=\"MiB 001\" X=\"10\" Y=\"11\" Icon=\"TREASURE\" Facet=\"0\" Data=\"keep\"/>\n" +
                "<Unknown Value=\"keep\"/>\n" +
                "<Marker Name=\"Custom chart\" X=\"20\" Y=\"21\" Icon=\"TREASURE\" Facet=\"0\"/>\n" +
                "<Marker Name=\"MiB 003\" X=\"30\" Y=\"31\" Icon=\"TREASURE\" Facet=\"0\"/>\n" +
                "<Marker Name=\"MiB 004\" X=\"40\" Y=\"41\" Icon=\"TREASURE\" Facet=\"0\"/>\n" +
                "</Pack>";
            File.WriteAllText(path, xml, new UTF8Encoding(false));
            byte[] before = File.ReadAllBytes(path);

            MarkerFileStore store = new MarkerFileStore(directory);
            Assert(store.NeedsSequentialRenumbering, "Expected numbering gaps to be detected.");
            int changed = store.RenumberSequentially();
            Assert(changed == 2, "Expected exactly two MiB labels to change.");
            Assert(!store.NeedsSequentialRenumbering, "Labels should be consecutive after renumbering.");

            XDocument saved = XDocument.Load(path, LoadOptions.PreserveWhitespace);
            XElement[] markers = saved.Root.Elements("Marker").ToArray();
            Assert(markers.Select(element => (string)element.Attribute("Name")).SequenceEqual(new[] { "MiB 001", "Custom chart", "MiB 002", "MiB 003" }), "Renumbering produced the wrong labels or changed a custom label.");
            Assert(markers.Select(element => (string)element.Attribute("X")).SequenceEqual(new[] { "10", "20", "30", "40" }), "Renumbering changed marker order or coordinates.");
            Assert((string)markers[0].Attribute("Data") == "keep", "Renumbering removed an unknown marker attribute.");
            Assert(saved.ToString().Contains("preserved note") && saved.Root.Element("Unknown") != null, "Renumbering removed unknown XML content.");
            Assert(File.Exists(store.BackupPath) && before.SequenceEqual(File.ReadAllBytes(store.BackupPath)), "Renumbering did not preserve the exact pre-change backup.");

            store.RestoreBackup();
            Assert(store.Markers.Select(marker => marker.Name).SequenceEqual(new[] { "MiB 001", "Custom chart", "MiB 003", "MiB 004" }), "Undo did not restore pre-renumber labels.");
            store.RenumberSequentially();
            MapMarker added = store.Add(50, 51);
            Assert(added.Name == "MiB 004", "A marker added after renumbering did not receive the next sequential label.");
        });
    }

    private static void RejectsOutsideEditDuringRenumber()
    {
        WithTemporaryDirectory(directory =>
        {
            string path = Path.Combine(directory, MarkerFileStore.MarkerFileName);
            string xml = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Pack Name=\"Test\" Revision=\"0\"><Marker Name=\"MiB 001\" X=\"1\" Y=\"2\" Icon=\"TREASURE\" Facet=\"0\"/><Marker Name=\"MiB 003\" X=\"3\" Y=\"4\" Icon=\"TREASURE\" Facet=\"0\"/></Pack>";
            File.WriteAllText(path, xml, new UTF8Encoding(false));
            MarkerFileStore store = new MarkerFileStore(directory);
            string[] namesBefore = store.Markers.Select(marker => marker.Name).ToArray();
            File.AppendAllText(path, "\n<!-- external renumber conflict -->");

            bool threw = false;
            try
            {
                store.RenumberSequentially();
            }
            catch (IOException)
            {
                threw = true;
            }

            Assert(threw, "Expected an outside-edit conflict during renumbering.");
            Assert(store.Markers.Select(marker => marker.Name).SequenceEqual(namesBefore), "Failed renumbering left in-memory labels changed.");
            Assert(File.ReadAllText(path).Contains("external renumber conflict"), "Renumbering overwrote the outside edit.");
            Assert(!File.Exists(store.BackupPath), "A failed renumber unexpectedly replaced the backup.");
        });
    }

    private static void TogglesCompletedIconOnly()
    {
        WithTemporaryDirectory(directory =>
        {
            string path = Path.Combine(directory, MarkerFileStore.MarkerFileName);
            string xml = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Pack Name=\"Custom\" Revision=\"7\"><!--keep--><Marker Name=\"MiB 001\" X=\"123\" Y=\"456\" Icon=\"TREASURE\" Facet=\"0\" Note=\"preserve\"/></Pack>";
            File.WriteAllText(path, xml, new UTF8Encoding(false));
            MarkerFileStore store = new MarkerFileStore(directory);
            MapMarker marker = store.Markers.Single();

            int changed = store.SetCompleted(new[] { marker }, true);
            Assert(changed == 1 && marker.IsCompleted, "Expected one completed marker.");
            XDocument completed = XDocument.Load(path, LoadOptions.PreserveWhitespace);
            XElement completedMarker = completed.Root.Elements("Marker").Single();
            Assert((string)completedMarker.Attribute("Icon") == MarkerFileStore.CompletedIcon, "Completed marker did not use LANDMARKX.");
            Assert((string)completedMarker.Attribute("Name") == "MiB 001", "Completion changed the marker name.");
            Assert((string)completedMarker.Attribute("X") == "123" && (string)completedMarker.Attribute("Y") == "456", "Completion changed coordinates.");
            Assert((string)completedMarker.Attribute("Facet") == "0" && (string)completedMarker.Attribute("Note") == "preserve", "Completion changed unrelated marker data.");
            Assert(completed.Root.Nodes().OfType<XComment>().Any(comment => comment.Value == "keep"), "Completion removed an unrelated XML comment.");

            store.Load();
            Assert(store.Markers.Single().IsCompleted, "Completed state did not survive reload.");
            changed = store.SetCompleted(store.Markers, false);
            Assert(changed == 1 && !store.Markers.Single().IsCompleted, "Expected the marker to return to active.");
            Assert((string)XDocument.Load(path).Root.Elements("Marker").Single().Attribute("Icon") == MarkerFileStore.DefaultIcon, "Active marker did not return to TREASURE.");
        });
    }

    private static void UndoRestoresCompletedIcon()
    {
        WithTemporaryDirectory(directory =>
        {
            MarkerFileStore store = new MarkerFileStore(directory);
            MapMarker marker = store.Add(12, 34);
            store.SetCompleted(new[] { marker }, true);
            Assert(store.Markers.Single().IsCompleted, "Marker was not marked done before undo.");
            store.RestoreBackup();
            Assert(!store.Markers.Single().IsCompleted, "Undo did not restore the active icon.");
            Assert(store.Markers.Single().Icon == MarkerFileStore.DefaultIcon, "Undo restored the wrong icon.");
        });
    }

    private static void RejectsOutsideEditDuringCompletion()
    {
        WithTemporaryDirectory(directory =>
        {
            MarkerFileStore store = new MarkerFileStore(directory);
            MapMarker marker = store.Add(21, 43);
            File.AppendAllText(store.FilePath, "\n<!-- external completion conflict -->");
            bool threw = false;
            try
            {
                store.SetCompleted(new[] { marker }, true);
            }
            catch (IOException)
            {
                threw = true;
            }

            Assert(threw, "Expected outside-edit conflict during completion.");
            Assert(marker.Icon == MarkerFileStore.DefaultIcon && !marker.IsCompleted, "Failed completion did not roll back the in-memory icon.");
            Assert(File.ReadAllText(store.FilePath).Contains("external completion conflict"), "Completion overwrote the outside edit.");
        });
    }

    private static void LeavesCustomIconsUnchanged()
    {
        WithTemporaryDirectory(directory =>
        {
            string path = Path.Combine(directory, MarkerFileStore.MarkerFileName);
            File.WriteAllText(path, "<Pack Name=\"Custom\" Revision=\"0\"><Marker Name=\"Imported\" X=\"1\" Y=\"2\" Icon=\"questmarker\" Facet=\"0\"/></Pack>");
            MarkerFileStore store = new MarkerFileStore(directory);
            int changed = store.SetCompleted(store.Markers, true);
            Assert(changed == 0, "A custom imported icon should not be overwritten.");
            Assert(store.Markers.Single().Icon == "questmarker", "Custom imported icon changed in memory.");
            Assert((string)XDocument.Load(path).Root.Elements("Marker").Single().Attribute("Icon") == "questmarker", "Custom imported icon changed on disk.");
        });
    }

    private static void MixedCompletionChangesEligibleMarkersOnly()
    {
        WithTemporaryDirectory(directory =>
        {
            string path = Path.Combine(directory, MarkerFileStore.MarkerFileName);
            File.WriteAllText(path,
                "<Pack Name=\"Custom\" Revision=\"0\">" +
                "<Marker Name=\"MiB 001\" X=\"1\" Y=\"2\" Icon=\"TREASURE\" Facet=\"0\"/>" +
                "<Marker Name=\"MiB 002\" X=\"3\" Y=\"4\" Icon=\"LANDMARKX\" Facet=\"0\"/>" +
                "<Marker Name=\"Imported\" X=\"5\" Y=\"6\" Icon=\"questmarker\" Facet=\"0\"/>" +
                "</Pack>");
            MarkerFileStore store = new MarkerFileStore(directory);
            int changed = store.SetCompleted(store.Markers, true);
            Assert(changed == 1, "Mixed selection should change only the active TREASURE marker.");
            Assert(store.Markers[0].IsCompleted && store.Markers[1].IsCompleted, "Managed markers did not end in the done state.");
            Assert(store.Markers[2].Icon == "questmarker", "Mixed completion overwrote a custom icon.");
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
