using System;
using System.Collections.Generic;

namespace Mycelia.App
{
    /// <summary>
    /// One analytics event: a snake_case name and ordered fields (text, whole numbers or flags). Levels and spores are
    /// named by their stable keys, never by position or colour (ADR-0005, ADR-0006), and nothing identifies the player.
    /// </summary>
    public sealed class AnalyticsEvent
    {
        readonly List<KeyValuePair<string, object>> fields = new List<KeyValuePair<string, object>>();

        public AnalyticsEvent(string name)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentException("An event needs a name.", nameof(name));
            Name = name;
        }

        public string Name { get; }
        public IReadOnlyList<KeyValuePair<string, object>> Fields => fields;

        public AnalyticsEvent With(string key, object value)
        {
            fields.Add(new KeyValuePair<string, object>(key, value));
            return this;
        }

        /// <summary>The value of a field, or null if the event has none by that key.</summary>
        public object Value(string key)
        {
            foreach (KeyValuePair<string, object> field in fields)
            {
                if (field.Key == key)
                    return field.Value;
            }
            return null;
        }

        public override string ToString() => Name;
    }

    /// <summary>Where analytics events go (design §8.2 slice 6). Logging never throws: analytics must not break the game.</summary>
    public interface IAnalytics
    {
        void Log(AnalyticsEvent analyticsEvent);
    }
}
