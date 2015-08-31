using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StatsdClient;

namespace WeirdScience.StatsD
{
    public class StatsdPublisher : ISciencePublisher
    {
        private string hostName;
        private int port;
        private string prefix;

        public StatsdPublisher(string hostName, int port)
            : this(hostName, port, string.Empty)
        { }
        public StatsdPublisher(string hostName, int port, string prefix)
        {
            this.hostName = hostName;
            this.port = port;
            this.prefix = prefix;
        }
        //public StatsDPublisher(string hostName, int port, string prefix, IStopWatchFactory swFactory)
        //{

        //}
        public void Publish(string message, IExperimentState state)
        {
            // Do nothing for now
        }

        public void Publish<T>(IExperimentResult<T> results)
        {
            // How can we Unit Test this? Could do integration test but would be overkill
            using (var udp = new StatsdUDP(hostName, port))
            {
                var statsd = new Statsd(udp, prefix);
                AddObservationStats(results.Name, results.Control, statsd);
                foreach (var kvp in results.Candidates)
                {
                    AddObservationStats(results.Name, kvp.Value, statsd);
                }
                statsd.Send();
            }
        }

        private static void AddObservationStats<T>(string experimentName, IObservation<T> observation, Statsd statsd)
        {
            statsd.Add<Statsd.Timing>(string.Format("{0}.{1}.{2}", experimentName, observation.Name, 
                "Microseconds"), (int)(observation.ElapsedTime.TotalMilliseconds * 1000));
            if (observation.IsMismatched)
            {
                // Gauge, Meter, or Count -- use Count for now, gives us # of mismatches / second
                statsd.Add<Statsd.Counting>(string.Format("{0}.{1}.{2}", experimentName, observation.Name,
                    "Mismatches"), 1);
            }
            
        }
    }
}
