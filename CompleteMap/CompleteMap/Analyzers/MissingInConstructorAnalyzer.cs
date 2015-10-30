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
    public class MissingInConstructorAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "MissingInConstructor";
        private static readonly LocalizableString Title = "MissingInConstructor";
        private static readonly LocalizableString MessageFormat = "MissingInConstructor";
        private static readonly LocalizableString Description = "MissingInConstructor";
        private const string Category = "Madness";
        private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Info, isEnabledByDefault: true, description: Description);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeConstructor, SyntaxKind.ArgumentList);
        }

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        private static void AnalyzeConstructor(SyntaxNodeAnalysisContext context)
        {
            var node = context.Node as ArgumentListSyntax;

            var createExpression = node?.Parent as ObjectCreationExpressionSyntax;
            if (createExpression == null)
                return;
            var semanticModel = context.SemanticModel;
            var typeSymbol = semanticModel.GetTypeInfo(createExpression);


            var constructors = typeSymbol.Type.GetMembers().Where(x => x.Kind == SymbolKind.Method).Cast<IMethodSymbol>().Where(x => x.MethodKind == MethodKind.Constructor);

            constructors = constructors.Where(x => HasMoreArguments(x, node.Arguments, semanticModel));

            foreach (var constructor in constructors)
            {
                var unimplemntedParameters = constructor.Parameters.Skip(node.Arguments.Count);
                ReportForUnimplemented(context, unimplemntedParameters, semanticModel, node, typeSymbol);
            }
        }

        private static void ReportForUnimplemented(SyntaxNodeAnalysisContext context, IEnumerable<IParameterSymbol> unimplemntedParameters, SemanticModel semanticModel, ArgumentListSyntax node, TypeInfo typeSymbol)
        {
            var typesymbols = semanticModel.GetTypeSymbols(node, typeSymbol);

            var symbols = typesymbols.Where(x => ImplementsSomethingFor(x.Type, unimplemntedParameters))
                .Distinct()
                .ToImmutableDictionary(x => x.Name, x => x.Name);//Why does this have to be a bloody string,string dictionary=?


            var diagnostic = Diagnostic.Create(Rule, context.Node.GetLocation(), symbols);

            context.ReportDiagnostic(diagnostic);
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
        
        private static bool ImplementsSomethingFor(ITypeSymbol type, IEnumerable<IParameterSymbol> mssingParameters)
        {
            return type.GetMembers().Where(x => x.Kind == SymbolKind.Property).Cast<IPropertySymbol>().Any(x => x.IsMissing(mssingParameters));
        }





    }
}
