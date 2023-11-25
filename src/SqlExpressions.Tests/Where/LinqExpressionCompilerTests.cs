using System.Linq.Expressions;
using SqlExpressions.Where.Compiling;
using Xunit;
using Xunit.Abstractions;

namespace SqlExpressions.Tests.Where;

public class LinqExpressionCompilerTests
{
    enum Status
    {
        Pending,
        Active,
        OnHold,
        Closed
    }
    class TestClass
    {
        public Status Status { get; set; }
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
    
    private static readonly List<TestClass> TestData = new()
    {
        new() {On = true, One = 1, Amount = 34, Three = "one", TimeOnly = new TimeOnly(23, 12)},
        new() {On = false, One = 2, Two = 2, Three = "three", Datetime = new DateTime(2022, 10, 10, 11, 12, 34)},
        new() {On = true, One = 3, Three = "three more", DateOnly = new DateOnly(2022, 10, 10)},
    };
    

    private readonly ITestOutputHelper output;

    public LinqExpressionCompilerTests(ITestOutputHelper output)
    {
        this.output = output;
    }

    [Theory]
    [InlineData("Status = 'Active'", 0)]
    [InlineData("Status = 'active'", 0)]
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
        var compiler = new LinqExpressionCompiler();
        var linqExpression = compiler.Compile<TestClass>(query.ParseWhere());

        output.WriteLine(linqExpression.ToString());

        var results = TestData.Where(linqExpression.Compile());
        Assert.Equal(expectedToFind, results.Count());
    }

    [Fact]
    public void WhenPropertyTypeIsNotNullableIsNullCompilesToConstant()
    {
        var compiler = new LinqExpressionCompiler();
        var linqExpression = compiler.Compile<TestClass>("One is null".ParseWhere());

        output.WriteLine(linqExpression.ToString());

        var results = TestData.Where(linqExpression.Compile());
        Assert.Empty(results);
    }
    
    [Theory]
    [InlineData("1 = 1", 3)]
    [InlineData("1 <> 1", 0)]
    [InlineData("1 > 1", 0)]
    [InlineData("1 >= 1", 3)]
    [InlineData("1 < 1", 0)]
    [InlineData("1 <= 1", 3)]
    [InlineData("'Yes' = 'No'", 0)]
    public void CompileConstantBinaryExpression(string expression, int expected)
    {
        var compiler = new LinqExpressionCompiler();
        var linqExpression = compiler.Compile<TestClass>(expression.ParseWhere());

        output.WriteLine(linqExpression.ToString());

        var results = TestData.Where(linqExpression.Compile());
        Assert.Equal(expected, results.Count());
    }

    [Theory]
    [InlineData("Amount like 45.23", false, 0)]
    [InlineData("Three like null", false, 0)]
    [InlineData("Three like ''", true, 0)]
    [InlineData("Three like 'three'", true, 1)]
    [InlineData("Three like 'three%'", true, 2)]
    [InlineData("Three like '%hree'", true, 1)]
    [InlineData("Three like '%'", true, 3)]
    [InlineData("Three like '%hre%'", true, 2)]
    public void CompileLikeExpression(string expression, bool isValid, int expected)
    {
        var compiler = new LinqExpressionCompiler();
        var intermediateExpression = expression.ParseWhere();

        if (!isValid)
        {
            Assert.Throws<ArgumentException>(() => compiler.Compile<TestClass>(intermediateExpression));
            return;
        }

        var linqExpression = compiler.Compile<TestClass>(intermediateExpression);

        output.WriteLine(linqExpression.ToString());
        
        var results = TestData.Where(linqExpression.Compile());
        Assert.Equal(expected, results.Count());
    }
    

    [Theory]
    [InlineData("Amount not like 45.23", false, 0)]
    [InlineData("Three not like null", false, 0)]
    [InlineData("Three not like ''", true, 3)]
    [InlineData("Three not like 'three'", true, 2)]
    [InlineData("Three not like 'three%'", true, 1)]
    [InlineData("Three not like '%hree'", true, 2)]
    [InlineData("Three not like '%'", true, 0)]
    [InlineData("Three not like '%hre%'", true, 1)]
    public void CompileNotLikeExpression(string expression, bool isValid, int expected)
    {
        var compiler = new LinqExpressionCompiler();
        var intermediateExpression = expression.ParseWhere();

        if (!isValid)
        {
            Assert.Throws<ArgumentException>(() => compiler.Compile<TestClass>(intermediateExpression));
            return;
        }

        var linqExpression = compiler.Compile<TestClass>(intermediateExpression);

        output.WriteLine(linqExpression.ToString());
        
        var results = TestData.Where(linqExpression.Compile());
        Assert.Equal(expected, results.Count());
    }

    [Fact(Skip = "Not supported yet")]
    public void CompileInExpression()
    {
        
    }

}