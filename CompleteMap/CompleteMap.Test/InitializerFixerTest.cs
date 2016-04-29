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
    public class InitializerFixerTest : CodeFixVerifier
    {
        [TestMethod]
        public void ImplementFromField()
        {
            var result = DataHelper.Result("InitializerFixerfromField");
            VerifyCSharpFix(DataHelper.SomeCode, result,2);
        }


        [TestMethod]
        public void ImplementFromLocal()
        {
            var result = DataHelper.Result("InitializerFixerfromLocal");
            VerifyCSharpFix(DataHelper.SomeCode, result, 3);
        }
        

        [TestMethod]
        public void ImplementFromProperty()
        {
            var result = DataHelper.Result("InitializerFixerfromPropertyWithInheritance");
            VerifyCSharpFix(DataHelper.SomeCode, result, 4);
        }
        [TestMethod]
        public void FillInBlanks()
        {
            var result = DataHelper.Result("InitializerFixerFillBlanks");
            VerifyCSharpFix(DataHelper.SomeCode, result, 5);
        }
        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new InitializerFixer();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new MissingInInitializerAnalyzer();
        }
    }
}
