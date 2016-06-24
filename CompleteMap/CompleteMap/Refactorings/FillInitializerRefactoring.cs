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
    public class FillInitializerRefactoring : CodeRefactoringProvider
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
            var node = token.Parent.FirstAncestorOrSelf<InitializerExpressionSyntax>();
            

            var createExpression = node?.Parent as ObjectCreationExpressionSyntax;
            if (createExpression == null)
                return;
            var semanticModel = (SemanticModel)await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var typeSymbol = ModelExtensions.GetTypeInfo(semanticModel, createExpression);
            var properties = typeSymbol.Type.GetBaseTypesAndThis().SelectMany(x => x.GetMembers()).Where(x => x.Kind == SymbolKind.Method).Cast<IMethodSymbol>().Where(x => x.MethodKind == MethodKind.PropertySet);

            var unimplemntedProperties =
                properties.Where(
                    x =>
                        node.Expressions.All(
                            y => ((IdentifierNameSyntax)((AssignmentExpressionSyntax)y).Left).Identifier.Text != x.PropertyName()));
            if (unimplemntedProperties.Any())
            {
                ReportForUnimplemented(context, unimplemntedProperties, semanticModel, node, typeSymbol);
            }
        }
        private static void ReportForUnimplemented(CodeRefactoringContext context,
                                                   IEnumerable<IMethodSymbol> unimplemntedProperties,
                                                   SemanticModel semanticModel,
                                                   InitializerExpressionSyntax node,
                                                   TypeInfo typeSymbol)
        {
            var typesymbols = semanticModel.GetTypeSymbols(node, typeSymbol);
            var symbols = typesymbols.Where(x => ImplementsSomethingFor(x.Type, unimplemntedProperties))
                .Distinct();


            foreach (var symbol in symbols)
                context.RegisterRefactoring(
                    new FillInitializerFrom(node, symbol.Name, semanticModel, context.Document));

            context.RegisterRefactoring(
                new FillInitializer(node, semanticModel, context.Document));
        }

        private static bool ImplementsSomethingFor(ITypeSymbol type, IEnumerable<IMethodSymbol> missingprops)
        {
            return type.GetBaseTypesAndThis().SelectMany(x => x.GetMembers()).Where(x => x.Kind == SymbolKind.Property).Cast<IPropertySymbol>().Any(x => x.IsMissing(missingprops));
        }
        private class FillInitializer: CodeAction
        {
            private readonly InitializerExpressionSyntax node;
            private readonly SemanticModel semanticModel;
            private readonly Document document;


            public FillInitializer(InitializerExpressionSyntax node, SemanticModel semanticModel, Document document)
            {
                this.node = node;
                this.semanticModel = semanticModel;
                this.document = document;
                Title = $"Fill in blanks";
            }


            public override string Title { get; }


            protected override async Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
            {
                var createExpression = node.Parent as ObjectCreationExpressionSyntax;
                var typeSymbol = semanticModel.GetTypeInfo(createExpression, cancellationToken).Type;
                var missingprops = GetMissingProperties(node, typeSymbol);


                var newExpression = node.AddExpressions(missingprops.Select(x =>
                    SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                    SyntaxFactory.IdentifierName(x.PropertyName()), x.Parameters.First().Type.DefaultExpression(x.PropertyName()))).Cast<ExpressionSyntax>().ToArray());

                var root = await document.GetSyntaxRootAsync();
                var newroot = root.ReplaceNode(node, newExpression);
                return document.WithSyntaxRoot(newroot);
            }
            

        }

        private class FillInitializerFrom : CodeAction
        {
            private readonly InitializerExpressionSyntax node;
            private readonly string sourcename;
            private readonly SemanticModel semanticModel;
            private readonly Document document;


            public FillInitializerFrom(InitializerExpressionSyntax node, string sourcename, SemanticModel semanticModel, Document document)
            {
                this.node = node;
                this.sourcename = sourcename;
                this.semanticModel = semanticModel;
                this.document = document;
                Title = $"Fill from {sourcename}";
            }


            public override string Title { get; }


            protected override async Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
            {
                var targetTypeInfo = semanticModel.GetTypeInfo(node.Parent, cancellationToken);

                ITypeSymbol typesymbol = targetTypeInfo.Type ?? semanticModel.GetDeclaredSymbol(node.Parent).ContainingType;//this really can't be right... but it works... I have to learn how to do this at one point..
                var sourceType = this.node.GetSourceType(sourcename, semanticModel, typesymbol);

                var newExpression = ImplementAllSettersFromExpression(node, sourcename, typesymbol, sourceType);
                var root = await document.GetSyntaxRootAsync(cancellationToken);
                var newroot = root.ReplaceNode(node, newExpression);
                return document.WithSyntaxRoot(newroot);
            }

            private static SyntaxNode ImplementAllSettersFromExpression(InitializerExpressionSyntax expression,
                                                                                     string sourcename,
                                                                                     ITypeSymbol targetTypeInfo,
                                                                                     ITypeSymbol sourceType)
            {
                var missingprops = GetMissingProperties(expression, targetTypeInfo);

                var newproperties =
                    sourceType.GetBaseTypesAndThis().SelectMany(x => x.GetMembers()).Where(x => x.Kind == SymbolKind.Property).Cast<IPropertySymbol>().Where(x => x.IsMissing(missingprops));
                var newExpression = expression.AddExpressions(
                    newproperties.Select(x =>
                                             SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                                                                                SyntaxFactory.IdentifierName(x.Name),
                                                                                SyntaxFactory.MemberAccessExpression(
                                                                                    SyntaxKind.SimpleMemberAccessExpression,
                                                                                    SyntaxFactory.IdentifierName(sourcename),
                                                                                    SyntaxFactory.IdentifierName(x.Name))))
                        .Cast<ExpressionSyntax>().ToArray());
                return newExpression;
            }

        }
        private static IEnumerable<IMethodSymbol> GetMissingProperties(InitializerExpressionSyntax expression, ITypeSymbol typeSymbol)
        {
            var properties =
                typeSymbol.GetBaseTypesAndThis().SelectMany(x => x.GetMembers()).Where(x => x.Kind == SymbolKind.Method).Cast<IMethodSymbol>().Where(
                    x => x.MethodKind == MethodKind.PropertySet);

            var missingprops = GetUnimplemntedProperties(expression, properties);
            return missingprops;
        }

        private static IEnumerable<IMethodSymbol> GetUnimplemntedProperties(InitializerExpressionSyntax expression, IEnumerable<IMethodSymbol> properties)
        {
            var uniimplemntedProperties =
                properties.Where(
                    x =>
                        expression.Expressions.All(
                            y => ((IdentifierNameSyntax)((AssignmentExpressionSyntax)y).Left).Identifier.Text != x.PropertyName()));
            return uniimplemntedProperties;
        }

    }
}
