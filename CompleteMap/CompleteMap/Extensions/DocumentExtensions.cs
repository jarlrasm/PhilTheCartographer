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
                                                     Func<T, string, ITypeSymbol, SemanticModel, ITypeSymbol, SyntaxNode> implement
                                                     )
            where T : SyntaxNode
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
            
            var targetTypeInfo = semanticModel.GetTypeInfo(expression.Parent, cancellationToken);

            ITypeSymbol typesymbol = targetTypeInfo.Type??semanticModel.GetDeclaredSymbol(expression.Parent).ContainingType;//this really can't be right... but it works... I have to learn how to do this at one point..
            var sourceType = GetSourceType(expression, sourcename, semanticModel, typesymbol);

            var newExpression = implement(expression, sourcename, typesymbol, semanticModel, sourceType);
            var root = await document.GetSyntaxRootAsync(cancellationToken);
            var newroot = root.ReplaceNode(expression, newExpression);
            return document.WithSyntaxRoot(newroot);
        }


        public static ITypeSymbol GetSourceType(this SyntaxNode expression, string sourcename, SemanticModel semanticModel, ITypeSymbol typeSymbol)
        {
            if (sourcename=="this")
            {
                return (ITypeSymbol)semanticModel.GetDeclaredSymbol(expression.Ancestors().OfType<ClassDeclarationSyntax>().First());
            }
            var sourceType = semanticModel.LookupSymbols(expression.SpanStart)
                .OfType<ILocalSymbol>()
                .Where(x => x.Type != typeSymbol)
                .Where(x => x.Name == sourcename)
                .Select(x => x.Type)
                .Concat(
                    semanticModel.LookupSymbols(expression.SpanStart)
                        .OfType<IParameterSymbol>()
                        .Where(x => x.Type != typeSymbol)
                        .Where(x => x.Name == sourcename)
                        .Select(x => x.Type)
                )
                .Concat(
                    semanticModel.LookupSymbols(expression.SpanStart)
                        .OfType<IPropertySymbol>()
                        .Where(x => x.Type != typeSymbol)
                        .Where(x => x.Name == sourcename)
                        .Select(x => x.Type)
                )
                .Concat(
                    semanticModel.LookupSymbols(expression.SpanStart)
                        .OfType<IFieldSymbol>()
                        .Where(x => x.Type != typeSymbol)
                        .Where(x => x.Name == sourcename)
                        .Select(x => x.Type)
                )
                .First();
            return sourceType;
        }
    }
}
