using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

using Phil.Extensions;

namespace Phil.Refactorings
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp)]
    public class FillConstrutorRefactoring : CodeRefactoringProvider
    {
        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var document = context.Document;
            var textSpan = context.Span;
            var cancellationToken = context.CancellationToken;
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var token = root.FindToken(textSpan.Start);
            if (token.Parent == null)
            {
                return;
            }
            var node = token.Parent.FirstAncestorOrSelf<ArgumentListSyntax>();

            var createExpression = node?.Parent as ObjectCreationExpressionSyntax;
            if (createExpression == null)
                return;
            var semanticModel = (SemanticModel)await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var typeSymbol = ModelExtensions.GetTypeInfo(semanticModel, createExpression);


            var constructors = typeSymbol.Type.GetMembers().Where(x => x.Kind == SymbolKind.Method).Cast<IMethodSymbol>().Where(x => x.MethodKind == MethodKind.Constructor);

            constructors = constructors.Where(x => HasMoreArguments(x, node.Arguments, semanticModel));

            foreach (var constructor in constructors)
            {
                ReportForUnimplemented(context, constructor, semanticModel, node, typeSymbol);
            }
        }
        private static void ReportForUnimplemented(CodeRefactoringContext context, IMethodSymbol constructor, SemanticModel semanticModel, ArgumentListSyntax node, TypeInfo typeSymbol)
        {
            var unimplemntedParameters = constructor.Parameters.Skip(node.Arguments.Count);
            var typesymbols = semanticModel.GetTypeSymbols(node, typeSymbol);

            var symbols = typesymbols.Where(x => ImplementsSomethingFor(x.Type, unimplemntedParameters))
                .Distinct();


            foreach(var symbol in symbols)
            context.RegisterRefactoring(
                new FixConstructor(constructor, node, symbol.Name, semanticModel, context.Document));
        }


        private class FixConstructor : CodeAction
        {
            private readonly IMethodSymbol constructor;
            private readonly ArgumentListSyntax node;
            private readonly string sourcename;
            private readonly SemanticModel semanticModel;
            private readonly Document document;


            public FixConstructor(IMethodSymbol constructor, ArgumentListSyntax node, string sourcename, SemanticModel semanticModel, Document document)
            {
                this.constructor = constructor;
                this.node = node;
                this.sourcename = sourcename;
                this.semanticModel = semanticModel;
                this.document = document;
                Title = $"Fill constructor from {sourcename}";
            }


            public override string Title { get; }


            protected override async Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
            {
                var targetTypeInfo = semanticModel.GetTypeInfo(node.Parent, cancellationToken);

                ITypeSymbol typesymbol = targetTypeInfo.Type ?? semanticModel.GetDeclaredSymbol(node.Parent).ContainingType;//this really can't be right... but it works... I have to learn how to do this at one point..
                var sourceType = this.node.GetSourceType(sourcename, semanticModel, typesymbol);

                var newExpression = ImplementConstructorFromExpression(node, sourcename, sourceType);
                var root = await document.GetSyntaxRootAsync(cancellationToken);
                var newroot = root.ReplaceNode(node, newExpression);
                return document.WithSyntaxRoot(newroot);
            }


            private  SyntaxNode ImplementConstructorFromExpression(ArgumentListSyntax expression,
                                                                             string sourcename,
                                                                             ITypeSymbol sourceType)
            {
                var newExpression = expression;
                foreach (var param in constructor.Parameters.Skip(expression.Arguments.Count()))
                {
                    var identifier =
                        sourceType.GetMembers().Where(x => x.Kind == SymbolKind.Property).Cast<IPropertySymbol>().FirstOrDefault(
                            x => x.Type == param.Type && x.Name.ToLower() == param.Name.ToLower());
                    if (identifier != null)
                    {
                        newExpression =
                            newExpression.AddArguments(
                                SyntaxFactory.Argument(SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                                                                            SyntaxFactory.IdentifierName(sourcename),
                                                                                            SyntaxFactory.IdentifierName(identifier.Name))));
                    }
                    else
                    {
                        newExpression =
                            newExpression.AddArguments(SyntaxFactory.Argument(param.Type.DefaultExpression(param.Name)));
                    }
                }
                return newExpression;
            }
            
        
        }
        private static bool ImplementsSomethingFor(ITypeSymbol type, IEnumerable<IParameterSymbol> mssingParameters)
        {
            return type.GetMembers().Where(x => x.Kind == SymbolKind.Property).Cast<IPropertySymbol>().Any(x => x.IsMissing(mssingParameters));
        }


        private static bool HasMoreArguments(IMethodSymbol constructor, SeparatedSyntaxList<ArgumentSyntax> arguments, SemanticModel semanticModel)
        {
            if (constructor.Parameters.Count() <= arguments.Count)
                return false;
            for (int i = 0; i > arguments.Count; i++)
            {
                var argtype = semanticModel.GetTypeInfo(arguments[i]);
                if (constructor.Parameters[i].Type != argtype.Type)
                    return false;
            }
            return true;
        }
    }
}
