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
                            new DiagnosticResultLocation("Test0.cs", 8, 13)
                        }
                }
            };
            string code =
@"      class Test
        {
            public int I { get; set; }
            public string S { get; set; }
        }
        class Test2
        {
            public Test2(Test test,Test3 t3)
            {
                I=test.I;
            }
            public int I { get; }
            public string S { get; }
        }
        class Test3
        {
            public string Wut
        }";
            VerifyCSharpDiagnostic(code, expected);
        }
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new MissingInConstructorBodyAnalyzer();
        }
    }
}
