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


        [Theory, AutoMoqData]
        public void Method(Mock<ISciencePublisher> publisher, Mock<IExperimentSteps<string, string>> steps,
            string name, string b)
        {

            var sut = new Experiment<string, string>(name, publisher.Object);

            Assert.True(publisher != null);
        }



    }
}
