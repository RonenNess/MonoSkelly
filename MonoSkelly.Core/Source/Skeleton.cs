/**
 * MonoSkelly Skeleton.
 * Ronen Ness 2021
 */
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;


namespace MonoSkelly.Core
{
    /// <summary>
    /// Define skeleton pose.
    /// </summary>
    public class Skeleton
    {
        // internal animation step to represent transformations.
        AnimationStep _defaultPose = new AnimationStep(null);

        /// <summary>
        /// All the data we store for a bone.
        /// </summary>
        class BonePreviewMesh
        {
            /// <summary>
            /// Bone debug mesh transformation.
            /// </summary>
            public Transformation Transform;

            /// <summary>
            /// Should we render bone debug mesh.
            /// </summary>
            public bool Visible;
        }

        // preview meshes data
        Dictionary<string, BonePreviewMesh> _previewMeshesData = new Dictionary<string, BonePreviewMesh>();

        // animations attached to this skeleton
        Dictionary<string, Animation> _animations = new Dictionary<string, Animation>();

        /// <summary>
        /// Get animation keys.
        /// </summary>
        public IEnumerable<string> Animations => _animations.Keys;

        /// <summary>
        /// Get all aliases.
        /// </summary>
        public IEnumerable<string> Aliases => _aliases.Keys;

        // model used to show bone handles and bones
        static Model _handleModel;
        static Model _boneModel;
        static Texture2D _boneTexture;

        // bone aliases
        Dictionary<string, string> _aliases = new Dictionary<string, string>();

        /// <summary>
        /// Set the model to use when drawing bones handles and bone parts for debug preview.
        /// </summary>
        /// <param name="handle">Model to use for bones handle.</param>
        /// <param name="bone">Model to use for drawing the bone itself.</param>
        /// <param name="boneTexture">Texture to use to render bones.</param>
        public static void SetDebugModels(Model handle, Model bone, Texture2D boneTexture)
        {
            _handleModel = handle;
            _boneModel = bone;
            _boneTexture = boneTexture;
        }

        /// <summary>
        /// Static constructor.
        /// </summary>
        static Skeleton()
        {
            // method to load Vector3
            Sini.IniFile.DefaultConfig.CustomParsers[typeof(Vector3)] = (string val) =>
            {
                if (val.Contains("{X:"))
                {
                    val = val.Replace("{X:", "").Replace(" Y:", ",").Replace(" Z:", ",").Trim('}');
                }
                var parts = val.Split(',');
                return new Vector3() { X = float.Parse(parts[0]), Y = float.Parse(parts[1]), Z = float.Parse(parts[2]) };
            };
        }

        /// <summary>
        /// Create the composite mesh.
        /// </summary>
        public Skeleton()
        {
        }

        /// <summary>
        /// Start and return a new animation instance.
        /// </summary>
        public AnimationState BeginAnimation(string name)
        {
            return new AnimationState(this, _animations[name]);
        }

        /// <summary>
        /// Set alias for bone.
        /// </summary>
        public void SetAlias(string bone, string alias)
        {
            _aliases = _aliases.Where(x => x.Value != bone).ToDictionary(pair => pair.Key, pair => pair.Value);
            _aliases[alias] = bone;
        }

        /// <summary>
        /// Check if an alias exists under this skeleton.
        /// </summary>
        public bool HaveAlias(string alias)
        {
            return _aliases.ContainsKey(alias);
        }

        /// <summary>
        /// Get alias for a bone, or null if not set.
        /// </summary>
        public string GetBoneAlias(string bone)
        {
            foreach (var alias in _aliases)
            {
                if (alias.Value == bone) { return alias.Key; }
            }
            return null;
        }

        /// <summary>
        /// Get alias value or null if not set.
        /// </summary>
        public string ResolveAlias(string alias)
        {
            _aliases.TryGetValue(alias, out string ret);
            return ret;
        }

        /// <summary>
        /// Delete an animation.
        /// </summary>
        public void DeleteAnimation(string animation)
        {
            _animations.Remove(animation);
        }

