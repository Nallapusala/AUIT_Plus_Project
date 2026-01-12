using UnityEngine;

public class StepEventTester : MonoBehaviour
{
    public ExperimentLogger logger;

    private int step = 0;

    void Update()
    {
        if (logger == null) return;

        // N = next step started
        if (Input.GetKeyDown(KeyCode.N))
        {
            step++;
            logger.LogEvent("step_start", $"step_{step}");
        }

        // D = step done
        if (Input.GetKeyDown(KeyCode.D))
        {
            logger.LogEvent("step_done", $"step_{step}");
        }

        // E = error
        if (Input.GetKeyDown(KeyCode.E))
        {
            logger.LogEvent("error", $"step_{step}");
        }

        // T = end trial
        if (Input.GetKeyDown(KeyCode.T))
        {
            logger.EndTrial("tester_end");
        }
    }
}