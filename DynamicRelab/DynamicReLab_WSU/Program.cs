using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml.Linq;

class DynamicRelab_WSU
{
    static void Main()
    {
        // Load the XML document
        string inputFilePath = "wsu.xml";
        string outputFilePath = "insert_node_wsu.xml";
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
        XElement newCourse = new XElement("course",
            new XElement("footnote", "NEW"),
            new XElement("sln", "99999"),
            new XElement("prefix", "CS"),
            new XElement("crs", "505"),
            new XElement("lab"),
            new XElement("sect", "01"),
            new XElement("title", "ADV ALGORITHMS"),
            new XElement("credit", "4.0"),
            new XElement("days", "M,W"),
            new XElement("times",
                new XElement("start", "14:00"),
                new XElement("end", "15:30")
            ),
            new XElement("place",
                new XElement("bldg", "ENGR"),
                new XElement("room", "101")
            ),
            new XElement("instructor", "DR. SMITH"),
            new XElement("limit", "60"),
            new XElement("enrolled", "0")
        );

        doc.Root.Add(newCourse);

        // Measure memory before relabeling the new node
        long memoryBeforeRelabel = GetMemoryUsageInBytes();
        stopwatch.Restart();

        int newNodeLevel = 1;
        string newNodeBinaryPath = GenerateBinaryPath(doc.Root.Elements().Count());
        newCourse.SetAttributeValue("label", $"{newNodeLevel},{newNodeBinaryPath},{regionID++}");

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
