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
        /// <summary>
        /// Sends properly formatted StatsD messages prefixed by "[Experiment Name]." and 
        /// suffixed by ".[Current Name].[Current Step]"
        /// </summary>
        /// <example>
        /// state = new ExperimentState { Name = "Candidate 1", ExperimentName = "Science!", 
        ///     CurrentStep = Operations.OnMismatch };
        /// Publish("gaugor:333|g", state) -->
        /// Sends StatsD with: Name: "Science!.gaugor.Candidate_1.OnMismatch", Value: 333, Type: Gauge
        /// </example>
        /// <remarks>see https://github.com/etsy/statsd/blob/master/docs/metric_types.md for formatting</remarks>
        /// <param name="message"></param>
        /// <param name="state"></param>
        public void Publish(string message, IExperimentState state)
        {
            // Do nothing for now
            // gaugor:333|g
            if (!string.IsNullOrEmpty(message))
            {
                using (var udp = new StatsdUDP(hostName, port))
                {
                    var statsd = new Statsd(udp, prefix);
                    TrySendMessage(message, state.ExperimentName,
                        string.Format("{0}.{1}", state.Name.Replace(" ", "_"), state.CurrentStep),
                        statsd);
                }
            }
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
            statsd.Add<Statsd.Timing>(string.Format("{0}.{1}.Results.Microseconds", experimentName, observation.Name),
                (int)(observation.ElapsedTime.TotalMilliseconds * 1000));
            if (observation.IsMismatched)
            {
                // Gauge, Meter, or Count -- use Count for now, gives us # of mismatches / second
                statsd.Add<Statsd.Counting>(string.Format("{0}.{1}.Results.Mismatches", experimentName, observation.Name)
                    , 1);
            }   
        }

        private static bool TrySendMessage(string message, string prefix, string suffix, Statsd statsd)
        {
            bool result = false;
            var arr = message.Split(':');
            if (arr.Length == 2)
            {
                var name = string.Format("{0}.{1}.{2}", prefix, arr[0], suffix);
                arr = arr[1].Split('|');
                int iNum;
                double dNum;
                if (arr.Length == 2)
                {
                    switch (arr[1])
                    {
                        case "g":
                            if (double.TryParse(arr[0], out dNum))
                            {
                                statsd.Send<Statsd.Gauge>(name, dNum);
                                result = true;
                            }
                            break;
                        case "s":
                            statsd.Send<Statsd.Set>(name, arr[0]);
                            result = true;
                            break;
                        case "ms":
                            if (int.TryParse(arr[0], out iNum))
                            {
                                statsd.Send<Statsd.Timing>(name, iNum);
                                result = true;
                            }
                            break;
                        case "c":
                            if (int.TryParse(arr[0], out iNum))
                            {
                                statsd.Send<Statsd.Counting>(name, iNum);
                                result = true;
                            }
                            break;
                    }
                }                
            }
            return result;
        }
    }
}
