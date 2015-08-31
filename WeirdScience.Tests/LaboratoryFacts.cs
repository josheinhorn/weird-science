using Moq;
using System;
using Xunit;

namespace WeirdScience.Tests
{
    public class LaboratoryFacts
    {
        #region Public Methods

        [Theory, AutoMoqData]
        public void AreEqual(Mock<ISciencePublisher> publisher, Mock<IScienceExperiment<string, string>> experiment,
            string ctrlResult, Mock<IExperimentSteps<string, string>> steps)
        {
            //Setup
            publisher.SetupAllProperties();
            experiment.SetupAllProperties();
            steps.SetupAllProperties();
            experiment.SetupGet(x => x.Steps).Returns(steps.Object);
            Func<string> control = () => ctrlResult;
            AreEqualDelegate<string> areEqual = (ctrl, cand) => ctrl == cand;
            //Exercise
            var sut = new Laboratory(publisher.Object, true);
            sut.CreateExperiment(experiment.Object)
                .Control(control)
                .AreEqual(areEqual);

            //Verify
            steps.VerifySet(x => x.AreEqual = areEqual, Times.Once);
        }

        [Theory, AutoMoqData]
        public void Candidate_One(Mock<ISciencePublisher> publisher,
            Mock<IScienceExperiment<string, string>> experiment, string candResult,
            Mock<IExperimentSteps<string, string>> steps, string name)
        {
            //Setup
            publisher.SetupAllProperties();
            experiment.SetupAllProperties();
            steps.SetupAllProperties();
            experiment.SetupGet(x => x.Steps).Returns(steps.Object);
            Func<string> candidate = () => candResult;

            //Exercise
            var sut = new Laboratory(publisher.Object, true);
            sut.CreateExperiment(experiment.Object)
                .Candidate(name, candidate);

            //Verify
            steps.Verify(x => x.AddCandidate(name, candidate), Times.Once);
        }

        [Theory, AutoMoqData]
        public void Candidate_Two(Mock<ISciencePublisher> publisher,
            Mock<IScienceExperiment<string, string>> experiment, string candResult,
            Mock<IExperimentSteps<string, string>> steps, string name1, string name2)
        {
            //Setup
            publisher.SetupAllProperties();
            experiment.SetupAllProperties();
            steps.SetupAllProperties();
            experiment.SetupGet(x => x.Steps).Returns(steps.Object);
            Func<string> candidate = () => candResult;

            //Exercise
            var sut = new Laboratory(publisher.Object, true);
            sut.CreateExperiment(experiment.Object)
                .Candidate(name1, candidate)
                .Candidate(name2, candidate);

            //Verify
            steps.Verify(x => x.AddCandidate(name1, candidate), Times.Once);
            steps.Verify(x => x.AddCandidate(name2, candidate), Times.Once);
        }

        [Theory, AutoMoqData]
        public void Control(Mock<ISciencePublisher> publisher, Mock<IScienceExperiment<string, string>> experiment,
            string ctrlResult, Mock<IExperimentSteps<string, string>> steps)
        {
            //Setup
            publisher.SetupAllProperties();
            experiment.SetupAllProperties();
            steps.SetupAllProperties();
            experiment.SetupGet(x => x.Steps).Returns(steps.Object);
            Func<string> control = () => ctrlResult;

            //Exercise
            var sut = new Laboratory(publisher.Object, true);
            sut.CreateExperiment(experiment.Object)
                .Control(control);

            //Verify
            steps.VerifySet(x => x.Control = control, Times.Once);
        }

        [Theory, AutoMoqData]
        public void Ignore(Mock<ISciencePublisher> publisher, Mock<IScienceExperiment<string, string>> experiment,
            string ctrlResult, Mock<IExperimentSteps<string, string>> steps)
        {
            //Setup
            publisher.SetupAllProperties();
            experiment.SetupAllProperties();
            steps.SetupAllProperties();
            experiment.SetupGet(x => x.Steps).Returns(steps.Object);
            Func<string> control = () => ctrlResult;
            IgnoreDelegate<string> ignore = (ctrl, cand) => ctrl == cand;
            //Exercise
            var sut = new Laboratory(publisher.Object, true);
            sut.CreateExperiment(experiment.Object)
                .Control(control)
                .Ignore(ignore);

            //Verify
            steps.VerifySet(x => x.Ignore = ignore, Times.Once);
        }

        [Theory, AutoMoqData]
        public void OnError(Mock<ISciencePublisher> publisher, Mock<IScienceExperiment<string, string>> experiment,
            string ctrlResult, Mock<IExperimentSteps<string, string>> steps)
        {
            //Setup
            publisher.SetupAllProperties();
            experiment.SetupAllProperties();
            steps.SetupAllProperties();
            experiment.SetupGet(x => x.Steps).Returns(steps.Object);
            Func<string> control = () => ctrlResult;
            bool raised = false;
            EventHandler<ErrorEventArgs> onError = (sender, e) => raised = true;
            //Exercise
            var sut = new Laboratory(publisher.Object, true);
            sut.CreateExperiment(experiment.Object)
                .Control(control)
                .OnError(onError);
            steps.Raise(x => x.OnErrorEvent += null, new ErrorEventArgs());

            //Verify
            Assert.True(raised);
        }

