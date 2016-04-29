using CompleteMap.Test.Helpers;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Phil.Analyzers;

using TestHelper;

namespace Phil.Test
{
    [TestClass]
    public class MissingInInitializerTest : DiagnosticVerifier
    { 
        [TestMethod]
        public void FindDiagnostics()
        {
            var expected = new[]
            {
                new DiagnosticResult
                {
                    Id = "MissingInInitializer",
                    Message = "MissingInInitializer",
                    Severity = DiagnosticSeverity.Info,
                    Locations =
                        new[]
                        {
                            new DiagnosticResultLocation("Test0.cs", 44, 37)
                        }
                },
                new DiagnosticResult
                {
                    Id = "MissingInInitializer",
                    Message = "MissingInInitializer",
                    Severity = DiagnosticSeverity.Info,
                    Locations =
                        new[]
                        {
                            new DiagnosticResultLocation("Test0.cs", 45, 43)
                        }
                }
            };

            VerifyCSharpDiagnostic(DataHelper.SomeCode, expected);
        }
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new MissingInInitializerAnalyzer();
        }
    }
}
