using System.Linq.Expressions;
using SqlExpressions.Where.Compiling;
using Xunit;
using Xunit.Abstractions;

namespace SqlExpressions.Tests.Where;

public class LinqExpressionCompilerTests
{
    class TestClass
    {
        public bool On { get; set; }
        public int One { get; set; }
        public int? Two { get; set; }
        public decimal Amount { get; set; } 
        public string Three { get; set; } = null!;
        public string? Four { get; set; }
        public DateOnly DateOnly { get; set; }
        public DateTime Datetime { get; set; }
        public TimeOnly TimeOnly { get; set; }
    }
    
    private readonly ITestOutputHelper output;

    public LinqExpressionCompilerTests(ITestOutputHelper output)
    {
        this.output = output;
    }

    [Theory]
    [InlineData("One = 1", 1)]
    [InlineData("One <> 2", 2)]
    [InlineData("One > 2", 1)]
    [InlineData("One < 2", 1)]
    [InlineData("One >= 2", 2)]
    [InlineData("One <= 2", 2)]
    [InlineData("2 <= One", 2)]
    [InlineData("false = On", 1)]
    [InlineData("true = On", 2)]
    [InlineData("Amount > 33.5", 1)]
    [InlineData("Amount is null", 0)]
    [InlineData("Amount is not null", 3)]
    [InlineData("Two = 2", 1)]
    [InlineData("Three is null", 0)]
    [InlineData("Three is not null", 3)]
    [InlineData("Four is null", 3)]
    [InlineData("Four is not null", 0)]
    [InlineData("DateOnly = '2022-10-10'", 1)]
    [InlineData("TimeOnly = '11:12pm'", 1)]
    [InlineData("DateTime = '2022-10-10T11:12:34'", 1)]
    public void Test(string query, int expectedToFind)
    {
        List<TestClass> objs = new List<TestClass>
        {
            new() { On = true, One = 1, Amount = 34, Three = "three", TimeOnly = new TimeOnly(23, 12)},
            new() { On = false, One = 2, Two = 2, Three = "three", Datetime = new DateTime(2022, 10, 10, 11,12,34)},
            new() { On = true, One = 3, Three = "three", DateOnly = new DateOnly(2022, 10, 10)},
        };

        var compiler = new LinqExpressionCompiler();
        var linqExpression = compiler.Compile<TestClass>(query.ParseWhere());
        
        output.WriteLine(linqExpression.ToString());

        var results = objs.Where(linqExpression.Compile());
        Assert.Equal(expectedToFind, results.Count());
    }
    
}