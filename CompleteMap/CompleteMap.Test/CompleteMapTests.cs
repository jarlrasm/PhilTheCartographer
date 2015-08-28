using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using TestHelper;
using CompleteMap;

namespace CompleteMap.Test
{
    [TestClass]
    public class CompleteMapTests : CodeFixVerifier
    {
        //Diagnostic and CodeFix both triggered and checked for
        [TestMethod]
        public void FindallDiagnostics()
        {
            var test = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;

    namespace ConsoleApplication1
    {
        class Test
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
                    var t2=new Test2()
                    {
                        I=default(int),
                        S=default(String)
                    }
                    var t=new Test()
                    {
                        I=default(int)
                    };
                }
        }
    }";
            var expected = new[]
            {
                new DiagnosticResult
                {
                    Id = "CompleteConstructorFrom",
                    Message = "Map from t",
                    Severity = DiagnosticSeverity.Info,
                    Locations =
                        new[]
                        {
                            new DiagnosticResultLocation("Test0.cs", 33, 37)
                        }
                },
                new DiagnosticResult
                {
                    Id = "CompleteConstructorFrom",
                    Message = "Map from eh",
                    Severity = DiagnosticSeverity.Info,
                    Locations =
                        new[]
                        {
                            new DiagnosticResultLocation("Test0.cs", 33, 37)
                        }
                },
                
                new DiagnosticResult
                {
                    Id = "CompleteFrom",
                    Message = "Map from t2",
                    Severity = DiagnosticSeverity.Info,
                    Locations =
                        new[]
                        {
                            new DiagnosticResultLocation("Test0.cs", 39, 21)
                        }
                },
                new DiagnosticResult
                {
                    Id = "CompleteBlank",
                    Message = "Fill in all blanks",
                    Severity = DiagnosticSeverity.Info,
                    Locations =
                        new[]
                        {
                            new DiagnosticResultLocation("Test0.cs", 39, 21)
                        }
                }
            };

            VerifyCSharpDiagnostic(test, expected);
        }


    //Diagnostic and CodeFix both triggered and checked for
        [TestMethod]
        public void ImplementMissingSetters()
        {
            var test = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;

    namespace ConsoleApplication1
    {
        class Test
        {
                public int I{get;set;}
                public string S{get;set;}
        }
        class TypeName
        {   
                public void T()
                {
                    var t=new Test
                    {
                        I=default(int)
                    };
                }
        }
    }";
            var expected = new DiagnosticResult
            {
                Id = "CompleteBlank",
                Message = "Fill in all blanks",
                Severity = DiagnosticSeverity.Info,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 21, 21)
                        }
            };

            VerifyCSharpDiagnostic(test, expected);

            var fixtest = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;

    namespace ConsoleApplication1
    {
        class Test
        {
                public int I{get;set;}
                public string S{get;set;}
        }
        class TypeName
        {   
                public void T()
                {
                    var t=new Test
                    {
                        I=default(int)
,
                        S = default(String)
                    };
                }
        }
    }";
            VerifyCSharpFix(test, fixtest);
        }

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new CompleteMapCodeFixProvider();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new CompleteMapAnalyzer();
        }
    }
}