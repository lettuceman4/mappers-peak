using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

#if UNITY_EDITOR
[InitializeOnLoad]
#endif

public class DivideVector2ByDeltaTimeProcessor : InputProcessor<Vector2>
{
#if UNITY_EDITOR
    static DivideVector2ByDeltaTimeProcessor()
    {
        Initialize();
    }
#endif
    [RuntimeInitializeOnLoadMethod] 
    private static void Initialize()
    {
        InputSystem.RegisterProcessor<DivideVector2ByDeltaTimeProcessor>();
    }

    public override Vector2 Process(Vector2 value, InputControl control)
    {
        return value / Time.deltaTime;
    }
}