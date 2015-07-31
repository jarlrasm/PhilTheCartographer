using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;

namespace CompleteMap
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(CompleteMapCodeFixProvider))]
    public class CompleteMapCodeFixProvider : CodeFixProvider
    {

        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(CompleteMapAnalyzer.DiagnosticFromId, CompleteMapAnalyzer.DiagnosticId); }
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            RegisterFixFrom(context, root);
            RegisterFixBlank(context, root);
        }


        private void RegisterFixBlank(CodeFixContext context, SyntaxNode root)
        {
            var diagnostic = context.Diagnostics.FirstOrDefault(x=>x.Id==CompleteMapAnalyzer.DiagnosticId);
            if(diagnostic==null)
                return;
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            // Find the type declaration identified by the diagnostic.
            var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<InitializerExpressionSyntax>().First();

            // Register a code action that will invoke the fix.
            context.RegisterCodeFix(
                CodeAction.Create(
                    title : diagnostic.GetMessage(),
                    createChangedDocument : c => ImplementAllSetters(context.Document, declaration, c),
                    equivalenceKey : diagnostic.GetMessage()),
                diagnostic);
        }

        private void RegisterFixFrom(CodeFixContext context, SyntaxNode root)
        {
            var diagnostics = context.Diagnostics.Where(x=>x.Id==CompleteMapAnalyzer.DiagnosticFromId);
            foreach (var diagnostic in diagnostics)
            {
                var diagnosticSpan = diagnostic.Location.SourceSpan;

                // Find the type declaration identified by the diagnostic.
                var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<InitializerExpressionSyntax>().First();

                // Register a code action that will invoke the fix.
                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: diagnostic.GetMessage(),
                        createChangedDocument: c => ImplementAllSettersFrom(context.Document, declaration, c,diagnostic.Properties["local"]),
                        equivalenceKey: diagnostic.GetMessage()),
                    diagnostic);

            }
        }


        private async Task<Document> ImplementAllSetters(Document document, InitializerExpressionSyntax expression, CancellationToken cancellationToken)
        {
            var createExpression = expression.Parent as ObjectCreationExpressionSyntax;
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
            var typeSymbol = semanticModel.GetTypeInfo(createExpression, cancellationToken);
            var missingprops = GetMissingProperties(expression, typeSymbol);


            var newExpression = expression.AddExpressions(missingprops.Select(x=>
                SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                SyntaxFactory.IdentifierName(PropertyName(x)),SyntaxFactory.DefaultExpression(SyntaxFactory.ParseTypeName(x.Parameters.First().Type.Name)))).Cast<ExpressionSyntax>().ToArray());

            var root = await document.GetSyntaxRootAsync();
            var newroot = root.ReplaceNode(expression, newExpression);
            return document.WithSyntaxRoot(newroot);
        }


        private static IEnumerable<IMethodSymbol> GetUnImplemntedProperties(InitializerExpressionSyntax expression, IEnumerable<IMethodSymbol> properties)
        {
            var uniimplemntedProperties =
                properties.Where(
                    x =>
                        expression.Expressions.All(
                            y => ((IdentifierNameSyntax)((AssignmentExpressionSyntax)y).Left).Identifier.Text != PropertyName(x)));
            return uniimplemntedProperties;
        }


        private async Task<Document> ImplementAllSettersFrom(Document document, InitializerExpressionSyntax expression, CancellationToken cancellationToken, string sourcename)
        {
            var createExpression = expression.Parent as ObjectCreationExpressionSyntax;
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
            var typeSymbol = semanticModel.GetTypeInfo(createExpression, cancellationToken);
            var missingprops = GetMissingProperties(expression, typeSymbol);

            var localsymbol = semanticModel.LookupSymbols(expression.SpanStart)
                .OfType<ILocalSymbol>().
                Where(x => x.Type != typeSymbol.Type)
                .First(x=>x.Name==sourcename);

            var newproperties=localsymbol.Type.GetMembers().Where(x => x.Kind == SymbolKind.Property).Cast<IPropertySymbol>().Where(x => IsMissing(x, missingprops));
            var newExpression = expression.AddExpressions(
                newproperties.Select(x =>
                SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                SyntaxFactory.IdentifierName(x.Name), 
                SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,  SyntaxFactory.IdentifierName(sourcename), SyntaxFactory.IdentifierName(x.Name))))
                .Cast<ExpressionSyntax>().ToArray());
              
            var root = await document.GetSyntaxRootAsync();
            var newroot = root.ReplaceNode(expression, newExpression);
            return document.WithSyntaxRoot(newroot);
        }


        private static IEnumerable<IMethodSymbol> GetMissingProperties(InitializerExpressionSyntax expression, TypeInfo typeSymbol)
        {
            var properties =
                typeSymbol.Type.GetMembers().Where(x => x.Kind == SymbolKind.Method).Cast<IMethodSymbol>().Where(
                    x => x.MethodKind == MethodKind.PropertySet);

            var missingprops = GetUnImplemntedProperties(expression, properties);
            return missingprops;
        }


        private static bool IsMissing(IPropertySymbol symbol, IEnumerable<IMethodSymbol> missingprops)
        {
            return missingprops.Any(x => Compare(symbol, x));
        }


        public static string PropertyName(ISymbol symbol) => symbol.Name.Substring("set_".Length);

        private static bool Compare(IPropertySymbol symbol, IMethodSymbol x)
        {
            return PropertyName(x) == symbol.Name && x.Parameters.First().Type == symbol.Type;
        }
    }
}