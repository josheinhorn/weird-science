# Weird Science
Weird Science is a lightweight .NET library that helps to perform experiments in sensitive environments when Unit Tests just won't do. This was directly inspired by Github's Ruby [Scientist](https://github.com/github/scientist) library. As they say, this is meant to aid in "carefully refactoring critical paths."

## A simple example

The below example creates an experiment using `Laboratory`'s static helper method and uses the Fluent Interface to build up an Experiment. (Note that the methods `DoSomething` and `DoSomethingElse` return `string` results)
```C#
Laboratory.SetPublisher(new MyCustomPublisher());
```
```C#
List<string> foo = GetStringList();
string result = Laboratory.DoScience("Science!", () => DoSomething(foo))
   .Candidate("candidate", () => DoSomethingElse(foo))
   .PreCondition(() => foo.Count > 3)
   .Setup(() => foo.Add("bar"))
   .SetContext(() => new MyContextObject { Time = DateTime.Now, Name = "bar" })
   .AreEqual((ctrl, cand) => ctrl.Length == cand.Length)
   .OnMismatch((ctrl, cand, ctrlExcp, candExcp) => "Oops! Mismatch!!")
   .Ignore((ctrl, cand) => cand.StartsWith("Hello"))
   .OnError((err) => "Yikes, An error occurred!! " + err.ErrorMessage)
   .Teardown(() => { foo.Remove("bar"); return foo.Count + " items left.";})
   .Run(); //Executes everything and calls Publisher to write output

// Continue execution of regular program, result is output from DoSomething(foo)
...
```
## What do I use this for?
Good question! Basically, you use it to set up "Experiments" that can run in sensitive (read 'Production') environments. An Experiment should really be thought of in the classical sense of the word &mdash; you run a set of Candidates against a Control and evaluate the results.

One intended use case is when Unit Tests, Integration Tests, and QA tests are impractical. This is when it is simply too difficult to predict or recreate the conditions that exist in a Production environment, such as high load or unknown/inconsistent data states.

Another use case is when the stakes are simple too high to rely on test environments to properly evaluate impacts of major changes before rolling out to Production. How many times has your boss or coworker told you not to refactor a set of classes because the risk of breaking Production is too high?

## How it works
Though you can't really tell from the simple example, an `IExperiment` object is getting configured by the fluent interface, and then executed by the `Run()` method.

The Experiment internals take care of handling Exceptions, running the Control and Candidates, tracking internal state, recording results to pass to the Publisher, and of course returning the exact result that the Control returns (including throwing an Exception if that is what the Control does).

When the Experiment is run, it uses the delegates to determine the flow of execution. For instance, the Experiment Candidates won't run unless the `PreCondition` delegate evaluates to `true`. The use of delegates allows users of Weird Science to customize each experiment with just a few lines of code instead of declaring entire classes for simple tasks (though as you'll see later you can also extend the `Experiment` class).

At a high level, general flow of execution (while handling Exceptions and tracking state) is

**Control**
  1. SetContext
  - Control
  - Prepare
  - OnError

**Candidates**

Each Candidate follows the same process (unless an internal error occurs)
  1. PreCondition
  - SetContext
  - Setup
  - Candidate
  - Ignore
  - AreEqual
  - OnMismatch
  - Prepare
  - Teardown
  - OnError

It is important to note that `OnError` is only called when an Exception is thrown from within one of the steps (specifically in a provided delegate). If an Exception is thrown from within `OnError`, an internal exception occurs that will only be thrown if the proper flag is set (see below).

## The Publisher
This is where the real magic happens. Users are required to write their own implementations of the `ISciencePublisher` interface in order to fully take advantage of Weird Science. There are only two methods to implement:

#### `void Publish<T>(IExperimentResult<T> results)`
This method is called at the end of an Experiment run and is expected to perform some type of external I/O in order to persist some part of the results. `IExperimentResult<T>` contains a lot of info, including Elapsed Time, Exceptions thrown, the actual result (as set by the `Prepare` step) and a few other pieces of data.



#### `void Publish<T>(string message, IExperimentState<T> state)`
This method is called at various stages of the Experiment execution and is passed messages from the various delegate methods including:
- Setup
- OnMismatch
- OnError
- Teardown

