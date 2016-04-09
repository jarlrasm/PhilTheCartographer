using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Phil.Analyzers;
using Phil.Extensions;

namespace Phil.CodeFixProviders
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(InitializerFixer))]
    public class InitializerFixer : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(MissingInInitializerAnalyzer.DiagnosticId); }
        }

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            RegisterFixInitializer(context, root);
        }


        private void RegisterFixInitializer(CodeFixContext context, SyntaxNode root)
        {
            foreach (var diagnostic in context.Diagnostics)
            {
                var diagnosticSpan = diagnostic.Location.SourceSpan;

                // Find the type declaration identified by the diagnostic.
                var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<InitializerExpressionSyntax>().First();

                foreach (var solution in diagnostic.Properties)
                {
                    string title = string.Format("Map from {0}", solution.Value);
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            title : title,
                            createChangedDocument : c => context.Document.RunFix<InitializerExpressionSyntax>(
                                declaration, c, solution.Value,
                                ImplementAllSettersFromExpression
                                ),
                            equivalenceKey : title),
                        diagnostic);
                }
                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: "Fill in blanks",
                        createChangedDocument: c => ImplementAllSetters(context.Document, declaration, c),
                        equivalenceKey: "Fill in blanks"),
                    diagnostic);

            }
        }
        private async Task<Document> ImplementAllSetters(Document document, InitializerExpressionSyntax expression, CancellationToken cancellationToken)
        {
            var createExpression = expression.Parent as ObjectCreationExpressionSyntax;
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
            var typeSymbol = semanticModel.GetTypeInfo(createExpression, cancellationToken).Type;
            var missingprops = GetMissingProperties(expression, typeSymbol);


            var newExpression = expression.AddExpressions(missingprops.Select(x =>
                SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                SyntaxFactory.IdentifierName(x.PropertyName()), x.Parameters.First().Type.DefaultExpression(x.PropertyName()))).Cast<ExpressionSyntax>().ToArray());

            var root = await document.GetSyntaxRootAsync();
            var newroot = root.ReplaceNode(expression, newExpression);
            return document.WithSyntaxRoot(newroot);
        }




        private static SyntaxNode ImplementAllSettersFromExpression(InitializerExpressionSyntax expression,
                                                                                 string sourcename,
                                                                                 ITypeSymbol targetTypeInfo,
                                                                                 SemanticModel semanticModel,
                                                                                 ITypeSymbol sourceType)
        {
            var missingprops = GetMissingProperties(expression, targetTypeInfo);

            var newproperties =
                sourceType.GetMembers().Where(x => x.Kind == SymbolKind.Property).Cast<IPropertySymbol>().Where(x => x.IsMissing(missingprops));
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

        private static IEnumerable<IMethodSymbol> GetMissingProperties(InitializerExpressionSyntax expression, ITypeSymbol typeSymbol)
        {
            var properties =
                typeSymbol.GetBaseTypesAndThis().SelectMany(x=>x.GetMembers()).Where(x => x.Kind == SymbolKind.Method).Cast<IMethodSymbol>().Where(
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
