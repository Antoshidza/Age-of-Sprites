using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

#if UNITY_EDITOR
using UnityEditor;
[InitializeOnLoad]
#endif
[DisplayStringFormat("{firstTouch}+{secondTouch}")]
public class PinchingComposite : InputBindingComposite<float>
{
    [InputControl(layout = "Value")]
    public int firstTouch;
    [InputControl(layout = "Value")]
    public int secondTouch;

    public float negativeScale = 1f;
    public float positiveScale = 1f;

    private struct TouchStateComparer : IComparer<TouchState>
    {
        public int Compare(TouchState x, TouchState y) => 1;
    }

    // This method computes the resulting input value of the composite based
    // on the input from its part bindings.
    public override float ReadValue(ref InputBindingCompositeContext context)
    {
        var touch_0 = context.ReadValue<TouchState, TouchStateComparer>(firstTouch);
        var touch_1 = context.ReadValue<TouchState, TouchStateComparer>(secondTouch);

        if (touch_0.phase != TouchPhase.Moved || touch_1.phase != TouchPhase.Moved)
            return 0f;

        var startDistance = math.distance(touch_0.startPosition, touch_1.startPosition);
        var distance = math.distance(touch_0.position, touch_1.position);

        var unscaledValue = startDistance / distance - 1f; // startDistance divide by distance to invert value
        return unscaledValue * (unscaledValue < 0 ? negativeScale : positiveScale);
    }

    // This method computes the current actuation of the binding as a whole.
    public override float EvaluateMagnitude(ref InputBindingCompositeContext context) => 1f;

    static PinchingComposite() => InputSystem.RegisterBindingComposite<PinchingComposite>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init() { } // Trigger static constructor.
}