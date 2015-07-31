using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CompleteMap
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class CompleteMapAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CompleteBlank";
        public const string DiagnosticFromId = "CompleteFrom";

        // You can change these strings in the Resources.resx file. If you do not want your analyzer to be localize-able, you can use regular strings for Title and MessageFormat.
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Madness";

        private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Info, isEnabledByDefault: true, description: Description);
        private static DiagnosticDescriptor FromRule = new DiagnosticDescriptor(DiagnosticFromId, "Map", "Map from {0}", Category, DiagnosticSeverity.Info, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(FromRule,Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.ObjectInitializerExpression);
        }

        private static void Analyze(SyntaxNodeAnalysisContext context)
        {
            var node = context.Node as InitializerExpressionSyntax;

            var createExpression = node.Parent as ObjectCreationExpressionSyntax;
            var semanticModel = context.SemanticModel;
            var typeSymbol = semanticModel.GetTypeInfo(createExpression);
            var properties = typeSymbol.Type.GetMembers().Where(x => x.Kind == SymbolKind.Method).Cast<IMethodSymbol>().Where(x => x.MethodKind == MethodKind.PropertySet);
            
            var unimplemntedProperties =
                properties.Where(
                    x =>
                        node.Expressions.All(
                            y => ((IdentifierNameSyntax)((AssignmentExpressionSyntax)y).Left).Identifier.Text != PropertyName(x)));
            if(unimplemntedProperties.Any())
            { 
                var localsymbols = semanticModel.LookupSymbols(node.SpanStart)
                    .OfType<ILocalSymbol>().
                    Where(x => x.Type != typeSymbol.Type);
                foreach (var localsymbol in localsymbols.Where(x=>ImplementsSomethingFor(x.Type, unimplemntedProperties)))
                {
                    var prop = ImmutableDictionary<string, string>.Empty.Add("local",localsymbol.Name);
                    context.ReportDiagnostic(diagnostic : Diagnostic.Create(FromRule, context.Node.GetLocation(),prop, localsymbol.Name));
                }
                var diagnostic = Diagnostic.Create(Rule, context.Node.GetLocation());

                context.ReportDiagnostic(diagnostic);
            }
        }
        public static string PropertyName(ISymbol symbol)  => symbol.Name.Substring("set_".Length);


        private static bool ImplementsSomethingFor(ITypeSymbol type, IEnumerable<IMethodSymbol> missingprops)
        {
            return type.GetMembers().Where(x => x.Kind == SymbolKind.Property).Cast<IPropertySymbol>().Any(x => IsMissing(x,missingprops));
        }

        private static bool IsMissing(IPropertySymbol symbol,IEnumerable<IMethodSymbol> missingprops)
        {
            return missingprops.Any(x => Compare(symbol, x));
        }


        private static bool Compare(IPropertySymbol symbol, IMethodSymbol x)
        {
            return PropertyName(x) == symbol.Name && x.Parameters.First().Type == symbol.Type;
        }
    }
}
