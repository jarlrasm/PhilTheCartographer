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
            var code = DataHelper.SomeCode;
            var result = DataHelper.Result("ConstructorBodyFillFromArgument");
            VerifyCSharpFix(code, result);
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
