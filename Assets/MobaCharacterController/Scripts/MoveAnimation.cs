using System.Collections;
using UnityEngine;

public class MoveAnimation : MonoBehaviour
{
    Vector3 lastPos;
    // Update is called once per frame
    public Animator animator;
    public string moveSpeedKey = "Speed";
    public string motionSpeedKey = "MotionSpeed";
    public float motionSpeedFactor = 0.2f;

    void Start()
    {
        lastPos = transform.position;
    }

    float _curSpeed;
    void Update()
    {
        _curSpeed = animator.GetFloat(moveSpeedKey);
        
        var offset = transform.position - lastPos;
        offset.y = 0;
        if(offset.magnitude <= 0.01f) 
            offset = Vector3.zero;
        
        float speed = offset.magnitude / Time.deltaTime;
        animator.SetFloat("Speed", speed);
        animator.SetFloat(motionSpeedKey, speed * motionSpeedFactor);

        lastPos = transform.position;
    }
    private void OnFootstep(AnimationEvent animationEvent)
    {
            // if (animationEvent.animatorClipInfo.weight > 0.5f)
            // {
            //     if (FootstepAudioClips.Length > 0)
            //     {
            //         var index = Random.Range(0, FootstepAudioClips.Length);
            //         AudioSource.PlayClipAtPoint(FootstepAudioClips[index], transform.TransformPoint(_controller.center), FootstepAudioVolume);
            //     }
            // }
    }

    private void OnLand(AnimationEvent animationEvent)
    {
        // if (animationEvent.animatorClipInfo.weight > 0.5f)
        // {
        //     AudioSource.PlayClipAtPoint(LandingAudioClip, transform.TransformPoint(_controller.center), FootstepAudioVolume);
        // }
    }
}
