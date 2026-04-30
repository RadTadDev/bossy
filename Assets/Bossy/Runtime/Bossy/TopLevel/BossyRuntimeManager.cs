using System;
using UnityEngine;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Bossy
{
    /// <summary>
    /// Creates the runtime for bossy
    /// </summary>
    public class BossyRuntimeManager
    {
        /// <summary>
        /// Invoked after the runtime is created and entered.
        /// </summary>
        public event Action OnEnterRuntime;
        
        /// <summary>
        /// Invoked just before the runtime is destroyed.
        /// </summary>
        public event Action OnExitRuntime;
        
        private GameObject _root;
        
        public BossyRuntimeManager()
        {
#if UNITY_EDITOR
            EditorApplication.playModeStateChanged -= OnStateChange;
            EditorApplication.playModeStateChanged += OnStateChange;
            
            if (Application.isPlaying)
            {
                EnterRuntime();
            }
#else
            EnterRuntime();
#endif 
        }

#if UNITY_EDITOR
        private void OnStateChange(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                EnterRuntime();
            }
            else if (state == PlayModeStateChange.ExitingPlayMode)
            {
                ExitRuntime();
            }
        }
#endif

        private void EnterRuntime()
        {
            if (_root == null)
            {
                _root = new GameObject("[Bossy]");
                _root.AddComponent<BossyRuntime>();
                Object.DontDestroyOnLoad(_root);
            }

            OnEnterRuntime?.Invoke();
        }

        private void ExitRuntime()
        {
            OnExitRuntime?.Invoke();
            
            if (_root != null)
            {
                Object.Destroy(_root);
            }
        }
    }
}