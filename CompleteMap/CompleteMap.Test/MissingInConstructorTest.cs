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

namespace Phil.Test
{
    [TestClass]
    public class MissingInConstructorTest : DiagnosticVerifier
    {
        [TestMethod]
        public void FindDiagnostics()
        {
            var expected = new[]
            {
                new DiagnosticResult
                {
                    Id = "MissingInConstructor",
                    Message = "MissingInConstructor",
                    Severity = DiagnosticSeverity.Info,
                    Locations =
                        new[]
                        {
                            new DiagnosticResultLocation("Test0.cs", 23, 25)
                        }
                }
            };

            VerifyCSharpDiagnostic(DataHelper.SomeCode(), expected);
        }
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new MissingInConstructorAnalyzer();
        }
    }
}
