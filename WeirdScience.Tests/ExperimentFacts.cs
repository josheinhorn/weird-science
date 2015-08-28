using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.DataAnnotations;
using Ploeh.AutoFixture.Xunit2;
using Moq;
using Ploeh.AutoFixture.AutoMoq;

namespace WeirdScience.Tests
{
    public class ExperimentFacts
    {
        #region Public Classes

        public class TheStepsWithErrors
        {
            //Exceptions thrown from within Steps (besides Control and Candidate)
        }

        //TODO: Write Unit Tests for all Step delegates and calls to Publisher
        public class TheStepsWithoutErrors
        {
            #region Public Methods

            [Theory, AutoMoqData]
            public void AreEqual(Mock<ISciencePublisher> publisher, Mock<IExperimentSteps<string, string>> steps,
               Mock<IExperimentState<string>> state, string name, string ctrlResult, string candResult,
               string candName)
            {
                //Setup
                var mismatched = false;
                Func<string, string, bool> areEqual = (ctrl, cand) =>
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
                    r => r.Candidates.All(kvp => kvp.Value.IsMismatched == mismatched))), Times.AtLeastOnce);
            }

            [Theory, AutoMoqData]
            public void Candidate_Exception_Thrown(Mock<ISciencePublisher> publisher,
                Mock<IExperimentSteps<string, string>> steps, Mock<IExperimentState<string>> state,
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
                //Exercise
                var sut = new Experiment<string, string>(name, publisher.Object, state.Object, steps.Object, true);
                var result = sut.Run(); //No exceptions should be thrown
                //Verify
                steps.Verify(x => x.GetCandidates(), Times.AtLeastOnce); //Setup called
                steps.Verify(x => x.OnMismatch, Times.AtLeastOnce);
                steps.Verify(x => x.OnError, Times.AtLeastOnce);
                //Results published
                publisher.Verify(x => x.Publish(It.Is<IExperimentResult<string>>(
                    r => r.Candidates.All(kvp => kvp.Key.Equals(candName) && kvp.Value.ExceptionThrown
                    && kvp.Value.Name.Equals(candName) && kvp.Value.IsMismatched
                    && kvp.Value.ExperimentError.LastException == excp))), Times.Once);
            }

            [Theory, AutoMoqData]
            public void Candidate_No_Exception(Mock<ISciencePublisher> publisher,
                Mock<IExperimentSteps<string, string>> steps, Mock<IExperimentState<string>> state,
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
                    && kvp.Value.Name.Equals(candName)))), Times.Once);
            }

            [Theory, AutoMoqData]
            public void Control_And_Candidate_Throw_Different_Exceptions(Mock<ISciencePublisher> publisher,
               Mock<IExperimentSteps<string, string>> steps, Mock<IExperimentState<string>> state,
               string name, string candName, InvalidProgramException ctrlExcp, ApplicationException candExcp)
            {
                //Setup
                steps.DefaultValue = DefaultValue.Empty;
                steps.SetupAllProperties();
                Func<string> control = () =>
                {
                    throw ctrlExcp;
                };
                Func<string> candidate = () => { throw candExcp; };
                steps.SetupGet(x => x.Control)
                    .Returns(control);
                steps.Setup(x => x.GetCandidates())
                    .Returns(new Dictionary<string, Func<string>> { { candName, candidate } });
                state.SetupAllProperties();
                //Exercise
                var sut = new Experiment<string, string>(name, publisher.Object, state.Object, steps.Object, true);
                var thrown = Assert.Throws<InvalidProgramException>(() => sut.Run()); //Exceptions should be thrown
                //Verify
                steps.Verify(x => x.Control, Times.AtLeastOnce);
                steps.Verify(x => x.GetCandidates(), Times.AtLeastOnce); //GetCandidates STILL called
                steps.Verify(x => x.OnMismatch, Times.AtLeastOnce); //Diff exceptions, they're mismatched
                steps.Verify(x => x.OnError, Times.AtLeastOnce);
                //Results published
                publisher.Verify(x => x.Publish(It.Is<IExperimentResult<string>>(
                    r => r.Control.ExceptionThrown && r.Control.ExperimentError.LastException == ctrlExcp
                    && r.Candidates.All(kvp => kvp.Value.IsMismatched && kvp.Value.ExceptionThrown
                    && kvp.Value.ExperimentError.LastException == candExcp))),
                    Times.Once);
                Assert.Equal(thrown, ctrlExcp);
            }

            [Theory, AutoMoqData]
            public void Control_And_Candidate_Throw_Same_Exception(Mock<ISciencePublisher> publisher,
               Mock<IExperimentSteps<string, string>> steps, Mock<IExperimentState<string>> state,
               string name, string candName, InvalidProgramException excp)
            {
                //Setup
                steps.DefaultValue = DefaultValue.Empty;
                steps.SetupAllProperties();
                Func<string> function = () =>
                {
                    throw excp;
                };
                steps.SetupGet(x => x.Control)
                    .Returns(function);
                steps.Setup(x => x.GetCandidates())
                    .Returns(new Dictionary<string, Func<string>> { { candName, function } });
                state.SetupAllProperties();
                //Exercise
                var sut = new Experiment<string, string>(name, publisher.Object, state.Object, steps.Object, true);
                var thrown = Assert.Throws<InvalidProgramException>(() => sut.Run()); //Exceptions should be thrown
                //Verify
                steps.Verify(x => x.Control, Times.AtLeastOnce);
                steps.Verify(x => x.GetCandidates(), Times.AtLeastOnce); //GetCandidates STILL called
                steps.Verify(x => x.OnMismatch, Times.Never); //Same exception, they're not mismatched
                steps.Verify(x => x.OnError, Times.AtLeastOnce);
                //Results published
                publisher.Verify(x => x.Publish(It.Is<IExperimentResult<string>>(
                    r => r.Control.ExceptionThrown && r.Control.ExperimentError.LastException == excp
                    && r.Candidates.All(kvp => !kvp.Value.IsMismatched && kvp.Value.ExceptionThrown
                    && kvp.Value.ExperimentError.LastException == excp))),
                    Times.Once);
                Assert.Equal(thrown, excp);
            }

            [Theory, AutoMoqData]
            public void Control_Exception_Thrown(Mock<ISciencePublisher> publisher,
                Mock<IExperimentSteps<string, string>> steps, Mock<IExperimentState<string>> state,
                string name, string ctrlResult, string candResult, string candName, InvalidProgramException excp)
            {
                //Setup
                steps.DefaultValue = DefaultValue.Empty;
                steps.SetupAllProperties();

                Func<string> control = () =>
                {
                    throw excp;
                };
                SetupControlAndCandidate(steps, ctrlResult, candResult, candName);
                steps.Setup(x => x.Control)
                    .Returns(control);
                state.SetupAllProperties();
                //Exercise
                var sut = new Experiment<string, string>(name, publisher.Object, state.Object, steps.Object, true);
                var thrown = Assert.Throws<InvalidProgramException>(() => sut.Run()); //Exceptions should be thrown
                //Verify
                steps.Verify(x => x.Control, Times.AtLeastOnce);
                steps.Verify(x => x.GetCandidates(), Times.AtLeastOnce); //GetCandidates STILL called
                steps.Verify(x => x.OnMismatch, Times.AtLeastOnce);
                steps.Verify(x => x.OnError, Times.AtLeastOnce);
                //Results published
                publisher.Verify(x => x.Publish(It.Is<IExperimentResult<string>>(
                    r => r.Control.ExceptionThrown && r.Control.ExperimentError.LastException == excp
                    && r.Candidates.All(kvp => kvp.Value.IsMismatched))),
                    Times.Once);
                Assert.Equal(thrown, excp);
            }

            [Theory, AutoMoqData]
            public void Control_No_Exception(Mock<ISciencePublisher> publisher,
                Mock<IExperimentSteps<string, string>> steps, Mock<IExperimentState<string>> state, string name,
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
                    r => r.Control.Value == ctrlResult)), Times.Once);
                Assert.Equal(ctrlResult, result);
            }

            [Theory, AutoMoqData]
            public void Ignore(Mock<ISciencePublisher> publisher, Mock<IExperimentSteps<string, string>> steps,
                Mock<IExperimentState<string>> state, string name, string ctrlResult, string candResult, 
                string candName)
            {
                //Setup
                bool wasIgnored = false;
                Func<string, string, bool> ignore = (ctrl, cand) =>
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
                steps.Verify(x => x.OnMismatch, Times.Never);
                Assert.True(wasIgnored);
            }
            [Theory, AutoMoqData]
            public void OnMismatch(Mock<ISciencePublisher> publisher, Mock<IExperimentSteps<string, string>> steps,
               Mock<IExperimentState<string>> state, string name, string ctrlResult, string candResult,
               string candName, string msg)
            {
                //Setup
                var mismatchedCalled = false;
                Func<string, string, Exception, Exception, string> onMismatch = (ctrl, cand, ctrlExc, candExc) =>
                {
                    mismatchedCalled = ctrl == ctrlResult && cand == candResult;
                    return msg;
                };
                steps.DefaultValue = DefaultValue.Empty;
                steps.SetupAllProperties();
                steps.SetupGet(x => x.AreEqual).Returns((x, y) => false);
                steps.SetupGet(x => x.OnMismatch).Returns(onMismatch);
                SetupControlAndCandidate(steps, ctrlResult, candResult, candName);
                state.SetupAllProperties();
                SetupStateSnapshot(state);
                //Exercise
                var sut = new Experiment<string, string>(name, publisher.Object, state.Object, steps.Object, true);
                var result = sut.Run();
                //Verify
                steps.Verify(x => x.OnMismatch, Times.AtLeastOnce); //Setup called
                //Message published
                publisher.Verify(x => x.Publish(msg,
                    It.Is<IExperimentState<string>>(y => y.CurrentStep == Operations.OnMismatch)), Times.AtLeastOnce);
                Assert.True(mismatchedCalled);
            }

            [Theory, AutoMoqData]
            public void PreCondition(Mock<ISciencePublisher> publisher, Mock<IExperimentSteps<string, string>> steps,
                Mock<IExperimentState<string>> state, string name, string ctrlResult, string candResult, string candName)
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
                steps.Verify(x => x.Setup, Times.AtLeastOnce);
                Assert.True(conditionRan);
            }
            [Theory, AutoMoqData]
            public void Prepare(Mock<ISciencePublisher> publisher, Mock<IExperimentSteps<string, string>> steps,
               Mock<IExperimentState<string>> state, string name, string ctrlResult, string candResult,
               string candName, string prepared)
            {
                //Setup
                var preparedCalled = false;
                Func<string, string> prepare = (val) =>
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
                steps.Verify(x => x.Prepare, Times.AtLeastOnce); //Setup called
                //Results prepared
                publisher.Verify(x => x.Publish(It.Is<IExperimentResult<string>>(
                    r => r.Candidates.All(kvp => kvp.Value.Value == prepared) && r.Control.Value == prepared)),
                    Times.AtLeastOnce);
                Assert.True(preparedCalled);
            }

            [Theory, AutoMoqData]
            public void SetContext(Mock<ISciencePublisher> publisher, Mock<IExperimentSteps<string, string>> steps,
               Mock<IExperimentState<string>> state, string name, string ctrlResult, string candResult, string ctxt,
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
                steps.Verify(x => x.SetContext, Times.AtLeastOnce); //Setup called
                //Message published
                publisher.Verify(x => x.Publish(It.Is<IExperimentResult<string>>(
                    r => r.Control.Context.Equals(ctxt) && r.Candidates.All(kvp => kvp.Value.Context.Equals(ctxt))))
                    , Times.AtLeastOnce);
            }

            [Theory, AutoMoqData]
            public void Setup(Mock<ISciencePublisher> publisher, Mock<IExperimentSteps<string, string>> steps,
                Mock<IExperimentState<string>> state, string name, string ctrlResult, string candResult, string msg,
                string candName)
            {
                //Setup
                Func<string> setup = () =>
                {
                    return msg;
                };
                steps.DefaultValue = DefaultValue.Empty;
                steps.SetupAllProperties();
                steps.SetupGet(x => x.Setup).Returns(setup);
                SetupControlAndCandidate(steps, ctrlResult, candResult, candName);
                state.SetupAllProperties();
                SetupStateSnapshot(state);
                //Exercise
                var sut = new Experiment<string, string>(name, publisher.Object, state.Object, steps.Object, true);
                var result = sut.Run();
                //Verify
                steps.Verify(x => x.Setup, Times.AtLeastOnce); //Setup called
                //Message published
                publisher.Verify(x => x.Publish(msg, 
                    It.Is<IExperimentState<string>>(y => y.CurrentStep == Operations.Setup)), Times.AtLeastOnce);
            }
            [Theory, AutoMoqData]
            public void Teardown(Mock<ISciencePublisher> publisher, Mock<IExperimentSteps<string, string>> steps,
                Mock<IExperimentState<string>> state, string name, string ctrlResult, string candResult, string msg,
                string candName)
            {
                //Setup
                Func<string> teardown = () =>
                {
                    return msg;
                };
                steps.DefaultValue = DefaultValue.Empty;
                steps.SetupAllProperties();
                steps.SetupGet(x => x.Teardown).Returns(teardown);
                SetupControlAndCandidate(steps, ctrlResult, candResult, candName);
                state.SetupAllProperties();
                SetupStateSnapshot(state);
                //Exercise
                var sut = new Experiment<string, string>(name, publisher.Object, state.Object, steps.Object, true);
                var result = sut.Run();
                //Verify
                steps.Verify(x => x.Setup, Times.AtLeastOnce); //Teardown called
                //Message published
                publisher.Verify(x => x.Publish(msg,
                    It.Is<IExperimentState<string>>(y => y.CurrentStep == Operations.Teardown)), Times.AtLeastOnce);
            }

            #endregion Public Methods

            #region Private Methods

            private void SetupControlAndCandidate(Mock<IExperimentSteps<string, string>> steps, 
                string ctrlResult, string candResult, string candName)
            {
                steps.SetupGet(x => x.Control).Returns(new Func<string>(() => ctrlResult));
                steps.Setup(x => x.GetCandidates())
                    .Returns(new Dictionary<string, Func<string>> { { candName, () => candResult } });
            }
            private void SetupStateSnapshot(Mock<IExperimentState<string>> state)
            {
                Operations step = Operations.Internal;
                state.SetupGet(x => x.CurrentStep).Returns(() => step);
                state.SetupSet<Operations>(x => x.CurrentStep = It.IsAny<Operations>())
                    .Callback(x => step = x);
                state.Setup(x => x.Snapshot())
                    .Returns(() =>
                    {
                        var ss = Mock.Of<IExperimentState<string>>();
                        ss.CurrentStep = step;
                        return ss;
                    });
            }

            #endregion Private Methods
        }

        #endregion Public Classes
    }
}
