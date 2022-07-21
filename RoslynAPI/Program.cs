// See https://aka.ms/new-console-template for more information

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

var sourceCode = $@"
using System;

namespace A;

class B
{{
    public void C()
    {{
        Console.WriteLine(D.Something);
    }}
}}

static class D
{{
    public static string Something = ""A"";
}}
";

var workspace = new AdhocWorkspace();
var project = workspace.AddProject("Sample", LanguageNames.CSharp);

project = project.AddMetadataReferences(new[] {
    MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
    // NOTES: Console comes from System.Console
    MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
});

workspace.TryApplyChanges(project.Solution);
var doc = workspace.AddDocument(project.Id, "Roslyn", SourceText.From(sourceCode));

var semanticModel = await doc.GetSemanticModelAsync();

new Walker(semanticModel).Visit((await doc.GetSyntaxTreeAsync())?.GetRoot());

class Walker : CSharpSyntaxWalker
{
    private readonly SemanticModel? _semanticModel;

    public Walker(SemanticModel? semanticModel)
    {
        _semanticModel = semanticModel;
    }
    
    /// <summary>Called when the visitor visits a IdentifierNameSyntax node.</summary>
    public override void VisitIdentifierName(IdentifierNameSyntax node)
    {
        var symbol = _semanticModel.GetSymbolInfo(node).Symbol;

        if(symbol is INamedTypeSymbol namedTypeSymbol)
        {
            Console.WriteLine($"{node}: {namedTypeSymbol.TypeKind}");
        }
        else
        {
            Console.WriteLine($"{node}: {symbol?.Kind}");
        }
        
        base.VisitIdentifierName(node);
    }
}
