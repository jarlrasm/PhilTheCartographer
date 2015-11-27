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
    public class ConstructorFixer : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create( MissingInConstructorAnalyzer.DiagnosticId); }
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

                // Find the type declaration identified by the diagnostic.
                var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<ArgumentListSyntax>().FirstOrDefault();
                if (declaration == null)
                    return;
                foreach (var solution in diagnostic.Properties)
                {
                    string title = string.Format("Fill constructor from {0}", solution.Value);
                    // Register a code action that will invoke the fix.
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            title: title,
                            createChangedDocument: c => context.Document.RunFix( declaration, c, solution.Value, ImplementConstructorFromExpression),
                            equivalenceKey: title),
                        diagnostic);

                }

            }
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
                        newExpression.AddArguments(SyntaxFactory.Argument(param.Type.DefaultExpression( param.Name)));
                }
            }
            return newExpression;
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
