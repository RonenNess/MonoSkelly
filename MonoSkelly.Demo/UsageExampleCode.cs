using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Text;

namespace MonoSkelly.Demo
{
    /// <summary>
    /// This class is the tutorial code from the README.md file of the repository.
    /// </summary>
    internal class UsageExampleCode : Game
    {
        private GraphicsDeviceManager _graphics;

        // camera for view & projection metrices
        MonoSkelly.Core.Camera _camera;

        // player models (body parts, key = bone alias)
        Dictionary<string, Model> _playerParts;

        // player skeleton and active animation
        MonoSkelly.Core.Skeleton _playerSkeleton;
        MonoSkelly.Core.AnimationState _animation;

        public UsageExampleCode()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            _camera = new Core.Camera(_graphics);
            base.Initialize();
        }

        protected override void LoadContent()
        {
            // load player models
            _playerParts = new Dictionary<string, Model>();
            _playerParts["head"] = Content.Load<Model>("tutorial/head");
            _playerParts["body"] = Content.Load<Model>("tutorial/body");
            _playerParts["arm_left"] = Content.Load<Model>("tutorial/arm");
            _playerParts["arm_right"] = Content.Load<Model>("tutorial/arm");
            _playerParts["leg_left"] = Content.Load<Model>("tutorial/leg");
            _playerParts["leg_right"] = Content.Load<Model>("tutorial/leg");

            // fix models base transform (messed up in export)
            foreach (var model in _playerParts)
            {
                model.Value.Root.Transform = Matrix.CreateRotationY(MathHelper.Pi / 2);
            }

            // load skeleton and init animation
            _playerSkeleton = new MonoSkelly.Core.Skeleton();
            _playerSkeleton.LoadFrom("Content/tutorial/robot.ini");
            _animation = _playerSkeleton.BeginAnimation("idle");
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            // update camera
            _camera.LookAt = new Vector3(0, 15, 0);
            _camera.Position = new Vector3(0, 45, -45);
            _camera.Update();

            // advance animation by 'deltaTime' (usually can be gameTime.ElapsedGameTime.TotalSeconds)
            _animation.Update((float)gameTime.ElapsedGameTime.TotalSeconds);

            // player world model (you can translate this to move player around)
            var playerWorldMatrix = Matrix.Identity;

            // now draw the player models
            foreach (var part in _playerParts)
            {
                var bone = _animation.GetBoneTransform(part.Key);
                part.Value.Draw(bone * playerWorldMatrix, _camera.View, _camera.Projection);
            }

            base.Draw(gameTime);
        }
    }
}
