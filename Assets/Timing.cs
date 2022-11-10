using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Timing
{
    static Dictionary<string, float> timers = new Dictionary<string, float>();
    static Dictionary<string, float> batchTimers = new Dictionary<string, float>();
    static Dictionary<string, int> batchCounts = new Dictionary<string, int>();

    public static void Start(string name)
    {
        timers[name] = Time.realtimeSinceStartup;
    }

    public static void Batch(string name)
    {
        float result = Time.realtimeSinceStartup - timers[name];
        timers.Remove(name);
        if (batchTimers.ContainsKey(name))
        {
            batchTimers[name] += result * 1000;
            batchCounts[name]++;
        }
        else
        {
            batchTimers[name] = result * 1000;
            batchCounts[name] = 1;
        }
    }

    public static void StopBatch(string name)
    {
        Debug.Log(name + ": " + batchTimers[name] + "(" + batchCounts[name] + ")");
        batchTimers.Remove(name);
    }

    public static void Stop(string name)
    {
        float result = Time.realtimeSinceStartup - timers[name];
        timers.Remove(name);
        Debug.Log(name + ": " + result * 1000);
    }
}
