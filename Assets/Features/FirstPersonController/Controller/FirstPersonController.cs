using System;
using UniRx;
using UniRx.Triggers;
using UnityEngine;


/// <summary>
///     Controller that handles the character controls and camera controls of the first person player.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour, ICharacterSignals
{
    [Header("References")]
    [SerializeField] private FirstPersonControllerInput firstPersonControllerInput;
    private CharacterController _characterController;
    private Camera _camera;

    float ICharacterSignals.StrideLength => throw new NotImplementedException();

    ReactiveProperty<bool> ICharacterSignals.IsRunning => throw new NotImplementedException();

    IObservable<Vector3> ICharacterSignals.Moved => throw new NotImplementedException();

    IObservable<Unit> ICharacterSignals.Landed => throw new NotImplementedException();

    IObservable<Unit> ICharacterSignals.Jumped => throw new NotImplementedException();

    IObservable<Unit> ICharacterSignals.Stepped => throw new NotImplementedException();

    public IObservable<Unit> Jumped => _jumped;
    private Subject<Unit> _jumped;

    private float _strideLength = 2.5f;
    public float StrideLength => _strideLength;

    private ReactiveProperty<bool> _isRunning;
    public ReactiveProperty<bool> IsRunning => _isRunning;

    private Subject<Vector3> _moved;
    public IObservable<Vector3> Moved => _moved;

    private readonly float _walkingSpeed = 5f;
    private readonly float _runSpeed = 10f;
    private readonly float _jumpSpeed = 5f;


    private readonly float stickToGroundForceMagnitude = 5f;
    
    [Header("Look Properties")]
    [Range(-90, 0)][SerializeField] private float minViewAngle = -60f;
    [Range(0, 90)][SerializeField] private float maxViewAngle = 60f;

    private void Awake() {
        _isRunning = new ReactiveProperty<bool>(false);
        _moved = new Subject<Vector3>().AddTo(this);
        _characterController = GetComponent<CharacterController>();
        _camera = GetComponentInChildren<Camera>();
    }

    private void Start()
    {
        // inital force to the character 
        _characterController.Move(-stickToGroundForceMagnitude * this.transform.up);
        var jumpLatch = LatchObservables.Latch(this.UpdateAsObservable(), firstPersonControllerInput.Jump, false);

        // handle move and zipping of move with jump
        firstPersonControllerInput.Move
            .Zip(jumpLatch, (m, j) => new MoveInputData(m, j))
            .Where(moveInputData => moveInputData.Jump || moveInputData.Move != Vector2.zero)
            .Subscribe(input =>
            {
                // determine whether the char was grounded before starting the calculations in the frame
                var wasGrounded = this._characterController.isGrounded;

                // vertical movements are the player's y-axis
                var verticalVelocity = 0f;

                // if char is grounded and wna jump
                if (input.Jump && wasGrounded)
                {
                    verticalVelocity = _jumpSpeed;
                    _jumped.OnNext(Unit.Default);
                }

                // if char is in air: apply gravity 
                else if (!wasGrounded)
                {
                    verticalVelocity = _characterController.velocity.y + (Physics.gravity.y * Time.deltaTime * 3.0f);
                }
                
                // on the ground: push down a little
                else
                {
                    verticalVelocity = -Math.Abs(stickToGroundForceMagnitude);
                }

                // horizontal movement
                var currentSpeed = firstPersonControllerInput.Run.Value ? _runSpeed : _walkingSpeed;
                var horizontalVelocity = currentSpeed * input.Move;

                // integrate both horizontal and vertical velocity
                var characterVelocity = this.transform.TransformVector(new Vector3(horizontalVelocity.x, -verticalVelocity, horizontalVelocity.y));

                var motion = characterVelocity * Time.deltaTime;
                _characterController.Move(motion);

                var tempIsRunning = false;

                // both started and ended this frame on the ground
                if (wasGrounded && _characterController.isGrounded)
                {
                    _moved.OnNext(_characterController.velocity * Time.deltaTime);
                    if (_characterController.velocity.magnitude > 0)
                    {
                        // the char is running if the input is active and the char is actlly moving on the ground
                        tempIsRunning = firstPersonControllerInput.Run.Value;
                    }
                }
                _isRunning.Value = tempIsRunning;
            }).AddTo(this);

        firstPersonControllerInput.Look.Where(v => v != Vector2.zero).Subscribe(input =>
        {
            var horizontalLook = input.x * Time.deltaTime * Vector3.up;
            transform.localRotation *= Quaternion.Euler(horizontalLook);
            var verticalLook = input.y * Time.deltaTime * Vector3.left;
            
            // calculate the rotation applied to the local rotation of the cam transform, save it so we can clamp the rotation
            var newQ = _camera.transform.localRotation * Quaternion.Euler(verticalLook);

            // clamp the new rotation
            _camera.transform.localRotation = RotationTools.ClampRotationAroundXAxis(newQ, -maxViewAngle, -minViewAngle);

        }).AddTo(this);


    }

}

public struct MoveInputData
{
    public readonly Vector2 Move;
    public readonly bool Jump;

    public MoveInputData(Vector2 move, bool jump)
    {
        this.Move = move;
        this.Jump = jump;
    }
}
