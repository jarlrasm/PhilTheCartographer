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
    public class ConstructorFixerTest : CodeFixVerifier
    {
        [TestMethod]
        public void FillFromArgument()
        {
            var result = DataHelper.Result("ConstructorFillFromArgument");
            VerifyCSharpFix(DataHelper.SomeCode, result);
        }
        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new ConstructorFixer();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new MissingInConstructorAnalyzer();
        }

    }
}