`state` contains relevant info for the context of the call.

### Basic example
A basic (albeit not very useful) example:

```C#
public class ConsolePublisher : ISciencePublisher
{
    private StringBuilder messages = new StringBuilder();
    public virtual void Publish<T>(string message, IExperimentState<T> state)
    {
        if (!string.IsNullOrEmpty(message))
            messages.AppendFormat("{3} - Message from Experiment '{0}' in Step "
            + "'{1}': {2}\n", state.Name, state.CurrentStep, message,
            state.Timestamp.ToLongTimeString());
    }
    public virtual void Publish<T>(IExperimentResult<T> results)
    {
        Console.WriteLine("Experiment '{0}' Results", results.Name);
        PublishObservation(results.Control); //Writes some basic info to std out
        foreach (var obs in results.Candidates)
        {
            PublishObservation(obs.Value); //Writes some basic info to std out
        }
        Console.WriteLine(messages.ToString());
        messages.Clear();
    }
    private static void PublishObservation<T>(IObservation<T> observation)
    {
        Console.WriteLine(observation.Name);
        Console.WriteLine("Took: {0} ms, Exception?: {1}{2}, Output Value: {3}",
            observation.ElapsedMilliseconds, observation.ExceptionThrown,
            observation.ExceptionThrown ? string.Format("(Exception: {0})",
            observation.ExperimentError.LastException.Message)
            : string.Empty, observation.Value);
    }
}
```

## The Laboratory
The Laboratory class has a set of static methods to get Experiments running quickly. It is important to note that you must call the `SetPublisher(ISciencePublisher)` method and pass in the Publisher of your choosing in order to reap the benefits of Weird Science.

If you desire a bit more control over your workspace, you can instantiate your own `Laboratory<T, TPublish>` and pass in the desired constructor arguments. You can also actually extend the `Laboratory` class if even greater flexibility is desired.

```C#
 var lab = new Laboratory<string, char>("Science!", new StatsDPublisher());
 lab
    .Control(() => DoSomething(foo))
    .Candidate(() => DoSomething(bar))
    ...
```

If you're feeling even more adventurous, you can even pass in a custom `IExperiment` to the `Laboratory`!

## The Experiment
Behind the scenes of the `Laboratory`, the majority of work is actually being done by an `IExperiment` object. The basic implementation is `WeirdScience.Experiment` but users of the library are more than welcome to extend and override certain methods to give even greater control. For instance, you might _always_ want to do the exact same thing for the `OnMismatch` step. Instead of passing the same delegate all the time, you could create your own `Experiment`:

```C#
public class MyCustomExperiment<T, TPublish> : Experiment<T, TPublish>
{
  public MyCustomExperiment(string name, ISciencePublisher publisher)
    : base(string name, ISciencePublisher publisher)
    { }
  public override string OnMismatch(T control, T candidate,
    Exception controlException, Exception candidateException)
  {
      // Can still use delegates in addition to custom implementation
      // by calling the Base method
      var baseMsg = base.OnMismatch(control, candidate, controlException,
        candidateException, controlException);
      return string.Format("There was a mismatch: {0}, {1}, {2}, {3}",
        candidate, control, candidateException, controlException);
  }
}
```

```C#
var myExperiment =
    new MyCustomExperiment<string, string>("Customized!", new CustomPublisher());
var myLab = new Laboratory<string, string>(myExperiment);
myLab
   .Control(() => DoSomething(foo))
   .Candidate(() => DoSomething(bar))
   ...
```

The base implementation of `Experiment` handles Exceptions and tracking state, so implementers do not need to worry about the internal details.

## The Steps
As you can see from the earlier example, there are a number of optional steps that users can define when using the out-of-the-box functionality.

### Control
This is possibly the most important step &mdash; it is the actual function that should run for this Experiment. Its result will be used to compare to each of the Candidates and will be returned as the final output.

This Step runs for the Control.

_Delegate Type:_ `Func<T>`

### PreCondition
This is a function that determines whether or not the Experiment should actually run a Candidate, including all of the subsequent steps. If this evaluates to `false`, none of the other steps will run for the Candidate. This allows users to limit the effects of running Experiments in Production by limiting when an Experiment is allowed to run. This could be as simple as running Candidates only a percentage of the time or as complex as checking the time of day and the CPU/Memory load of the running Process.

