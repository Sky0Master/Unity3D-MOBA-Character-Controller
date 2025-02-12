using UnityEngine;

public class MoveAnimation : MonoBehaviour
{
    Vector3 lastPos;
    // Update is called once per frame
    public Animator animator;
    void Start()
    {
        lastPos = transform.position;
    }
    void Update()
    {
        var offset = transform.position - lastPos;
        offset.y = 0;
        if(offset.magnitude <= 0.01f) offset = Vector3.zero;
        lastPos = transform.position;
        animator.SetFloat("Speed", offset.magnitude / Time.deltaTime);
    }
}
