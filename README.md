# SqlExpressions

Library to help you parse a Where and Order BY expression.

It can produce an AST and also compile it to an actual SQL string.

The Where expression can also be compiled into Linq Expression tree to be passed to a Linq query.

# How to use

## To SQL String

```
var expression = "OrderNo > '2342234' and Status in ['Open', 'Processing']".ParseWhere();
var sqlWhere = expression.CompileToString((name) => $"[Orders].[{name}]");
```

Should produce:

```
[Orders].[OrderNo] > '2342234' and [Orders].[Status] in ('Open', 'Processing')
```

## To Linq Expression

```
var expression = "OrderNo > '2342234' and Status in ['Open', 'Processing']".ParseWhere();
var linqExpression = expression.CompileToLinq<Order>();

var orders = DataSet.Orders.Where(linqExpression);

```

# Status

This is most definitely a work in progress.  The following are outstanding for a v1.0:

- [ ] Be able to identify a date/datetime string as a Date/DateTime type (e.g. '2022-01-01 12:23:23')
- [ ] Convert the `in [ ... ]` syntax to standard sql `in (...)`.


# Recognition

This library would not be possible without relying on the awesome [SuperPower](https://github.com/datalust/superpower/) by [Datalust](https://datalust.co/).