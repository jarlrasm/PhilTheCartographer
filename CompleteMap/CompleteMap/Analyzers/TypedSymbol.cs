using Microsoft.CodeAnalysis;

namespace Phil.Analyzers
{
    public class TypedSymbol
    {
        public string Name { get; internal set; }
        public ITypeSymbol Type { get; internal set; }
    }
}