        /// <summary>
        /// Create a new animation.
        /// </summary>
        public Animation CreateAnimation(string animation)
        {
            var ret = new Animation(this, animation);
            _animations[animation] = ret;
            return ret;
        }

        /// <summary>
        /// Clone existing animation.
        /// </summary>
        public Animation CloneAnimation(string source, string newName)
        {
            var ret = _animations[source].Clone();
            _animations[newName] = ret;
            return ret;
        }

        /// <summary>
        /// Get animation instance.
        /// </summary>
        public Animation GetAnimation(string animation)
        {
            return _animations[animation];
        }

        /// <summary>
        /// Check if an animation exist.
        /// </summary>
        public bool AnimationExists(string animation)
        {
            return _animations.ContainsKey(animation);
        }

        /// <summary>
        /// Add a bone model and set its transformation.
        /// </summary>
        /// <param name="path">Bone path to set.</param>
        /// <param name="transformation">Bone transformation.</param>
        public void SetBonePreviewModel(string path, Transformation transformation)
        {
            _previewMeshesData[path].Transform = transformation;
            _previewMeshesData[path].Visible = true;
        }

        /// <summary>
        /// Add a bone model and set its transformation.
        /// </summary>
        /// <param name="path">Bone path to set.</param>
        /// <param name="offset">Bone model offset.</param>
        /// <param name="size">Bone model size.</param>
        public void SetBonePreviewModel(string path, Vector3 offset, Vector3 size)
        {
            _previewMeshesData[path].Transform = new Transformation() { Offset = offset, Scale = size };
            _previewMeshesData[path].Visible = true;
        }

        /// <summary>
        /// Remove a bone model and set its transformation.
        /// </summary>
        /// <param name="path">Bone path to set.</param>
        public void RemoveBonePreviewModel(string path)
        {
            if (_previewMeshesData.ContainsKey(path))
            {
                _previewMeshesData[path].Visible = false;
            }
        }

        /// <summary>
        /// Return if a given bone have debug mesh rendering.
        /// </summary>
        public bool HavePreviewModel(string path)
        {
            return _previewMeshesData.ContainsKey(path) && _previewMeshesData[path].Visible;
        }

        /// <summary>
        /// Get bone preview model transformations.
        /// </summary>
        public Transformation GetPreviewModelTransform(string path)
        {
            if (_previewMeshesData.TryGetValue(path, out BonePreviewMesh ret))
            {
                return ret.Transform;
            }
            return Transformation.Identity;
        }

        /// <summary>
        /// Set a bone's transform.
        /// </summary>
        public void SetTransform(string path, Transformation transform)
        {
            _defaultPose.SetTransform(path, transform);
        }

        /// <summary>
        /// Set a bone's transform.
        /// </summary>
        public void SetTransform(string path, string animation, int step, Vector3 offset, Vector3 rotation, Vector3 scale)
        {
            var transform = new Transformation() { Offset = offset, Rotation = rotation, Scale = scale };

            if (string.IsNullOrEmpty(animation) || step < 0)
            {
                _defaultPose.SetTransform(path, transform);
            }
            else
            {
                _animations[animation].GetStep(step).SetTransform(path, transform);
            }
        }

        /// <summary>
        /// Get a bone's transformations.
        /// </summary>
        public Transformation GetTransform(string path, string animation, int stepIndex, bool useAliases = true)
        {
            if (useAliases)
            {
                path = ResolveAlias(path) ?? path;
            }

            if (string.IsNullOrEmpty(animation) || stepIndex < 0)
            {
                return _defaultPose.GetTransform(path);
            }

            return _animations[animation].GetStep(stepIndex).GetTransform(path);
        }

        /// <summary>
        /// Save skeleton data to .ini file.
        /// </summary>
        /// <param name="path">INI file path.</param>
        public void SaveTo(string path)
        {
            var sini = Sini.IniFile.CreateEmpty();
            SaveTo(sini);
            sini.SaveTo(path);
        }

