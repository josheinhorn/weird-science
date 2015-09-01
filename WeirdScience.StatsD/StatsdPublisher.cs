using StatsdClient;
using System.Text.RegularExpressions;

namespace WeirdScience.StatsD
{
    public class StatsdPublisher : ISciencePublisher
    {
        #region Private Fields

        private static readonly Regex splitterPattern = new Regex(@"[:\|]");
        private static readonly Regex statsdPattern = new Regex(@"^[^:\|]+:(\d+\|(g|c|ms)|[^:\|]+\|s)$");
        private static readonly Regex whitespace = new Regex(@"\s+");
        private readonly string hostName;
        private readonly int port;
        private readonly string prefix;

        #endregion Private Fields

        #region Public Constructors

        public StatsdPublisher(string hostName, int port)
            : this(hostName, port, string.Empty)
        { }

        public StatsdPublisher(string hostName, int port, string prefix)
        {
            this.hostName = hostName;
            this.port = port;
            this.prefix = prefix;
        }

        #endregion Public Constructors

        //public StatsDPublisher(string hostName, int port, string prefix, IStopWatchFactory swFactory)
        //{

        #region Public Methods

        //}
        /// <summary>
        /// Sends properly formatted StatsD messages prefixed by "[Experiment Name].[Current Name].[Current Step]."
        /// </summary>
        /// <example>
        /// state = new ExperimentState { Name = "Candidate 1", ExperimentName = "Science!",
        /// CurrentStep = Operations.OnMismatch }; Publish("gaugor:333|g", state) --&gt; Sends
        /// StatsD with: Name: "Science!.Candidate_1.OnMismatch.gaugor", Value: 333, Type: Gauge
        /// </example>
        /// <remarks>
        /// see https://github.com/etsy/statsd/blob/master/docs/metric_types.md for formatting
        /// </remarks>
        /// <param name="message"></param>
        /// <param name="state"></param>
        public void Publish(string message, IExperimentState state)
        {
            // Do nothing for now gaugor:333|g
            if (!string.IsNullOrEmpty(message))
            {
                using (var udp = new StatsdUDP(hostName, port))
                {
                    var statsd = new Statsd(udp, prefix);
                    TrySendMessage(message, string.Format("{0}.{1}.{2}", state.ExperimentName,
                        whitespace.Replace(state.Name, "_"), state.CurrentStep),
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

        #endregion Public Methods

        #region Private Methods

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

        private static bool TrySendMessage(string message, string prefix, Statsd statsd)
        {
            bool result = false;
            if (statsdPattern.IsMatch(message)) //Shortcut if it's not a valid string
            {
                var arr = splitterPattern.Split(message);
                if (arr.Length == 3)
                {
                    var name = prefix + arr[0];
                    var value = arr[1];
                    var type = arr[2];
                    int iNum;
                    double dNum;
                    switch (type)
                    {
                        case "g":
                            if (double.TryParse(value, out dNum))
                            {
                                statsd.Send<Statsd.Gauge>(name, dNum);
                                result = true;
                            }
                            break;

                        case "s":
                            statsd.Send<Statsd.Set>(name, value);
                            result = true;
                            break;

                        case "ms":
                            if (int.TryParse(value, out iNum))
                            {
                                statsd.Send<Statsd.Timing>(name, iNum);
                                result = true;
                            }
                            break;

                        case "c":
                            if (int.TryParse(value, out iNum))
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

        #endregion Private Methods
    }
}