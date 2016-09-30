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

using Phil.Core;
using Phil.Extensions;

namespace Phil.Refactorings
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp)]
    public class FillConstrutorBodyRefactoring : CodeRefactoringProvider
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
            var node = token.Parent.FirstAncestorOrSelf <ConstructorDeclarationSyntax>();
            if (node != null)
            {
                var semanticModel = (SemanticModel)await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                
                var body = node.Body;
                
                var properties = semanticModel.GetTypeSymbols(node)
                    .Where(x => x.Name != "this");
                var unimplemntedProperties =
                    properties.Where(
                        x =>
                            body.Statements.OfType<ExpressionStatementSyntax>()
                            .Where(ex => ex.Expression is AssignmentExpressionSyntax)
                            .Select(ex => ex.Expression as AssignmentExpressionSyntax)
                            .All(
                                y => GetNameSyntax(y)?.Identifier.Text != x.Name));
                if (unimplemntedProperties.Any())
                {
                    ReportForUnimplemented(context, unimplemntedProperties, semanticModel, node);
                }
            }
        }
        private static SimpleNameSyntax GetNameSyntax(AssignmentExpressionSyntax assignmentExpressionSyntax)
        {
            if (assignmentExpressionSyntax.Left is IdentifierNameSyntax)
                return assignmentExpressionSyntax.Left as IdentifierNameSyntax;
            return (assignmentExpressionSyntax.Left as MemberAccessExpressionSyntax)?.Name;
        }


        private static void ReportForUnimplemented(CodeRefactoringContext context, IEnumerable<NamedSymbol> unimplemntedProperties, SemanticModel semanticModel, ConstructorDeclarationSyntax node)
        {
            ;
            var symbols = node.ParameterList.Parameters.Where(x => ImplementsSomethingFor(x.Type, unimplemntedProperties, semanticModel))
                .Distinct();

            foreach (var symbol in symbols)
                context.RegisterRefactoring(
                    new FixConstructorBody( node.Body, symbol.Identifier.Text, semanticModel, context.Document));
        }

        private static bool ImplementsSomethingFor(TypeSyntax type, IEnumerable<NamedSymbol> unimplemntedProperties, SemanticModel semanticModel)
        {
            return semanticModel.GetTypeInfo(type).Type.GetMembers().Where(x => x.Kind == SymbolKind.Property).Cast<IPropertySymbol>().Any(x => x.IsMissing(unimplemntedProperties));

        }
        private class FixConstructorBody : CodeAction
        {
            private readonly string sourcename;
            private readonly SemanticModel semanticModel;
            private readonly Document document;
            private BlockSyntax node;


            public FixConstructorBody( BlockSyntax node, string sourcename, SemanticModel semanticModel, Document document)
            {
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

                var newExpression = ImplementConstructorBody(node, sourcename, typesymbol, sourceType);
                var root = await document.GetSyntaxRootAsync(cancellationToken);
                var newroot = root.ReplaceNode(node, newExpression);
                return document.WithSyntaxRoot(newroot);
            }

            private SyntaxNode ImplementConstructorBody(BlockSyntax declaration,
                                                                                 string sourcename,
                                                                                 ITypeSymbol targetTypeInfo,
                                                                                 ITypeSymbol sourceType)
            {
                var missingprops = GetMissingProperties(declaration, targetTypeInfo);

                var newproperties =
                    sourceType.GetMembers().Where(x => x.Kind == SymbolKind.Property).Cast<IPropertySymbol>().Where(x => x.IsMissing(missingprops));
                var newExpression = declaration.AddStatements(
                    newproperties.Select(x =>
                                             SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                                                                                SyntaxFactory.IdentifierName(x.Name),
                                                                                SyntaxFactory.MemberAccessExpression(
                                                                                    SyntaxKind.SimpleMemberAccessExpression,
                                                                                    SyntaxFactory.IdentifierName(sourcename),
                                                                                    SyntaxFactory.IdentifierName(x.Name))))
                        .Cast<ExpressionSyntax>().Select(SyntaxFactory.ExpressionStatement).ToArray<StatementSyntax>());
                return newExpression;
            }
            private static IEnumerable<IPropertySymbol> GetMissingProperties(BlockSyntax expression, ITypeSymbol typeSymbol)
            {
                var properties =
                    typeSymbol.GetMembers().Where(x => x.Kind == SymbolKind.Property).Cast<IPropertySymbol>();

                var missingprops = GetUnimplemntedProperties(expression, properties);
                return missingprops;
            }
            private static IEnumerable<IPropertySymbol> GetUnimplemntedProperties(BlockSyntax expression, IEnumerable<IPropertySymbol> properties)
            {
                return properties.Where(
                    x =>
                        expression.Statements.OfType<ExpressionStatementSyntax>()
                        .Where(ex => ex.Expression is AssignmentExpressionSyntax)
                        .Select(ex => ex.Expression as AssignmentExpressionSyntax)
                        .All(
                            y => ((IdentifierNameSyntax)(y).Left).Identifier.Text != x.Name));
            }

        }
    }
}
