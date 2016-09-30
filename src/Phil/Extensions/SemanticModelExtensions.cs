using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Phil.Core;
using Phil.Refactorings;

namespace Phil.Extensions
{
    public static class SemanticModelExtensions
    {
        public static IEnumerable<NamedSymbol> GetTypeSymbols(this SemanticModel semanticModel, SyntaxNode node)
        {
            var typesymbols = semanticModel.LookupSymbols(node.SpanStart)
                .OfType<ILocalSymbol>()
                .Where(x => x.Locations.Any() && x.Locations.First().GetLineSpan().StartLinePosition < node.GetLocation().GetLineSpan().StartLinePosition)
                .Select(x => new NamedSymbol( x.Name,  x.Type))
                .Concat(
                    semanticModel.LookupSymbols(node.SpanStart)
                        .OfType<IParameterSymbol>()
                        .Select(x => new NamedSymbol( x.Name,  x.Type))
                ).Concat(
                    semanticModel.LookupSymbols(node.SpanStart)
                        .OfType<IPropertySymbol>()
                        .Select(x => new NamedSymbol( x.Name,  x.Type))
                ).Concat(
                    semanticModel.LookupSymbols(node.SpanStart)
                        .OfType<IFieldSymbol>()
                        .Select(x => new NamedSymbol( x.Name,  x.Type))
                );
            if (!semanticModel.GetEnclosingSymbol(node.SpanStart).IsStatic)
            {
                var classDeclaration = node.Ancestors().OfType<ClassDeclarationSyntax>().FirstOrDefault();
                if(classDeclaration!=null)
                { 
                    typesymbols = typesymbols.Concat(new NamedSymbol[]
                    {
                        new NamedSymbol( "this",(ITypeSymbol)semanticModel.GetDeclaredSymbol(classDeclaration))
                    });
                }
            }
            return typesymbols;

        }
        public static IEnumerable<NamedSymbol> GetTypeSymbols(this SemanticModel semanticModel, SyntaxNode node, TypeInfo ignoredTypeSymbol)
        {
            return semanticModel.GetTypeSymbols(node).Where(x=>!x.TypeSymbol.Equals(ignoredTypeSymbol.Type));
        }
    }
}