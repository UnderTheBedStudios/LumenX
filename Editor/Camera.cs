using System;
using System.Collections.Generic;
using System.Numerics;
using Avalonia.Input;

namespace LumenX;

public class Camera
{
    public Vector3 Position = new(0,0,3);
    public float Yaw = -90f, Pitch = 0f;
    public float MoveSpeed = 5f, LookSensitivity = 0.15f;

    public void ApplyMouseDelta(double dx, double dy)
    {
        Yaw     += (float)dx * LookSensitivity;
        Pitch   -= (float)dy * LookSensitivity;
        Pitch = Math.Clamp(Pitch, -89f, 89f);
    }

    public void Update(HashSet<Key> keys, float dt)
    {
        var forward = Forward;
        var right = Vector3.Normalize(Vector3.Cross(forward, Vector3.UnitY));
        float speed = MoveSpeed * dt;

        if (keys.Contains(Key.W)) Position += forward * speed;
        if (keys.Contains(Key.S)) Position -= forward * speed;
        if (keys.Contains(Key.A)) Position -= right * speed;
        if (keys.Contains(Key.D)) Position += right * speed;
        if (keys.Contains(Key.Q)) Position -= Vector3.UnitY * speed;
        if (keys.Contains(Key.E)) Position += Vector3.UnitY * speed;
    }

    public Matrix4x4 GetViewMatrix() =>
        Matrix4x4.CreateLookAt(Position, Position + Forward, Vector3.UnitY);

    public Vector3 Forward => Vector3.Normalize(new Vector3(
            MathF.Cos(ToRad(Yaw)) * MathF.Cos(ToRad(Pitch)),
            MathF.Sin(ToRad(Pitch)),
            MathF.Sin(ToRad(Yaw)) * MathF.Cos(ToRad(Pitch))
    ));
    private static float ToRad(float deg) => deg * MathF.PI / 180f;
}