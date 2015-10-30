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

using Phil.Extensions;

namespace Phil.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class MissingInInitializerAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "MissingInInitializer";
        private static readonly LocalizableString Title = "MissingInInitializer";
        private static readonly LocalizableString MessageFormat = "MissingInInitializer";
        private static readonly LocalizableString Description = "MissingInInitializer";
        private const string Category = "Madness";
        private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Info, isEnabledByDefault: true, description: Description);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeInitializer, SyntaxKind.ObjectInitializerExpression);
        }

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create( Rule); } }

        private static void AnalyzeInitializer(SyntaxNodeAnalysisContext context)
        {
            var node = context.Node as InitializerExpressionSyntax;

            var createExpression = node?.Parent as ObjectCreationExpressionSyntax;
            if (createExpression == null)
                return;
            var semanticModel = context.SemanticModel;
            var typeSymbol = ModelExtensions.GetTypeInfo(semanticModel, createExpression);
            var properties = typeSymbol.Type.GetMembers().Where(x => x.Kind == SymbolKind.Method).Cast<IMethodSymbol>().Where(x => x.MethodKind == MethodKind.PropertySet);

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
        private static void ReportForUnimplemented(SyntaxNodeAnalysisContext context,
                                                   IEnumerable<IMethodSymbol> unimplemntedProperties,
                                                   SemanticModel semanticModel,
                                                   InitializerExpressionSyntax node,
                                                   TypeInfo typeSymbol)
        {
            var typesymbols = semanticModel.GetTypeSymbols(node, typeSymbol);
            var symbols = typesymbols.Where(x => ImplementsSomethingFor(x.Type, unimplemntedProperties))
                .Distinct()
                .ToImmutableDictionary(x => x.Name, x => x.Name);//Why does this have to be a bloody string,string dictionary=?
            

            var diagnostic = Diagnostic.Create(Rule, context.Node.GetLocation(),symbols);

            context.ReportDiagnostic(diagnostic);
        }

        private static bool ImplementsSomethingFor(ITypeSymbol type, IEnumerable<IMethodSymbol> missingprops)
        {
            return type.GetMembers().Where(x => x.Kind == SymbolKind.Property).Cast<IPropertySymbol>().Any(x => IsMissing(x, missingprops));
        }
        private static bool IsMissing(IPropertySymbol symbol, IEnumerable<IMethodSymbol> missingprops)
        {
            return missingprops.Any(x => Compare(symbol, x));
        }


        private static bool Compare(IPropertySymbol symbol, IMethodSymbol x)
        {
            return x.PropertyName() == symbol.Name && x.Parameters.First().Type == symbol.Type;
        }

    }
}
