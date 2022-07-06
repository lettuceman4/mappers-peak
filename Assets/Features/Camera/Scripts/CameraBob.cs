using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using UniRx.Triggers;
using UnityEngine.Serialization;

[RequireComponent(typeof(Camera))]
public class CameraBob : MonoBehaviour
{
    [SerializeField] 
    private GameObject characterSignalsInterfaceTarget;
    private ICharacterSignals _characterSignals;
    private Camera _camera; 
    private Vector3 initialPosition; 
    public float walkBobMagnitude = 0.05f; 
    public float runBobMagnitude = 0.10f; 
    public AnimationCurve bob = new AnimationCurve(
        new Keyframe(0.00f, 0f),
        new Keyframe(0.25f, 1f),
        new Keyframe(0.50f, 0f),
        new Keyframe(0.75f, -1f),
        new Keyframe(1.00f, 0f));
    
    private void Awake() 
    { 
        _characterSignals = characterSignalsInterfaceTarget.GetComponent<ICharacterSignals>();
        _camera = GetComponent<Camera>(); 
        initialPosition = _camera.transform.localPosition; 
    }

    private void Start()
    {
        var distance = 0f; _characterSignals.Moved.Subscribe(w => {
            // Accumulate distance walked (modulo stride length).
            distance += w.magnitude;
            distance %= _characterSignals.StrideLength;
            
            // Use distance to evaluate the bob curve.
            var magnitude = _characterSignals.IsRunning.Value ? runBobMagnitude : walkBobMagnitude;
            var deltaPos = magnitude * bob.Evaluate(distance /
                _characterSignals.StrideLength) * Vector3.up;

            // Adjust camera position.
            _camera.transform.localPosition = initialPosition + deltaPos;
        }).AddTo(this);
    }
}