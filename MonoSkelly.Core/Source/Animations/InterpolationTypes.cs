/**
 * MonoSkelly InterpolationTypes.
 * Ronen Ness 2021
 */

namespace MonoSkelly.Core
{
    /// <summary>
    /// Ways to interpolate between steps.
    /// </summary>
    public enum InterpolationTypes
    {
        /// <summary>
        /// Movement turns slower towards the end of the step.
        /// </summary>
        SmoothDamp,

        /// <summary>
        /// Interpolates between two values using a cubic equation.
        /// </summary>
        SmoothStep,

        /// <summary>
        /// Linear interpolation, movement is even throughout the entire duration of the step.
        /// </summary>
        Linear,

        /// <summary>
        /// Spherical Linear Interpolation, useful for vectors with direction.
        /// </summary>
        SphericalLinear
    }
}
