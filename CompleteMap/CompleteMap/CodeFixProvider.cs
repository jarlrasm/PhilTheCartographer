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
            get { return ImmutableArray.Create(CompleteMapAnalyzer.DiagnosticFromId, CompleteMapAnalyzer.DiagnosticId,CompleteMapAnalyzer.DiagnosticConstructorFromId); }
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
            RegisterFixConstructorFrom(context, root);
        }

        private void RegisterFixConstructorFrom(CodeFixContext context, SyntaxNode root)
        {
            var diagnostics = context.Diagnostics.Where(x => x.Id == CompleteMapAnalyzer.DiagnosticConstructorFromId);
            foreach (var diagnostic in diagnostics)
            {
                var diagnosticSpan = diagnostic.Location.SourceSpan;

                // Find the type declaration identified by the diagnostic.
                var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<ArgumentListSyntax>().First();

                // Register a code action that will invoke the fix.
                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: diagnostic.GetMessage(),
                        createChangedDocument: c => RunFix(context.Document, declaration, c, diagnostic.Properties["local"], ImplementConstructorFromExpression),
                        equivalenceKey: diagnostic.GetMessage()),
                    diagnostic);

            }
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
                        createChangedDocument: c => RunFix<InitializerExpressionSyntax>(
                            context.Document, declaration, c,diagnostic.Properties["local"], 
                            ImplementAllSettersFromExpression
                            ),
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
                SyntaxFactory.IdentifierName(PropertyName(x)),DefaultExpression(x.Parameters.First().Type, PropertyName(x)))).Cast<ExpressionSyntax>().ToArray());

            var root = await document.GetSyntaxRootAsync();
            var newroot = root.ReplaceNode(expression, newExpression);
            return document.WithSyntaxRoot(newroot);
        }


        private static IEnumerable<IMethodSymbol> GetUnimplemntedProperties(InitializerExpressionSyntax expression, IEnumerable<IMethodSymbol> properties)
        {
            var uniimplemntedProperties =
                properties.Where(
                    x =>
                        expression.Expressions.All(
                            y => ((IdentifierNameSyntax)((AssignmentExpressionSyntax)y).Left).Identifier.Text != PropertyName(x)));
            return uniimplemntedProperties;
        }

        
        private async Task<Document> RunFix<T>(Document document, T expression, CancellationToken cancellationToken, string sourcename,Func<T,string,TypeInfo, SemanticModel, ITypeSymbol,  SyntaxNode> implement)
            where T:SyntaxNode
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

        private static SyntaxNode ImplementAllSettersFromExpression(InitializerExpressionSyntax expression,
                                                                                 string sourcename,
                                                                                 TypeInfo targetTypeInfo,
                                                                                 SemanticModel semanticModel,
                                                                                 ITypeSymbol sourceType)
        {
            var missingprops = GetMissingProperties(expression, targetTypeInfo);

            var newproperties =
                sourceType.GetMembers().Where(x => x.Kind == SymbolKind.Property).Cast<IPropertySymbol>().Where(x => IsMissing(x, missingprops));
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


        private static SyntaxNode ImplementConstructorFromExpression(ArgumentListSyntax expression,
                                                                             string sourcename,
                                                                             TypeInfo targetTypeInfo,
                                                                             SemanticModel semanticModel,
                                                                             ITypeSymbol sourceType)
        {
            var constructors =
                targetTypeInfo.Type.GetMembers().Where(x => x.Kind == SymbolKind.Method).Cast<IMethodSymbol>().Where(
                    x => x.MethodKind == MethodKind.Constructor);

            var constructor =
                constructors.Where(x => HasMoreArguments(x, expression.Arguments, semanticModel)).OrderByDescending(x => x.Parameters.Count()).First
                    ();
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
                        newExpression.AddArguments(SyntaxFactory.Argument(DefaultExpression(param.Type, param.Name)));
                }
            }
            return newExpression;
        }



        private static ITypeSymbol GetSourceType(SyntaxNode expression, string sourcename, SemanticModel semanticModel, TypeInfo typeSymbol)
        {
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


        private static ExpressionSyntax DefaultExpression(ITypeSymbol type,string argname)
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

        private static IEnumerable<IMethodSymbol> GetMissingProperties(InitializerExpressionSyntax expression, TypeInfo typeSymbol)
        {
            var properties =
                typeSymbol.Type.GetMembers().Where(x => x.Kind == SymbolKind.Method).Cast<IMethodSymbol>().Where(
                    x => x.MethodKind == MethodKind.PropertySet);

            var missingprops = GetUnimplemntedProperties(expression, properties);
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