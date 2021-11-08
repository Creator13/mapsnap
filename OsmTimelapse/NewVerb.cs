// ReSharper disable ClassNeverInstantiated.Global

using CommandLine;

namespace OsmTimelapse.CommandLine;

[Verb("init", HelpText = "Create and initialize a new project in the current folder.")]
public record NewOptions
{
    
    [Value(0, MetaName = "Name", HelpText = "The name for this project", Required = true)] 
    public string Name { get; init; }

    [Value(1, MetaName = "Coordinate A", HelpText = "An arbitrary coordinate", Required = true)] 
    public string CoordA { get; init; }

    [Value(2, MetaName = "Coordinate B", HelpText = "An arbitrary coordinate different from A", Required = true)]
    public string CoordB { get; init; }

    [Option('t', "file-type", Required = false)]
    public string FileType { get; init; }

    [Option('f', "name-format", Required = false)] 
    public string NameFormat { get; init; }
}
