using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace CC26
{
    // Spawns robots in order, hands control to one at a time, and soft resets the level.
    public class RobotQueue : MonoBehaviour
    {
        // Obstacles will subscribe to restore their start state.
        public static event Action LevelReset;

        [Header("Robots")]
        [Tooltip("Robots in spawn order.")]
        [SerializeField] private List<RobotDefinition> robots = new();
        [Tooltip("Where each robot spawns.")]
        [SerializeField] private Transform spawnPoint;

        [Header("Input")]
        [Tooltip("Decommissions the active robot.")]
        [SerializeField] private InputActionReference failureAction;
        [Tooltip("Soft resets the level.")]
        [SerializeField] private InputActionReference resetAction;

        [Header("Timing")]
        [Tooltip("Delay before the next robot spawns (s).")]
        [SerializeField] private float spawnDelay = 0.75f;
        [Tooltip("Delay before auto reset when out of robots (s).")]
        [SerializeField] private float outOfRobotsResetDelay = 1.5f;

        [Header("Camera")]
        [Tooltip("Follows the active robot.")]
        [SerializeField] private CinemachineCamera followCamera;

        [Header("UI")]
        [Tooltip("Parent for icons. Use a Horizontal Layout Group.")]
        [SerializeField] private Transform iconContainer;
        [Tooltip("Icon prefab. Sprite is set from the robot definition.")]
        [SerializeField] private Image iconPrefab;
        [SerializeField] private Color upcomingColor = new(1f, 1f, 1f, 0.6f);
        [SerializeField] private Color activeColor = Color.white;
        [SerializeField] private Color usedColor = new(0.3f, 0.3f, 0.3f, 0.6f);

        public Robot ActiveRobot { get; private set; }

        private readonly List<Robot> spawned = new();
        private readonly List<Image> icons = new();
        private int index;
        private Coroutine pending;

        private void OnEnable()
        {
            failureAction.action.performed += OnFailure;
            resetAction.action.performed += OnReset;
            failureAction.action.Enable();
            resetAction.action.Enable();
        }

        private void OnDisable()
        {
            failureAction.action.performed -= OnFailure;
            resetAction.action.performed -= OnReset;
        }

        private void Start()
        {
            foreach (RobotDefinition def in robots)
            {
                Image icon = Instantiate(iconPrefab, iconContainer);
                icon.sprite = def.icon;
                icons.Add(icon);
            }

            ResetLevel();
        }

        public void ResetLevel()
        {
            if (pending != null) StopCoroutine(pending);
            pending = null;

            foreach (Robot robot in spawned)
            {
                if (robot == null) continue;
                robot.Explode();
                Destroy(robot.gameObject);
            }
            spawned.Clear();
            ActiveRobot = null;
            index = -1;

            LevelReset?.Invoke();
            SpawnNext();
        }

        private void SpawnNext()
        {
            index++;

            if (index >= robots.Count)
            {
                RefreshIcons();
                pending = StartCoroutine(After(outOfRobotsResetDelay, ResetLevel));
                return;
            }

            Robot robot = Instantiate(robots[index].prefab, spawnPoint.position, Quaternion.identity);
            robot.Decommissioned += OnDecommissioned;
            robot.Activate();
            spawned.Add(robot);
            ActiveRobot = robot;

            if (followCamera != null) followCamera.Follow = robot.transform;
            RefreshIcons();
        }

        private void OnDecommissioned(Robot robot)
        {
            robot.Decommissioned -= OnDecommissioned;
            ActiveRobot = null;
            RefreshIcons();
            pending = StartCoroutine(After(spawnDelay + robot.ExtraSpawnDelay, SpawnNext));
        }

        private void RefreshIcons()
        {
            for (int i = 0; i < icons.Count; i++)
            {
                bool used = i < index || (i == index && ActiveRobot == null);
                icons[i].color = used ? usedColor : i == index ? activeColor : upcomingColor;
            }
        }

        // Paused when timeScale is 0
        private IEnumerator After(float delay, Action action)
        {
            yield return new WaitForSeconds(delay);
            pending = null;
            action();
        }

        // Input fires while paused, so ignore it then
        private void OnFailure(InputAction.CallbackContext _)
        {
            if (Time.timeScale > 0f && ActiveRobot != null) ActiveRobot.Decommission();
        }

        private void OnReset(InputAction.CallbackContext _)
        {
            if (Time.timeScale > 0f) ResetLevel();
        }
    }
}
