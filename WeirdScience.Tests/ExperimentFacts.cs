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

namespace WeirdScience.Tests
{
    public class ExperimentFacts
    {
        //TODO: Write Unit Tests for all Step delegates and calls to Publisher
        public class TheSteps
        {
            [Theory, AutoMoqData]
            public void Ignore(Mock<ISciencePublisher> publisher, Mock<IExperimentSteps<string, string>> steps,
                Mock<IExperimentState<string>> state, string name, string ctrlResult, string candResult)
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
                SetupControlAndCandidate(steps, ctrlResult, candResult);
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

            private void SetupControlAndCandidate(Mock<IExperimentSteps<string, string>> steps, 
                string ctrlResult, string candResult)
            {
                steps.SetupGet(x => x.Control).Returns(new Func<string>(() => ctrlResult));
                steps.Setup(x => x.GetCandidates())
                    .Returns(new Dictionary<string, Func<string>> { { "a", () => candResult } });
            }
        }
    }
}
