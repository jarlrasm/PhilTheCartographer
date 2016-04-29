using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Phil.Test.TestCode
{
    class ParentlessClass
    {
        public ParentlessClass(int integer)
        {
            Integer = integer;
        }
        public ParentlessClass ParentlessClass { get; set; }
        public int Integer { get; set; }
        public string SomeString { get; set; }
    }
    abstract class BaseClass
    {
        public int Integer { get; set; }
    }
    class SubClass : BaseClass
    {
        public SubClass()
        { }
        public SubClass(ParentlessClass test, int integer)
        {
            Integer = integer;
            SomeString = test.SomeString;
        }
        public string SomeString { get; set; }
    }

    class TypeName
    {
        public SubClass field;

        public SubClass Getter
        {
            get { return field; }
        }
        public void T(ParentlessClass eh)
        {
            var t2 = new SubClass() { Integer = default(int)};
            var t = new ParentlessClass() { };
        }
    }
}