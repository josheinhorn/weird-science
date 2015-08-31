using Moq;
using System;
using System.Linq;
using Xunit;

namespace WeirdScience.Tests
{
    public class ExperimentStepsFacts
    {
        // TODO: Possibly write tests around Properties, if they ever actually do something besides
        //       encapsulate values
        #region Public Methods

        [Theory, AutoMoqData]
        public void Add_Candidates_Same_Name_Throws_Exception(Mock<Func<string>> candidate, string name,
                    Mock<Func<string>> candidate2)
        {
            var sut = new ExperimentSteps<string, string>();
            sut.AddCandidate(name, candidate.Object);
            var exception = Assert.Throws<ArgumentException>(
                () => sut.AddCandidate(name, candidate2.Object));
        }

        [Theory, AutoMoqData]
        public void AddCandidates_And_GetCandidates(Mock<Func<string>> candidate, string name)
        {
            var sut = new ExperimentSteps<string, string>();
            sut.AddCandidate(name, candidate.Object);
            var candidates = sut.GetCandidates();

            Assert.Equal(candidates.First(x => x.Key == name).Value, candidate.Object);
        }

        [Theory, AutoMoqData]
        public void OnError(ErrorEventArgs args)
        {
            bool raised = false;
            EventHandler<ErrorEventArgs> handler = (sender, e) => raised = e == args;
            var sut = new ExperimentSteps<string, string>();
            sut.OnErrorEvent += handler;
            sut.OnError(args);

            //Verify
            Assert.True(raised);
        }

        [Theory, AutoMoqData]
        public void OnMismatch(MismatchEventArgs<string> args)
        {
            bool raised = false;
            EventHandler<MismatchEventArgs<string>> handler = (sender, e) => raised = e == args;
            var sut = new ExperimentSteps<string, string>();
            sut.OnMismatchEvent += handler;
            sut.OnMismatch(args);

            //Verify
            Assert.True(raised);
        }

        [Theory, AutoMoqData]
        public void Setup(ExperimentEventArgs args)
        {
            bool raised = false;
            EventHandler<ExperimentEventArgs> handler = (sender, e) => raised = e == args;
            var sut = new ExperimentSteps<string, string>();
            sut.SetupEvent += handler;
            sut.Setup(args);

            //Verify
            Assert.True(raised);
        }

        [Theory, AutoMoqData]
        public void Teardown(ExperimentEventArgs args)
        {
            bool raised = false;
            EventHandler<ExperimentEventArgs> handler = (sender, e) => raised = e == args;
            var sut = new ExperimentSteps<string, string>();
            sut.TeardownEvent += handler;
            sut.Teardown(args);

            //Verify
            Assert.True(raised);
        }

        #endregion Public Methods
    }
}