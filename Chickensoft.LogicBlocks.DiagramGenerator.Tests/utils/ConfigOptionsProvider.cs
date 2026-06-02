namespace Chickensoft.LogicBlocks.DiagramGenerator.Tests;

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

public class ConfigOptionsProvider(AnalyzerConfigOptions options) : AnalyzerConfigOptionsProvider
{
	public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => GlobalOptions;

  public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => GlobalOptions;

  public override AnalyzerConfigOptions GlobalOptions { get; } = options;
}

public class ConfigOptions(Dictionary<string, string> optionsDict) : AnalyzerConfigOptions
{
	public override bool TryGetValue(string key, [NotNullWhen(true)] out string? value) => optionsDict.TryGetValue(key, out value);
}
