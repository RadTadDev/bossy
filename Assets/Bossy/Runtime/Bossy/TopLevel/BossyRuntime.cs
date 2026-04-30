using System;
using UnityEngine;

namespace Bossy
{
    /// <summary>
    /// A manager that provides runtime services to Bossy.
    /// </summary>
    internal sealed class BossyRuntime : MonoBehaviour
    {
        /// <summary>
        /// The single runtime, only valid during playmode.
        /// </summary>
        internal static BossyRuntime Instance { get; set; }

        // Allow this to be set before this runtime is created
        private static Action _inputPoll;
        
        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            name = "[Bossy]";
            DontDestroyOnLoad(gameObject);
        }
        
        /// <summary>
        /// Set a callback to run each update tick.
        /// </summary>
        /// <param name="inputPoll"></param>
        public static void SetInputPoll(Action inputPoll)
        {
            _inputPoll = inputPoll;
        }

        /// <summary>
        /// Gets a child object of the runtime.
        /// </summary>
        /// <param name="childName">The name of the child object.</param>
        /// <returns>The constructed object.</returns>
        public GameObject CreateChildObject(string childName)
        {
            var go = new GameObject(childName);
            go.transform.SetParent(transform);
            return go;
        }
        
        private void OnDestroy()
        {
            Instance = null;
        }

        private void Update()
        {
            _inputPoll?.Invoke();
        }
    }
}