        /// <summary>
        /// Save skeleton data to .ini file.
        /// </summary>
        /// <param name="ini">INI file instance.</param>
        public void SaveTo(Sini.IniFile ini)
        {
            // get bone keys (we use this multiple times so keep the order!)
            var boneKeys = _previewMeshesData.Keys.ToArray();

            // write bones
            int i = 0;
            foreach (var bone in _defaultPose.ReadOnlyTransformations)
            {
                ini.SetValue("bones", $"bone_{i}_path", bone.Key);
                ini.SetValue("bones", $"bone_{i}_offset", bone.Value.Offset.ToString());
                ini.SetValue("bones", $"bone_{i}_rotation", bone.Value.Rotation.ToString());
                ini.SetValue("bones", $"bone_{i}_scale", bone.Value.Scale.ToString());
                i++;
            }
            ini.SetValue("bones", "count", i.ToString());

            // write renderables
            i = 0;
            foreach (var renderable in _previewMeshesData)
            {
                if (renderable.Value.Visible)
                {
                    ini.SetValue("meshes", $"mesh_{i}_parent", renderable.Key);
                    ini.SetValue("meshes", $"mesh_{i}_offset", renderable.Value.Transform.Offset.ToString());
                    ini.SetValue("meshes", $"mesh_{i}_scale", renderable.Value.Transform.Scale.ToString());
                    ini.SetValue("meshes", $"mesh_{i}_rotation", renderable.Value.Transform.Rotation.ToString());
                    i++;
                }
            }
            ini.SetValue("meshes", "count", i.ToString());

            // save aliases
            foreach (var alias in _aliases)
            {
                if (!string.IsNullOrWhiteSpace(alias.Key))
                {
                    ini.SetValue("aliases", alias.Key, alias.Value);
                }
            }

            // save animations
            ini.SetValue("animations", "keys", String.Join(',', _animations.Keys));
            foreach (var animation in _animations)
            {
                var section = "animation_" + animation.Key;
                ini.SetValue(section, "steps_count", animation.Value.StepsCount.ToString());
                ini.SetValue(section, "repeats", animation.Value.Repeats.ToString());

                i = 0;
                foreach (var step in animation.Value.Steps)
                {
                    ini.SetValue(section, $"step_{i}_name", step.Name);
                    ini.SetValue(section, $"step_{i}_duration", step.Duration.ToString());
                    ini.SetValue(section, $"step_{i}_inter_position", step.PositionInterpolation.ToString());
                    ini.SetValue(section, $"step_{i}_inter_scale", step.ScaleInterpolation.ToString());
                    ini.SetValue(section, $"step_{i}_inter_rotation", step.RotationInterpolation.ToString());

                    int j = 0;
                    foreach (var bone in boneKeys)
                    {
                        var transform = step.GetTransform(bone);
                        ini.SetValue(section, $"step_{i}_bone_{j}_offset", transform.Offset.ToString());
                        ini.SetValue(section, $"step_{i}_bone_{j}_rotation", transform.Rotation.ToString());
                        ini.SetValue(section, $"step_{i}_bone_{j}_scale", transform.Scale.ToString());
                        j++;
                    }
                    i++;
                }
            }
        }

        /// <summary>
        /// Load skeleton data from .ini file.
        /// </summary>
        /// <param name="path">INI file path.</param>
        public void LoadFrom(String path)
        {
            var sini = new Sini.IniFile(path);
            LoadFrom(sini);
        }

