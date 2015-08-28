using System;
using System.Text;

namespace WeirdScience
{
    public class ConsolePublisher : ISciencePublisher
    {
        #region Private Fields

        private StringBuilder messages = new StringBuilder();

        #endregion Private Fields

        #region Public Methods

        public virtual void Publish(string message, IExperimentState state)
        {
            if (!string.IsNullOrEmpty(message))
                messages.AppendFormat("  {3} - Message from Experiment '{0}' in Step '{1}': {2}\n",
                    state.Name, state.CurrentStep, message, state.Timestamp.ToLongTimeString());
        }

        public virtual void Publish<T>(IExperimentResult<T> results)
        {
            Console.WriteLine("*************** Experiment '{0}' Results ***************", results.Name);
            PublishObservation(results.Control);
            foreach (var obs in results.Candidates)
            {
                PublishObservation(obs.Value);
            }
            Console.WriteLine(messages.ToString());
            var stars = new StringBuilder();
            for (int i = 0; i < results.Name.Length; i++)
            {
                stars.Append('*');
            }
            Console.WriteLine("*****************************************************" + stars.ToString());
            messages.Clear();
        }

        #endregion Public Methods

        #region Private Methods

        private static void PublishObservation<T>(IObservation<T> observation)
        {
            Console.WriteLine("  " + observation.Name);
            Console.WriteLine("    Took: {0} ms, Exception Thrown: {1}{2}, Output Value: {3}",
                observation.ElapsedMilliseconds, observation.ExceptionThrown, observation.ExceptionThrown ?
                string.Format("(Exception: {0})", observation.ExperimentError.LastException.Message) : string.Empty,
                observation.Value);
        }

        #endregion Private Methods
    }
}