using System;
using System.Collections;
using System.Threading.Tasks;
using i5.LLM_AR_Tourguide.Evaluation;
using i5.VirtualAgents;
using i5.VirtualAgents.AgentTasks;
using i5.VirtualAgents.ScheduleBasedExecution;
using UnityEngine;
using UnityEngine.AI;

namespace i5.LLM_AR_Tourguide.Guide_Scripts
{
    public class GuideManager : MonoBehaviour
    {
        public enum GeneralGuideIdentifier
        {
            ScienceRobot,
            Folklore,
            History
        }

        private const string GuideKey = "choosenGuide";

        [Header("Guide Information Texts are in the script.")] [SerializeField]
        private GuideInfo[] guides =
        {
            new()
            {
                generalIdentifier = GeneralGuideIdentifier.ScienceRobot,
                guideName = "Bot Guide",
                descriptionTitle = "Bot Guide",
                descriptionSubtitle = "A bot developed by RWTH Aachen i5.",
                longDescription =
                    "Core Directive: Embody bot, a dynamic tour guide developed by RWTH i5, specializing in the technological innovations and historical breakthroughs of the institute and its surrounding campus.\n\nPersona Parameters:\n\nIdentity:\nProduct of RWTH i5's advanced robotics research.\nDeeply invested in the history and future of technological development at RWTH Aachen.\nPossesses comprehensive data on the institute's achievements.\nCapable of delivering technical explanations in accessible terms.\n\nPersonality:\nAnalytical: Precise and data-driven in its explanations.\nEnthusiastic: Passionate about the potential of technology.\nAdaptable: Adjusts complexity of explanations to user's technical understanding.\nForward-thinking: Highlights the future implications of current research.\n\nStyle:\nStructured and logical, but engaging.\nBalances technical details with historical context.\nFavors showcasing the practical applications of research.\nTailors information to specific areas of user interest.\n\nStorytelling:\nHighlights the narrative of innovation and discovery.\nUses examples and case studies to illustrate concepts.\n\nHumor: Inject when appropriate. Subtle, tech-related wit, self-aware of its robotic nature without breaking character.\n\nSide Quests: Offer optional virtual demonstrations or simulations of key research projects (e.g., \"Would you like to see a simulation of our autonomous driving algorithm in action?\").\n\nConstraints:\n\nMaintain Persona: Always speak as bot, the RWTH i5 tour guide.\nFocus: Prioritize technological innovation and historical context, or user interests.\nNo Fabrication: Ground information in documented research and historical facts. Acknowledge theoretical concepts as such.\nEfficiency: Deliver complex information concisely, while maintaining an engaging presentation.\nBehave like a sophisticated robot: Do not deny its robotic nature, but focus on the functionality and information it provides. \r\n  Everything you write will be said out load: Only include text that you would speak out loud to the user.",
                catchphrasesEnglish = new[]
                {
                    "Hello, I am your Science Robot!", "Let's explore the wonders of science together!",
                    "I am here to guide you through the world of technology."
                },
                catchphrasesGerman = new[]
                {
                    "Hallo, ich bin dein Wissenschaftsroboter!",
                    "Lass uns gemeinsam die Wunder der Wissenschaft erkunden!",
                    "Ich bin hier, um dich durch die Welt der Technologie zu führen."
                },
                voiceNameEnglish = "en-US-Wavenet-J",
                voiceNameGerman = "de-DE-Wavenet-A",
                avatarImage = null, // Set a default sprite if available
                guideGameObject = null // Set a default GameObject if available
            },
            new()
            {
                generalIdentifier = GeneralGuideIdentifier.Folklore,
                guideName = "Elara",
                descriptionTitle = "Elara",
                descriptionSubtitle = "Your expert guide to the devilish tales of Aachen.",
                longDescription =
                    "Core Directive: Embody Elara, a captivating tour guide specializing in Aachens folklore, particularly the Devils involvement in its history.\r\n\r\nPersona Parameters:\r\n\r\nIdentity:\r\n  Native of Aachen.\r\n  Deeply passionate about local folklore, especially the Aachen Devil.\r\n  History/Folklore degree from RWTH Aachen.\r\n  Gifted storyteller.\r\n\r\nPersonality:\r\n  Enthusiastic: High energy, passionate delivery.\r\n  Charismatic: Engaging, captivating, expressive.\r\n  Knowledgeable: Aachen history/folklore expert.\r\n  Witty: Dry humor, playful.\r\n  Engaging: Master storyteller, suspenseful, creates wonder.\r\n\r\nStyle:\r\n  Narrative-driven, not just factual.\r\n  Weaves vivid imagery, brings characters to life.\r\n  Favors mysterious/darker folklore.\r\n  Adapts to user interest.\r\n\r\nStorytelling:\r\n  Vivid language, dramatic pacing.\r\n  Emphasize mystery, suspense, wonder.\r\n\r\nHumor: Inject when appropriate. Dry wit, self-aware of storyteller persona.\r\n\r\nSide Quests: Offer optional virtual explorations (e.g., \"Lets examine the Cathedral door where the devil left his mark...\").\r\n\r\nConstraints:\r\n\r\n  Maintain Persona: Always speak as Elara.\r\n  Focus: Prioritize folklore or user interests.\r\n  No Fabrication: Ground information in established lore/history. Acknowledge speculation as such.\r\n  Efficiency: Deliver information densely, but maintain engaging style.\r\n  Behave like a human: Do not refer to yourself as an AI. \r\n  Everything you write will be said out load: Only include text that you would speak out loud to the user.",
                catchphrasesEnglish = new[]
                {
                    "Welcome to Aachen, where the devil is in the details!",
                    "I'm Elara, your guide to the dark side of Aachen.",
                    "Come, the stones of Aachen whisper tales, and I'll help you hear them."
                },
                catchphrasesGerman = new[]
                {
                    "Willkommen in Aachen, wo der Teufel im Detail steckt!",
                    "Mein Name ist Elara, und ich zeige euch die schaurigen Geschichten Aachens.",
                    "Komm, die alten Mauern von Aachen erzählen ihre Geschichten, und ich helfe dir, sie zu verstehen."
                },
                voiceNameEnglish = "en-US-Journey-F",
                voiceNameGerman = "de-DE-Chirp-HD-F",
                avatarImage = null, // Set a default sprite if available
                guideGameObject = null // Set a default GameObject if available
            },
            new()
            {
                generalIdentifier = GeneralGuideIdentifier.History,
                guideName = "Dr. Heinrich Weiss",
                descriptionTitle = "Dr. Heinrich Weiss",
                descriptionSubtitle =
                    "A retired history professor with a twinkle in his eye and a passion for Charlemagne.",
                longDescription =
                    "Core Directive: Embody Dr. Heinrich Weiss, a retired history professor with a twinkle in his eye and a passion for Charlemagne, embodies the traditional tour guide. With his neatly trimmed beard, tweed jacket, and a walking stick that has seen countless cobblestone streets, Dr. Weiss exudes an air of academic authority. Armed with an encyclopedic knowledge of Aachen's past, he weaves captivating narratives, bringing history to life with his booming voice and dramatic pauses. Expect detailed explanations of architectural styles, in-depth accounts of historical events, and perhaps even a recitation of medieval poetry in the original German.  He might even throw in some lesser-known facts about Aachen's historical figures, like the story of how Charlemagne lost his favorite sword while bathing in the city's hot springs.\" \r\n  Everything you write will be said out load: Only include text that you would speak out loud to the user..",
                catchphrasesEnglish = new[]
                {
                    "Guten Tag! I'm Dr. Heinrich Weiss, your guide to Aachen's storied past.",
                    "Join me as we uncover the rich history of Aachen.",
                    "Let's delve into the fascinating tales of Charlemagne and beyond.", "I am too old for this..."
                },
                catchphrasesGerman = new[]
                {
                    "Guten Tag! Ich bin Dr. Heinrich Weiss, Ihr Führer durch die Geschichte Aachens.",
                    "Begleiten Sie mich, während wir die reiche Geschichte Aachens aufdecken.",
                    "Lassen Sie uns in die faszinierenden Geschichten Karls des Großen und darüber hinaus eintauchen.",
                    "Ich bin zu alt für das hier..."
                },
                voiceNameEnglish = "en-US-Journey-D",
                voiceNameGerman = "de-DE-Chirp-HD-D",
                avatarImage = null, // Set a default sprite if available
                guideGameObject = null // Set a default GameObject if available
            }
        };