        /// <summary>
        /// Load skeleton data from .ini file.
        /// </summary>
        /// <param name="ini">INI file instance.</param>
        public void LoadFrom(Sini.IniFile ini)
        {
            // to convert bone index to path while deserializing
            List<string> boneIndexToPath = new List<string>();

            // load bones
            var bonesCount = ini.GetInt("bones", "count");
            for (var i = 0; i < bonesCount; i++)
            {
                var path = ini.GetStr("bones", $"bone_{i}_path");
                var offset = ini.GetCustomType("bones", $"bone_{i}_offset", Vector3.Zero);
                var rotation = ini.GetCustomType("bones", $"bone_{i}_rotation", Vector3.Zero);
                var scale = ini.GetCustomType("bones", $"bone_{i}_scale", Vector3.One);
                boneIndexToPath.Add(path);
                AddBone(path, offset, rotation, scale);
            }

            // load debug meshes
            var meshesCount = ini.GetInt("meshes", "count");
            for (var i = 0; i < meshesCount; i++)
            {
                var path = ini.GetStr("meshes", $"mesh_{i}_parent");
                var offset = ini.GetCustomType("meshes", $"mesh_{i}_offset", Vector3.Zero);
                var scale = ini.GetCustomType("meshes", $"mesh_{i}_scale", Vector3.Zero);
                var rotation = ini.GetCustomType("meshes", $"mesh_{i}_rotation", Vector3.Zero);
                SetBonePreviewModel(path, new Transformation() { Offset = offset, Scale = scale, Rotation = rotation });
            }

            // load aliases
            _aliases.Clear();
            if (ini.ContainsSection("aliases"))
            {
                var aliases = ini.GetKeys("aliases");
                foreach (var alias in aliases)
                {
                    var bone = ini.GetStr("aliases", alias);
                    _aliases[alias] = bone;
                }
            }

            // load animations
            var animations = ini.GetStr("animations", "keys", "").Split(',');
            foreach (var animation in animations)
            {
                // special case to skip empty
                if (animation == String.Empty) { continue; }

                // create new animation type and get section name
                var animationInstance = new Animation(this, animation);
                var section = "animation_" + animation;

                // load if repeating animation
                animationInstance.Repeats = ini.GetBool(section, "repeats");

                // read animation steps
                var stepsCount = ini.GetInt(section, "steps_count");
                for (var i = 0; i < stepsCount; ++i)
                {
                    // create animation step
                    var step = new AnimationStep(animationInstance);
                    step.Name = ini.GetStr(section, $"step_{i}_name");
                    step.Duration = ini.GetFloat(section, $"step_{i}_duration");
                    step.PositionInterpolation = ini.GetEnum(section, $"step_{i}_inter_position", InterpolationTypes.Linear);
                    step.ScaleInterpolation = ini.GetEnum(section, $"step_{i}_inter_scale", InterpolationTypes.Linear);
                    step.RotationInterpolation = ini.GetEnum(section, $"step_{i}_inter_rotation", InterpolationTypes.SphericalLinear);

                    // read transform for step and bones
                    for (var j = 0; j < boneIndexToPath.Count; ++j)
                    {
                        var bone = boneIndexToPath[j];
                        var transform = new Transformation()
                        {
                            Offset = ini.GetCustomType(section, $"step_{i}_bone_{j}_offset", Vector3.Zero),
                            Rotation = ini.GetCustomType(section, $"step_{i}_bone_{j}_rotation", Vector3.Zero),
                            Scale = ini.GetCustomType(section, $"step_{i}_bone_{j}_scale", Vector3.Zero)
                        };
                        step.SetTransform(bone, transform);
                    }

                    // set step
                    animationInstance.AddStep(step);
                }

                // store animation
                _animations[animation] = animationInstance;
            }
        }

        /// <summary>
        /// Add a bone.
        /// </summary>
        /// <param name="fullPath">Bone full path.</param>
        public void AddBone(string fullPath)
        {
            AddBone(fullPath, Vector3.Zero, Vector3.Zero, Vector3.One);
        }

        /// <summary>
        /// Add a bone.
        /// </summary>
        /// <param name="fullPath">Bone full path.</param>
        /// <param name="offset">Bone offset from parent bone.</param>
        public void AddBone(string fullPath, Vector3 offset)
        {
            AddBone(fullPath, offset, Vector3.Zero, Vector3.One);
        }

        /// <summary>
        /// Add a bone.
        /// </summary>
        /// <param name="fullPath">Bone full path.</param>
        /// <param name="offset">Bone offset from parent bone.</param>
        /// <param name="rotation">Bone rotation.</param>
        public void AddBone(string fullPath, Vector3 offset, Vector3 rotation)
        {
            AddBone(fullPath, offset, rotation, Vector3.One);
        }

