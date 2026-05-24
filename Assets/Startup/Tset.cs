using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Startup
{
    public class Tset : MonoBehaviour
    {
        private void Update()
        {
            if (Keyboard.current.digit1Key.wasPressedThisFrame)
            {
                Debug.Log("This is info");
            }
            if (Keyboard.current.digit2Key.wasPressedThisFrame)
            {
                Debug.LogWarning("This is warning");
            }
            if (Keyboard.current.digit3Key.wasPressedThisFrame)
            {
                Debug.LogError("This is error");
            }
            if (Keyboard.current.digit4Key.wasPressedThisFrame)
            {
                Debug.LogException(new InvalidOperationException("This is an exception"));
            }
        }
    }
}