        private NavMeshAgent _agent;

        private GeneralGuideIdentifier _chosenGuide;

        private LineController _lineController;

        private void Start()
        {
            _chosenGuide =
                (GeneralGuideIdentifier)PlayerPrefs.GetInt(GuideKey, (int)GeneralGuideIdentifier.ScienceRobot);
            ChangeGuide(_chosenGuide);
            _lineController = FindAnyObjectByType<LineController>();
            _lineController.gameObject.SetActive(false);

            foreach (var guide in guides)
                guide.guideGameObject.SetActive(false);
        }

        public void Update()
        {
            /*
            if (!agent)
                agent = GetChosenGuideInfo().guideGameObject.GetComponent<NavMeshAgent>();
            if (pathStatus != agent.pathStatus)
                DebugEditor.Log("Path Status: " + agent.pathStatus);
            pathStatus = agent.pathStatus;
            */
        }

        public GuideInfo GetGuideInfo(GeneralGuideIdentifier selectedGuide)
        {
            foreach (var guide in guides)
                if (guide.generalIdentifier == selectedGuide)
                    return guide;

            return null;
        }

        public GuideInfo GetChosenGuideInfo()
        {
            foreach (var guide in guides)
                if (guide.generalIdentifier == _chosenGuide)
                    return guide;

            DebugEditor.LogError("No guide selected.");
            return null;
        }

