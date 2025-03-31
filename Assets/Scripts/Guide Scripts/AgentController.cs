using System.Collections;
using i5.VirtualAgents;
using i5.VirtualAgents.AgentTasks;
using i5.VirtualAgents.ScheduleBasedExecution;
using UnityEngine;

namespace i5.LLM_AR_Tourguide.Guide_Scripts
{
    public class AgentController : MonoBehaviour
    {
        // The agent which is controlled by this controller, set in the inspector
        public Agent agent;

        /// <summary>
        ///     The target which the agent should aim at.
        /// </summary>
        [Tooltip("The target which the agent should aim at.")] [SerializeField]
        private GameObject target;

        // The taskSystem of the agent
        protected ScheduleBasedTaskSystem taskSystem;

        /// <summary>
        ///     The time in seconds the agent should aim at the target.
        /// </summary>
        //[Tooltip("The time in seconds the agent should aim at the target.")]
        //[SerializeField] private int aimAtTime = 40;
        protected IEnumerator Start()
        {
            if (agent == null)
            {
                DebugEditor.LogWarning("Agent is not set in AgentController");
                yield break;
            }

            // Get the task system of the agent
            taskSystem = (ScheduleBasedTaskSystem)agent.TaskSystem;
            var task = taskSystem.Tasks;
            var talkingTask = new AgentAnimationTask("StartTalking", 5, "StopTalking");
            taskSystem.ScheduleTask(talkingTask, 5);

            //Add tasks below
            for (var i = 0; i < 100; i++)
            {
                var agentRotationTask = new AgentRotationTask(target);
                agentRotationTask.AngleThreshold = 10f;
                taskSystem.ScheduleTask(agentRotationTask, -1, "Left Arm");
                yield return new WaitForSeconds(7f);
            }
        }
    }
}