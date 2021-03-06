﻿using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace WeirdScience.Tests
{
    public class ExperimentFacts
    {
        #region Public Classes

        public class TheSteps
        {
            #region Protected Methods

            protected void SetupControlAndCandidate(Mock<IExperimentSteps<string, string>> steps,
                string ctrlResult, string candResult, string candName)
            {
                steps.SetupGet(x => x.Control).Returns(new Func<string>(() => ctrlResult));
                steps.Setup(x => x.GetCandidates())
                    .Returns(new Dictionary<string, Func<string>> { { candName, () => candResult } });
            }

            protected void SetupStateSnapshot(Mock<IExperimentState> state)
            {
                Operations step = Operations.Internal;
                state.SetupGet(x => x.CurrentStep).Returns(() => step);
                state.SetupSet<Operations>(x => x.CurrentStep = It.IsAny<Operations>())
                    .Callback(x => step = x);
                state.Setup(x => x.Snapshot())
                    .Returns(() =>
                    {
                        var ss = Mock.Of<IExperimentState>();
                        ss.CurrentStep = step;
                        return ss;
                    });
            }

            #endregion Protected Methods
        }

        /// <summary>
        /// Tests for when Exceptions are thrown from within the Steps
        /// </summary>
        public class TheStepsWithErrors : TheSteps
        {
            #region Public Methods

            [Theory, AutoMoqData]
            public void AreEqual(Mock<ISciencePublisher> publisher, Mock<IExperimentSteps<string, string>> steps,
               Mock<IExperimentState> state, string name, string ctrlResult, string candResult,
               string candName, InvalidProgramException excp, string errMsg)
            {
                //Setup
                AreEqualDelegate<string> areEqual = (ctrl, cand) =>
                {
                    throw excp;
                };
                bool excpPassed = false;
                Action<ErrorEventArgs> onError = (e) =>
                {
                    excpPassed = e.ExperimentError.LastException == excp
                        && e.ExperimentError.LastStep == Operations.AreEqual;
                    e.Publisher.Publish(errMsg, e.State);
                };
                steps.DefaultValue = DefaultValue.Empty;
                steps.SetupAllProperties();
                steps.SetupGet(x => x.AreEqual).Returns(areEqual);
                steps.Setup(x => x.OnError(It.IsAny<ErrorEventArgs>()))
                    .Callback(onError);
                SetupControlAndCandidate(steps, ctrlResult, candResult, candName);
                state.SetupAllProperties();
                SetupStateSnapshot(state);
                //Exercise
                var sut = new Experiment<string, string>(name, publisher.Object, state.Object, steps.Object, true);
                var result = sut.Run(); //no exception
                //Verify
                steps.Verify(x => x.AreEqual, Times.AtLeastOnce);
                steps.Verify(x => x.OnError(It.Is<ErrorEventArgs>(
                    a => a.State.CurrentStep == Operations.OnError && a.ExperimentError.LastException == excp
                    && a.ExperimentError.LastStep == Operations.AreEqual)), Times.AtLeastOnce);
                //Results correct
                publisher.Verify(x => x.Publish(It.Is<IExperimentResult<string>>(
                    r => !r.Control.ExceptionThrown && r.Control.Value == ctrlResult
                    && r.Candidates.All(kvp => kvp.Value.ExceptionThrown
                    && kvp.Value.ExperimentError.LastException == excp
                    && kvp.Value.ExperimentError.LastStep == Operations.AreEqual))), Times.Once);
                //Message published
                publisher.Verify(x => x.Publish(errMsg,
                   It.Is<IExperimentState>(y => y.CurrentStep == Operations.OnError)), Times.AtLeastOnce);
                Assert.True(excpPassed);
            }

            [Theory, AutoMoqData]
            public void Candidate(Mock<ISciencePublisher> publisher,
                Mock<IExperimentSteps<string, string>> steps, Mock<IExperimentState> state,
                string name, string ctrlResult, string candResult, string candName, InvalidProgramException excp,
                string errMsg)
            {
                //Setup
                steps.DefaultValue = DefaultValue.Empty;
                steps.SetupAllProperties();
                bool excpPassed = false;
                Action<ErrorEventArgs> onError = (e) =>
                {
                    excpPassed = e.ExperimentError.LastException == excp
                        && e.ExperimentError.LastStep == Operations.Candidate;
                    e.Publisher.Publish(errMsg, e.State);
                };
                Func<string> candidate = () =>
                {
                    throw excp;
                };
                SetupControlAndCandidate(steps, ctrlResult, candResult, candName);
                steps.Setup(x => x.GetCandidates())
                    .Returns(new Dictionary<string, Func<string>> { { candName, candidate } });
                steps.Setup(x => x.OnError(It.IsAny<ErrorEventArgs>()))
                    .Callback(onError);
                state.SetupAllProperties();
                SetupStateSnapshot(state);
                //Exercise
                var sut = new Experiment<string, string>(name, publisher.Object, state.Object, steps.Object, true);
                var result = sut.Run(); //No exceptions should be thrown
                //Verify
                steps.Verify(x => x.GetCandidates(), Times.AtLeastOnce);
                steps.Verify(x => x.OnError(It.Is<ErrorEventArgs>(
                    a => a.State.CurrentStep == Operations.OnError && a.ExperimentError.LastException == excp
                    && a.ExperimentError.LastStep == Operations.Candidate)), Times.AtLeastOnce);
                //Results published
                publisher.Verify(x => x.Publish(It.Is<IExperimentResult<string>>(
                    r => r.Candidates.All(kvp => kvp.Key.Equals(candName) && kvp.Value.ExceptionThrown
                    && kvp.Value.Name.Equals(candName) && kvp.Value.IsMismatched
                    && kvp.Value.ExperimentError.LastException == excp
                    && kvp.Value.ExperimentError.LastStep == Operations.Candidate))), Times.Once);
                publisher.Verify(x => x.Publish(errMsg,
                   It.Is<IExperimentState>(y => y.CurrentStep == Operations.OnError)), Times.AtLeastOnce);
                Assert.True(excpPassed);
            }

            [Theory, AutoMoqData]
            public void Control(Mock<ISciencePublisher> publisher,
                Mock<IExperimentSteps<string, string>> steps, Mock<IExperimentState> state,
                string name, string ctrlResult, string candResult, string candName, InvalidProgramException excp,
                string errMsg)
            {
                //Setup
                steps.DefaultValue = DefaultValue.Empty;
                steps.SetupAllProperties();
                bool excpPassed = false;
                Action<ErrorEventArgs> onError = (e) =>
                {
                    excpPassed = e.ExperimentError.LastException == excp
                        && e.ExperimentError.LastStep == Operations.Control;
                    e.Publisher.Publish(errMsg, e.State);
                };
                Func<string> control = () =>
                {
                    throw excp;
                };
                SetupControlAndCandidate(steps, ctrlResult, candResult, candName);
                steps.SetupGet(x => x.Control)
                    .Returns(control);
                steps.Setup(x => x.OnError(It.IsAny<ErrorEventArgs>()))
                    .Callback(onError);
                state.SetupAllProperties();
                SetupStateSnapshot(state);
                //Exercise
                var sut = new Experiment<string, string>(name, publisher.Object, state.Object, steps.Object, true);
                var result = Assert.Throws<InvalidProgramException>(() => sut.Run()); //Exceptions should be thrown
                //Verify
                steps.Verify(x => x.GetCandidates(), Times.AtLeastOnce); //Candidates still run
                steps.Verify(x => x.Control, Times.AtLeastOnce);
                steps.Verify(x => x.OnError(It.Is<ErrorEventArgs>(
                    a => a.State.CurrentStep == Operations.OnError && a.ExperimentError.LastException == excp
                    && a.ExperimentError.LastStep == Operations.Control)), Times.AtLeastOnce);
                //Results published
                publisher.Verify(x => x.Publish(It.Is<IExperimentResult<string>>(
                    r => r.Control.ExceptionThrown && r.Control.ExperimentError.LastException == excp
                    && r.Control.ExperimentError.LastStep == Operations.Control)), Times.Once);
                publisher.Verify(x => x.Publish(errMsg,
                   It.Is<IExperimentState>(y => y.CurrentStep == Operations.OnError)), Times.AtLeastOnce);
                Assert.True(excpPassed);
                Assert.Equal(result, excp);
            }

            [Theory(Skip = "Now we allow Control to not be set (candidate only experiments)"), AutoMoqData]
            public void Control_Not_Set_Throws_Exception(Mock<ISciencePublisher> publisher,
                Mock<IExperimentSteps<string, string>> steps, Mock<IExperimentState> state,
                string name, string ctrlResult, string candResult, string candName)
            {
                //Setup
                steps.DefaultValue = DefaultValue.Empty;
                steps.SetupAllProperties();
                Func<string> control = null;
                SetupControlAndCandidate(steps, ctrlResult, candResult, candName);
                steps.SetupGet(x => x.Control).Returns(control);
                state.SetupAllProperties();
                //Exercise
                var sut = new Experiment<string, string>(name, publisher.Object, state.Object, steps.Object, true);
                var result = Assert.Throws<InvalidOperationException>(() => sut.Run()); //Exceptions should be thrown
                //Verify
                steps.Verify(x => x.GetCandidates(), Times.Never); //Nothing runs
                steps.Verify(x => x.Control, Times.AtLeastOnce);
                // Nothing published
                publisher.Verify(x => x.Publish(It.IsAny<IExperimentResult<string>>()), Times.Never);
                publisher.Verify(x => x.Publish(It.IsAny<string>(), It.IsAny<IExperimentState>()),
                    Times.Never);
            }
            [Theory, AutoMoqData]
            public void Control_Not_Set(Mock<ISciencePublisher> publisher,
                Mock<IExperimentSteps<string, string>> steps, Mock<IExperimentState> state,
                string name, string ctrlResult, string candResult, string candName)
            {
                //Setup
                steps.DefaultValue = DefaultValue.Empty;
                steps.SetupAllProperties();
                Func<string> control = null;
                SetupControlAndCandidate(steps, ctrlResult, candResult, candName);
                steps.SetupGet(x => x.Control).Returns(control);
                state.SetupAllProperties();
                //Exercise
                var sut = new Experiment<string, string>(name, publisher.Object, state.Object, steps.Object, true);
                var result = sut.Run();
                //Verify
                steps.Verify(x => x.GetCandidates(), Times.AtLeastOnce); // still runs
                steps.Verify(x => x.Control, Times.AtLeastOnce);
                // Still publishes
                publisher.Verify(x => x.Publish(It.Is<IExperimentResult<string>>(r => r.Control == null)), 
                    Times.AtLeastOnce);
                Assert.Equal(default(string), result);

            }
            [Theory, AutoMoqData]
            public void Ignore(Mock<ISciencePublisher> publisher, Mock<IExperimentSteps<string, string>> steps,
                Mock<IExperimentState> state, string name, string ctrlResult, string candResult,
                string candName, InvalidProgramException excp, string errMsg)
            {
                //Setup
                IgnoreDelegate<string> ignore = (ctrl, cand) =>
                {
                    throw excp;
                };
                bool excpPassed = false;
                Action<ErrorEventArgs> onError = (e) =>
                {
                    excpPassed = e.ExperimentError.LastException == excp
                        && e.ExperimentError.LastStep == Operations.Ignore;
                    e.Publisher.Publish(errMsg, e.State);
                };
                steps.DefaultValue = DefaultValue.Empty;
                steps.SetupAllProperties();
                steps.SetupGet(x => x.Ignore).Returns(ignore);
                steps.Setup(x => x.OnError(It.IsAny<ErrorEventArgs>()))
                    .Callback(onError);
                SetupControlAndCandidate(steps, ctrlResult, candResult, candName);
                state.SetupAllProperties();
                SetupStateSnapshot(state);
                //Exercise
                var sut = new Experiment<string, string>(name, publisher.Object, state.Object, steps.Object, true);
                var result = sut.Run(); //no exception
                //Verify
                steps.Verify(x => x.Ignore, Times.AtLeastOnce);
                steps.Verify(x => x.OnError(It.Is<ErrorEventArgs>(
                    a => a.State.CurrentStep == Operations.OnError && a.ExperimentError.LastException == excp
                    && a.ExperimentError.LastStep == Operations.Ignore)), Times.AtLeastOnce);
                //Results correct
                publisher.Verify(x => x.Publish(It.Is<IExperimentResult<string>>(
                    r => !r.Control.ExceptionThrown && r.Control.Value == ctrlResult
                    && r.Candidates.All(kvp => kvp.Value.ExceptionThrown
                    && kvp.Value.ExperimentError.LastException == excp
                    && kvp.Value.ExperimentError.LastStep == Operations.Ignore))), Times.Once);
                //Message published
                publisher.Verify(x => x.Publish(errMsg,
                   It.Is<IExperimentState>(y => y.CurrentStep == Operations.OnError)), Times.AtLeastOnce);
                Assert.True(excpPassed);
            }

            [Theory, AutoMoqData]
            public void OnError_And_Control_ThrowInternalExceptions_False(Mock<ISciencePublisher> publisher,
                Mock<IExperimentSteps<string, string>> steps, Mock<IExperimentState> state, string name,
                string ctrlResult, string candResult, string candName, InvalidProgramException onErrorExcp,
                ApplicationException ctrlExcp)
            {
                //Setup
                Action<ErrorEventArgs> onError = (error) =>
                {
                    throw onErrorExcp;
                };
                Func<string> control = () => { throw ctrlExcp; };
                steps.DefaultValue = DefaultValue.Empty;
                steps.SetupAllProperties();
                steps.Setup(x => x.OnError(It.IsAny<ErrorEventArgs>()))
                    .Callback(onError);
                SetupControlAndCandidate(steps, ctrlResult, candResult, candName);
                steps.SetupGet(x => x.Control).Returns(control);
                state.SetupAllProperties();
                SetupStateSnapshot(state);
                //Exercise
                var sut = new Experiment<string, string>(name, publisher.Object, state.Object, steps.Object, false);
                // OnError Exception swallowed because throwOnInternalExceptions = false, but still
                // throws the control's Exception
                var result = Assert.Throws<ApplicationException>(() => sut.Run());
                //Verify
                steps.Verify(x => x.OnError(It.Is<ErrorEventArgs>(
                    a => a.State.CurrentStep == Operations.OnError && a.ExperimentError.LastException == ctrlExcp
                    && a.ExperimentError.LastStep == Operations.Control)), Times.AtLeastOnce);
                steps.Verify(x => x.GetCandidates(), Times.AtLeastOnce); //Candidates still run in this case
                //Final results ARE published, but the Control Observation is not properly filled out
                publisher.Verify(x => x.Publish(It.Is<IExperimentResult<string>>(
                    r => r.Control == null)), Times.AtLeastOnce);
                //Error Message NOT published
                publisher.Verify(x => x.Publish(It.IsAny<string>(),
                   It.Is<IExperimentState>(y => y.CurrentStep == Operations.OnError)), Times.Never);
                Assert.Equal(result, ctrlExcp); //Still throws the right exception
            }

            [Theory, AutoMoqData]
            public void OnError_And_Control_ThrowInternalExceptions_True(Mock<ISciencePublisher> publisher,
                Mock<IExperimentSteps<string, string>> steps, Mock<IExperimentState> state, string name,
                string ctrlResult, string candResult, string candName, InvalidProgramException onErrorExcp,
                ApplicationException ctrlExcp)
            {
                //Setup
                Action<ErrorEventArgs> onError = (error) =>
                {
                    throw onErrorExcp;
                };
                Func<string> control = () => { throw ctrlExcp; };
                steps.DefaultValue = DefaultValue.Empty;
                steps.SetupAllProperties();
                steps.Setup(x => x.OnError(It.IsAny<ErrorEventArgs>()))
                    .Callback(onError);
                SetupControlAndCandidate(steps, ctrlResult, candResult, candName);
                steps.SetupGet(x => x.Control).Returns(control);
                state.SetupAllProperties();
                SetupStateSnapshot(state);
                //Exercise
                var sut = new Experiment<string, string>(name, publisher.Object, state.Object, steps.Object, true);
                // Exception thrown because throwOnInternalExceptions = true, but its a
                // StepFailedException, not an ApplicationException as would be expected
                var result = Assert.Throws<StepFailedException>(() => sut.Run());
                //Verify
                steps.Verify(x => x.OnError(It.Is<ErrorEventArgs>(
                    a => a.State.CurrentStep == Operations.OnError && a.ExperimentError.LastException == ctrlExcp
                    && a.ExperimentError.LastStep == Operations.Control)), Times.AtLeastOnce);
                steps.Verify(x => x.GetCandidates(), Times.Never); //Candidates don't run in this case
                //Final results NOT published
                publisher.Verify(x => x.Publish(It.IsAny<IExperimentResult<string>>()), Times.Never);
                //Message NOT published
                publisher.Verify(x => x.Publish(It.IsAny<string>(),
                   It.Is<IExperimentState>(y => y.CurrentStep == Operations.OnError)), Times.Never);
                Assert.Equal(result.InnerException, onErrorExcp); // Inner comes from OnError, not Control
            }

            [Theory, AutoMoqData]
            public void OnError_ThrowInternalExceptions_False(Mock<ISciencePublisher> publisher,
                Mock<IExperimentSteps<string, string>> steps, Mock<IExperimentState> state, string name,
                string ctrlResult, string candResult, string candName, InvalidProgramException excp,
                ApplicationException otherExcp)
            {
                //Setup
                Action<MismatchEventArgs<string>> onMismatch = (e) =>
                {
                    throw otherExcp;
                };
                Action<ErrorEventArgs> onError = (error) =>
                {
                    throw excp;
                };
                steps.DefaultValue = DefaultValue.Empty;
                steps.SetupAllProperties();
                steps.SetupGet(x => x.AreEqual).Returns((x, y) => false); //Make sure to cause a mismatch
                steps.Setup(x => x.OnMismatch(It.IsAny<MismatchEventArgs<string>>())).Callback(onMismatch);
                steps.Setup(x => x.OnError(It.IsAny<ErrorEventArgs>()))
                    .Callback(onError);
                SetupControlAndCandidate(steps, ctrlResult, candResult, candName);
                state.SetupAllProperties();
                SetupStateSnapshot(state);
                //Exercise
                var sut = new Experiment<string, string>(name, publisher.Object, state.Object, steps.Object, false);
                var result = sut.Run(); //Exception swallowed because throwOnInternalExceptions = false!
                //Verify
                steps.Verify(x => x.OnMismatch(It.IsAny<MismatchEventArgs<string>>()), Times.AtLeastOnce);
                steps.Verify(x => x.OnError(It.Is<ErrorEventArgs>(
                    a => a.State.CurrentStep == Operations.OnError && a.ExperimentError.LastException == otherExcp
                    && a.ExperimentError.LastStep == Operations.OnMismatch)), Times.AtLeastOnce);
                //Final results NOT published
                publisher.Verify(x => x.Publish(It.IsAny<IExperimentResult<string>>()), Times.Never);
                //Message NOT published
                publisher.Verify(x => x.Publish(It.IsAny<string>(),
                   It.Is<IExperimentState>(y => y.CurrentStep == Operations.OnError)), Times.Never);
                Assert.Equal(result, ctrlResult);
            }

            [Theory, AutoMoqData]
            public void OnError_ThrowInternalExceptions_True(Mock<ISciencePublisher> publisher,
                Mock<IExperimentSteps<string, string>> steps, Mock<IExperimentState> state, string name,
                string ctrlResult, string candResult,
               string candName, InvalidProgramException excp, ApplicationException otherExcp)
            {
                //Setup
                Action<MismatchEventArgs<string>> onMismatch = (e) =>
                {
                    throw otherExcp;
                };
                Action<ErrorEventArgs> onError = (error) =>
                {
                    throw excp;
                };
                steps.DefaultValue = DefaultValue.Empty;
                steps.SetupAllProperties();
                steps.SetupGet(x => x.AreEqual).Returns((x, y) => false); //Make sure to cause a mismatch
                steps.Setup(x => x.OnMismatch(It.IsAny<MismatchEventArgs<string>>())).Callback(onMismatch);
                steps.Setup(x => x.OnError(It.IsAny<ErrorEventArgs>()))
                    .Callback(onError);
                SetupControlAndCandidate(steps, ctrlResult, candResult, candName);
                state.SetupAllProperties();
                SetupStateSnapshot(state);
                //Exercise
                var sut = new Experiment<string, string>(name, publisher.Object, state.Object, steps.Object, true);
                //Exception thrown because throwOnInternalExceptions = true!
                var result = Assert.Throws<StepFailedException>(() => sut.Run());
                //Verify
                steps.Verify(x => x.OnError(It.Is<ErrorEventArgs>(
                    a => a.State.CurrentStep == Operations.OnError && a.ExperimentError.LastException == otherExcp
                    && a.ExperimentError.LastStep == Operations.OnMismatch)), Times.AtLeastOnce);
                //Final results NOT published
                publisher.Verify(x => x.Publish(It.IsAny<IExperimentResult<string>>()), Times.Never);
                //Message NOT published
                publisher.Verify(x => x.Publish(It.IsAny<string>(),
                   It.Is<IExperimentState>(y => y.CurrentStep == Operations.OnError)), Times.Never);
                Assert.Equal(result.InnerException, excp);
            }

            [Theory, AutoMoqData]
            public void OnMismatch_ThrowInternalExceptions_False(Mock<ISciencePublisher> publisher,
                Mock<IExperimentSteps<string, string>> steps, Mock<IExperimentState> state, string name,
                string ctrlResult, string candResult, string candName, InvalidProgramException excp, string errMsg)
            {
                //Setup
                Action<MismatchEventArgs<string>> onMismatch = (e) =>
                {
                    throw excp;
                };
                bool excpPassed = false;
                Action<ErrorEventArgs> onError = (e) =>
                {
                    excpPassed = e.ExperimentError.LastException == excp
                        && e.ExperimentError.LastStep == Operations.OnMismatch;
                    e.Publisher.Publish(errMsg, e.State);
                };
                steps.DefaultValue = DefaultValue.Empty;
                steps.SetupAllProperties();
                steps.SetupGet(x => x.AreEqual).Returns((x, y) => false); //Make sure to cause a mismatch
                steps.Setup(x => x.OnMismatch(It.IsAny<MismatchEventArgs<string>>())).Callback(onMismatch);
                steps.Setup(x => x.OnError(It.IsAny<ErrorEventArgs>()))
                    .Callback(onError);
                SetupControlAndCandidate(steps, ctrlResult, candResult, candName);
                state.SetupAllProperties();
                SetupStateSnapshot(state);
                //Exercise
                var sut = new Experiment<string, string>(name, publisher.Object, state.Object, steps.Object, false);
                var result = sut.Run(); //Exception swallowed because throwOnInternalExceptions = false!
                //Verify
                steps.Verify(x => x.OnMismatch(It.IsAny<MismatchEventArgs<string>>()), Times.AtLeastOnce);
                steps.Verify(x => x.OnError(It.Is<ErrorEventArgs>(
                    a => a.State.CurrentStep == Operations.OnError && a.ExperimentError.LastException == excp
                    && a.ExperimentError.LastStep == Operations.OnMismatch)), Times.AtLeastOnce);
                //Final results NOT published
                publisher.Verify(x => x.Publish(It.IsAny<IExperimentResult<string>>()), Times.Never);
                //Message published
                publisher.Verify(x => x.Publish(errMsg,
                   It.Is<IExperimentState>(y => y.CurrentStep == Operations.OnError)), Times.AtLeastOnce);
                Assert.True(excpPassed);
                Assert.Equal(result, ctrlResult);
            }

            [Theory, AutoMoqData]
            public void OnMismatch_ThrowInternalExceptions_True(Mock<ISciencePublisher> publisher,
                Mock<IExperimentSteps<string, string>> steps, Mock<IExperimentState> state, string name,
                string ctrlResult, string candResult, string candName, InvalidProgramException excp, string errMsg)
            {
                //Setup
                Action<MismatchEventArgs<string>> onMismatch = (e) =>
                {
                    throw excp;
                };
                bool excpPassed = false;
                Action<ErrorEventArgs> onError = (e) =>
                {
                    excpPassed = e.ExperimentError.LastException == excp
                        && e.ExperimentError.LastStep == Operations.OnMismatch;
                    e.Publisher.Publish(errMsg, e.State);
                };
                steps.DefaultValue = DefaultValue.Empty;
                steps.SetupAllProperties();
                steps.SetupGet(x => x.AreEqual).Returns((x, y) => false); //Make sure to cause a mismatch
                steps.Setup(x => x.OnMismatch(It.IsAny<MismatchEventArgs<string>>())).Callback(onMismatch);
                steps.Setup(x => x.OnError(It.IsAny<ErrorEventArgs>()))
                    .Callback(onError);
                SetupControlAndCandidate(steps, ctrlResult, candResult, candName);
                state.SetupAllProperties();
                SetupStateSnapshot(state);
                //Exercise
                var sut = new Experiment<string, string>(name, publisher.Object, state.Object, steps.Object, true);
                //Exception thrown because throwOnInternalExceptions = true!
                var result = Assert.Throws<StepFailedException>(() => sut.Run());
                //Verify
                steps.Verify(x => x.OnMismatch(It.IsAny<MismatchEventArgs<string>>()), Times.AtLeastOnce);
                steps.Verify(x => x.OnError(It.Is<ErrorEventArgs>(
                    a => a.State.CurrentStep == Operations.OnError && a.ExperimentError.LastException == excp
                    && a.ExperimentError.LastStep == Operations.OnMismatch)), Times.AtLeastOnce);
                //Final results NOT published
                publisher.Verify(x => x.Publish(It.IsAny<IExperimentResult<string>>()), Times.Never);
                //Message published
                publisher.Verify(x => x.Publish(errMsg,
                   It.Is<IExperimentState>(y => y.CurrentStep == Operations.OnError)), Times.AtLeastOnce);
                Assert.True(excpPassed);
                Assert.Equal(result.InnerException, excp);
            }

            [Theory, AutoMoqData]
            public void PreCondition(Mock<ISciencePublisher> publisher, Mock<IExperimentSteps<string, string>> steps,
                Mock<IExperimentState> state, string name, string ctrlResult, string candResult,
                string candName, InvalidProgramException excp, string errMsg)
            {
                //Setup
                Func<bool> preCondition = () =>
                {
                    throw excp;
                };
                bool excpPassed = false;
                Action<ErrorEventArgs> onError = (e) =>
                {
                    excpPassed = e.ExperimentError.LastException == excp
                        && e.ExperimentError.LastStep == Operations.PreCondition;
                    e.Publisher.Publish(errMsg, e.State);
                };
                steps.DefaultValue = DefaultValue.Empty;
                steps.SetupAllProperties();
                steps.SetupGet(x => x.PreCondition).Returns(preCondition);
                steps.Setup(x => x.OnError(It.IsAny<ErrorEventArgs>()))
                    .Callback(onError);
                SetupControlAndCandidate(steps, ctrlResult, candResult, candName);
                state.SetupAllProperties();
                SetupStateSnapshot(state);
                //Exercise
                var sut = new Experiment<string, string>(name, publisher.Object, state.Object, steps.Object, true);
                var result = sut.Run(); //no exception
                //Verify
                steps.Verify(x => x.PreCondition, Times.AtLeastOnce);
                steps.Verify(x => x.OnError(It.Is<ErrorEventArgs>(
                    a => a.State.CurrentStep == Operations.OnError && a.ExperimentError.LastException == excp
                    && a.ExperimentError.LastStep == Operations.PreCondition)), Times.AtLeastOnce);
                //Results correct
                //publisher.Verify(x => x.Publish(It.Is<IExperimentResult<string>>(
                //    r => !r.Control.ExceptionThrown && r.Control.Value == ctrlResult
                //    && r.Candidates.All(kvp => kvp.Value.ExceptionThrown
                //    && kvp.Value.ExperimentError.LastException == excp
                //    && kvp.Value.ExperimentError.LastStep == Operations.PreCondition))), Times.Once);
                publisher.Verify(x => x.Publish(It.IsAny<IExperimentResult<string>>()), Times.Never);
                //Message published
                publisher.Verify(x => x.Publish(errMsg,
                   It.Is<IExperimentState>(y => y.CurrentStep == Operations.OnError)), Times.AtLeastOnce);
                Assert.True(excpPassed);
            }

            [Theory, AutoMoqData]
            public void Prepare(Mock<ISciencePublisher> publisher, Mock<IExperimentSteps<string, string>> steps,
               Mock<IExperimentState> state, string name, string ctrlResult, string candResult,
               string candName, InvalidProgramException excp, string errMsg)
            {
                //Setup
                PrepareDelegate<string, string> prepare = (val) =>
                {
                    throw excp;
                };
                bool excpPassed = false;
                Action<ErrorEventArgs> onError = (e) =>
                {
                    excpPassed = e.ExperimentError.LastException == excp
                        && e.ExperimentError.LastStep == Operations.Prepare;
                    e.Publisher.Publish(errMsg, e.State);
                };
                steps.DefaultValue = DefaultValue.Empty;
                steps.SetupAllProperties();
                steps.SetupGet(x => x.Prepare).Returns(prepare);
                steps.Setup(x => x.OnError(It.IsAny<ErrorEventArgs>()))
                    .Callback(onError);
                SetupControlAndCandidate(steps, ctrlResult, candResult, candName);
                state.SetupAllProperties();
                SetupStateSnapshot(state);
                //Exercise
                var sut = new Experiment<string, string>(name, publisher.Object, state.Object, steps.Object, true);
                var result = sut.Run(); //no exception
                //Verify
                steps.Verify(x => x.Prepare, Times.AtLeastOnce);
                steps.Verify(x => x.OnError(It.Is<ErrorEventArgs>(
                    a => a.State.CurrentStep == Operations.OnError && a.ExperimentError.LastException == excp
                    && a.ExperimentError.LastStep == Operations.Prepare)), Times.AtLeastOnce);
                //Results correct
                publisher.Verify(x => x.Publish(It.Is<IExperimentResult<string>>(
                    r => r.Control.ExceptionThrown && r.Control.ExperimentError.LastException == excp
                    && r.Control.ExperimentError.LastStep == Operations.Prepare
                    && r.Candidates.All(kvp => kvp.Value.ExceptionThrown
                    && kvp.Value.ExperimentError.LastException == excp
                    && kvp.Value.ExperimentError.LastStep == Operations.Prepare))), Times.Once);
                //Message published
                publisher.Verify(x => x.Publish(errMsg,
                   It.Is<IExperimentState>(y => y.CurrentStep == Operations.OnError)), Times.AtLeastOnce);
                Assert.True(excpPassed);
            }

            [Theory, AutoMoqData]
            public void SetContext(Mock<ISciencePublisher> publisher, Mock<IExperimentSteps<string, string>> steps,
               Mock<IExperimentState> state, string name, string ctrlResult, string candResult, string ctxt,
               string candName, InvalidProgramException excp, string errMsg)
            {
                //Setup
                Func<object> setContext = () =>
                {
                    throw excp;
                };
                bool excpPassed = false;
                Action<ErrorEventArgs> onError = (e) =>
                {
                    excpPassed = e.ExperimentError.LastException == excp
                        && e.ExperimentError.LastStep == Operations.SetContext;
                    e.Publisher.Publish(errMsg, e.State);
                };
                steps.DefaultValue = DefaultValue.Empty;
                steps.SetupAllProperties();
                steps.SetupGet(x => x.SetContext).Returns(setContext);
                steps.Setup(x => x.OnError(It.IsAny<ErrorEventArgs>()))
                    .Callback(onError);
                SetupControlAndCandidate(steps, ctrlResult, candResult, candName);
                state.SetupAllProperties();
                SetupStateSnapshot(state);
                //Exercise
                var sut = new Experiment<string, string>(name, publisher.Object, state.Object, steps.Object, true);
                var result = sut.Run(); //no exception
                //Verify
                steps.Verify(x => x.SetContext, Times.AtLeastOnce);
                steps.Verify(x => x.OnError(It.Is<ErrorEventArgs>(
                    a => a.State.CurrentStep == Operations.OnError && a.ExperimentError.LastException == excp
                    && a.ExperimentError.LastStep == Operations.SetContext)), Times.AtLeastOnce);
                //Results correct
                publisher.Verify(x => x.Publish(It.Is<IExperimentResult<string>>(
                    r => r.Control.ExceptionThrown && r.Control.Value == ctrlResult
                    && r.Control.ExperimentError.LastException == excp
                    && r.Control.ExperimentError.LastStep == Operations.SetContext
                    && r.Candidates.All(kvp => kvp.Value.ExceptionThrown
                    && kvp.Value.ExperimentError.LastException == excp
                    && kvp.Value.ExperimentError.LastStep == Operations.SetContext))), Times.Once);
                //Message published
                publisher.Verify(x => x.Publish(errMsg,
                   It.Is<IExperimentState>(y => y.CurrentStep == Operations.OnError)), Times.AtLeastOnce);
                Assert.True(excpPassed);
            }

            [Theory, AutoMoqData]
            public void Setup(Mock<ISciencePublisher> publisher, Mock<IExperimentSteps<string, string>> steps,
                Mock<IExperimentState> state, string name, string ctrlResult, string candResult, string msg,
                string candName, InvalidProgramException excp, string errMsg)
            {
                //Setup
                Action<ExperimentEventArgs> setup = (e) =>
                {
                    throw excp;
                };
                bool excpPassed = false;
                Action<ErrorEventArgs> onError = (e) =>
                {
                    excpPassed = e.ExperimentError.LastException == excp
                        && e.ExperimentError.LastStep == Operations.Setup;
                    e.Publisher.Publish(errMsg, e.State);
                };
                steps.DefaultValue = DefaultValue.Empty;
                steps.SetupAllProperties();
                steps.Setup(x => x.Setup(It.IsAny<ExperimentEventArgs>())).Callback(setup);
                steps.Setup(x => x.OnError(It.IsAny<ErrorEventArgs>()))
                    .Callback(onError);
                SetupControlAndCandidate(steps, ctrlResult, candResult, candName);
                state.SetupAllProperties();
                SetupStateSnapshot(state);
                //Exercise
                var sut = new Experiment<string, string>(name, publisher.Object, state.Object, steps.Object, true);
                var result = sut.Run(); //no exception
                //Verify
                steps.Verify(x => x.Setup(It.Is<ExperimentEventArgs>(a => a.State.CurrentStep == Operations.Setup)),
                    Times.AtLeastOnce);
                steps.Verify(x => x.OnError(It.Is<ErrorEventArgs>(
                    a => a.State.CurrentStep == Operations.OnError && a.ExperimentError.LastException == excp
                    && a.ExperimentError.LastStep == Operations.Setup)), Times.AtLeastOnce);
                //Results correct
                publisher.Verify(x => x.Publish(It.Is<IExperimentResult<string>>(
                    r => r.Control.ExceptionThrown && r.Control.Value == ctrlResult
                    && r.Control.ExperimentError.LastException == excp
                    && r.Control.ExperimentError.LastStep == Operations.Setup
                    && r.Candidates.All(kvp => kvp.Value.ExceptionThrown
                    && kvp.Value.ExperimentError.LastException == excp
                    && kvp.Value.ExperimentError.LastStep == Operations.Setup))), Times.Once);
                //Message published
                publisher.Verify(x => x.Publish(errMsg,
                   It.Is<IExperimentState>(y => y.CurrentStep == Operations.OnError)), Times.AtLeastOnce);
                Assert.True(excpPassed);
            }

            [Theory, AutoMoqData]
            public void Teardown(Mock<ISciencePublisher> publisher, Mock<IExperimentSteps<string, string>> steps,
                Mock<IExperimentState> state, string name, string ctrlResult, string candResult, string msg,
                string candName, InvalidProgramException excp, string errMsg)
            {
                //Setup
                Action<ExperimentEventArgs> setup = (e) =>
                {
                    throw excp;
                };
                bool excpPassed = false;
                Action<ErrorEventArgs> onError = (e) =>
                {
                    excpPassed = e.ExperimentError.LastException == excp
                        && e.ExperimentError.LastStep == Operations.Teardown;
                    e.Publisher.Publish(errMsg, e.State);
                };
                steps.DefaultValue = DefaultValue.Empty;
                steps.SetupAllProperties();
                steps.Setup(x => x.Teardown(It.IsAny<ExperimentEventArgs>())).Callback(setup);
                steps.Setup(x => x.OnError(It.IsAny<ErrorEventArgs>()))
                    .Callback(onError);
                SetupControlAndCandidate(steps, ctrlResult, candResult, candName);
                state.SetupAllProperties();
                SetupStateSnapshot(state);
                //Exercise
                var sut = new Experiment<string, string>(name, publisher.Object, state.Object, steps.Object, true);
                var result = sut.Run(); //no exception
                //Verify
                steps.Verify(x => x.Teardown(It.Is<ExperimentEventArgs>(a =>
                    a.State.CurrentStep == Operations.Teardown)), Times.AtLeastOnce);
                steps.Verify(x => x.OnError(It.Is<ErrorEventArgs>(
                    a => a.State.CurrentStep == Operations.OnError && a.ExperimentError.LastException == excp
                    && a.ExperimentError.LastStep == Operations.Teardown)), Times.AtLeastOnce);
                //Results correct
                publisher.Verify(x => x.Publish(It.Is<IExperimentResult<string>>(
                    r => r.Control.ExceptionThrown && r.Control.Value == ctrlResult
                    && r.Control.ExperimentError.LastException == excp
                    && r.Control.ExperimentError.LastStep == Operations.Teardown
                    && r.Candidates.All(kvp => kvp.Value.ExceptionThrown
                    && kvp.Value.ExperimentError.LastException == excp
                    && kvp.Value.ExperimentError.LastStep == Operations.Teardown))), Times.Once);
                //Message published
                publisher.Verify(x => x.Publish(errMsg,
                   It.Is<IExperimentState>(y => y.CurrentStep == Operations.OnError)), Times.AtLeastOnce);
                Assert.True(excpPassed);
            }

            #endregion Public Methods
        }

        /// <summary>
        /// Tests for when Steps run as expected. Exceptions can still be thrown from
        /// Control/Candidates, but should be handled gracefully if all other Steps do not throw Exceptions.
        /// </summary>
        public class TheStepsWithoutErrors : TheSteps
        {
            #region Public Methods

            [Theory, AutoMoqData]
            public void AreEqual(Mock<ISciencePublisher> publisher, Mock<IExperimentSteps<string, string>> steps,
               Mock<IExperimentState> state, string name, string ctrlResult, string candResult,
               string candName)
            {
                //Setup
                var mismatched = false;
                AreEqualDelegate<string> areEqual = (ctrl, cand) =>
                {
                    mismatched = ctrl.Length == cand.Length;
                    return !mismatched;
                };
                steps.DefaultValue = DefaultValue.Empty;
                steps.SetupAllProperties();
                steps.SetupGet(x => x.AreEqual).Returns(areEqual);
                SetupControlAndCandidate(steps, ctrlResult, candResult, candName);
                state.SetupAllProperties();
                //Exercise
                var sut = new Experiment<string, string>(name, publisher.Object, state.Object, steps.Object, true);
                var result = sut.Run();
                //Verify
                steps.Verify(x => x.AreEqual, Times.AtLeastOnce); //Setup called
                //Results is/isn't mismatched
                publisher.Verify(x => x.Publish(It.Is<IExperimentResult<string>>(
                    r => r.Candidates.All(kvp => kvp.Value.IsMismatched == mismatched))), Times.Once);
            }

            [Theory, AutoMoqData]
            public void Candidate_Exception_Thrown(Mock<ISciencePublisher> publisher,
                Mock<IExperimentSteps<string, string>> steps, Mock<IExperimentState> state,
                string name, string ctrlResult, string candResult, string candName, InvalidProgramException excp)
            {
                //Setup
                steps.DefaultValue = DefaultValue.Empty;
                steps.SetupAllProperties();

                Func<string> candidate = () =>
                {
                    throw excp;
                };
                SetupControlAndCandidate(steps, ctrlResult, candResult, candName);
                steps.Setup(x => x.GetCandidates())
                    .Returns(new Dictionary<string, Func<string>> { { candName, candidate } });
                state.SetupAllProperties();
                SetupStateSnapshot(state);
                //Exercise
                var sut = new Experiment<string, string>(name, publisher.Object, state.Object, steps.Object, true);
                var result = sut.Run(); //No exceptions should be thrown
                //Verify
                steps.Verify(x => x.GetCandidates(), Times.AtLeastOnce); //Setup called
                steps.Verify(x => x.OnMismatch(It.Is<MismatchEventArgs<string>>(
                    a => a.CandidateException == excp && a.Control == ctrlResult)), Times.AtLeastOnce);
                steps.Verify(x => x.OnError(It.Is<ErrorEventArgs>(r => r.ExperimentError.LastException == excp
                    && r.State.CurrentStep == Operations.OnError
                    && r.ExperimentError.LastStep == Operations.Candidate)),
                    Times.AtLeastOnce);
                //Results published
                publisher.Verify(x => x.Publish(It.Is<IExperimentResult<string>>(
                    r => r.Candidates.All(kvp => kvp.Key.Equals(candName) && kvp.Value.ExceptionThrown
                    && kvp.Value.Name.Equals(candName) && kvp.Value.IsMismatched
                    && kvp.Value.ExperimentError.LastException == excp
                    && kvp.Value.ExperimentError.LastStep == Operations.Candidate))), Times.Once);
            }

            [Theory, AutoMoqData]
            public void Candidate_No_Exception(Mock<ISciencePublisher> publisher,
                Mock<IExperimentSteps<string, string>> steps, Mock<IExperimentState> state,
                string name, string ctrlResult, string candResult, string candName)
            {
                //Setup
                steps.DefaultValue = DefaultValue.Empty;
                steps.SetupAllProperties();
                SetupControlAndCandidate(steps, ctrlResult, candResult, candName);
                state.SetupAllProperties();
                //Exercise
                var sut = new Experiment<string, string>(name, publisher.Object, state.Object, steps.Object, true);
                var result = sut.Run();
                //Verify
                steps.Verify(x => x.GetCandidates(), Times.AtLeastOnce); //Setup called
                //Results published
                publisher.Verify(x => x.Publish(It.Is<IExperimentResult<string>>(
                    r => r.Candidates.All(kvp => kvp.Key.Equals(candName) && kvp.Value.Value.Equals(candResult)
                    && kvp.Value.Name.Equals(candName) && !kvp.Value.ExceptionThrown))), Times.Once);
            }

            [Theory, AutoMoqData]
            public void Control_And_Candidate_Throw_Different_Exceptions(Mock<ISciencePublisher> publisher,
               Mock<IExperimentSteps<string, string>> steps, Mock<IExperimentState> state,
               string name, string candName, InvalidProgramException ctrlExcp, ApplicationException candExcp)
            {
                //Setup
                steps.DefaultValue = DefaultValue.Empty;
                steps.SetupAllProperties();
                Func<string> control = () => { throw ctrlExcp; };
                Func<string> candidate = () => { throw candExcp; };
                steps.SetupGet(x => x.Control)
                    .Returns(control);
                steps.Setup(x => x.GetCandidates())
                    .Returns(new Dictionary<string, Func<string>> { { candName, candidate } });
                state.SetupAllProperties();
                SetupStateSnapshot(state);
                //Exercise
                var sut = new Experiment<string, string>(name, publisher.Object, state.Object, steps.Object, true);
                var thrown = Assert.Throws<InvalidProgramException>(() => sut.Run()); //Exceptions should be thrown
                //Verify
                steps.Verify(x => x.Control, Times.AtLeastOnce);
                steps.Verify(x => x.GetCandidates(), Times.AtLeastOnce); //GetCandidates STILL called
                steps.Verify(x => x.OnMismatch(It.Is<MismatchEventArgs<string>>(
                    r => r.CandidateException == candExcp && r.ControlException == ctrlExcp)),
                    Times.AtLeastOnce); //Diff exceptions, they're mismatched
                steps.Verify(x => x.OnError(It.Is<ErrorEventArgs>(a => a.State.CurrentStep == Operations.OnError)),
                    Times.AtLeastOnce);
                //Results published
                publisher.Verify(x => x.Publish(It.Is<IExperimentResult<string>>(
                    r => r.Control.ExceptionThrown && r.Control.ExperimentError.LastException == ctrlExcp
                    && r.Control.ExperimentError.LastStep == Operations.Control
                    && r.Candidates.All(kvp => kvp.Value.IsMismatched && kvp.Value.ExceptionThrown
                    && kvp.Value.ExperimentError.LastException == candExcp
                    && kvp.Value.ExperimentError.LastStep == Operations.Candidate))),
                    Times.Once);
                Assert.Equal(thrown, ctrlExcp);
            }

            [Theory, AutoMoqData]
            public void Control_And_Candidate_Throw_Same_Exception(Mock<ISciencePublisher> publisher,
               Mock<IExperimentSteps<string, string>> steps, Mock<IExperimentState> state,
               string name, string candName, InvalidProgramException excp)
            {
                //Setup
                steps.DefaultValue = DefaultValue.Empty;
                steps.SetupAllProperties();
                Func<string> function = () => { throw excp; };
                steps.SetupGet(x => x.Control)
                    .Returns(function);
                steps.Setup(x => x.GetCandidates())
                    .Returns(new Dictionary<string, Func<string>> { { candName, function } });
                state.SetupAllProperties();
                SetupStateSnapshot(state);
                //Exercise
                var sut = new Experiment<string, string>(name, publisher.Object, state.Object, steps.Object, true);
                var thrown = Assert.Throws<InvalidProgramException>(() => sut.Run()); //Exceptions should be thrown
                //Verify
                steps.Verify(x => x.Control, Times.AtLeastOnce);
                steps.Verify(x => x.GetCandidates(), Times.AtLeastOnce); //GetCandidates STILL called
                steps.Verify(x => x.OnMismatch(It.IsAny<MismatchEventArgs<string>>()),
                    Times.Never); //Same exception, they're not mismatched
                steps.Verify(x => x.OnError(It.Is<ErrorEventArgs>(a => a.State.CurrentStep == Operations.OnError)),
                    Times.AtLeastOnce);
                //Results published
                publisher.Verify(x => x.Publish(It.Is<IExperimentResult<string>>(
                    r => r.Control.ExceptionThrown && r.Control.ExperimentError.LastException == excp
                    && r.Control.ExperimentError.LastStep == Operations.Control
                    && r.Candidates.All(kvp => !kvp.Value.IsMismatched && kvp.Value.ExceptionThrown
                    && kvp.Value.ExperimentError.LastException == excp
                    && kvp.Value.ExperimentError.LastStep == Operations.Candidate))),
                    Times.Once);
                Assert.Equal(thrown, excp);
            }

            [Theory, AutoMoqData]
            public void Control_Exception_Thrown(Mock<ISciencePublisher> publisher,
                Mock<IExperimentSteps<string, string>> steps, Mock<IExperimentState> state,
                string name, string ctrlResult, string candResult, string candName, InvalidProgramException excp)
            {
                //Setup
                steps.DefaultValue = DefaultValue.Empty;
                steps.SetupAllProperties();
                Func<string> control = () => { throw excp; };
                SetupControlAndCandidate(steps, ctrlResult, candResult, candName);
                steps.Setup(x => x.Control)
                    .Returns(control);
                state.SetupAllProperties();
                SetupStateSnapshot(state);
                //Exercise
                var sut = new Experiment<string, string>(name, publisher.Object, state.Object, steps.Object, true);
                var thrown = Assert.Throws<InvalidProgramException>(() => sut.Run()); //Exceptions should be thrown
                //Verify
                steps.Verify(x => x.Control, Times.AtLeastOnce);
                steps.Verify(x => x.GetCandidates(), Times.AtLeastOnce); //GetCandidates STILL called
                steps.Verify(x => x.OnMismatch(It.Is<MismatchEventArgs<string>>(
                    r => r.Candidate == candResult && r.ControlException == excp)),
                    Times.AtLeastOnce);
                steps.Verify(x => x.OnError(It.Is<ErrorEventArgs>(a => a.State.CurrentStep == Operations.OnError)),
                    Times.AtLeastOnce);
                //Results published
                publisher.Verify(x => x.Publish(It.Is<IExperimentResult<string>>(
                    r => r.Control.ExceptionThrown && r.Control.ExperimentError.LastException == excp
                    && r.Control.ExperimentError.LastStep == Operations.Control
                    && r.Candidates.All(kvp => kvp.Value.IsMismatched))),
                    Times.Once);
                Assert.Equal(thrown, excp);
            }

            [Theory, AutoMoqData]
            public void Control_No_Exception(Mock<ISciencePublisher> publisher,
                Mock<IExperimentSteps<string, string>> steps, Mock<IExperimentState> state, string name,
                string ctrlResult, string candResult, string candName)
            {
                //Setup
                steps.DefaultValue = DefaultValue.Empty;
                steps.SetupAllProperties();
                SetupControlAndCandidate(steps, ctrlResult, candResult, candName);
                state.SetupAllProperties();
                //Exercise
                var sut = new Experiment<string, string>(name, publisher.Object, state.Object, steps.Object, true);
                var result = sut.Run();
                //Verify
                steps.Verify(x => x.Control, Times.AtLeastOnce); //Setup called
                //Results published
                publisher.Verify(x => x.Publish(It.Is<IExperimentResult<string>>(
                    r => r.Control.Value == ctrlResult && !r.Control.ExceptionThrown)), Times.Once);
                Assert.Equal(ctrlResult, result);
            }

            [Theory, AutoMoqData]
            public void Ignore(Mock<ISciencePublisher> publisher, Mock<IExperimentSteps<string, string>> steps,
                Mock<IExperimentState> state, string name, string ctrlResult, string candResult,
                string candName)
            {
                //Setup
                bool wasIgnored = false;
                IgnoreDelegate<string> ignore = (ctrl, cand) =>
                {
                    wasIgnored = ctrl == ctrlResult && cand == candResult;
                    return wasIgnored;
                };
                steps.DefaultValue = DefaultValue.Empty;
                steps.SetupAllProperties();
                steps.SetupGet(x => x.Ignore).Returns(ignore);
                SetupControlAndCandidate(steps, ctrlResult, candResult, candName);
                state.SetupAllProperties();
                //Exercise
                var sut = new Experiment<string, string>(name, publisher.Object, state.Object, steps.Object, true);
                var result = sut.Run();
                //Verify
                steps.Verify(x => x.Ignore, Times.AtLeastOnce);
                steps.Verify(x => x.AreEqual, Times.Never);
                steps.Verify(x => x.OnMismatch(It.IsAny<MismatchEventArgs<string>>()), Times.Never);
                Assert.True(wasIgnored);
            }

            [Theory, AutoMoqData]
            public void OnMismatch(Mock<ISciencePublisher> publisher, Mock<IExperimentSteps<string, string>> steps,
               Mock<IExperimentState> state, string name, string ctrlResult, string candResult,
               string candName, string msg)
            {
                //Setup
                var mismatchedCalled = false;
                Action<MismatchEventArgs<string>> onMismatch = (e) =>
                {
                    mismatchedCalled = e.Control == ctrlResult && e.Candidate == candResult;
                    e.Publisher.Publish(msg, e.State);
                };
                steps.DefaultValue = DefaultValue.Empty;
                steps.SetupAllProperties();
                steps.SetupGet(x => x.AreEqual).Returns((x, y) => false);
                steps.Setup(x => x.OnMismatch(It.IsAny<MismatchEventArgs<string>>())).Callback(onMismatch);
                SetupControlAndCandidate(steps, ctrlResult, candResult, candName);
                state.SetupAllProperties();
                SetupStateSnapshot(state);
                //Exercise
                var sut = new Experiment<string, string>(name, publisher.Object, state.Object, steps.Object, true);
                var result = sut.Run();
                //Verify
                steps.Verify(x => x.OnMismatch(It.Is<MismatchEventArgs<string>>(
                    a => a.Candidate == candResult && a.Control == ctrlResult)), Times.AtLeastOnce);
                //Message published
                publisher.Verify(x => x.Publish(msg,
                    It.Is<IExperimentState>(y => y.CurrentStep == Operations.OnMismatch)), Times.AtLeastOnce);
                Assert.True(mismatchedCalled);
            }

            [Theory, AutoMoqData]
            public void PreCondition_True(Mock<ISciencePublisher> publisher, Mock<IExperimentSteps<string, string>> steps,
                Mock<IExperimentState> state, string name, string ctrlResult, string candResult,
                string candName)
            {
                //Setup
                bool conditionRan = false;
                Func<bool> preCondition = () =>
                {
                    conditionRan = true;
                    return conditionRan;
                };
                steps.DefaultValue = DefaultValue.Empty;
                steps.SetupAllProperties();
                steps.SetupGet(x => x.PreCondition).Returns(preCondition);
                SetupControlAndCandidate(steps, ctrlResult, candResult, candName);
                state.SetupAllProperties();
                //Exercise
                var sut = new Experiment<string, string>(name, publisher.Object, state.Object, steps.Object, true);
                var result = sut.Run();
                //Verify
                steps.Verify(x => x.PreCondition, Times.AtLeastOnce);
                steps.Verify(x => x.GetCandidates(), Times.AtLeastOnce);
                Assert.True(conditionRan);
            }
            [Theory, AutoMoqData]
            public void PreCondition_False(Mock<ISciencePublisher> publisher, Mock<IExperimentSteps<string, string>> steps,
                Mock<IExperimentState> state, string name, string ctrlResult, string candResult,
                string candName)
            {
                //Setup
                bool conditionRan = false;
                Func<bool> preCondition = () =>
                {
                    conditionRan = true;
                    return !conditionRan;
                };
                steps.DefaultValue = DefaultValue.Empty;
                steps.SetupAllProperties();
                steps.SetupGet(x => x.PreCondition).Returns(preCondition);
                SetupControlAndCandidate(steps, ctrlResult, candResult, candName);
                state.SetupAllProperties();
                //Exercise
                var sut = new Experiment<string, string>(name, publisher.Object, state.Object, steps.Object, true);
                var result = sut.Run();
                //Verify
                steps.Verify(x => x.PreCondition, Times.AtLeastOnce);
                steps.Verify(x => x.GetCandidates(), Times.AtLeastOnce);
                publisher.Verify(x => x.Publish(It.IsAny<IExperimentResult<string>>()), Times.Never);
                Assert.True(conditionRan);
            }
            [Theory, AutoMoqData]
            public void Prepare(Mock<ISciencePublisher> publisher, Mock<IExperimentSteps<string, string>> steps,
               Mock<IExperimentState> state, string name, string ctrlResult, string candResult,
               string candName, string prepared)
            {
                //Setup
                var preparedCalled = false;
                PrepareDelegate<string, string> prepare = (val) =>
                {
                    preparedCalled = val == candResult || val == ctrlResult;
                    return prepared;
                };
                steps.DefaultValue = DefaultValue.Empty;
                steps.SetupAllProperties();
                steps.SetupGet(x => x.Prepare).Returns(prepare);
                SetupControlAndCandidate(steps, ctrlResult, candResult, candName);
                state.SetupAllProperties();
                //Exercise
                var sut = new Experiment<string, string>(name, publisher.Object, state.Object, steps.Object, true);
                var result = sut.Run();
                //Verify
                steps.Verify(x => x.Prepare, Times.AtLeastOnce);
                //Results prepared
                publisher.Verify(x => x.Publish(It.Is<IExperimentResult<string>>(
                    r => r.Candidates.All(kvp => kvp.Value.Value == prepared) && r.Control.Value == prepared)),
                    Times.AtLeastOnce);
                Assert.True(preparedCalled);
            }

            [Theory, AutoMoqData]
            public void SetContext(Mock<ISciencePublisher> publisher, Mock<IExperimentSteps<string, string>> steps,
               Mock<IExperimentState> state, string name, string ctrlResult, string candResult, string ctxt,
               string candName)
            {
                //Setup
                Func<object> setContext = () =>
                {
                    return ctxt;
                };
                steps.DefaultValue = DefaultValue.Empty;
                steps.SetupAllProperties();
                steps.SetupGet(x => x.SetContext).Returns(setContext);
                SetupControlAndCandidate(steps, ctrlResult, candResult, candName);
                state.SetupAllProperties();
                //Exercise
                var sut = new Experiment<string, string>(name, publisher.Object, state.Object, steps.Object, true);
                var result = sut.Run();
                //Verify
                steps.Verify(x => x.SetContext, Times.AtLeastOnce);
                //Message published
                publisher.Verify(x => x.Publish(It.Is<IExperimentResult<string>>(
                    r => r.Control.Context.Equals(ctxt) && r.Candidates.All(kvp => kvp.Value.Context.Equals(ctxt))))
                    , Times.Once);
            }

            [Theory, AutoMoqData]
            public void Setup(Mock<ISciencePublisher> publisher, Mock<IExperimentSteps<string, string>> steps,
                Mock<IExperimentState> state, string name, string ctrlResult, string candResult, string msg,
                string candName)
            {
                //Setup
                Action<ExperimentEventArgs> setup = (e) =>
                {
                    e.Publisher.Publish(msg, e.State);
                };
                steps.DefaultValue = DefaultValue.Empty;
                steps.SetupAllProperties();
                steps.Setup(x => x.Setup(It.IsAny<ExperimentEventArgs>())).Callback(setup);
                SetupControlAndCandidate(steps, ctrlResult, candResult, candName);
                state.SetupAllProperties();
                SetupStateSnapshot(state);
                //Exercise
                var sut = new Experiment<string, string>(name, publisher.Object, state.Object, steps.Object, true);
                var result = sut.Run();
                //Verify
                steps.Verify(x => x.Setup(It.Is<ExperimentEventArgs>(
                    a => a.State.CurrentStep == Operations.Setup)), Times.AtLeastOnce); //Setup called
                //Message published
                publisher.Verify(x => x.Publish(msg,
                    It.Is<IExperimentState>(y => y.CurrentStep == Operations.Setup)), Times.AtLeastOnce);
            }

            [Theory, AutoMoqData]
            public void Teardown(Mock<ISciencePublisher> publisher, Mock<IExperimentSteps<string, string>> steps,
                Mock<IExperimentState> state, string name, string ctrlResult, string candResult, string msg,
                string candName)
            {
                //Setup
                Action<ExperimentEventArgs> teardown = (e) =>
                {
                    e.Publisher.Publish(msg, e.State);
                };
                steps.DefaultValue = DefaultValue.Empty;
                steps.SetupAllProperties();
                steps.Setup(x => x.Teardown(It.IsAny<ExperimentEventArgs>())).Callback(teardown);
                SetupControlAndCandidate(steps, ctrlResult, candResult, candName);
                state.SetupAllProperties();
                SetupStateSnapshot(state);
                //Exercise
                var sut = new Experiment<string, string>(name, publisher.Object, state.Object, steps.Object, true);
                var result = sut.Run();
                //Verify
                steps.Verify(x => x.Teardown(It.Is<ExperimentEventArgs>(
                    a => a.State.CurrentStep == Operations.Teardown)), Times.AtLeastOnce); //Teardown called
                //Message published
                publisher.Verify(x => x.Publish(msg,
                    It.Is<IExperimentState>(y => y.CurrentStep == Operations.Teardown)), Times.AtLeastOnce);
            }

            #endregion Public Methods
        }

        #endregion Public Classes
    }
}