        /// <summary>
        /// Add a bone.
        /// </summary>
        /// <param name="fullPath">Bone full path.</param>
        /// <param name="offset">Bone offset from parent bone.</param>
        /// <param name="rotation">Bone rotation.</param>
        /// <param name="scale">Bone scale.</param>
        public void AddBone(string fullPath, Vector3 offset, Vector3 rotation, Vector3 scale)
        {
            var parts = fullPath.Split('/');
            var name = parts.Last();
            var parent = parts.Length == 1 ? null : fullPath.Substring(0, fullPath.LastIndexOf('/'));
            AddBone(name, parent, offset, rotation, scale);
        }


        /// <summary>
        /// Add a bone.
        /// </summary>
        /// <param name="name">Bone name.</param>
        /// <param name="parent">Bone parent path.</param>
        public void AddBone(string name, string parent)
        {
            AddBone(name, parent, Vector3.Zero, Vector3.Zero, Vector3.One);
        }

        /// <summary>
        /// Add a bone.
        /// </summary>
        /// <param name="name">Bone name.</param>
        /// <param name="parent">Bone parent path.</param>
        /// <param name="offset">Bone offset from parent bone.</param>
        /// <param name="rotation">Bone rotation.</param>
        /// <param name="scale">Bone scale.</param>
        public void AddBone(string name, string parent, Vector3 offset, Vector3 rotation, Vector3 scale)
        {
            // add to flat names list
            var path = !string.IsNullOrEmpty(parent) ? string.Join('/', new string[] { parent, name }) : name;
            _previewMeshesData[path] = new BonePreviewMesh() { 
                Visible = false, 
                Transform = Transformation.Identity 
            };

            // set default transform
            SetTransform(path, null, 0, offset, rotation, scale);
        }

        /// <summary>
        /// Clone a bone.
        /// </summary>
        public void CloneBone(string boneToClone, string clonedName)
        {
            // rename original path to cloned path
            var parent = boneToClone.Substring(0, boneToClone.LastIndexOf('/'));
            var fromPath = boneToClone;
            var toPath = parent + '/' + clonedName;
            string ReplacePrefix(string path)
            {
                return toPath + path.Substring(fromPath.Length);
            }

            // clone bone parts
            var keys = _previewMeshesData.Keys.ToArray();
            foreach (var bone in keys)
            {
                if (bone.StartsWith(boneToClone))
                {
                    var newPath = ReplacePrefix(bone);
                    AddBone(newPath);
                    SetTransform(newPath, GetTransform(bone, null, 0));
                    if (_previewMeshesData[bone].Visible)
                    {
                        SetBonePreviewModel(newPath, _previewMeshesData[bone].Transform);
                    }
                }
            }
        }

        /// <summary>
        /// Rename a bone.
        /// </summary>
        public void RenameBone(string fromPath, string toPath)
        {
            // rename bones
            string ReplacePrefix(string path)
            {
                return toPath + path.Substring(fromPath.Length);
            }
            _previewMeshesData = _previewMeshesData.ToDictionary(pair => pair.Key.StartsWith(fromPath) ? ReplacePrefix(pair.Key) : pair.Key, pair => pair.Value);
            
            // rename in default pose
            _defaultPose.RenameBone(fromPath, toPath);

            // rename animation bones
            foreach (var animation in _animations.Values)
            {
                animation.RenameBone(fromPath, toPath);
            }
        }

        /// <summary>
        /// Return if a bone exists.
        /// </summary>
        public bool BoneExists(string name, string parent)
        {
            return _previewMeshesData.ContainsKey(parent + '/' + name);
        }

        /// <summary>
        /// Get a nested dictionary of bone names.
        /// </summary>
        Dictionary<string, object> GetNestedBoneKeysDictionary()
        {
            var ret = new Dictionary<string, object>();
            var keys = _previewMeshesData.Keys.OrderBy(x => x);
            foreach (var boneKey in keys)
            {
                var parts = boneKey.Split('/');
                var curr = ret;
                foreach (var part in parts)
                {
                    if (!curr.ContainsKey(part)) { curr[part] = new Dictionary<string, object>(); }
                    curr = curr[part] as Dictionary<string, object>;
                }
            }
            return ret;
        }