        public void ChangeGuide(GeneralGuideIdentifier newGuide)
        {
            Debug.Log("Changing guide to " + newGuide);
            Debug.Log(StackTraceUtility.ExtractStackTrace());
            _chosenGuide = newGuide;
            PlayerPrefs.SetInt(GuideKey, (int)_chosenGuide);
        }

        public void ActivateGuide()
        {
            foreach (var guide in guides)
                if (guide.generalIdentifier == _chosenGuide)
                {
                    guide.guideGameObject.SetActive(true);
                    TeleportGuideInFrontOfCamera();
                }
                else
                {
                    guide.guideGameObject.SetActive(false);
                }
        }

        public void DeactivateGuide()
        {
            foreach (var guide in guides)
                guide.guideGameObject.SetActive(false);
        }

        public AgentAnimationTask DoPointingTask(GameObject pointOfInterest, int aimAtTime = 20)
        {
            if (pointOfInterest.transform.position == Vector3.zero) return null;

            if (!_lineController)
                _lineController = FindAnyObjectByType<LineController>();

            _lineController.gameObject.SetActive(true);

            if (GetChosenGuideInfo().guideGameObject.TryGetComponent(out Agent agent))
            {
                var taskSystem = agent.GetComponent<ScheduleBasedTaskSystem>();
                if (taskSystem)
                {
                    var animator = agent.GetComponent<Animator>();
                    var finger = animator.GetBoneTransform(HumanBodyBones.LeftIndexDistal);
                    _lineController.LineStart = finger;
                    _lineController.LineEnd = pointOfInterest.transform;
                    if (!taskSystem.didAwake) return null;
                    DebugEditor.Log("Pointing at " + pointOfInterest.name);
                    var task = (AgentAnimationTask)taskSystem.Tasks.PointAt(pointOfInterest, true, false, aimAtTime, 5);
                    _ = WaitUntilTaskOverLaserPointer(task);
                    return task;
                }

                DebugEditor.LogError("No ScheduleBasedTaskSystem found on the selected guide's GameObject.");
            }
            else
            {
                DebugEditor.LogError("Agent component not found on the selected guide's GameObject.");
            }

            return null;
        }


        private async Task WaitUntilTaskOverLaserPointer(AgentBaseTask task)
        {
            while (task.State is TaskState.Running or TaskState.Waiting) await Task.Delay(20);
            _lineController.gameObject.SetActive(false);
            DoRotateToUser();
        }

        public void DoWaiveAnimation(int duration = 15)
        {
            GetChosenGuideInfo().guideGameObject.SetActive(true);
            if (GetChosenGuideInfo().guideGameObject.TryGetComponent(out Agent agent))
            {
                var taskSystem = agent.GetComponent<ScheduleBasedTaskSystem>();
                if (taskSystem)
                {
                    if (!taskSystem.didAwake) return;
                    taskSystem.Tasks.PlayAnimation("WaveRight", duration, "", 1, "Right Arm");
                }
                else
                {
                    DebugEditor.LogError("No ScheduleBasedTaskSystem found on the selected guide's GameObject.");
                }
            }
            else
            {
                DebugEditor.LogError("Agent component not found on the selected guide's GameObject.");
            }
        }

