using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Phil.Refactorings;

namespace Phil.Extensions
{
    public static class SemanticModelExtensions
    {
        public static IEnumerable<TypedSymbol> GetTypeSymbols(this SemanticModel semanticModel, SyntaxNode node)
        {
            var typesymbols = semanticModel.LookupSymbols(node.SpanStart)
                .OfType<ILocalSymbol>()
                .Where(x => x.Locations.Any() && x.Locations.First().GetLineSpan().StartLinePosition < node.GetLocation().GetLineSpan().StartLinePosition)
                .Select(x => new TypedSymbol { Name = x.Name, Type = x.Type })
                .Concat(
                    semanticModel.LookupSymbols(node.SpanStart)
                        .OfType<IParameterSymbol>()
                        .Select(x => new TypedSymbol { Name = x.Name, Type = x.Type })
                ).Concat(
                    semanticModel.LookupSymbols(node.SpanStart)
                        .OfType<IPropertySymbol>()
                        .Select(x => new TypedSymbol { Name = x.Name, Type = x.Type })
                ).Concat(
                    semanticModel.LookupSymbols(node.SpanStart)
                        .OfType<IFieldSymbol>()
                        .Select(x => new TypedSymbol { Name = x.Name, Type = x.Type })
                );
            if (!semanticModel.GetEnclosingSymbol(node.SpanStart).IsStatic)
            {
                var classDeclaration = node.Ancestors().OfType<ClassDeclarationSyntax>().FirstOrDefault();
                if(classDeclaration!=null)
                { 
                    typesymbols = typesymbols.Concat(new TypedSymbol[]
                    {
                        new TypedSymbol()
                        {Name = "this",Type =  (ITypeSymbol)semanticModel.GetDeclaredSymbol(classDeclaration)}
                    });
                }
            }
            return typesymbols;

        }
        public static IEnumerable<TypedSymbol> GetTypeSymbols(this SemanticModel semanticModel, SyntaxNode node, TypeInfo ignoredTypeSymbol)
        {
            return semanticModel.GetTypeSymbols(node).Where(x=>!x.Type.Equals(ignoredTypeSymbol.Type));
        }
    }
}