        /// <summary>
        /// Get items ordered by containers and items for display.
        /// </summary>
        public Tuple<string, string>[] GetFlatDisplayList()
        {
            // get bone names nested dict
            var _bonesNestedDict = GetNestedBoneKeysDictionary();

            // build sorted list
            List<string> ret = new List<string>();
            Stack<string> path = new Stack<string>();
            void WalkPart(Dictionary<string, object> curr)
            {
                foreach (var key in curr.Keys.OrderBy(x => x))
                {
                    path.Push(key);
                    var currPath = string.Join('/', path.Reverse());
                    ret.Add(currPath);
                    WalkPart(curr[key] as Dictionary<string, object>);
                    path.Pop();
                }
            }
            WalkPart(_bonesNestedDict);

            // convert to flat array with spaces for nesting
            return ret.Select(x => new Tuple<string, string>(new string(' ', x.Count(f => f == '/') * 2) + x.Split('/').Last(), x)).ToArray();
        }

        /// <summary>
        /// Debug draw bone parts.
        /// </summary>
        /// <param name="view">View matrix to draw with.</param>
        /// <param name="projection">Projection matrix to draw with.</param>
        /// <param name="world">World matrix to draw the bones at.</param>
        /// <param name="animation">Animation id or null to use default pose.</param>
        /// <param name="animationTime">Animation offset.</param>
        /// <param name="drawOutline">If true, will draw outlines.</param>
        /// <param name="selectedBone">Selected bone path.</param>
        /// <param name="enableLights">Enable lightings.</param>
        public void DebugDrawBones(Matrix view, Matrix projection, Matrix world, string animation, float animationTime, bool drawOutline, string selectedBone, bool enableLights = true)
        {
            if (_boneModel == null) { throw new Exception("Must call SetDebugModels() and provide debug models before attempting to draw bone parts!"); }

            var depthState = new DepthStencilState();
            depthState.DepthBufferEnable = true;
            depthState.DepthBufferWriteEnable = true;

            // draw bones
            foreach (var bone in _previewMeshesData)
            {
                if (!bone.Value.Visible) { continue; }
                var boneTransform = GetWorldMatrix(bone.Key, animation, animationTime);
                foreach (var mesh in _boneModel.Meshes)
                {
                    foreach (BasicEffect effect in mesh.Effects)
                    {
                        effect.EnableDefaultLighting();
                        effect.LightingEnabled = enableLights;
                        effect.DiffuseColor = Color.White.ToVector3();
                        effect.GraphicsDevice.DepthStencilState = depthState;
                        effect.Texture = _boneTexture;
                        effect.TextureEnabled = _boneTexture != null;
                        effect.GraphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;
                        effect.View = view;
                        effect.Projection = projection;
                        effect.World = bone.Value.Transform.ToMatrix() * boneTransform * world;
                    }
                    mesh.Draw();
                }
            }

            // draw outlines
            if (drawOutline)
            {
                foreach (var bone in _previewMeshesData)
                {
                    if (!bone.Value.Visible) { continue; }
                    var boneTransform = GetWorldMatrix(bone.Key, animation, animationTime);
                    foreach (var mesh in _boneModel.Meshes)
                    {
                        foreach (BasicEffect effect in mesh.Effects)
                        {
                            effect.LightingEnabled = false;
                            effect.DiffuseColor = bone.Key == selectedBone ? Color.Red.ToVector3() : Color.Black.ToVector3();
                            float outlineScale = bone.Key == selectedBone ? 0.075f : 0.05f;
                            effect.GraphicsDevice.DepthStencilState = depthState;
                            effect.GraphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;
                            effect.View = view;
                            effect.Projection = projection;
                            var meshTrans = bone.Value.Transform;
                            meshTrans.Scale = new Vector3(-(meshTrans.Scale.X + outlineScale), meshTrans.Scale.Y + outlineScale, meshTrans.Scale.Z + outlineScale);
                            effect.World = meshTrans.ToMatrix() * boneTransform * world;
                        }
                        mesh.Draw();
                    }
                }
            }
        }

