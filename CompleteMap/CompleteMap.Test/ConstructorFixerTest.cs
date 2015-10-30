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
            var result =
@"class Test
{
    public int I{get;set;}
    public string S{get;set;}
}
class Test2
{
    public Test2()
    {}
    public Test2(int i, string s)
    {
        I=i,S=s
    }
    public int I{get;set;}
    public string S{get;set;}
}
class TypeName
{   
    public Test2 field;
    public Test2 Getter{return field;}
    public void T(Test eh)
    {
        var t2=new Test2(eh.I, eh.S) {I=default(int),S=""S""};
        var t=new Test(){I=default(int)};
    }
}";
            VerifyCSharpFix(DataHelper.SomeCode(), result, 0);
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
