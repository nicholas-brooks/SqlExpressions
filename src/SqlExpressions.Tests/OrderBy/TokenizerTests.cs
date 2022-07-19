using SqlExpressions.OrderBy.Parsing;
using Xunit;

namespace SqlExpressions.Tests.OrderBy;

public class TokenizerTests
{
    [Fact]
    public void Test()
    {
        var tokenizer = new OrderByTokenizer();
        var list = tokenizer.Tokenize("OrderNo asc, OrderDate desc");
        Assert.NotEmpty(list);
        Assert.Equal(OrderByToken.Identifier, list.ElementAt(0).Kind);
        Assert.Equal(OrderByToken.Ascending, list.ElementAt(1).Kind);
        Assert.Equal(OrderByToken.Comma, list.ElementAt(2).Kind);
        Assert.Equal(OrderByToken.Identifier, list.ElementAt(3).Kind);
        Assert.Equal(OrderByToken.Descending, list.ElementAt(4).Kind);
    }
    
}