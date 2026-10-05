using UnityEngine;

namespace CC26
{
    [CreateAssetMenu(fileName = "Robot_", menuName = "CC26/Robot Definition")]
    public class RobotDefinition : ScriptableObject
    {
        [Tooltip("Robot prefab spawned by the queue.")]
        public Robot prefab;

        [Tooltip("Icon shown in the robot queue UI.")]
        public Sprite icon;
    }
}
