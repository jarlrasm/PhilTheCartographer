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
        var t2=new Test2(){I=default(int),S=""S""};
        var t=new Test(){I=default(int), S = field.S };
    }
}";
            VerifyCSharpFix(DataHelper.SomeCode(), result,0);
        }


        [TestMethod]
        public void ImplementFromLocal()
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
        var t2=new Test2(){I=default(int),S=""S""};
        var t=new Test(){I=default(int), S = t2.S };
    }
}";
            VerifyCSharpFix(DataHelper.SomeCode(), result, 1);
        }

        [TestMethod]
        public void ImplementFromProperty()
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
        var t2=new Test2(){I=default(int),S=""S""};
        var t=new Test(){I=default(int), S = Getter.S };
    }
}";
            VerifyCSharpFix(DataHelper.SomeCode(), result, 2);
        }

        [TestMethod]
        public void ImplementFromPropertyWithInheritance()
        {
            var result =
@"
class TestBase
{
    public string S{get;set;}
}
class Test : TestBase
{
    public int I{get;set;}
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
        var t2=new Test2(){I=default(int),S=""S""};
        var t=new Test(){I=default(int), S = Getter.S };
    }
}";
            VerifyCSharpFix(DataHelper.CodeWithInheritance(), result, 2);
        }
        [TestMethod]
        public void FillInBlanks()
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
        var t2=new Test2(){I=default(int),S=""S""};
        var t=new Test(){I=default(int), S = ""S"" };
    }
}";
            VerifyCSharpFix(DataHelper.SomeCode(), result, 3);
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
