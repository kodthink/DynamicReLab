﻿using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;
using System.Linq;
using System.Diagnostics;

public class XmlNode
{
    public string Name { get; set; }
    public List<XmlNode> Children { get; set; } = new List<XmlNode>();
    public ReLabLabel Label { get; set; }
    public XElement Element { get; set; }

    public XmlNode(string name, XElement element)
    {
        Name = name;
        Element = element;
    }

    public void AddChild(XmlNode child)
    {
        Children.Add(child);
    }
}

public class ReLabLabel
{
    public int Level { get; set; }
    public int Ordinal { get; set; }
    public int RID { get; set; }

    public ReLabLabel(int level, int ordinal, int rid)
    {
        Level = level;
        Ordinal = ordinal;
        RID = rid;
    }

    public override string ToString()
    {
        return $"[{Level},{Ordinal},{RID}]";
    }
}

public class ReLab
{
    private int currentOrdinal = 0;

    public void LabelTree(XmlNode root)
    {
        currentOrdinal = 0; // Reset ordinal counter
        AssignLabels(root, 0);
    }

    private void AssignLabels(XmlNode node, int level)
    {
        currentOrdinal++;
        node.Label = new ReLabLabel(level, currentOrdinal, 0);

        foreach (var child in node.Children)
        {
            AssignLabels(child, level + 1);
        }

        if (node.Children.Count > 0)
        {
            int rID = node.Children[node.Children.Count - 1].Label.Ordinal;
            SetRID(node, rID);
        }
    }

    private void SetRID(XmlNode node, int rID)
    {
        node.Label.RID = rID;
        foreach (var child in node.Children)
        {
            SetRID(child, rID);
        }
    }

    public void InsertNode(XmlNode parent, XmlNode newNode)
    {
        //Stopwatch stopwatch = new Stopwatch();
        //long initialMemory = Process.GetCurrentProcess().WorkingSet64 / 1024; // KB;
        //long finalMemory = Process.GetCurrentProcess().WorkingSet64 / 1024;;

        //stopwatch.Restart();
        //initialMemory = Process.GetCurrentProcess().WorkingSet64 / 1024;

        parent.AddChild(newNode);
        //LabelTree(parent); // Relabel the tree after insertion

        //finalMemory = Process.GetCurrentProcess().WorkingSet64 / 1024;
        //stopwatch.Stop();

        //Console.WriteLine($"Time taken to label new insert node: {stopwatch.ElapsedMilliseconds} ms");
        //Console.WriteLine($"Memory Used During Relabeling After Insertion: {finalMemory - initialMemory} KB");
    }
}

public class XmlLabeler
{
    public static XmlNode BuildTree(XElement element)
    {
        var node = new XmlNode(element.Name.LocalName, element);

        foreach (var childElement in element.Elements())
        {
            var childNode = BuildTree(childElement);
            node.AddChild(childNode);
        }

        return node;
    }

    public static void ExportLabeledXml(XmlNode node, string outputPath)
    {
        var labeledDoc = new XDocument(AddLabelsToXml(node));
        labeledDoc.Save(outputPath);
    }

    private static XElement AddLabelsToXml(XmlNode node)
    {
        var element = new XElement(node.Element.Name,
            new XAttribute("label", node.Label.ToString()));

        foreach (var child in node.Children)
        {
            element.Add(AddLabelsToXml(child));
        }

        foreach (var attribute in node.Element.Attributes())
        {
            element.Add(new XAttribute(attribute.Name, attribute.Value));
        }

        foreach (var textNode in node.Element.Nodes().OfType<XText>())
        {
            element.Add(new XText(textNode.Value));
        }

        return element;
    }

    public static List<XmlNode> QueryNodes(XmlNode root, string path)
    {
        var pathParts = path.Split('/');
        return QueryNodesRecursive(root, pathParts, 0);
    }

    private static List<XmlNode> QueryNodesRecursive(XmlNode current, string[] pathParts, int level)
    {
        var result = new List<XmlNode>();

        if (level >= pathParts.Length)
        {
            return result;
        }

        var currentPathPart = pathParts[level];

        if (current.Name == currentPathPart || currentPathPart == "*")
        {
            if (level == pathParts.Length - 1)
            {
                result.Add(current);
            }
            else
            {
                foreach (var child in current.Children)
                {
                    result.AddRange(QueryNodesRecursive(child, pathParts, level + 1));
                }
            }
        }

        return result;
    }

    public static void Main(string[] args)
    {
        string inputPath = "wsu.xml";
        string outputPath = "labeled_wsu.xml"; 

        XElement rootElement = XElement.Load(inputPath);
        XmlNode rootNode = BuildTree(rootElement);

        ReLab relab = new ReLab();

        Stopwatch stopwatch = new Stopwatch();
        
        
        stopwatch.Start();
        long initialMemory = GetMemoryUsage();

        relab.LabelTree(rootNode);

        long finalMemory = GetMemoryUsage();
        stopwatch.Stop();
        
        Console.WriteLine($"Initial labeling time: {stopwatch.ElapsedMilliseconds} ms");
        Console.WriteLine($"Memory Used During Labeling: {finalMemory - initialMemory} KB");

        // Insert a new node
        XElement newElement = new XElement("newCourse",
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
        XmlNode newNode = new XmlNode("newCourse", newElement);
        
        relab.InsertNode(rootNode, newNode);
        stopwatch.Restart();
        initialMemory = GetMemoryUsage();
        //relab.InsertNode(rootNode, newNode);
        relab.LabelTree(rootNode);

        finalMemory = GetMemoryUsage();
        stopwatch.Stop();

        Console.WriteLine($"Time taken to label new insert node: {stopwatch.ElapsedMilliseconds} ms");
        Console.WriteLine($"Memory Used During Relabeling After Insertion: {finalMemory - initialMemory} KB");

        ExportLabeledXml(rootNode, outputPath);
        Console.WriteLine($"Labeled XML has been saved to {outputPath}");
     
    }

    public static long GetMemoryUsage()
    {
        return Process.GetCurrentProcess().WorkingSet64 / 1024; // KB
    }
}