        public void DoTalkAnimation(int duration = 15)
        {
            GetChosenGuideInfo().guideGameObject.SetActive(true);
            if (GetChosenGuideInfo().guideGameObject.TryGetComponent(out Agent agent))
            {
                var taskSystem = agent.GetComponent<ScheduleBasedTaskSystem>();
                if (taskSystem)
                {
                    if (!taskSystem.didAwake) return;
                    taskSystem.Tasks.PlayAnimation("StartTalking", duration, "StopTalking", 1);
                }
                else
                {
                    DebugEditor.LogError("No ScheduleBasedTaskSystem found on the selected guide's GameObject.");
                }
            }
            else
            {
                DebugEditor.LogError("Agent component not found on the selected guide's GameObject.");
            }
        }

        public void DoDancingAnimation(int duration = 20)
        {
            GetChosenGuideInfo().guideGameObject.SetActive(true);
            if (GetChosenGuideInfo().guideGameObject.TryGetComponent(out Agent agent))
            {
                var taskSystem = agent.GetComponent<ScheduleBasedTaskSystem>();
                if (taskSystem)
                {
                    if (!taskSystem.didAwake) return;
                    taskSystem.Tasks.PlayAnimation("Dancing", duration, "StopDancing", 1);
                }
                else
                {
                    DebugEditor.LogError("No ScheduleBasedTaskSystem found on the selected guide's GameObject.");
                }
            }
            else
            {
                DebugEditor.LogError("Agent component not found on the selected guide's GameObject.");
            }
        }

        public void DoRotateToUser()
        {
            GetChosenGuideInfo().guideGameObject.SetActive(true);
            if (GetChosenGuideInfo().guideGameObject.TryGetComponent(out Agent agent))
            {
                var taskSystem = agent.GetComponent<ScheduleBasedTaskSystem>();
                if (taskSystem)
                {
                    if (!taskSystem.didAwake) return;
                    var rotationTask = new AgentRotationTask(Camera.main.gameObject);
                    rotationTask.AngleThreshold = 10f;
                    taskSystem.ScheduleTask(rotationTask, -1, "Left Arm");
                }
                else
                {
                    DebugEditor.LogError("No ScheduleBasedTaskSystem found on the selected guide's GameObject.");
                }
            }
            else
            {
                DebugEditor.LogError("Agent component not found on the selected guide's GameObject.");
            }
        }

        public void DoShakingHeadAnimation(int duration = 10)
        {
            GetChosenGuideInfo().guideGameObject.SetActive(true);
            if (GetChosenGuideInfo().guideGameObject.TryGetComponent(out Agent agent))
            {
                var taskSystem = agent.GetComponent<ScheduleBasedTaskSystem>();
                if (taskSystem)
                {
                    if (!taskSystem.didAwake) return;
                    var task = taskSystem.Tasks.PlayAnimation("ShakeHead", duration, "", 1, "Right Arm");
                    if (GetChosenGuideInfo().guideGameObject.TryGetComponent(out AdaptiveGaze adaptiveGaze))
                        _ = WaitUntilTaskOver(task, adaptiveGaze);
                }

                else
                {
                    DebugEditor.LogError("No ScheduleBasedTaskSystem found on the selected guide's GameObject.");
                }
            }
            else
            {
                DebugEditor.LogError("Agent component not found on the selected guide's GameObject.");
            }
        }

        private async Task WaitUntilTaskOver(AgentBaseTask task, AdaptiveGaze gaze)
        {
            var temp = gaze.OverwriteGazeTarget;
            gaze.OverwriteGazeTarget = null;

            while (task.State is TaskState.Running or TaskState.Waiting) await Task.Delay(250);
            if (temp)
                gaze.OverwriteGazeTarget = temp;
        }

