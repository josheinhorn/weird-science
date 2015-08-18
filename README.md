# Weird Science
A lightweight .NET library that helps to perform experiments in sensitive environments when Unit Tests just won't do.

## Examples using Fluent Syntax

The below example creates an experiment using `Laboratory`'s static helper method. Note that the methods `DoSomething` and `DoSomethingElse` return `string` results:

```C#
Laboratory.SetPublisher(new MyCustomPublisher());
var foo = new List<string>();
var result = Laboratory.DoScience("Science!", () => DoSomething(foo), true)
   .Candidate("candidate", () => { foo.Add("bar"); return DoSomethingElse(foo); })
   .AreEqual((ctrl, cand) => ctrl.Length == cand.Length)
   .OnMismatch((ctrl, cand, ctrlExcp, candExcp) => "Oops! Mismatch!!")
   .Ignore((ctrl, cand) => cand.StartsWith("Hello"))
   .OnError((err) => "Yikes, An error occurred!! " + err.ErrorMessage)
   .Teardown(() => "ending with " + foo.Count + " items.")
   .Setup(() => "starting with " + foo.Count + " items.")
   .Run();
```