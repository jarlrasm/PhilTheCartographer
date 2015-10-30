using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;

using Phil.Analyzers;

namespace Phil.Extensions
{
    static public class SemanticModelExtensions
    {
        public static IEnumerable<TypedSymbol> GetTypeSymbols(this SemanticModel semanticModel, SyntaxNode node, TypeInfo typeSymbol)
        {
            var typesymbol = semanticModel.LookupSymbols(node.SpanStart)
                .OfType<ILocalSymbol>().
                Where(x => x.Type != typeSymbol.Type)
                .Where(x => x.Locations.First().GetLineSpan().StartLinePosition < node.GetLocation().GetLineSpan().StartLinePosition)
                .Select(x => new TypedSymbol { Name = x.Name, Type = x.Type })
                .Concat(
                    semanticModel.LookupSymbols(node.SpanStart)
                        .OfType<IParameterSymbol>()
                        .Where(x => x.Type != typeSymbol.Type)
                        .Select(x => new TypedSymbol { Name = x.Name, Type = x.Type })
                ).Concat(
                    semanticModel.LookupSymbols(node.SpanStart)
                        .OfType<IPropertySymbol>()
                        .Where(x => x.Type != typeSymbol.Type)
                        .Select(x => new TypedSymbol { Name = x.Name, Type = x.Type })
                ).Concat(
                    semanticModel.LookupSymbols(node.SpanStart)
                        .OfType<IFieldSymbol>()
                        .Where(x => x.Type != typeSymbol.Type)
                        .Select(x => new TypedSymbol { Name = x.Name, Type = x.Type })
                );
            return typesymbol;
        }
    }
}