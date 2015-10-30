using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CompleteMap.Test.Helpers;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Phil.Analyzers;

using TestHelper;

namespace CompleteMap.Test
{
    [TestClass]
    public class MissingInInitializerTest : DiagnosticVerifier
    {        //Diagnostic and CodeFix both triggered and checked for
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
                            new DiagnosticResultLocation("Test0.cs", 24, 25)
                        }
                }
            };

            VerifyCSharpDiagnostic(DataHelper.SomeCode(), expected);
        }
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new MissingInInitializerAnalyzer();
        }
    }
}
