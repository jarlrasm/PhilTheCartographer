using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Phil.Analyzers;

namespace Phil.Extensions
{
    public static class SymbolExtensions
    {
        public static string PropertyName(this ISymbol symbol) => symbol.Name.Substring("set_".Length);
        public static ExpressionSyntax DefaultExpression(this ITypeSymbol type, string argname)
        {
            if (type.ToString() == "int")
                return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(0));
            if (type.ToString() == "long")
                return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(0));
            if (type.ToString() == "decimal")
                return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(0));
            if (type.ToString() == "string")
                return SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(argname));
            if (type.ToString() == "bool")
                return SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression);
            if (type.IsReferenceType || type.Name == "Nullable")
                return SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression);
            return SyntaxFactory.DefaultExpression(SyntaxFactory.ParseTypeName(type.ToString()));
        }
        public static bool IsMissing(this IPropertySymbol symbol, IEnumerable<IMethodSymbol> missingprops)
        {
            return missingprops.Any(x => Compare(symbol, x));
        }


        private static bool Compare(IPropertySymbol symbol, IMethodSymbol property)
        {
            return property.PropertyName() == symbol.Name && property.Parameters.First().Type.Equals(symbol.Type);
        }
        public static bool IsMissing(this IPropertySymbol symbol, IEnumerable<IParameterSymbol> mssingParameters)
        {
            return mssingParameters.Any(x => Compare(symbol, x));
        }

        private static bool Compare(IPropertySymbol symbol, IParameterSymbol parameter)
        {
            return parameter.Name.ToLower() == symbol.Name.ToLower() && parameter.Type.Equals(symbol.Type);
        }
        public static bool IsMissing(this IPropertySymbol symbol, IEnumerable<TypedSymbol> missingprops)
        {
            return missingprops.Any(x => Compare(symbol, x));
        }
        private static bool Compare(IPropertySymbol symbol, TypedSymbol parameter)
        {
            return parameter.Name.ToLower() == symbol.Name.ToLower() && parameter.Type.Equals(symbol.Type);
        }

        public static bool IsMissing(this IPropertySymbol symbol, IEnumerable<IPropertySymbol> mssingParameters)
        {
            return mssingParameters.Any(x => Compare(symbol, x));
        }

        private static bool Compare(IPropertySymbol symbol, IPropertySymbol parameter)
        {
            return parameter.Name.ToLower() == symbol.Name.ToLower() && parameter.Type.Equals(symbol.Type);
        }
    }
}
