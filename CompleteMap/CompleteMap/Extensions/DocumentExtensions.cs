using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Phil.Extensions
{
    public static class DocumentExtensions
    {
        public static async Task<Document> RunFix<T>(this Document document,
                                                     T expression,
                                                     CancellationToken cancellationToken,
                                                     string sourcename,
                                                     Func<T, string, TypeInfo, SemanticModel, ITypeSymbol, SyntaxNode> implement)
            where T : SyntaxNode
        {
            var createExpression = expression.Parent as ObjectCreationExpressionSyntax;
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
            var targetTypeInfo = semanticModel.GetTypeInfo(createExpression, cancellationToken);

            var sourceType = GetSourceType(expression, sourcename, semanticModel, targetTypeInfo);

            var newExpression = implement(expression, sourcename, targetTypeInfo, semanticModel, sourceType);
            var root = await document.GetSyntaxRootAsync();
            var newroot = root.ReplaceNode(expression, newExpression);
            return document.WithSyntaxRoot(newroot);
        }


        private static ITypeSymbol GetSourceType(SyntaxNode expression, string sourcename, SemanticModel semanticModel, TypeInfo typeSymbol)
        {
            if (sourcename=="this")
            {
                return (ITypeSymbol)semanticModel.GetDeclaredSymbol(expression.Ancestors().OfType<ClassDeclarationSyntax>().First());
            }
            var sourceType = semanticModel.LookupSymbols(expression.SpanStart)
                .OfType<ILocalSymbol>()
                .Where(x => x.Type != typeSymbol.Type)
                .Where(x => x.Name == sourcename)
                .Select(x => x.Type)
                .Concat(
                    semanticModel.LookupSymbols(expression.SpanStart)
                        .OfType<IParameterSymbol>()
                        .Where(x => x.Type != typeSymbol.Type)
                        .Where(x => x.Name == sourcename)
                        .Select(x => x.Type)
                )
                .Concat(
                    semanticModel.LookupSymbols(expression.SpanStart)
                        .OfType<IPropertySymbol>()
                        .Where(x => x.Type != typeSymbol.Type)
                        .Where(x => x.Name == sourcename)
                        .Select(x => x.Type)
                )
                .Concat(
                    semanticModel.LookupSymbols(expression.SpanStart)
                        .OfType<IFieldSymbol>()
                        .Where(x => x.Type != typeSymbol.Type)
                        .Where(x => x.Name == sourcename)
                        .Select(x => x.Type)
                )
                .First();
            return sourceType;
        }
    }
}
