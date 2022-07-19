# SqlExpressions

Library to help you parse a Where and Order BY clause.

It can produce an AST and also compile it to an actual SQL string.

# How to use

```
var expression = "OrderNo > '2342234' and Status in ['Open', 'Processing']".ParseWhere();
var sqlWhere = expression.Compile((name) => $"[Orders].[{name}]");
```

Should produce:

```
[Orders].[OrderNo] > '2342234' and [Orders].[Status] in ('Open', 'Processing')
```

# Status

This is most definitely a work in progress.  The following are outstanding for a v1.0:

- [ ] Be able to identify a date/datetime string as a Date/DateTime type (e.g. '2022-01-01 12:23:23')
- [ ] Convert the `in [ ... ]` syntax to standard sql `in (...)`.
- [ ] Write a compiler to generate a Linq Expression based on a provided Type rather than just producing a sql-based string.


# Recognition

This library would not be possible without relying on the awesome [SuperPower](https://github.com/datalust/superpower/) by [Datalust](https://datalust.co/).