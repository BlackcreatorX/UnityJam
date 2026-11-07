using System;
using System.Collections;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class AimBallBoard : MonoBehaviour
{
    enum State { Ready, Flying, Resolving }

    [Header("Referencias")]
    public Transform ball;
    public Transform ballStart;
    public Transform hole;
    public Transform aimPivot;

    [Header("Agujero")]
    public float holeRadius = 0.45f;
    public float holeTravel = 2.0f;
    public float holeSpeed = 0.9f;

    [Header("Puntos por precisión (fracción del radio)")]
    [Range(0f, 1f)] public float bullseye = 0.35f;
    [Range(0f, 1f)] public float good = 0.7f;

    [Header("Tiro")]
    public float ballSpeed = 8f;
    public float maxAngle = 50f;
    public float missDistance = 0.6f;
    public float sinkTime = 0.15f;
    public float resetDelay = 0.4f;

    [Header("Estado")]
    public bool inputEnabled;
    public bool paused;

    public event Action<int> Resolved;

    public Vector3 HoleCenter => holeCenter;

    State state = State.Ready;
    Vector3 dir;
    Vector3 ballScale;
    Vector3 holeCenter;
    float holeTime;
    float aimAngle;
    Camera cam;

    void Awake()
    {
        cam = Camera.main;
        if (ball) ballScale = ball.localScale;
        if (hole) holeCenter = hole.position;
        ResetBall();
    }

    void Update()
    {
        if (paused) return;

        MoveHole(Time.deltaTime);

        switch (state)
        {
            case State.Ready:
                UpdateAim();
                if (inputEnabled && ThrowPressed()) Throw();
                break;

            case State.Flying:
                Fly(Time.deltaTime);
                break;
        }

        if (aimPivot) aimPivot.gameObject.SetActive(state == State.Ready && inputEnabled);
    }

    void MoveHole(float dt)
    {
        holeTime += dt;
        var p = holeCenter;
        p.x += Mathf.Sin(holeTime * holeSpeed) * holeTravel;
        hole.position = p;
    }

    void UpdateAim()
    {
        if (!cam) cam = Camera.main;
        if (!cam) return;

        Vector3 d = MouseWorld() - ballStart.position;
        float angle = Mathf.Atan2(d.x, d.y) * Mathf.Rad2Deg;
        aimAngle = Mathf.Clamp(angle, -maxAngle, maxAngle);

        if (aimPivot)
        {
            aimPivot.position = ballStart.position;
            aimPivot.rotation = Quaternion.Euler(0f, 0f, -aimAngle);
        }
    }

    void Throw()
    {
        float r = aimAngle * Mathf.Deg2Rad;
        dir = new Vector3(Mathf.Sin(r), Mathf.Cos(r), 0f);
        state = State.Flying;
    }

    void Fly(float dt)
    {
        Vector3 prev = ball.position;
        Vector3 next = prev + dir * ballSpeed * dt;
        float hy = hole.position.y;

        if (prev.y < hy && next.y >= hy)
        {
            float t = (hy - prev.y) / (next.y - prev.y);
            float x = Mathf.Lerp(prev.x, next.x, t);
            float dx = Mathf.Abs(x - hole.position.x);

            if (dx <= holeRadius)
            {
                ball.position = new Vector3(x, hy, prev.z);
                StartCoroutine(Sink(Score(dx)));
                return;
            }
        }

        ball.position = next;
        if (next.y > hy + missDistance) StartCoroutine(Miss());
    }

    int Score(float dx)
    {
        float f = dx / holeRadius;
        if (f <= bullseye) return 3;
        if (f <= good) return 2;
        return 1;
    }

    IEnumerator Sink(int points)
    {
        state = State.Resolving;

        Vector3 from = ball.position;
        float t = 0f;
        while (t < 1f)
        {
            if (!paused)
            {
                t += Time.deltaTime / sinkTime;
                var target = new Vector3(hole.position.x, hole.position.y, from.z);
                ball.position = Vector3.Lerp(from, target, t);
                ball.localScale = Vector3.Lerp(ballScale, Vector3.zero, t);
            }
            yield return null;
        }

        Resolved?.Invoke(points);
        yield return new WaitForSeconds(resetDelay);
        ResetBall();
    }

    IEnumerator Miss()
    {
        state = State.Resolving;
        ball.localScale = Vector3.zero;
        Resolved?.Invoke(0);
        yield return new WaitForSeconds(resetDelay);
        ResetBall();
    }

    public void ResetBall()
    {
        StopAllCoroutines();
        if (ball && ballStart)
        {
            ball.position = ballStart.position;
            ball.localScale = ballScale;
        }
        state = State.Ready;
    }

    Vector3 MouseWorld()
    {
#if ENABLE_INPUT_SYSTEM
        Vector2 screen = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
#else
        Vector2 screen = Input.mousePosition;
#endif
        var w = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -cam.transform.position.z));
        w.z = 0f;
        return w;
    }

    static bool ThrowPressed()
    {
#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        var mouse = Mouse.current;
        return (mouse != null && mouse.leftButton.wasPressedThisFrame)
            || (kb != null && kb.spaceKey.wasPressedThisFrame);
#else
        return Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space);
#endif
    }
}