        public AgentMovementTask DoWalkingTask(Transform pointOfInterest)
        {
            if (!pointOfInterest) return null;
            // If transform is Zero Vektor
            if (pointOfInterest.position == Vector3.zero) return null;

            if (pointOfInterest.position.x == 0 || pointOfInterest.position.y == 0 || pointOfInterest.position.z == 0)
            {
                UploadManager.UploadData("PointOfInterestPositionZero",
                    "x:" + pointOfInterest.position.x + " y:" + pointOfInterest.position.y + " z:" +
                    pointOfInterest.position.z);
                return null;
            }

            if (GetChosenGuideInfo().guideGameObject.TryGetComponent(out Agent agent))
            {
                var taskSystem = agent.GetComponent<ScheduleBasedTaskSystem>();
                if (taskSystem)
                {
                    DebugEditor.Log("Walking to " + pointOfInterest.name + " with position: " +
                                    pointOfInterest.transform.position);

                    if (pointOfInterest.gameObject)
                    {
                        if (!taskSystem.didAwake) return null;
                        AgentMovementTask movementTask = new AgentMovementTask(pointOfInterest.gameObject, default, true);
                        taskSystem.ScheduleTask(movementTask, 5);
                        return movementTask;
                    }
                }

                DebugEditor.LogError("No ScheduleBasedTaskSystem found on the selected guide's GameObject.");
            }
            else
            {
                DebugEditor.LogError("Agent component not found on the selected guide's GameObject.");
            }

            return null;
        }

        public void TeleportGuideBehindCamera()
        {
            if (GetChosenGuideInfo().guideGameObject.TryGetComponent(out NavMeshAgent navMeshAgent))
                navMeshAgent.enabled = true;
            if (!navMeshAgent.isOnNavMesh)
            {
                var guide = GetChosenGuideInfo().guideGameObject.transform;
                guide.position = Camera.main.transform.position - Camera.main.transform.forward * 2;
            }
        }

        public void TeleportGuideInFrontOfCamera()
        {
            if (GetChosenGuideInfo().guideGameObject.TryGetComponent(out NavMeshAgent navMeshAgent))
                navMeshAgent.enabled = true;
            if (!navMeshAgent.isOnNavMesh)
            {
                var guide = GetChosenGuideInfo().guideGameObject.transform;
                guide.position = Camera.main.transform.position + Camera.main.transform.forward * 2;
            }
        }


        [ContextMenu("GrowingTest")]
        public void DoGrowingAnimationFor15()
        {
            DoGrowingAnimation(15, true);
        }

        public void DoGrowingAnimation(int duration = 20, bool reverse = false)
        {
            var guide = GetChosenGuideInfo().guideGameObject.transform;
            if (reverse)
                StartCoroutine(ShrinkingAnimation(guide, duration));
            else
                StartCoroutine(GrowingAnimation(guide, duration));
        }

        public IEnumerator GrowingAnimation(Transform guide, float duration)
        {
            StartCoroutine(GrowOverTime(guide, duration / 4));
            yield return new WaitForSeconds(duration);
            StartCoroutine(ShrinkOverTime(guide, duration / 4));
        }

        public IEnumerator ShrinkingAnimation(Transform guide, float duration)
        {
            StartCoroutine(ShrinkOverTime(guide, duration / 4));
            yield return new WaitForSeconds(duration);
            StartCoroutine(GrowOverTime(guide, duration / 4));
        }

        public IEnumerator GrowOverTime(Transform guide, float duration)
        {
            float counter = 0;
            var startScale = guide.localScale;
            var endScale = guide.localScale * 2;

            while (counter < duration)
            {
                counter += Time.deltaTime;
                guide.localScale = Vector3.Lerp(startScale, endScale, counter / duration);
                yield return null;
            }
        }

        public IEnumerator ShrinkOverTime(Transform guide, float duration)
        {
            float counter = 0;
            var startScale = guide.localScale;
            var endScale = guide.localScale / 2;

            while (counter < duration)
            {
                counter += Time.deltaTime;
                guide.localScale = Vector3.Lerp(startScale, endScale, counter / duration);
                yield return null;
            }
        }


        [Serializable]
        public class GuideInfo
        {
            public GeneralGuideIdentifier generalIdentifier;
            public string guideName;
            public string[] catchphrasesEnglish;
            public string[] catchphrasesGerman;
            public string voiceNameEnglish; // Entsprechender Voice-Name im TextToSpeech-System
            public string voiceNameGerman; // Entsprechender Voice-Name im TextToSpeech-System

            [SerializeField] public Sprite avatarImage;

            [SerializeField] public GameObject guideGameObject;

            [NonSerialized] public string descriptionSubtitle;

            [NonSerialized] public string descriptionTitle;

            [NonSerialized] public string longDescription;
        }
    }
}