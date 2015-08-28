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
        public const string DiagnosticConstructorFromId = "CompleteConstructorFrom";

        // You can change these strings in the Resources.resx file. If you do not want your analyzer to be localize-able, you can use regular strings for Title and MessageFormat.
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Madness";

        private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Info, isEnabledByDefault: true, description: Description);
        private static DiagnosticDescriptor FromRule = new DiagnosticDescriptor(DiagnosticFromId, "Map", "Map from {0}", Category, DiagnosticSeverity.Info, isEnabledByDefault: true, description: Description);
        private static DiagnosticDescriptor ConstructorFromRule = new DiagnosticDescriptor(DiagnosticConstructorFromId, "Map", "Map from {0}", Category, DiagnosticSeverity.Info, isEnabledByDefault: true, description: Description);
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(FromRule,Rule,ConstructorFromRule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeInitializer, SyntaxKind.ObjectInitializerExpression);
            context.RegisterSyntaxNodeAction(AnalyzeConstructor, SyntaxKind.ArgumentList);
        }

        private static void AnalyzeInitializer(SyntaxNodeAnalysisContext context)
        {
            var node = context.Node as InitializerExpressionSyntax;

            var createExpression = node?.Parent as ObjectCreationExpressionSyntax;
            if (createExpression == null)
                return;
            var semanticModel = context.SemanticModel;
            var typeSymbol = semanticModel.GetTypeInfo(createExpression);
            var properties = typeSymbol.Type.GetMembers().Where(x => x.Kind == SymbolKind.Method).Cast<IMethodSymbol>().Where(x => x.MethodKind == MethodKind.PropertySet);
            
            var unimplemntedProperties =
                properties.Where(
                    x =>
                        node.Expressions.All(
                            y => ((IdentifierNameSyntax)((AssignmentExpressionSyntax)y).Left).Identifier.Text != PropertyName(x)));
            if (unimplemntedProperties.Any())
            {
                ReportForUnimplemented(context, unimplemntedProperties, semanticModel, node, typeSymbol);
            }
        }
        private static void AnalyzeConstructor(SyntaxNodeAnalysisContext context)
        {
            var node = context.Node as ArgumentListSyntax;

            var createExpression = node?.Parent as ObjectCreationExpressionSyntax;
            if (createExpression == null)
                return;
            var semanticModel = context.SemanticModel;
            var typeSymbol = semanticModel.GetTypeInfo(createExpression);


            var constructors = typeSymbol.Type.GetMembers().Where(x => x.Kind == SymbolKind.Method).Cast<IMethodSymbol>().Where(x => x.MethodKind == MethodKind.Constructor);

            constructors = constructors.Where(x => HasMoreArguments(x, node.Arguments,semanticModel));

            foreach (var constructor in constructors)
            {
                var unimplemntedParameters = constructor.Parameters.Skip(node.Arguments.Count);
                    ReportForUnimplemented(context, unimplemntedParameters, semanticModel, node, typeSymbol);
            }
        }

        private static void ReportForUnimplemented(SyntaxNodeAnalysisContext context,
                                                   IEnumerable<IMethodSymbol> unimplemntedProperties,
                                                   SemanticModel semanticModel,
                                                   InitializerExpressionSyntax node,
                                                   TypeInfo typeSymbol)
        {
                var typesymbols = GetTypeSymbols(semanticModel, node, typeSymbol);
                foreach (var symbol in typesymbols.Where(x => ImplementsSomethingFor(x.Type, unimplemntedProperties)))
                {
                    var prop = ImmutableDictionary<string, string>.Empty.Add("local", symbol.Name);
                    context.ReportDiagnostic(diagnostic: Diagnostic.Create(FromRule, context.Node.GetLocation(), prop, symbol.Name));
                }
                var diagnostic = Diagnostic.Create(Rule, context.Node.GetLocation());

                context.ReportDiagnostic(diagnostic);
        }



        private static void ReportForUnimplemented(SyntaxNodeAnalysisContext context, IEnumerable<IParameterSymbol> unimplemntedParameters, SemanticModel semanticModel, ArgumentListSyntax node, TypeInfo typeSymbol)
        {
            var typesymbols = GetTypeSymbols(semanticModel, node, typeSymbol);
            foreach (var symbol in typesymbols.Where(x => ImplementsSomethingFor(x.Type, unimplemntedParameters)))
            {
                var prop = ImmutableDictionary<string, string>.Empty.Add("local", symbol.Name);
                context.ReportDiagnostic(diagnostic: Diagnostic.Create(ConstructorFromRule, context.Node.GetLocation(), prop, symbol.Name));
            }
        }

        private class TypedSymbol
        {
            public string Name { get; internal set; }
            public ITypeSymbol Type { get; internal set; }
        }
        private static IEnumerable<TypedSymbol> GetTypeSymbols(SemanticModel semanticModel, SyntaxNode node, TypeInfo typeSymbol)
        {
            var typesymbol = semanticModel.LookupSymbols(node.SpanStart)
                .OfType<ILocalSymbol>().
                Where(x => x.Type != typeSymbol.Type)
                .Select(x => new TypedSymbol { Name = x.Name, Type = x.Type })
                .Concat(
                    semanticModel.LookupSymbols(node.SpanStart)
                        .OfType<IParameterSymbol>()
                        .Where(x => x.Type != typeSymbol.Type)
                        .Select(x => new TypedSymbol { Name = x.Name, Type = x.Type })
                );
            return typesymbol;
        }


        private static bool HasMoreArguments(IMethodSymbol constructor, SeparatedSyntaxList<ArgumentSyntax> arguments,SemanticModel semanticModel)
        {
            if (constructor.Parameters.Count() <= arguments.Count)
                return false;
            for(int i=0;i>arguments.Count;i++)
            {
                var argtype = semanticModel.GetTypeInfo(arguments[i]);
                if (constructor.Parameters[i].Type != argtype.Type)
                    return false;
            }
            return true;
        }

        

        public static string PropertyName(ISymbol symbol)  => symbol.Name.Substring("set_".Length);


        private static bool ImplementsSomethingFor(ITypeSymbol type, IEnumerable<IMethodSymbol> missingprops)
        {
            return type.GetMembers().Where(x => x.Kind == SymbolKind.Property).Cast<IPropertySymbol>().Any(x => IsMissing(x,missingprops));
        }
        private static bool ImplementsSomethingFor(ITypeSymbol type, IEnumerable<IParameterSymbol> mssingParameters)
        {
            return type.GetMembers().Where(x => x.Kind == SymbolKind.Property).Cast<IPropertySymbol>().Any(x => IsMissing(x, mssingParameters));
        }

        private static bool IsMissing(IPropertySymbol symbol, IEnumerable<IParameterSymbol> mssingParameters)
        {
            return mssingParameters.Any(x => Compare(symbol, x));
        }

        private static bool Compare(IPropertySymbol symbol, IParameterSymbol x)
        {
            return x.Name.ToLower() == symbol.Name.ToLower() && x.Type == symbol.Type;
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
