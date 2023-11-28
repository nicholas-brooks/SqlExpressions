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
        public Guid Id { get; set; }
    }
    
    private static readonly List<TestClass> TestData = new()
    {
        new() {On = true, One = 1, Amount = 34, Three = "one", TimeOnly = new TimeOnly(23, 12), Status = Status.Closed},
        new() {On = false, One = 2, Two = 2, Three = "three", Datetime = new DateTime(2022, 10, 10, 11, 12, 34), Status = Status.Pending},
        new() {On = true, One = 3, Three = "three more", DateOnly = new DateOnly(2022, 10, 10), Id = Guid.Parse("A05CAC98-19DC-4AF2-90FE-ED384C35E34F"), Status = Status.Active},
    };
    

    private readonly ITestOutputHelper output;

    public LinqExpressionCompilerTests(ITestOutputHelper output)
    {
        this.output = output;
    }

    [Theory]
    [InlineData("Status = 'Active'", 1)]
    [InlineData("Status = 'active'", 1)]
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
    [InlineData("Id = 'A05CAC98-19DC-4AF2-90FE-ED384C35E34F'", 1)]
    public void Test(string expression, int expected)
    {
        TestCaseFilterExpression(expression, true, expected);
    }

    [Fact]
    public void WhenPropertyTypeIsNotNullableIsNullCompilesToConstant()
    {
        TestCaseFilterExpression("One is null", true, 0);
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
        TestCaseFilterExpression(expression, true, expected);
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
        TestCaseFilterExpression(expression, isValid, expected);
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
        TestCaseFilterExpression(expression, isValid, expected);
    }

    [Theory]
    [InlineData("One in [1, 2, 4]", true, 2)]
    [InlineData("Status in ['Active', 'OnHold']", true, 1)]
    [InlineData("Three in ['one', 'three']", true, 2)]
    public void CompileInExpression(string expression, bool isValid, int expected)
    {
        TestCaseFilterExpression(expression, isValid, expected);
    }
    
    [Theory]
    [InlineData("One not in [1, 2, 4]", true, 1)]
    [InlineData("Status not in ['Active', 'OnHold']", true, 2)]
    [InlineData("Three not in ['one', 'three']", true, 1)]
    public void CompileNotInExpression(string expression, bool isValid, int expected)
    {
        TestCaseFilterExpression(expression, isValid, expected);
    }
        
    [Theory]
    [InlineData("One not in [2] and Three = 'one'", true, 1)]
    public void CompileAndExpression(string expression, bool isValid, int expected)
    {
        TestCaseFilterExpression(expression, isValid, expected);
    }
        
    [Theory]
    [InlineData("One not in [2] or Three = 'one'", true, 2)]
    public void CompileOrExpression(string expression, bool isValid, int expected)
    {
        TestCaseFilterExpression(expression, isValid, expected);
    }

    [Theory]
    [InlineData("One = 1 and (Three = 'one' or Three like 'three%')", 1)]
    public void CompileCompoundExpression(string expression, int expected)
    {
        TestCaseFilterExpression(expression, true, expected);
    }
    
    private void TestCaseFilterExpression(string expression, bool isValid, int expected)
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
}