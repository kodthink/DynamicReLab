using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml.Linq;

class DynamicRelab_NASA
{
    static void Main()
    {
        // File Paths
        string inputFilePath = "nasa.xml";
        string outputFilePath = "insert_nasa.xml";

        // Load XML Document
        XDocument doc = XDocument.Load(inputFilePath);

        // Initialize Labeling Process
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

        // Measure Initial Memory Usage
        long initialMemory = GetMemoryUsageInBytes();

        while (queue.Count > 0)
        {
            var (node, currentLevel, currentBinaryPath, currentRegionID) = queue.Dequeue();

            // Label the Current Node
            node.SetAttributeValue("label", $"{currentLevel},{currentBinaryPath},{currentRegionID}");

            // Enqueue Child Nodes
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

        // Define XML Namespace
        XNamespace xlink = "http://www.w3.org/XML/XLink/0.9";

        // Add and Label a New Dataset Node
        XElement newDataset = new XElement("dataset",
            new XAttribute("subject", "astronomy"),
            new XAttribute(XNamespace.Xmlns + "xlink", xlink),

            new XElement("title", "Proper Motions of Stars in the Zone Catalogue -40 to -52 degrees of 20843 Stars for 1900"),
            new XElement("altname", new XAttribute("type", "ADC"), "1005"),
            new XElement("altname", new XAttribute("type", "CDS"), "I/5"),
            new XElement("altname", new XAttribute("type", "brief"), "Proper Motions in Cape Zone Catalogue -40/-52"),

            new XElement("reference",
                new XElement("source",
                    new XElement("other",
                        new XElement("title", "Proper Motions of Stars in the Zone Catalogue"),
                        new XElement("author",
                            new XElement("initial", "J"),
                            new XElement("lastName", "Spencer")
                        ),
                        new XElement("author",
                            new XElement("initial", "J"),
                            new XElement("lastName", "Jackson")
                        ),
                        new XElement("name", "His Majesty's Stationery Office, London"),
                        new XElement("publisher", "???"),
                        new XElement("city", "???"),
                        new XElement("date",
                            new XElement("year", "1936")
                        )
                    )
                )
            ),

            new XElement("keywords",
                new XAttribute("parentListURL", "http://messier.gsfc.nasa.gov/xml/keywordlists/adc_keywords.html"),
                new XElement("keyword", new XAttribute(xlink + "href", "Positional_data.html"), "Positional data"),
                new XElement("keyword", new XAttribute(xlink + "href", "Proper_motions.html"), "Proper motions")
            ),

            new XElement("descriptions",
                new XElement("description",
                    new XElement("para", "This catalog, listing the proper motions of 20,843 stars from the Cape Astrographic Zones...")
                ),
                new XElement("details")
            ),

            new XElement("identifier", "I_5.xml")
        );

        doc.Root.Add(newDataset);

        // Measure Memory Before Relabeling the New Node
        long memoryBeforeRelabel = GetMemoryUsageInBytes();
        stopwatch.Restart();

        int newNodeLevel = 1;
        string newNodeBinaryPath = GenerateBinaryPath(doc.Root.Elements().Count());
        newDataset.SetAttributeValue("label", $"{newNodeLevel},{newNodeBinaryPath},{regionID++}");

        stopwatch.Stop();
        long memoryAfterRelabel = GetMemoryUsageInBytes();
        TimeSpan newNodeTimeTaken = stopwatch.Elapsed;

        // Save Updated XML
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
