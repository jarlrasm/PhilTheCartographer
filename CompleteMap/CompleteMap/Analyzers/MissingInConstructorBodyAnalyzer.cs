using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FindSymbols;

using Phil.Extensions;

namespace Phil.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class MissingInConstructorBodyAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "MissingInConstructorBody";
        private static readonly LocalizableString Title = "MissingInConstructorBody";
        private static readonly LocalizableString MessageFormat = "MissingInConstructorBody";
        private static readonly LocalizableString Description = "MissingInConstructorBody";
        private const string Category = "Madness";
        private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Info, isEnabledByDefault: true, description: Description);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeConstructorBody, SyntaxKind.ConstructorDeclaration);
        }

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        private static void AnalyzeConstructorBody(SyntaxNodeAnalysisContext context)
        {
            var node = context.Node as ConstructorDeclarationSyntax;
            if (node != null)
            {
                var parameters = node.ParameterList;
                var body = node.Body;

                var semanticModel = context.SemanticModel;
                var properties = semanticModel.GetTypeSymbols(node)
                    .Where(x=>x.Name!="this");
                var unimplemntedProperties =
                    properties.Where(
                        x =>
                            body.Statements.OfType<ExpressionStatementSyntax>()
                            .Where(ex=>ex.Expression is AssignmentExpressionSyntax)
                            .Select(ex=>ex.Expression as AssignmentExpressionSyntax)
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


        private static void ReportForUnimplemented(SyntaxNodeAnalysisContext context, IEnumerable<TypedSymbol> unimplemntedProperties, SemanticModel semanticModel, ConstructorDeclarationSyntax node)
        {;
            var symbols = node.ParameterList.Parameters.Where(x => ImplementsSomethingFor(x.Type, unimplemntedProperties,semanticModel))
                .Distinct()
                .ToImmutableDictionary(x => x.Identifier.Text, x => x.Identifier.Text);

            var diagnostic = Diagnostic.Create(Rule, context.Node.GetLocation(), symbols);

            context.ReportDiagnostic(diagnostic);
        }

        private static bool ImplementsSomethingFor(TypeSyntax type, IEnumerable<TypedSymbol> unimplemntedProperties,SemanticModel semanticModel)
        {
            return semanticModel.GetTypeInfo(type).Type.GetMembers().Where(x => x.Kind == SymbolKind.Property).Cast<IPropertySymbol>().Any(x => x.IsMissing(unimplemntedProperties));

        }
    }
}
