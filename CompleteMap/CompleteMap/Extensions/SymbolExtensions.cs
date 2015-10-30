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
    }
}
