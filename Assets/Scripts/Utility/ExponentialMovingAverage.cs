using System;
using UnityEngine;

[Serializable]
public abstract class ExponentialMovingAverage<T> where T : struct
{
    protected abstract class Number
    {
        protected readonly T value;
    
        protected Number() {}

        protected Number(T value)
        {
            this.value = value;
        }

        protected abstract Number Add(Number x);
        protected abstract Number Multiply(float x);

        public static Number operator +(Number a, Number b) => a.Add(b);
        public static Number operator *(float a, Number b) => b.Multiply(a);
        public static implicit operator T(Number n) => n.value;
    }

    public const string TIME_CONSTANT_TOOLTIP = "Time constant for the exponential moving average filter. This is represents about a fifth of the time " +
                                                "to reach steady-state after a transient or half of the time to reach 86.5% of steady-state, etc.";
    
    [Tooltip(TIME_CONSTANT_TOOLTIP)]
    [SerializeField] private float timeConstant;

    private int index;
    protected readonly Number[] samples;
    protected readonly Number[] movingAverage;
    
    protected ExponentialMovingAverage(Number[] samples, Number[] movingAverage, float? tau = null)
    {
        this.samples = samples;
        this.movingAverage = movingAverage;
        timeConstant = tau ?? timeConstant;
    }

    /// <summary>
    /// The current value of the moving average.
    /// </summary>
    public T Value => movingAverage[index];

    /// <summary>
    /// The time constant of the filter.
    /// </summary>
    public float Tau => timeConstant;

    protected abstract Number GetNumber(T value);

    /// <summary>
    /// Compute the new average based on the given <see cref="sample"/> and <paramref name="deltaTime"/>.
    /// </summary>
    /// <param name="sample">A new value to use to compute the moving average.</param>
    /// <param name="deltaTime">The delta time since the last sample was added.</param>
    /// <returns>The new value of the moving average when the given sample is added.</returns>
    protected virtual Number GetUpdatedAverage(T sample, float deltaTime)
    {
        int nextIndex = (index + 1) % 2;
        samples[nextIndex] = GetNumber(sample);

        float t = deltaTime / timeConstant;
        float w = Mathf.Exp(-t);
        float w2 = (1f - w) / t;

        return w * movingAverage[index] + (1f - w2) * samples[nextIndex] + (w2 - w) * samples[index];
    }

    /// <summary>
    /// Add a sample to the moving average.
    /// </summary>
    /// <param name="sample">The value of the sample to add.</param>
    /// <param name="deltaTime">The time delta since the last sample added.</param>
    public void AddSample(T sample, float deltaTime)
    {
        Number newAverage = GetUpdatedAverage(sample, deltaTime);
        index = ++index % 2;
        movingAverage[index] = newAverage;
    }

    /// <summary>
    /// Reset the moving average to the given <paramref name="value"/>.
    /// </summary>
    /// <param name="value">The new <see cref="Value"/> of the moving average.</param>
    public void Reset(T value)
    {
        Array.Fill(movingAverage, GetNumber(value));
        Array.Fill(samples, GetNumber(value));
    }
    
    public static implicit operator T(ExponentialMovingAverage<T> average) => average.Value;
}

[Serializable]
public class ExpMovingAverageVector : ExponentialMovingAverage<Vector2>
{
    protected class Vector : Number
    {
        public Vector() {}
        public Vector(Vector2 value) : base(value) {}

        protected override Number Add(Number x) => new Vector(value + x);
        protected override Number Multiply(float x) => new Vector(value * x);
        public static implicit operator Vector(Vector2 v) => new(v);
    }
    
    public ExpMovingAverageVector() : this(null) {}
    
    public ExpMovingAverageVector(float? tau = null) : 
        base(new Number[] { new Vector(), new Vector() },
            new Number[] { new Vector(), new Vector() },
            tau) { }

    protected override Number GetNumber(Vector2 value) => new Vector(value);

    public static implicit operator Vector3(ExpMovingAverageVector avg) => avg.Value;
}

[Serializable]
public class ExpMovingAverageFloat : ExponentialMovingAverage<float>
{
    protected class Float : Number
    {
        public Float() {}
        public Float(float value) : base(value) {}

        protected override Number Add(Number x) => new Float(value + x);
        protected override Number Multiply(float x) => new Float(value * x);

        public static implicit operator Float(float x) => new(x);
    }
    
    public ExpMovingAverageFloat() : this(null) {}
    
    public ExpMovingAverageFloat(float? tau = null) :
        base(new Number[] { new Float(), new Float() },
            new Number[] { new Float(), new Float() },
            tau) {}

    protected override Number GetNumber(float value) => new Float(value);
}
