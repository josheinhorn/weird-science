﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeirdScience
{
    public class ConsolePublisher : ISciencePublisher
    {
        private StringBuilder messages = new StringBuilder();
        public virtual void Publish<T>(string message, IExperimentState<T> state)
        {
            if (!string.IsNullOrEmpty(message))
                messages.AppendFormat("  {3} - Message from Experiment '{0}' in Step '{1}': {2}\n", 
                    state.Name, state.Step, message, state.Timestamp.ToLongTimeString());
        }

        public virtual void Publish<T>(IExperimentResult<T> results)
        {
            Console.WriteLine("*************** Experiment '{0}' Results ***************", results.Name);
            PublishObservation<T>(results.Control);
            foreach (var obs in results.Candidates)
            {
                PublishObservation<T>(obs.Value);
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

        private void PublishObservation<T>(IObservation<T> observation)
        {
            Console.WriteLine("  " + observation.Name);
            Console.WriteLine("    Took: {0} ms, Exception Thrown: {1}{2}, Output Value: {3}",
                observation.ElapsedMilliseconds, observation.ExceptionThrown, observation.ExceptionThrown ?
                string.Format("(Exception: {0})", observation.Error.LastException) : string.Empty,
                observation.Value);
        }
    }
}