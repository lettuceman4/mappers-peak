using System;
using UniRx;
using UniRx.Triggers;
using UnityEngine;


public class InputActionBasedFirstPersonControllerInput : FirstPersonControllerInput
{
    private MainInput inputActions;
    private float lookSmoothingFactor = 14.0f;

    // Override private observable to publish data
    public override IObservable<Vector2> Move => _move;
    private IObservable<Vector2> _move;

    public override IObservable<Vector2> Look => _look;
    private IObservable<Vector2> _look;

    private ReadOnlyReactiveProperty<bool> _run;
    public override ReadOnlyReactiveProperty<bool> Run => _run;

    private Subject<Unit> _jump;
    public override IObservable<Unit> Jump => _jump;

    private void Awake()
    {
        inputActions = new MainInput();

        Debug.Assert(inputActions != null);

        // Hide the mouse cursor and lock it in the game window.
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        _move = this.UpdateAsObservable()
            .Select(_ =>
            {
                return inputActions.Character.Move.ReadValue<Vector2>();
            });
        var smoothLookValue = new Vector2(0, 0);
        _look = this.UpdateAsObservable()
            .Select(_ =>
            {
                var rawLookValue = inputActions.Character.Look.ReadValue<Vector2>();

                smoothLookValue = new Vector2(
                    Mathf.Lerp(smoothLookValue.x, rawLookValue.x, lookSmoothingFactor * Time.deltaTime),
                    Mathf.Lerp(smoothLookValue.y, rawLookValue.y, lookSmoothingFactor * Time.deltaTime)
                );

                return smoothLookValue;
            });
        _run = this.UpdateAsObservable()
            .Select(_ => inputActions.Character.Run.ReadValueAsObject() != null)
            .ToReadOnlyReactiveProperty();
        _jump = new Subject<Unit>().AddTo(this);
        inputActions.Character.Jump.performed += context =>
        {
            _jump.OnNext(Unit.Default);
        };
    }

    private void OnEnable()
    {
        inputActions.Enable();
    }

    private void OnDisable()
    {
        inputActions.Disable();
    }

    private void Update()
    {
        Debug.Log("Update in InputActionBasedFirstPersonControllerInput");
    }
 
}
