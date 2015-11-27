using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CompleteMap.Test.Helpers;

using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Phil.Analyzers;
using Phil.CodeFixProviders;

using TestHelper;

namespace Phil.Test
{
    [TestClass]
    public class ConstructorBodyFixerTest : CodeFixVerifier
    {
        [TestMethod]
        public void FillFromArgument()
        {
            var code =
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
            var result =
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
        S = test.S;
    }
            public int I { get; }
            public string S { get; }
        }
        class Test3
        {
            public string Wut
        }";
            VerifyCSharpFix(code, result, 0);
        }
        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new ConstructorBodyFixer();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new MissingInConstructorBodyAnalyzer();
        }

    }
}