        /// <summary>
        /// Delete a bone and all its children.
        /// </summary>
        public void Delete(string path)
        {
            // remove all bones that start with path
            _previewMeshesData = _previewMeshesData.Where(pair => !pair.Key.StartsWith(path)).ToDictionary(pair => pair.Key, pair => pair.Value);
        }

        /// <summary>
        /// Pick a bone from ray.
        /// </summary>
        public string PickBone(Ray ray, Matrix world, string animation, float animationTime, float handlesSize = 0.5f)
        {
            var distance = float.MaxValue;
            string selected = null;
            foreach (var bone in _previewMeshesData)
            {
                var boneTransform = GetWorldMatrix(bone.Key, animation, animationTime);
                var sphere = new BoundingSphere(Vector3.Zero, handlesSize);
                sphere = sphere.Transform(world * boneTransform);
                var intersect = ray.Intersects(sphere);
                if (intersect != null)
                {
                    if (intersect.Value < distance)
                    {
                        distance = intersect.Value;
                        selected = bone.Key;
                    }
                }
            }
            return selected;
        }

        /// <summary>
        /// Get world matrix for a given bone, animation, and animation offset.
        /// </summary>
        Matrix GetWorldMatrix(string bone, string animation, float offset)
        {
            // no animation? get default pose
            if (string.IsNullOrEmpty(animation))
            {
                return _defaultPose.GetBone(bone).WorldMatrix;
            }

            // no steps in animation? get default pose
            var animationInstance = BeginAnimation(animation);
            if (animationInstance.StepsCount == 0)
            {
                return _defaultPose.GetBone(bone).WorldMatrix;
            }
            // got valid animation? play it and get transformation
            else
            {
                animationInstance.Update(offset);
                return animationInstance.GetBoneTransform(bone);
            }
        }

        /// <summary>
        /// Debug draw bone handles.
        /// </summary>
        /// <param name="view">View matrix to draw with.</param>
        /// <param name="projection">Projection matrix to draw with.</param>
        /// <param name="world">World matrix to draw handles on.</param>
        /// <param name="animation">Animation id or null to use default pose.</param>
        /// <param name="animationTime">Animation offset.</param>
        /// <param name="scale">Handles scale.</param>
        /// <param name="color">Color to use for handles.</param>
        /// <param name="selectedColor">Color to use for the selected bone.</param>
        /// <param name="selectedBone">Path of the bone currently selected.</param>
        public void DebugDrawBoneHandles(Matrix view, Matrix projection, Matrix world, string animation, float animationTime, float scale, Color color, Color selectedColor, string selectedBone)
        {
            if (_handleModel == null) { throw new Exception("Must call SetDebugModels() and provide debug models before attempting to draw bone parts!"); }

            var depthState = new DepthStencilState();
            depthState.DepthBufferEnable = false; 
            depthState.DepthBufferWriteEnable = true; 

            foreach (var bone in _previewMeshesData)
            {
                var boneTransform = GetWorldMatrix(bone.Key, animation, animationTime);
                foreach (var mesh in _handleModel.Meshes)
                {
                    foreach (BasicEffect effect in mesh.Effects)
                    {
                        effect.LightingEnabled = false;
                        effect.DiffuseColor = bone.Key == selectedBone ? selectedColor.ToVector3() : color.ToVector3();
                        effect.GraphicsDevice.DepthStencilState = depthState;
                        effect.GraphicsDevice.RasterizerState = RasterizerState.CullNone;
                        effect.View = view;
                        effect.Projection = projection;
                        effect.World = Matrix.CreateScale(scale) * boneTransform;
                    }
                    mesh.Draw();
                }
            }
        }

        /// <summary>
        /// Get all transformations as a dictionary without animations.
        /// </summary>
        public ReadOnlyDictionary<string, Transformation> GetAllDefaultTransformations()
        {
            return _defaultPose.ReadOnlyTransformations;
        }
    }
}
