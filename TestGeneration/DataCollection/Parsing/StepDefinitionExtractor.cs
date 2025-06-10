using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using DataCollection.Models;
using DataCollection.Utils;

namespace DataCollection.Parsing;

public class StepDefinitionExtractor
{
    private static readonly string[] StepAttributes = { "Given", "When", "Then" };

    public List<StepDefinitionPair> ExtractPairsFromDirectory(string directoryPath)
    {
        var pairs = new List<StepDefinitionPair>();
        var csFiles = Directory.GetFiles(directoryPath, "*.cs", SearchOption.AllDirectories);

        foreach (var file in csFiles)
        {
            var text = File.ReadAllText(file);
            var tree = CSharpSyntaxTree.ParseText(text);
            var root = tree.GetCompilationUnitRoot();

            var methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>();

            foreach (var method in methods)
            {
                var attributes = method.AttributeLists.SelectMany(a => a.Attributes);
                foreach (var attr in attributes)
                {
                    var attrName = attr.Name.ToString();

                    if (StepAttributes.Any(sa => attrName.Contains(sa)))
                    {
                        var attrText = attr.ToFullString().Trim();
                        var signature = method.Identifier.Text + method.ParameterList.ToString();
                        var body = method.Body?.ToFullString().Trim() ?? "{}";
                        
                        if (!MethodBodyFilter.HasRealCode(body))
                            continue;
                        
                        pairs.Add(new StepDefinitionPair
                        {
                            AttributeText = attrText,
                            MethodSignature = signature,
                            MethodBody = body,
                            SourceFile = file
                        });

                        break;
                    }
                }
            }
        }

        return pairs;
    }
}