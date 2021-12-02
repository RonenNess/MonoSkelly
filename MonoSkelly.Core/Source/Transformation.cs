/**
 * MonoSkelly Transformations.
 * Ronen Ness 2021
 */
using Microsoft.Xna.Framework;


namespace MonoSkelly.Core
{
    /// <summary>
    /// Bone's transformation.
    /// </summary>
    public struct Transformation
    {
        /// <summary>
        /// Offset from parent.
        /// </summary>
        public Vector3 Offset;

        /// <summary>
        /// Rotation.
        /// </summary>
        public Vector3 Rotation;

        /// <summary>
        /// Scale.
        /// </summary>
        public Vector3 Scale;

        // convert to radians
        static float ToRads = (System.MathF.PI / 180);

        /// <summary>
        /// Convert transformations to matrix.
        /// </summary>
        public Matrix ToMatrix()
        {
            var scale = Matrix.CreateScale(Scale);
            var rotateMatrix = Matrix.CreateFromYawPitchRoll(Rotation.X * ToRads, Rotation.Y * ToRads, Rotation.Z * ToRads);
            var translation = Matrix.CreateTranslation(Offset.X, Offset.Y, Offset.Z);
            return scale * rotateMatrix * translation;
        }

        /// <summary>
        /// Lerp between two transformations.
        /// </summary>
        public static Transformation Lerp(Transformation first, Transformation second, float amount)
        {
            return new Transformation()
            {
                Offset = Vector3.Lerp(first.Offset, second.Offset, amount),
                Rotation = Vector3.Lerp(first.Rotation, second.Rotation, amount),
                Scale = Vector3.Lerp(first.Scale, second.Scale, amount)
            };
        }

        /// <summary>
        /// Identity transformation.
        /// </summary>
        public static readonly Transformation Identity = new Transformation() { Offset = Vector3.Zero, Rotation = Vector3.Zero, Scale = Vector3.One };

        /// <summary>
        /// All-zero transformations.
        /// </summary>
        public static readonly Transformation Empty = new Transformation();
    }
}
