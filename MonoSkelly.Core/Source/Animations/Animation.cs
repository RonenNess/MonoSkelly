/**
 * MonoSkelly Animation.
 * Ronen Ness 2021
 */
using System.Collections.Generic;


namespace MonoSkelly.Core
{
    /// <summary>
    /// Represent an animation attached to a skeleton.
    /// </summary>
    public class Animation
    {
        // animation steps
        List<AnimationStep> _steps = new List<AnimationStep>();

        /// <summary>
        /// Get animation steps count.
        /// </summary>
        public int StepsCount => _steps.Count;

        /// <summary>
        /// If true, animation will interpolate from last step back to first step.
        /// If false, will just freeze on last step.
        /// </summary>
        public bool Repeats;

        /// <summary>
        /// Get animation steps.
        /// </summary>
        public IReadOnlyList<AnimationStep> Steps => _steps.AsReadOnly();

        // skeleton this animation is attached to.
        Skeleton _skeleton;

        /// <summary>
        /// Animation name.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Create the animation instance.
        /// </summary>
        /// <param name="skeleton">Base skeleton.</param>
        /// <param name="name">Animation name.</param>
        internal Animation(Skeleton skeleton, string name)
        {
            _skeleton = skeleton;
            Name = name;
        }

        /// <summary>
        /// Clone this animation.
        /// </summary>
        public Animation Clone()
        {
            var clone = new Animation(_skeleton, Name);
            foreach (var step in _steps)
            {
                clone.AddStep(step.Clone());
            }
            return clone;
        }

        /// <summary>
        /// Remove an animation step by index.
        /// </summary>
        public void RemoveStep(int index)
        {
            _steps.RemoveAt(index);
        }

        /// <summary>
        /// Rename a bone.
        /// </summary>
        public void RenameBone(string fromPath, string toPath)
        {
            // rename animation bones
            foreach (var step in _steps)
            {
                step.RenameBone(fromPath, toPath);
            }
        }

        /// <summary>
        /// Add animation step.
        /// </summary>
        /// <param name="stepName">Optional step name.</param>
        /// <param name="duration">Step duration.</param>
        /// <param name="copyTransformFrom">If set, will copy all transformations from this step.</param>
        public void AddStep(string stepName = null, float duration = 1f, AnimationStep copyTransformFrom = null)
        {
            // create step
            var step = (copyTransformFrom != null) ? copyTransformFrom.Clone() : new AnimationStep();
            step.Name = stepName;
            step.Duration = duration;

            // set all bones default transforms
            if (copyTransformFrom == null)
            {
                foreach (var transform in _skeleton.GetAllDefaultTransformations())
                {
                    step.SetTransform(transform.Key, transform.Value);
                }
            }

            // add step
            AddStep(step);
        }

        /// <summary>
        /// Add an animation step.
        /// </summary>
        /// <param name="step">Animation step to add.</param>
        public void AddStep(AnimationStep step)
        {
            _steps.Add(step);
        }

        /// <summary>
        /// Set an animation step value.
        /// </summary>
        /// <param name="index">Index to set.</param>
        /// <param name="step">New animation step value.</param>
        public void SetStep(int index, AnimationStep step)
        {
            _steps[index] = step;
        }

        /// <summary>
        /// Split a given step based on time offset.
        /// </summary>
        public void Split(float time)
        {
            // iterate steps to find which one to split
            var currOffset = 0f;
            var index = 0;
            foreach (var step in _steps.ToArray())
            {
                // hit the begining of step, nothing to do
                if (time == currOffset) { return; }

                // did we find the step to break?
                if ((time > currOffset) && (time < currOffset + step.Duration))
                {  
                    var originDuration = step.Duration;
                    step.Duration = time - currOffset;
                    var newStep = step.Clone();
                    newStep.Duration = originDuration - step.Duration;
                    _steps.Insert(index, newStep);   
                    return;
                }

                // advance offset and index
                currOffset += step.Duration;
                index++;
            }
        }

        /// <summary>
        /// Get animation step from index.
        /// </summary>
        /// <param name="wrapIfOutOfIndex">If true, will wrap index if out of range.</param>
        public AnimationStep GetStep(int index, bool wrapIfOutOfIndex = false)
        {
            if (index >= _steps.Count)
            {
                if (wrapIfOutOfIndex)
                {
                    index = index % _steps.Count;
                }
                else
                {
                    index = _steps.Count - 1;
                }
            }
            return _steps[index];
        }
    }

}
