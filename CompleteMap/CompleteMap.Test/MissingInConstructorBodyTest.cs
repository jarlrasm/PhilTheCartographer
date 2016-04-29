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
    public class MissingInConstructorBodyTest : DiagnosticVerifier
    {
        [TestMethod]
        public void FindDiagnostics()
        {
            var expected = new[]
            {
                new DiagnosticResult
                {
                    Id = "MissingInConstructorBody",
                    Message = "MissingInConstructorBody",
                    Severity = DiagnosticSeverity.Info,
                    Locations =
                        new[]
                        {
                            new DiagnosticResultLocation("Test0.cs", 11, 9)
                        }
                },

                new DiagnosticResult
                {
                    Id = "MissingInConstructorBody",
                    Message = "MissingInConstructorBody",
                    Severity = DiagnosticSeverity.Info,
                    Locations =
                        new[]
                        {
                            new DiagnosticResultLocation("Test0.cs", 25, 9)
                        }
                },

                new DiagnosticResult
                {
                    Id = "MissingInConstructorBody",
                    Message = "MissingInConstructorBody",
                    Severity = DiagnosticSeverity.Info,
                    Locations =
                        new[]
                        {
                            new DiagnosticResultLocation("Test0.cs", 27, 9)
                        }
                }
            };
            string code = DataHelper.SomeCode;
            VerifyCSharpDiagnostic(code, expected);
        }
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new MissingInConstructorBodyAnalyzer();
        }
    }
}
