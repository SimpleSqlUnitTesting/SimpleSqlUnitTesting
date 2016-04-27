# Simple SQL Unit Testing Framework
Framework that greatly simplifies the development of SQL Unit Tests in Visual Studio.

## Benefits
Everyone that has used the SQL Unit Tests feature of Visual Studio will know that is far from perfect. The main problems being the weird bugs that prevent properly saving the files, and the cumbersome way of specifying the assertions. Other benefits are:
- No RESX!
- Work directly with MSTest test classes.
- Assertions for a whole table (no more Scalar Value Conditions for each of your cells!) 
- Fluent and terse assertions.
- You can paste results from SSMS directly into your assertion.
- Outputs joined SQL.
- Base classes for wrapping your test in a rollbacked transaction, either local or distributed.

## Getting started

## Feedback
Please provide feedback and ask questions [here](https://github.com/simplesqlunittesting/simplesqlunittesting/issues/new).