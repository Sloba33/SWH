using UnityEngine;
using UnityEngine.UI;

using System;
using System.Diagnostics;
public class TraceableButton : MonoBehaviour
{
    private Button _button;
    void Update()
    {
        if (!GetComponent<Button>().interactable)
        {
            // This will spam the console when button is disabled
            UnityEngine.Debug.LogWarning($"{name} is NOT interactable! Investigating...", this);

            // Check common reasons
            CanvasGroup canvasGroup = GetComponentInParent<CanvasGroup>();
            if (canvasGroup != null && !canvasGroup.interactable)
            {
                UnityEngine.Debug.LogError($"Parent CanvasGroup '{canvasGroup.name}' is disabling interaction!", canvasGroup);
            }

            // Only log once per disable state
            // this.enabled = false;
        }
    }
    void Start()
    {
        _button = GetComponent<Button>();
        if (_button != null)
        {
            // Log initial state
            UnityEngine.Debug.Log($"[TRACE] {name} initial state - Interactable: {_button.interactable}", this);
        }
    }

    public void SetInteractableWithTrace(bool state, string callerInfo = "")
    {
        if (_button != null && _button.interactable != state)
        {
            // Get stack trace to see who called this
            StackTrace stackTrace = new StackTrace(true);
            string caller = string.IsNullOrEmpty(callerInfo) ? GetCallerMethod(stackTrace) : callerInfo;

            UnityEngine.Debug.Log($"[TRACE] {name} interactable changed to: {state} by: {caller}", this);
            _button.interactable = state;
        }
    }

    private string GetCallerMethod(StackTrace stackTrace)
    {
        // Skip the current method and get the caller
        for (int i = 1; i < stackTrace.FrameCount; i++)
        {
            StackFrame frame = stackTrace.GetFrame(i);
            var method = frame.GetMethod();
            string className = method.DeclaringType?.Name ?? "Unknown";
            string methodName = method.Name;

            // Skip Unity and system methods
            if (!className.Contains("TraceableButton") && !className.Contains("UnityEngine"))
            {
                return $"{className}.{methodName} (Line:{frame.GetFileLineNumber()})";
            }
        }
        return "Unknown";
    }
}