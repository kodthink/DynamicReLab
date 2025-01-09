using System;
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
        string inputPath = "SwissProt.xml"; 
        string outputPath = "labeled_SwissProt.xml"; 

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
        XElement newElement = new XElement("Entry",
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
        XmlNode newNode = new XmlNode("Entry", newElement);
        
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
