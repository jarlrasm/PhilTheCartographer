using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
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
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ConstructorFixer))]
    public class ConstructorBodyFixer : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create( MissingInConstructorBodyAnalyzer.DiagnosticId); }
        }

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            RegisterFixConstructor(context, root);
        }
        private void RegisterFixConstructor(CodeFixContext context, SyntaxNode root)
        {
            foreach (var diagnostic in context.Diagnostics)
            {
                var diagnosticSpan = diagnostic.Location.SourceSpan;
                
                var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<ConstructorDeclarationSyntax>().FirstOrDefault();
                if (declaration == null)
                    return;
                foreach (var solution in diagnostic.Properties)
                {
                    string title = string.Format("Fill constructor from {0}", solution.Value);
                    // Register a code action that will invoke the fix.
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            title: title,
                            createChangedDocument: c => context.Document.RunFix(declaration.Body, c, solution.Value, ImplementConstructorBody),
                            equivalenceKey: title),
                        diagnostic);

                }

            }
        }
        
        private SyntaxNode ImplementConstructorBody(BlockSyntax declaration, 
                                                                             string sourcename,
                                                                             ITypeSymbol targetTypeInfo,
                                                                             SemanticModel semanticModel,
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