This Step does _not_ run for the Control.

_Delegate Type:_ `Func<bool>`

### SetContext
This is a function that returns a single object. This object is later passed to the Publisher to give the publish process greater insight into the process.

This Step _does_ run for the Control.

_Delegate Type:_ `Func<object>`

### Setup
This is a function that runs before the Candidate is actually run, and optionally can return a `string`. The `string` result is passed to the Publisher. This Step can be used to prepare objects for use by the Candidates (such as cloning the original input object) as well as write messages to be used in the Publish process.

This Step does _not_ run for the Control.

_Delegate Type:_ `Func<string>` _or_ `Action`

### Candidate
This is a function that will run and its result will be compared to the result of the Control. Users can define multiple Candidates and each must be assigned a unique name (`string` value).

This Step does _not_ run for the Control.

_Delegate Type:_ `Func<T>`

### Ignore
This function determines whether or not to ignore a set of Candidate and Control results from being compared. If this evaluates to `true`, the AreEqual and OnMismatch steps will not run for this Candidate. If it evaluates to `false` (default) the AreEqual method will be invoked (if it is set).

This Step does _not_ run for the Control.

_Delegate Type:_ `Func<T, T, bool>`

### AreEqual
This function determines if two results are equivalent &mdash; `true` if they are and `false` if they are not. The default for this is to use `EqualityComparer<T>.Default`. There is no guarantee that both values are not null.

This Step does _not_ run for the Control.

_Delegate Type:_ `Func<T, T, bool>`

### OnMismatch
This function is invoked if a Candidate result is not equal to the Control result, if one throws and Exception and the other does not, or if both throw Exceptions but the Exceptions are not equal. It can optionally return a `string` message that will be passed to the Publisher along with the current state of the Experiment.  

**_Important_**: Because this step runs when Exceptions are thrown, there is very little fallback if this method throws an Exception. Though Weird Science _does_ handle such Exceptions, the Experiment execution is completely interrupted.

This Step does _not_ run for the Control.

_Delegate Type:_ `Func<T, T, Exception, Exception, string>` or `Action<T, T, Exception, Exception>`

### Prepare
This function can transform or alter the result of the Control and Candidates before results are stored for Publish. This is most often used when the actual result object is large (e.g. list of complex objects) or when you actually only care about a very small part of the result object. This function may return the same Type as the Control/Candidate functions, or an entirely different Type.

This Step _does_ run for the Control.

_Delegate Type:_ `Func<T, TPublish>`

### Teardown
This is a function that runs after the Candidate is run, and optionally can return a `string`. The `string` result is passed to the Publisher. This Step can be used to clean up objects (such as restoring things to a previous state) as well as write messages to be used in the Publish process.

This Step does _not_ run for the Control.

_Delegate Type:_ `Func<string>` or `Action`

### OnError
This function runs when an Exception is thrown by one of the other steps. It will receive an `IExperimentError` object which contains relevant info about the failure, including the Exception thrown and which Step failed. It can optionally return a `string` that will be sent to the Publisher along with the current state.

**_Important_**: There is very little fallback for this method if an Exception is thrown. Though Weird Science _does_ handle such Exceptions, the Experiment execution is completely interrupted.

This Step _does_ run for the Control.

_Delegate Type:_ `Func<IExperimentError, string>` _or_ `Action<IExperimentError>`

## Future work
There are definite plans to add two additional steps, `SetTimeout` and `OnTimeout`. These steps will allow users to define the maximum amount of time the Experiment should wait for Candidate results before moving on and then take an action if there is a time out.

Having a time out value is meant to be used in situations where the performance of a Candidate is unknown or when the execution of the program simple cannot afford to wait.

One possible use of the `OnTimeout` step could be to set a state (e.g. static variable) that would be used in the `PreCondition` step to activate/deactive certain Experiments when performance is suffering.

There is also a tentative plan to add a `RunInParallel` step which would return a `bool` that determines if the Candidates should run on separate Threads. This could potentially speed up performance but would greatly increase the complexity of running an Experiment since users would have to address concurrency issues with shared resources.
