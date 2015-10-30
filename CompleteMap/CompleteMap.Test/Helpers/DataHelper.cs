using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompleteMap.Test.Helpers
{
    public class DataHelper
    {
        public static string SomeCode()=>
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
        var t=new Test(){I=default(int)};
    }
}";
    }
}
