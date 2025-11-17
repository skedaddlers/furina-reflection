using System.Collections.Generic;
using UnityEngine;

namespace DDAMAPEKitFramework
{
    /// <summary>
    /// Monitor component of MAPE-K loop
    /// Observes sensors and monitors player attributes
    /// </summary>
    public class Monitor : Observable
    {
        private PlayerModel playerModel;
        private SystemStateLog systemStateLog;

        public Monitor(PlayerModel playerModel, SystemStateLog log)
        {
            this.playerModel = playerModel;
            this.systemStateLog = log;
        }

        public void Observe(List<Sensor> sensors)
        {
            bool needsAnalysis = false;

            foreach (var sensor in sensors)
            {
                var reading = sensor.Read();
                if (reading != null)
                {
                    // Update player model attribute
                    playerModel.UpdateAttribute(reading.attributeId, reading.value);
                    
                    // Log the reading
                    systemStateLog.LogReading(reading);

                    // Check if value is outside threshold
                    var attribute = playerModel.GetAttribute(reading.attributeId);
                    if (attribute != null && !attribute.threshold.IsInRange(reading.value))
                    {
                        needsAnalysis = true;
                        Debug.Log($"[Monitor] Attribute {attribute.label} out of threshold: {reading.value}");
                    }
                }
            }

            if (needsAnalysis)
            {
                NotifyObservers(systemStateLog.GetLatestLog());
            }
        }
    }

    /// <summary>
    /// Base class for sensors that read game data
    /// </summary>
    public abstract class Sensor : MonoBehaviour
    {
        protected int attributeId;
        protected string attributeLabel;

        public abstract SensorReading Read();
    }

    /// <summary>
    /// Data structure for sensor readings
    /// </summary>
    public class SensorReading
    {
        public int attributeId;
        public float value;
        public float timestamp;

        public SensorReading(int id, float value)
        {
            this.attributeId = id;
            this.value = value;
            this.timestamp = Time.time;
        }
    }

    /// <summary>
    /// System state log for tracking readings over time
    /// </summary>
    public class SystemStateLog
    {
        private List<LogEntry> entries = new List<LogEntry>();
        private const int MAX_ENTRIES = 1000;

        public void LogReading(SensorReading reading)
        {
            entries.Add(new LogEntry
            {
                attributeId = reading.attributeId,
                value = reading.value,
                timestamp = reading.timestamp
            });

            // Maintain max size
            if (entries.Count > MAX_ENTRIES)
            {
                entries.RemoveAt(0);
            }
        }

        public LogEntry GetLatestLog()
        {
            return entries.Count > 0 ? entries[entries.Count - 1] : null;
        }

        public List<LogEntry> GetRecentLogs(int count)
        {
            int startIndex = Mathf.Max(0, entries.Count - count);
            return entries.GetRange(startIndex, Mathf.Min(count, entries.Count));
        }

        public class LogEntry
        {
            public int attributeId;
            public float value;
            public float timestamp;
        }
    }

    /// <summary>
    /// Observable base class for Observer pattern
    /// </summary>
    public abstract class Observable
    {
        private List<IObserver> observers = new List<IObserver>();

        public void Subscribe(IObserver observer)
        {
            if (!observers.Contains(observer))
            {
                observers.Add(observer);
            }
        }

        public void Unsubscribe(IObserver observer)
        {
            observers.Remove(observer);
        }

        protected void NotifyObservers(object data)
        {
            foreach (var observer in observers)
            {
                observer.Update(data);
            }
        }
    }

    /// <summary>
    /// Observer interface for Observer pattern
    /// </summary>
    public interface IObserver
    {
        void Update(object data);
    }
}