using System.Linq.Expressions;
using SqlExpressions.Where.Compiling;
using Xunit;

namespace SqlExpressions.Tests.Where;

public class LinqExpressionCompilerTests
{
    class TestClass
    {
        public int One { get; set; }
        public int? Two { get; set; }
        public string Three { get; set; } = null!;
        public string? Four { get; set; }
    }

    [Fact]
    public void Test()
    {
        var expression = "One = 1".ParseWhere();
        var compiler = new LinqExpressionCompiler();
        var linqExpression = compiler.Compile<TestClass>(expression);
        var objs = new List<TestClass>
        {
            new TestClass() { One = 1, Three = "three" },
            new TestClass() { One = 2, Three = "three" },
            new TestClass() { One = 3, Three = "three" },
        };
        var lambda = Expression.Lambda<Func<TestClass, bool>>(linqExpression).Compile();
        var results = objs.Where(lambda);
        Assert.Single(results);
    }
    
}