        [Theory, AutoMoqData]
        public void OnMismatch(Mock<ISciencePublisher> publisher, Mock<IScienceExperiment<string, string>> experiment,
            string ctrlResult, Mock<IExperimentSteps<string, string>> steps)
        {
            //Setup
            publisher.SetupAllProperties();
            experiment.SetupAllProperties();
            steps.SetupAllProperties();
            experiment.SetupGet(x => x.Steps).Returns(steps.Object);
            Func<string> control = () => ctrlResult;
            bool raised = false;
            EventHandler<MismatchEventArgs<string>> onMismatch = (sender, e) => raised = true;
            //Exercise
            var sut = new Laboratory(publisher.Object, true);
            sut.CreateExperiment(experiment.Object)
                .Control(control)
                .OnMismatch(onMismatch);
            steps.Raise(x => x.OnMismatchEvent += null, new MismatchEventArgs<string>());

            //Verify
            Assert.True(raised);
        }

        [Theory, AutoMoqData]
        public void PreCondition(Mock<ISciencePublisher> publisher, Mock<IScienceExperiment<string, string>> experiment,
            string ctrlResult, Mock<IExperimentSteps<string, string>> steps)
        {
            //Setup
            publisher.SetupAllProperties();
            experiment.SetupAllProperties();
            steps.SetupAllProperties();
            experiment.SetupGet(x => x.Steps).Returns(steps.Object);
            Func<string> control = () => ctrlResult;
            Func<bool> preCondition = () => true;
            //Exercise
            var sut = new Laboratory(publisher.Object, true);
            sut.CreateExperiment(experiment.Object)
                .Control(control)
                .PreCondition(preCondition);

            //Verify
            steps.VerifySet(x => x.PreCondition = preCondition, Times.Once);
        }

        [Theory, AutoMoqData]
        public void Prepare(Mock<ISciencePublisher> publisher, Mock<IScienceExperiment<string, string>> experiment,
            string ctrlResult, Mock<IExperimentSteps<string, string>> steps)
        {
            //Setup
            publisher.SetupAllProperties();
            experiment.SetupAllProperties();
            steps.SetupAllProperties();
            experiment.SetupGet(x => x.Steps).Returns(steps.Object);
            Func<string> control = () => ctrlResult;
            PrepareDelegate<string, string> prepare = (val) => val;
            //Exercise
            var sut = new Laboratory(publisher.Object, true);
            sut.CreateExperiment(experiment.Object)
                .Control(control)
                .Prepare(prepare);

            //Verify
            steps.VerifySet(x => x.Prepare = prepare, Times.Once);
        }

        [Theory, AutoMoqData]
        public void SetContext(Mock<ISciencePublisher> publisher, Mock<IScienceExperiment<string, string>> experiment,
            string ctrlResult, Mock<IExperimentSteps<string, string>> steps)
        {
            //Setup
            publisher.SetupAllProperties();
            experiment.SetupAllProperties();
            steps.SetupAllProperties();
            experiment.SetupGet(x => x.Steps).Returns(steps.Object);
            Func<string> control = () => ctrlResult;
            Func<object> setContext = () => true;
            //Exercise
            var sut = new Laboratory(publisher.Object, true);
            sut.CreateExperiment(experiment.Object)
                .Control(control)
                .SetContext(setContext);

            //Verify
            steps.VerifySet(x => x.SetContext = setContext, Times.Once);
        }

        [Theory, AutoMoqData]
        public void Setup(Mock<ISciencePublisher> publisher, Mock<IScienceExperiment<string, string>> experiment,
            string ctrlResult, Mock<IExperimentSteps<string, string>> steps)
        {
            //Setup
            publisher.SetupAllProperties();
            experiment.SetupAllProperties();
            steps.SetupAllProperties();
            experiment.SetupGet(x => x.Steps).Returns(steps.Object);
            Func<string> control = () => ctrlResult;
            bool raised = false;
            EventHandler<ExperimentEventArgs> setup = (sender, e) => raised = true;
            //Exercise
            var sut = new Laboratory(publisher.Object, true);
            sut.CreateExperiment(experiment.Object)
                .Control(control)
                .Setup(setup);
            steps.Raise(x => x.SetupEvent += null, new ExperimentEventArgs());

            //Verify
            Assert.True(raised);
        }

        [Theory, AutoMoqData]
        public void Teardown(Mock<ISciencePublisher> publisher, Mock<IScienceExperiment<string, string>> experiment,
            string ctrlResult, Mock<IExperimentSteps<string, string>> steps)
        {
            //Setup
            publisher.SetupAllProperties();
            experiment.SetupAllProperties();
            steps.SetupAllProperties();
            experiment.SetupGet(x => x.Steps).Returns(steps.Object);
            Func<string> control = () => ctrlResult;
            bool raised = false;
            EventHandler<ExperimentEventArgs> teardown = (sender, e) => raised = true;
            //Exercise
            var sut = new Laboratory(publisher.Object, true);
            sut.CreateExperiment(experiment.Object)
                .Control(control)
                .Teardown(teardown);
            steps.Raise(x => x.TeardownEvent += null, new ExperimentEventArgs());

            //Verify
            Assert.True(raised);
        }

        #endregion Public Methods
    }
}