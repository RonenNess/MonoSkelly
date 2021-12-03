/**
 * MonoSkelly AnimationStep.
 * Ronen Ness 2021
 */
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace MonoSkelly.Core
{
    /// <summary>
    /// A single animation step.
    /// </summary>
    public class AnimationStep
    {
        /// <summary>
        /// Animation step name.
        /// </summary>
        public string Name;

        /// <summary>
        /// Animation step duration in seconds.
        /// </summary>
        public float Duration;

        /// <summary>
        /// Interpolation for movement.
        /// </summary>
        public InterpolationTypes PositionInterpolation = InterpolationTypes.Linear;

        /// <summary>
        /// Interpolation for scale.
        /// </summary>
        public InterpolationTypes ScaleInterpolation = InterpolationTypes.Linear;

        /// <summary>
        /// Interpolation for rotation.
        /// </summary>
        public InterpolationTypes RotationInterpolation = InterpolationTypes.SphericalLinear;

        // all transformations
        Dictionary<string, Transformation> _transforms = new Dictionary<string, Transformation>();

        /// <summary>
        /// Get transformations as readonly dictionary.
        /// </summary>
        public ReadOnlyDictionary<string, Transformation> ReadOnlyTransformations { get; private set; }

        /// <summary>
        /// Individual bone data in animation step.
        /// </summary>
        public struct BoneData
        {
            public int Parent;
            public Vector3 Scale;
            public Quaternion Rotation; 
            public Vector3 Translation;
            public Matrix WorldMatrix;
        }

        // step transformations presented as flat bones list.
        BoneData[] _bones;
        bool _bonesDirty = true;

        // convert bone string name to bone index
        Dictionary<string, int> _bonePathToIndex = new Dictionary<string, int>();

        // parent animation
        Animation _animation;

        /// <summary>
        /// Create the animation step.
        /// </summary>
        public AnimationStep(Animation animation)
        {
            _animation = animation;
            ReadOnlyTransformations = new ReadOnlyDictionary<string, Transformation>(_transforms);
        }

        /// <summary>
        /// Clone this animation step.
        /// </summary>
        public AnimationStep Clone(Animation parentAnimation = null)
        {
            var clone = new AnimationStep(parentAnimation ?? _animation);
            clone.Name = Name;
            clone.Duration = Duration;
            foreach (var transform in _transforms)
            {
                clone._transforms[transform.Key] = transform.Value;
            }
            clone.ReadOnlyTransformations = new ReadOnlyDictionary<string, Transformation>(_transforms);
            return clone;
        }

        /// <summary>
        /// Rename a bone.
        /// </summary>
        public void RenameBone(string fromPath, string toPath)
        {
            string ReplacePrefix(string path)
            {
                return toPath + path.Substring(fromPath.Length);
            }
            _transforms = _transforms.ToDictionary(pair => pair.Key.StartsWith(fromPath) ? ReplacePrefix(pair.Key) : pair.Key, pair => pair.Value);
            _bonePathToIndex = _bonePathToIndex.ToDictionary(pair => pair.Key.StartsWith(fromPath) ? ReplacePrefix(pair.Key) : pair.Key, pair => pair.Value);
            ReadOnlyTransformations = new ReadOnlyDictionary<string, Transformation>(_transforms);
        }

        /// <summary>
        /// Set bone transformations for this step.
        /// </summary>
        /// <param name="bone">Bone id to set.</param>
        /// <param name="transform">Transformation to set.</param>
        public void SetTransform(string bone, Transformation transform)
        {
            _transforms[bone] = transform;

            // reset flat bones structure so we'll rebuild it
            // this is very slow, but SetTransform is not meant to be called in runtime, only in editor.
            _bonesDirty = true; 
        }

        /// <summary>
        /// Get transformation for a given bone.
        /// </summary>
        /// <param name="bone">Bone to get animation step for.</param>
        /// <returns>Transformation for given bone in this animation step.</returns>
        public Transformation GetTransform(string bone)
        {
            _transforms.TryGetValue(bone, out Transformation ret);
            return ret;
        }

        /// <summary>
        /// Build bones flat structure.
        /// </summary>
        void BuildFlatBonesStructure()
        {
            // reset path-to-index dictionary
            _bonePathToIndex.Clear();

            // resize bones array if needed
            if (_bones == null || _bones.Length != _transforms.Count)
            {
                _bones = new BoneData[_transforms.Count];
            }

            // method to build a single bone
            int index = 0;
            void BuildBone(string path)
            {
                // already built? skip
                if (_bonePathToIndex.ContainsKey(path)) { return; }

                // build parent first
                var parentSplit = path.LastIndexOf('/');
                string parentPath = null;
                if (parentSplit > 0)
                {
                    parentPath = path.Substring(0, parentSplit);
                    BuildBone(parentPath);
                }

                // update path-to-index dictionary
                _bonePathToIndex[path] = index;

                // get component
                var localMatrix = GetTransform(path).ToMatrix();
                localMatrix.Decompose(out Vector3 scale, out Quaternion rotation, out Vector3 translation);

                // get parent
                var parentIndex = parentPath != null ? _bonePathToIndex[parentPath] : -1;

                // build bone
                _bones[index] = new BoneData()
                {
                    Parent = parentIndex,
                    Scale = scale,
                    Rotation = rotation,
                    Translation = translation,
                    WorldMatrix = localMatrix * (parentIndex == -1 ? Matrix.Identity : _bones[parentIndex].WorldMatrix)
                };

                // set next index
                index++;
            }

            // build bones array
            foreach (var transform in _transforms.Keys)
            {
                BuildBone(transform);
            }

            // update readonly dict
            ReadOnlyTransformations = new ReadOnlyDictionary<string, Transformation>(_transforms);

            // no longer dirty
            _bonesDirty = false;
        }

        /// <summary>
        /// Get bone by its full path.
        /// </summary>
        /// <param name="createDefaultIfMissing">If true, will create a default bone based on skeleton if missing. If false, wil throw exception.</param>
        public BoneData GetBone(string bone, bool createDefaultIfMissing = true)
        {
            // build bones flat array
            if (_bonesDirty || _bones == null)
            {
                BuildFlatBonesStructure();
            }

            // return bone
            if (_bonePathToIndex.TryGetValue(bone, out int index))
            {
                return GetBone(index);
            }

            // if not found in animation step, take default
            if (createDefaultIfMissing)
            {
                var transform = _animation._skeleton.GetTransform(bone, null, 0, false);
                SetTransform(bone, transform);
                return GetBone(bone, false);
            }

            // if got here it means bone is missing and we should throw
            throw new System.Exception($"Missing bone '{bone}'!");
        }

        /// <summary>
        /// Get bone by index.
        /// </summary>
        public BoneData GetBone(int bone)
        {
            // build bones flat array
            if (_bonesDirty || _bones == null)
            {
                BuildFlatBonesStructure();
            }

            // return bone
            return _bones[bone];
        }
    }
}
