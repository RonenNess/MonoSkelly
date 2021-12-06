/**
 * MonoSkelly AnimationsBlender.
 * Ronen Ness 2021
 */
using Microsoft.Xna.Framework;


namespace MonoSkelly.Core
{
    /// <summary>
    /// Helper class to mix two animations.
    /// </summary>
    public class AnimationsBlender
    {
        /// <summary>
        /// Animation to blend from.
        /// </summary>
        public AnimationState BlendFrom;

        /// <summary>
        /// Animation to blend to.
        /// </summary>
        public AnimationState BlendTo;

        /// <summary>
        /// Blend factor from 'BlendFrom' to 'BlendTo'.
        /// 0.0 = BlendFrom, 1.0 = BlendTo.
        /// </summary>
        public float BlendFactor
        {
            get { return _factor; }
            set
            {
                if (value < 0) value = 0;
                else if (value > 1) value = 1;
                _factor = value;
            }
        }

        // merging factor.
        float _factor;

        /// <summary>
        /// Callback to invoke when one of the animations is done.
        /// </summary>
        /// <param name="isSource">If true, the finished animation is the 'BlendFrom' animation. If false, its the 'BlendTo' one.</param>
        /// <param name="name">Animation name.</param>
        public delegate void AnimationDoneCallback(bool isSource, string name);

        /// <summary>
        /// Animation done callback.
        /// </summary>
        public AnimationDoneCallback OnAnimationDone;

        /// <summary>
        /// Create the animations blender.
        /// </summary>
        public AnimationsBlender(AnimationState blendFrom, AnimationState blendTo)
        {
            BlendFrom = blendFrom;
            BlendTo = blendTo;
        }

        /// <summary>
        /// Switch between to and from animations.
        /// </summary>
        public void Switch(bool invertFactor = true)
        {
            var temp = BlendTo;
            BlendTo = BlendFrom;
            BlendFrom = temp;
            if (invertFactor) { _factor = 1f - _factor; }
        }

        /// <summary>
        /// Switch source and target animations, invert the blend factor (so there won't be a jump), and set the new blend target animation.
        /// </summary>
        /// <param name="blendTo">New animation to blend to.</param>
        public void SetNextAnimation(AnimationState blendTo)
        {
            Switch();
            BlendTo = blendTo;
        }

        /// <summary>
        /// Get bone absolute transformation for current animation step.
        /// </summary>
        /// <param name="bone">Bone id to get.</param>
        /// <param name="useAliases">If true, 'bone' parameter may be an alias instead of a bone path.</param>
        /// <returns>Bone transformation for current animation state.</returns>
        public Matrix GetBoneTransform(string bone, bool useAliases = true)
        {
            if (_factor <= 0)
            {
                return BlendFrom.GetBoneTransform(bone, useAliases);
            }

            if (_factor >= 1f)
            {
                return BlendTo.GetBoneTransform(bone, useAliases);
            }

            var fromMat = BlendFrom.GetBoneTransform(bone, useAliases);
            var toMat = BlendTo.GetBoneTransform(bone, useAliases);
            return Matrix.Lerp(fromMat, toMat, _factor);
        }

        /// <summary>
        /// Advance both animations by given delta time.
        /// </summary>
        /// <param name="deltaTime">Time passed since last frame.</param>
        public void Update(float deltaTime)
        {
            Update(deltaTime, out bool _, out int _, out bool _, out int _);
        }

        /// <summary>
        /// Advance both animations by given delta time.
        /// </summary>
        public void Update(float deltaTime, out bool didFinish1, out int stepsFinished1, out bool didFinish2, out int stepsFinished2)
        {
            BlendFrom.Update(deltaTime, out didFinish1, out stepsFinished1);
            if (didFinish1) { OnAnimationDone?.Invoke(true, BlendFrom.Name); }

            BlendTo.Update(deltaTime, out didFinish2, out stepsFinished2);
            if (didFinish2) { OnAnimationDone?.Invoke(false, BlendTo.Name); }
        }
    }
}
