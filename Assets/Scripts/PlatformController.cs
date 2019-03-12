﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Collider2D))]
public class PlatformController : MonoBehaviour {

    public PlatformWaypoint currentWaypoint;
    public float maxSpeed;
    public float accelerationDistance;
    public float decelerationDistance;
    public float waitTime;
    public float crumbleTime;
    public float restoreTime;
    public bool onlyPlayerCrumble;

    [SerializeField]
    private Vector2 speed = Vector2.zero;
    private float currentWaitTime = 0;
    private float currentCrumbleTime = 0;
    private float currentRestoreTime = 0;
    private bool crumbled = false;
    private List<Controller2D> actors = new List<Controller2D>();
    private Animator animator;
    private Collider2D myCollider;
    private PhysicsConfig pConfig;

    private static readonly string ANIMATION_CRUMBLING = "crumbling";
    private static readonly string ANIMATION_CRUMBLE = "crumble";
    private static readonly string ANIMATION_RESTORE = "restore";

    /// <summary>
    /// Start is called on the frame when a script is enabled just before
    /// any of the Update methods is called the first time.
    /// </summary>
    void Start() {
        animator = GetComponent<Animator>();
        myCollider = GetComponent<Collider2D>();
        pConfig = GameObject.FindObjectOfType<PhysicsConfig>();
        if (!pConfig) {
            pConfig = (PhysicsConfig) new GameObject().AddComponent(typeof(PhysicsConfig));
            pConfig.gameObject.name = "Physics Config";
            Debug.LogWarning("PhysicsConfig not found on the scene! Using default config.");
        }
    }

    // Update is called once per frame
    void Update() {
        if (crumbled) {
            if (currentRestoreTime > 0) {
                currentRestoreTime -= Time.deltaTime;
                if (currentRestoreTime <= 0) {
                    Restore();
                }
            }
        } else {
            if (currentCrumbleTime > 0) {
                currentCrumbleTime -= Time.deltaTime;
                if (currentCrumbleTime <= 0) {
                    crumbled = true;
                    animator.SetTrigger(ANIMATION_CRUMBLE);
                    myCollider.enabled = false;
                    if (restoreTime > 0) {
                        currentRestoreTime = restoreTime;
                    }
                }
            }
            if (currentWaypoint) {
                if (currentWaitTime > 0) {
                    currentWaitTime -= Time.deltaTime;
                    return;
                }
                Vector2 distance = currentWaypoint.transform.position - transform.position;
                if (distance.magnitude <= decelerationDistance) {
                    if (distance.magnitude > 0) {
                        speed -= Time.deltaTime * distance.normalized * maxSpeed * maxSpeed /
                            (2 * decelerationDistance);
                    } else {
                        speed = Vector2.zero;
                    }
                } else if (speed.magnitude < maxSpeed) {
                    if (accelerationDistance > 0) {
                        speed += Time.deltaTime * distance.normalized * maxSpeed * maxSpeed /
                            (2 * accelerationDistance);
                    }
                    if (speed.magnitude > maxSpeed || accelerationDistance <= 0) {
                        speed = distance.normalized * maxSpeed;
                    }
                }
                Vector3 newPos = Vector2.MoveTowards(transform.position, currentWaypoint.transform.position,
                    speed.magnitude * Time.deltaTime);
                Vector2 velocity = newPos - transform.position;
                if (speed.y > 0) {
                    MoveActors(velocity);
                    transform.position = newPos;
                } else {
                    transform.position = newPos;
                    MoveActors(velocity);
                }
                distance = currentWaypoint.transform.position - transform.position;
                if (distance.magnitude < 0.00001f) {
                    speed = Vector2.zero;
                    currentWaypoint = currentWaypoint.nextWaipoint;
                    currentWaitTime = waitTime;
                }
            }
        }
    }

    /// <summary>
    /// Moves all the actors touching the platform along it's own direction
    /// </summary>
    /// <param name="velocity">Velocity in which the actors should be moved</param>
    private void MoveActors(Vector2 velocity) {
        foreach (Controller2D actor in actors) {
            actor.Move(velocity);
        }
    }

    /// <summary>
    /// Sent when another object enters a trigger collider attached to this
    /// object (2D physics only).
    /// </summary>
    /// <param name="other">The other Collider2D involved in this collision.</param>
    void OnTriggerStay2D(Collider2D other) {
        if (crumbled) {
            return;
        }
        Controller2D actor = other.GetComponent<Controller2D>();
        if (actor && !actors.Contains(actor)) {
            // doesn't attach to the actor if it's a 1 way platform and the actor is below it
            if (LayerMask.GetMask(LayerMask.LayerToName(gameObject.layer)) == pConfig.owPlatformMask &&
                (actor.transform.position.y < transform.position.y || actor.TotalSpeed.y > 0)) {
                return;
            } else {
                actors.Add(actor);
                if (crumbleTime > 0 && currentCrumbleTime <= 0) {
                    if (!onlyPlayerCrumble ||
                        LayerMask.GetMask(LayerMask.LayerToName(actor.gameObject.layer)) == pConfig.playerMask) {
                        currentCrumbleTime = crumbleTime;
                        animator.SetTrigger(ANIMATION_CRUMBLING);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Sent when another object leaves a trigger collider attached to
    /// this object (2D physics only).
    /// </summary>
    /// <param name="other">The other Collider2D involved in this collision.</param>
    void OnTriggerExit2D(Collider2D other) {
        Controller2D actor = other.GetComponent<Controller2D>();
        if (actor && actors.Contains(actor)) {
            actors.Remove(actor);
            actor.ApplyForce(speed);
        }
    }

    public void Restore() {
        crumbled = false;
        myCollider.enabled = true;
        animator.SetTrigger(ANIMATION_RESTORE);
    }
}