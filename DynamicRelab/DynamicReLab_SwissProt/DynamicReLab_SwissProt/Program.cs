using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml.Linq;

class DynamicRelab_SwissProt
{
    static void Main()
    {
        // Load the XML document
        string inputFilePath = "SwissProt.xml";
        string outputFilePath = "insert_node_SwissProt.xml";
        XDocument doc = XDocument.Load(inputFilePath);

        // Initialize the labeling process
        int initialRegionID = 1;
        Queue<(XElement node, int level, string binaryPath, int regionID)> queue = new Queue<(XElement, int, string, int)>();

        int level = 1;
        int regionID = initialRegionID;
        foreach (var rootNode in doc.Root.Elements())
        {
            string binaryPath = GenerateBinaryPath(1); // Root nodes start with binary path "1"
            queue.Enqueue((rootNode, level, binaryPath, regionID++));
        }

        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();

        // Measure initial memory usage
        long initialMemory = GetMemoryUsageInBytes();

        while (queue.Count > 0)
        {
            var (node, currentLevel, currentBinaryPath, currentRegionID) = queue.Dequeue();

            // Label the current node
            node.SetAttributeValue("label", $"{currentLevel},{currentBinaryPath},{currentRegionID}");

            // Enqueue child nodes
            int childIndex = 1;
            foreach (var childNode in node.Elements())
            {
                int childLevel = currentLevel + 1;
                string childBinaryPath = currentBinaryPath + GenerateBinaryPath(childIndex++);
                int childRegionID = (childIndex == 2) ? currentRegionID : currentRegionID + 1;
                queue.Enqueue((childNode, childLevel, childBinaryPath, childRegionID));
            }
        }

        stopwatch.Stop();
        long finalMemory = GetMemoryUsageInBytes();
        TimeSpan totalTimeTaken = stopwatch.Elapsed;

        doc.Save(outputFilePath);
        Console.WriteLine("Labeled XML file has been saved.");
        Console.WriteLine($"Total time taken for labeling: {totalTimeTaken.TotalMilliseconds} ms");
        Console.WriteLine($"Memory used during labeling: {finalMemory - initialMemory} KB");

        // Add and label a new node
        XElement newEntry = new XElement("Entry",
            new XAttribute("id", "200K_HUMAN"),
            new XAttribute("class", "STANDARD"),
            new XAttribute("mtype", "PRT"),
            new XAttribute("seqlen", "1200"),

            new XElement("AC", "P99999"),
            new XElement("Mod", new XAttribute("date", "10-JAN-2024"), new XAttribute("Rel", "55"), new XAttribute("type", "Created")),
            new XElement("Mod", new XAttribute("date", "10-JAN-2024"), new XAttribute("Rel", "55"), new XAttribute("type", "Last sequence update")),
            new XElement("Mod", new XAttribute("date", "20-FEB-2024"), new XAttribute("Rel", "56"), new XAttribute("type", "Last annotation update")),

            new XElement("Descr", "200 KDA SIGNALING RECEPTOR PROTEIN"),
            new XElement("Species", "Homo sapiens (Human)"),
            new XElement("Org", "Eukaryota"),
            new XElement("Org", "Metazoa"),
            new XElement("Org", "Chordata"),
            new XElement("Org", "Craniata"),
            new XElement("Org", "Vertebrata"),
            new XElement("Org", "Mammalia"),
            new XElement("Org", "Primates"),
            new XElement("Org", "Hominidae"),
            new XElement("Org", "Homo"),

            new XElement("Ref", new XAttribute("num", "1"), new XAttribute("pos", "SEQUENCE FROM N.A"),
                new XElement("Comment", "STRAIN=REFERENCE"),
                new XElement("DB", "PUBMED"),
                new XElement("MedlineID", "98765432"),
                new XElement("Author", "Smith J."),
                new XElement("Author", "Doe A."),
                new XElement("Author", "Brown T."),
                new XElement("Cite", "J. Biol. Chem. 299:1234-1245(2024)")
            ),
            new XElement("Ref", new XAttribute("num", "2"), new XAttribute("pos", "ERRATUM"),
                new XElement("Author", "Smith J."),
                new XElement("Cite", "J. Biol. Chem. 300:2345-2346(2024)")
            ),

            new XElement("EMBL", new XAttribute("prim_id", "X12345"), new XAttribute("sec_id", "CAA12345")),
            new XElement("INTERPRO", new XAttribute("prim_id", "IPR001234"), new XAttribute("sec_id", "-")),
            new XElement("INTERPRO", new XAttribute("prim_id", "IPR005678"), new XAttribute("sec_id", "-")),
            new XElement("PFAM", new XAttribute("prim_id", "PF00123"), new XAttribute("sec_id", "SIGNAL"), new XAttribute("status", "1")),
            new XElement("PFAM", new XAttribute("prim_id", "PF00456"), new XAttribute("sec_id", "DOMAIN"), new XAttribute("status", "1")),

            new XElement("Keyword", "Signaling"),
            new XElement("Keyword", "Receptor"),
            new XElement("Keyword", "Transmembrane"),

            new XElement("Features",
                new XElement("DOMAIN", new XAttribute("from", "60"), new XAttribute("to", "120"),
                    new XElement("Descr", "TRANSMEMBRANE DOMAIN")
                ),
                new XElement("DOMAIN", new XAttribute("from", "300"), new XAttribute("to", "450"),
                    new XElement("Descr", "SIGNAL TRANSDUCTION DOMAIN")
                ),
                new XElement("DOMAIN", new XAttribute("from", "700"), new XAttribute("to", "900"),
                    new XElement("Descr", "ATP BINDING DOMAIN")
                ),
                new XElement("BINDING", new XAttribute("from", "850"), new XAttribute("to", "860"),
                    new XElement("Descr", "GTP BINDING SITE")
                )
            )
        );

        // Add the new node to the root
        doc.Root.Add(newEntry);

        // Measure memory before relabeling the new node
        long memoryBeforeRelabel = GetMemoryUsageInBytes();
        stopwatch.Restart();

        int newNodeLevel = 1;
        string newNodeBinaryPath = GenerateBinaryPath(doc.Root.Elements().Count());
        newEntry.SetAttributeValue("label", $"{newNodeLevel},{newNodeBinaryPath},{regionID++}");

        stopwatch.Stop();
        long memoryAfterRelabel = GetMemoryUsageInBytes();
        TimeSpan newNodeTimeTaken = stopwatch.Elapsed;

        doc.Save(outputFilePath);
        Console.WriteLine("Updated XML file with the new node has been saved.");
        Console.WriteLine($"Time taken to insert and label the new node: {newNodeTimeTaken.TotalMilliseconds} ms");
        Console.WriteLine($"Memory used during relabeling the new node: {memoryAfterRelabel - memoryBeforeRelabel} KB");
    }

    static string GenerateBinaryPath(int index)
    {
        return Convert.ToString(index, 2).PadLeft(3, '0'); 
    }

    static long GetMemoryUsageInBytes()
    {
        return Process.GetCurrentProcess().WorkingSet64 / 1024; // KB
    }
}
