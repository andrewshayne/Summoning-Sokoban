using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Assets.Scripts;

public class DecorationController : MonoBehaviour
{
    public Animator animator;
    public int id = 0;

    void Awake()
    {
        animator.SetInteger("id", id);
    }
}
