using System.Text;
using System.Xml;

namespace McpTools.Confluence.Services;

/// <summary>
/// Generates draw.io-compatible mxGraph XML from a simple list of nodes and edges.
/// The output can be uploaded as a draw.io attachment to Confluence.
/// </summary>
public static class MxGraphBuilder
{
    /// <summary>
    /// Builds a complete draw.io XML file from the given nodes and edges.
    /// </summary>
    public static string Build(IReadOnlyList<DiagramNode> nodes, IReadOnlyList<DiagramEdge> edges)
    {
        var sb = new StringBuilder();
        using var writer = XmlWriter.Create(sb, new XmlWriterSettings
        {
            Indent = true,
            OmitXmlDeclaration = true,
        });

        writer.WriteStartElement("mxfile");
        writer.WriteAttributeString("host", "Confluence");
        writer.WriteAttributeString("type", "device");

        writer.WriteStartElement("diagram");
        writer.WriteAttributeString("name", "Page-1");
        writer.WriteAttributeString("id", "page-1");

        writer.WriteStartElement("mxGraphModel");
        writer.WriteAttributeString("dx", "1000");
        writer.WriteAttributeString("dy", "800");
        writer.WriteAttributeString("grid", "1");
        writer.WriteAttributeString("gridSize", "10");
        writer.WriteAttributeString("guides", "1");
        writer.WriteAttributeString("tooltips", "1");
        writer.WriteAttributeString("connect", "1");
        writer.WriteAttributeString("arrows", "1");
        writer.WriteAttributeString("fold", "1");
        writer.WriteAttributeString("page", "1");
        writer.WriteAttributeString("pageScale", "1");
        writer.WriteAttributeString("pageWidth", "1100");
        writer.WriteAttributeString("pageHeight", "850");

        writer.WriteStartElement("root");

        // Default parent cells (required by mxGraph)
        writer.WriteStartElement("mxCell");
        writer.WriteAttributeString("id", "0");
        writer.WriteEndElement();

        writer.WriteStartElement("mxCell");
        writer.WriteAttributeString("id", "1");
        writer.WriteAttributeString("parent", "0");
        writer.WriteEndElement();

        // Nodes
        foreach (var node in nodes)
        {
            WriteNode(writer, node);
        }

        // Edges
        foreach (var edge in edges)
        {
            WriteEdge(writer, edge);
        }

        writer.WriteEndElement(); // root
        writer.WriteEndElement(); // mxGraphModel
        writer.WriteEndElement(); // diagram
        writer.WriteEndElement(); // mxfile

        writer.Flush();
        return sb.ToString();
    }

    private static void WriteNode(XmlWriter writer, DiagramNode node)
    {
        var style = node.Shape switch
        {
            NodeShape.Rectangle     => "rounded=0;whiteSpace=wrap;html=1;",
            NodeShape.RoundedRect   => "rounded=1;whiteSpace=wrap;html=1;",
            NodeShape.Ellipse       => "ellipse;whiteSpace=wrap;html=1;",
            NodeShape.Diamond       => "rhombus;whiteSpace=wrap;html=1;",
            NodeShape.Cylinder      => "shape=cylinder3;whiteSpace=wrap;html=1;boundedLbl=1;backgroundOutline=1;size=15;",
            NodeShape.Hexagon       => "shape=hexagon;perimeter=hexagonPerimeter2;whiteSpace=wrap;html=1;fixedSize=1;size=20;",
            NodeShape.Parallelogram => "shape=parallelogram;perimeter=parallelogramPerimeter;whiteSpace=wrap;html=1;fixedSize=1;size=20;",
            NodeShape.StartEnd      => "rounded=1;whiteSpace=wrap;html=1;arcSize=50;",
            _                       => "rounded=1;whiteSpace=wrap;html=1;",
        };

        if (!string.IsNullOrEmpty(node.FillColor))
            style += $"fillColor={node.FillColor};";
        if (!string.IsNullOrEmpty(node.FontColor))
            style += $"fontColor={node.FontColor};";
        if (!string.IsNullOrEmpty(node.StrokeColor))
            style += $"strokeColor={node.StrokeColor};";

        writer.WriteStartElement("mxCell");
        writer.WriteAttributeString("id", node.Id);
        writer.WriteAttributeString("value", node.Label);
        writer.WriteAttributeString("style", style);
        writer.WriteAttributeString("vertex", "1");
        writer.WriteAttributeString("parent", "1");

        writer.WriteStartElement("mxGeometry");
        writer.WriteAttributeString("x", node.X.ToString());
        writer.WriteAttributeString("y", node.Y.ToString());
        writer.WriteAttributeString("width", node.Width.ToString());
        writer.WriteAttributeString("height", node.Height.ToString());
        writer.WriteAttributeString("as", "geometry");
        writer.WriteEndElement(); // mxGeometry

        writer.WriteEndElement(); // mxCell
    }

    private static void WriteEdge(XmlWriter writer, DiagramEdge edge)
    {
        var style = edge.Style switch
        {
            EdgeStyle.Solid  => "rounded=1;orthogonalLoop=1;jettySize=auto;html=1;entryX=0;entryY=0.5;entryDx=0;entryDy=0;exitX=1;exitY=0.5;exitDx=0;exitDy=0;",
            EdgeStyle.Dashed => "rounded=1;orthogonalLoop=1;jettySize=auto;html=1;dashed=1;entryX=0;entryY=0.5;entryDx=0;entryDy=0;exitX=1;exitY=0.5;exitDx=0;exitDy=0;",
            _                => "rounded=1;orthogonalLoop=1;jettySize=auto;html=1;",
        };

        writer.WriteStartElement("mxCell");
        writer.WriteAttributeString("id", edge.Id);
        writer.WriteAttributeString("value", edge.Label ?? "");
        writer.WriteAttributeString("style", style);
        writer.WriteAttributeString("edge", "1");
        writer.WriteAttributeString("parent", "1");
        writer.WriteAttributeString("source", edge.SourceId);
        writer.WriteAttributeString("target", edge.TargetId);

        writer.WriteStartElement("mxGeometry");
        writer.WriteAttributeString("relative", "1");
        writer.WriteAttributeString("as", "geometry");
        writer.WriteEndElement(); // mxGeometry

        writer.WriteEndElement(); // mxCell
    }
}

// ─── Diagram primitives ──────────────────────────────────────────────────────

public sealed record DiagramNode(
    string    Id,
    string    Label,
    int       X       = 0,
    int       Y       = 0,
    int       Width   = 140,
    int       Height  = 60,
    NodeShape Shape   = NodeShape.RoundedRect,
    string?   FillColor   = null,
    string?   FontColor   = null,
    string?   StrokeColor = null);

public sealed record DiagramEdge(
    string     Id,
    string     SourceId,
    string     TargetId,
    string?    Label = null,
    EdgeStyle  Style = EdgeStyle.Solid);

public enum NodeShape
{
    Rectangle,
    RoundedRect,
    Ellipse,
    Diamond,
    Cylinder,
    Hexagon,
    Parallelogram,
    StartEnd,
}

public enum EdgeStyle
{
    Solid,
    Dashed,
}
