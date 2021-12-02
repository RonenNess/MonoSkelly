/**
 * MonoSkelly AnimationState.
 * Ronen Ness 2021
 */
using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace MonoSkelly.Core
{
    /// <summary>
    /// Animation state to store animation progress.
    /// </summary>
    public struct AnimationState
    {
        // skeleton and animation prototype
        Skeleton _skeleton;
        Animation _animation;

        // current animation step
        AnimationStep _step;

        // next animation step
        AnimationStep _nextStep;

        /// <summary>
        /// Get current step progress, from 0.0 to 1.0.
        /// </summary>
        public float StepProgress => ElapsedTimeInStep / StepDuration;

        /// <summary>
        /// Elapsed time, in seconds, in given step.
        /// </summary>
        public float ElapsedTimeInStep { get; private set; }

        /// <summary>
        /// Get current step duration.
        /// </summary>
        public float StepDuration => _step != null ? _step.Duration : 0f;

        /// <summary>
        /// Get current step name or null.
        /// </summary>
        public string StepName => _step != null ? _step.Name : null;

        /// <summary>
        /// Animation step index.
        /// </summary>
        public int StepIndex { get; private set; }

        /// <summary>
        /// If true, loop animation to first step when done.
        /// If false, will remain stuck on last step.
        /// Note: set by default from animation.Repeats property.
        /// </summary>
        public bool Repeats;

        /// <summary>
        /// Get animation steps count.
        /// </summary>
        public int StepsCount => _animation.StepsCount;

        /// <summary>
        /// Get animation name.
        /// </summary>
        public string Name => _animation.Name;

        /// <summary>
        /// Create a new animation state.
        /// </summary>
        /// <param name="skeleton">Skeleton type.</param>
        /// <param name="animation">Animation instance.</param>
        internal AnimationState(Skeleton skeleton, Animation animation)
        {
            _skeleton = skeleton;
            _animation = animation;
            ElapsedTimeInStep = 0f;
            StepIndex = 0;
            _nextStep = _step = null;
            Repeats = animation.Repeats;
            _cachedTransforms = new Dictionary<string, Matrix>();
            Reset();
        }

        /// <summary>
        /// Reset animation state.
        /// </summary>
        public void Reset(int step = 0)
        {
            StepIndex = step;
            ElapsedTimeInStep = 0f;
            if (_animation.StepsCount > 0)
            {
                _step = _animation.GetStep(StepIndex);
                _nextStep = _animation.GetStep(StepIndex + 1, Repeats);
            }
            _cachedTransforms.Clear();
        }

        // cache transformed matrices.
        Dictionary<string, Matrix> _cachedTransforms;

        /// <summary>
        /// Get bone absolute transformation for current animation step.
        /// </summary>
        /// <param name="bone">Bone id to get.</param>
        /// <param name="useAliases">If true, 'bone' parameter may be an alias instead of a bone path.</param>
        /// <returns>Bone transformation for current animation state.</returns>
        public Matrix GetBoneTransform(string bone, bool useAliases = true)
        {
            // resolve alias if used
            if (useAliases)
            {
                bone = _skeleton.ResolveAlias(bone) ?? bone;
            }

            // get from cache
            if (_cachedTransforms.TryGetValue(bone, out Matrix cached))
            {
                return cached;
            }

            // get progress
            var a = StepProgress;

            // get bone 'from' and 'to' transformations
            var fromBone = _step.GetBone(bone);
            var toBone = _nextStep.GetBone(bone);

            // get parent transform
            Matrix parentTransform;
            if (fromBone.Parent >= 0)
            {
                var parent = bone.Substring(0, bone.LastIndexOf('/'));
                parentTransform = GetBoneTransform(parent);
            }
            else
            {
                parentTransform = Matrix.Identity;
            }

            // method to interpolate vector
            Vector3 InterpolateVector(Vector3 _from, Vector3 _to, InterpolationTypes type)
            {
                switch (type)
                {
                    case InterpolationTypes.Linear:
                        return Vector3.Lerp(_from, _to, a);

                    case InterpolationTypes.SmoothStep:
                        return new Vector3(MathHelper.SmoothStep(_from.X, _to.X, a), MathHelper.SmoothStep(_from.Y, _to.Y, a), MathHelper.SmoothStep(_from.Z, _to.Z, a));

                    case InterpolationTypes.SmoothDamp:
                        var a2 = a * (1f + 1f - a);
                        return Vector3.Lerp(_from, _to, a2);

                    case InterpolationTypes.SphericalLinear:
                        throw new System.Exception("Spherical Linear is not supported for vectors!");

                    default:
                        throw new System.Exception("Unknown interpolation method!");
                }
            }

            // method to interpolate quaternion
            Quaternion InterpolateQuaternion(Quaternion _from, Quaternion _to, InterpolationTypes type)
            {
                switch (type)
                {
                    case InterpolationTypes.Linear:
                        return Quaternion.Lerp(_from, _to, a);

                    case InterpolationTypes.SmoothStep:
                        throw new System.Exception("Smooth Step is not supported for quaternions!");

                    case InterpolationTypes.SmoothDamp:
                        var a2 = a * (1f + 1f - a);
                        return Quaternion.Slerp(_from, _to, a2);

                    case InterpolationTypes.SphericalLinear:
                        return Quaternion.Slerp(_from, _to, a);

                    default:
                        throw new System.Exception("Unknown interpolation method!");
                }
            }

            // interpolate values
            var scale = InterpolateVector(fromBone.Scale, toBone.Scale, _step.ScaleInterpolation);
            var translate = InterpolateVector(fromBone.Translation, toBone.Translation, _step.PositionInterpolation);
            var rotation = InterpolateQuaternion(fromBone.Rotation, toBone.Rotation, _step.RotationInterpolation);

            // build transformation
            var interpolated = Matrix.CreateScale(scale) * Matrix.CreateFromQuaternion(rotation) * Matrix.CreateTranslation(translate);
            var ret = interpolated * parentTransform;
            _cachedTransforms[bone] = ret;
            return ret;
        }

        /// <summary>
        /// Advance animation by given delta time.
        /// </summary>
        /// <param name="deltaTime">Time passed since last frame.</param>
        public void Update(float deltaTime)
        {
            Update(deltaTime, out bool _, out int _);
        }

        /// <summary>
        /// Advance animation by given delta time.
        /// </summary>
        /// <param name="deltaTime">Time passed since last frame.</param>
        /// <param name="didFinish">Turns true if animation ended.</param>
        /// <param name="stepsFinished">How many steps were completed in this update.</param>
        public void Update(float deltaTime, out bool didFinish, out int stepsFinished)
        {
            // no steps? skip
            if (_animation.StepsCount == 0)
            {
                didFinish = true;
                stepsFinished = 0;
                return;
            }

            // did finish?
            if (!Repeats && (StepIndex >= _animation.StepsCount))
            {
                didFinish = true;
                stepsFinished = 0;
                return;
            }

            // clear cached transformations
            _cachedTransforms.Clear();

            // reset out params
            stepsFinished = 0;
            didFinish = false;

            // advance current step
            ElapsedTimeInStep += deltaTime;

            // check if finish current step
            while (ElapsedTimeInStep >= StepDuration)
            {
                // advance step
                ElapsedTimeInStep -= StepDuration;
                StepIndex++;
                stepsFinished++;

                // wrap animation
                if (StepIndex >= _animation.StepsCount)
                {
                    didFinish = true;
                    if (!Repeats) { return; }
                    StepIndex = 0;
                }

                // get new step
                _step = _animation.GetStep(StepIndex);
                _nextStep = _animation.GetStep(StepIndex + 1, Repeats);
            }
        }
    }
}
