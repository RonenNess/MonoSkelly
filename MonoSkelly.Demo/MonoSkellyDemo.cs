using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;

namespace MonoSkelly.Demo
{
    /// <summary>
    /// A demo with a fighter that walks around with a sword and shield and can attack.
    /// </summary>
    public class MonoSkellyDemo : Game
    {
        // device and spritebatch
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        // camera
        Core.Camera _camera;

        // player skeleton, position and rotation
        Core.Skeleton _playerSkeleton;
        Vector3 _playerPosition;
        Quaternion _rotation = Quaternion.Identity;
        Quaternion _targetRotation = Quaternion.Identity;

        // properties to draw and animate skeleton for debug mode
        float _debugSkeletonanimationProgress;

        // current and previous animation
        string _currentAnimationName = "idle";
        string _previousAnimationName = "idle";

        /// <summary>
        /// What to render.
        /// </summary>
        enum DrawingMode
        {
            OnlyModel,
            OnlySkeleton,
            Both,
            _Count
        }
        DrawingMode _drawingMode = DrawingMode.OnlyModel;
        bool _shouldToggleDrawingMode;

        // font for help text
        SpriteFont _font;

        // animations for real player models
        Core.AnimationsBlender _animationsBlender;

        // level models
        Model _floor;

        // player parts
        Dictionary<string, Model> _playerParts = new Dictionary<string, Model>();

        // if true, it means we are locked on attacking animation and waiting for it to finish
        bool _lockedOnAttack;

        /// <summary>
        /// Create the demo app.
        /// </summary>
        public MonoSkellyDemo()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        /// <summary>
        /// Initialize demo.
        /// </summary>
        protected override void Initialize()
        {
            // set resolution
            _graphics.SynchronizeWithVerticalRetrace = true;
            _graphics.PreferredBackBufferWidth = _graphics.GraphicsDevice.DisplayMode.Width;
            _graphics.PreferredBackBufferHeight = _graphics.GraphicsDevice.DisplayMode.Height;
            _graphics.IsFullScreen = false;
            _graphics.ApplyChanges();

            // set window title and position
            Window.Title = "MonoSkelly - Demo";
            Window.IsBorderless = false;
            Window.AllowUserResizing = false;
            Window.Position = new Point(0, 0);

            // create camera
            _camera = new Core.Camera(_graphics);

            // init player skeleton and animation
            _playerSkeleton = new Core.Skeleton();
            _playerSkeleton.LoadFrom("Content/human.ini");
            var animation = _playerSkeleton.BeginAnimation("idle");
            _animationsBlender = new Core.AnimationsBlender(animation, animation);
            _animationsBlender.BlendFactor = 1f;

            // when attack animation finishes, unlock animations
            _animationsBlender.OnAnimationDone = (bool isSource, string name) =>
            {
                if (name == "attack") { _lockedOnAttack = false; }
            };

            base.Initialize();
        }

        /// <summary>
        /// Load content and models.
        /// </summary>
        protected override void LoadContent()
        {
            // create spritebatch
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            // set skeleton debug models
            Core.Skeleton.SetDebugModels(null, Content.Load<Model>("player/bone"), null);

            // set the texture of a model.
            void SetTexture(Model model, string path)
            {
                var effect = model.Meshes[0].Effects[0] as BasicEffect;
                effect.TextureEnabled = true;
                effect.Texture = Content.Load<Texture2D>(path);
            }

            // load font
            _font = Content.Load<SpriteFont>("font");

            // load ground model and texture
            _floor = Content.Load<Model>("environment/plane");
            SetTexture(_floor, "environment/grass");

            // method to load a player model
            void LoadPlayerPart(string modelName, string texturePath, Matrix transform)
            {
                var model = Content.Load<Model>("player/" + modelName);
                if (texturePath != null) { SetTexture(model, "player/" + texturePath); }
                model.Root.Transform = transform;
                _playerParts[modelName] = model;
            }

            // default transformations for player parts
            var rotationAndScaleFix = Matrix.CreateRotationX(-MathHelper.Pi / 2);

            // load player models
            LoadPlayerPart("head", "head_texture", rotationAndScaleFix);
            LoadPlayerPart("upper_torso", "body_texture", rotationAndScaleFix * Matrix.CreateTranslation(0f, 1.5f, 0f));
            LoadPlayerPart("lower_torso", "body_texture", rotationAndScaleFix);
            LoadPlayerPart("upper_arm_left", "body_texture", rotationAndScaleFix * Matrix.CreateTranslation(0.5f, -1.5f, 0f));
            LoadPlayerPart("upper_arm_right", "body_texture", rotationAndScaleFix * Matrix.CreateTranslation(-0.5f, -1.5f, 0f));
            LoadPlayerPart("lower_arm_left", "body_texture", rotationAndScaleFix * Matrix.CreateTranslation(0.65f, -1.85f, 0f));
            LoadPlayerPart("lower_arm_right", "body_texture", rotationAndScaleFix * Matrix.CreateTranslation(-0.65f, -1.85f, 0f));
            LoadPlayerPart("upper_leg_left", "body_texture", rotationAndScaleFix * Matrix.CreateTranslation(0f, -1.85f, 0f));
            LoadPlayerPart("upper_leg_right", "body_texture", rotationAndScaleFix * Matrix.CreateTranslation(0f, -1.85f, 0f));
            LoadPlayerPart("lower_leg_left", "body_texture", rotationAndScaleFix * Matrix.CreateTranslation(0f, -1.85f, 0f));
            LoadPlayerPart("lower_leg_right", "body_texture", rotationAndScaleFix * Matrix.CreateTranslation(0f, -1.85f, 0f));
            LoadPlayerPart("foot_left", "body_texture", rotationAndScaleFix * Matrix.CreateTranslation(0f, 0f, 1f));
            LoadPlayerPart("foot_right", "body_texture", rotationAndScaleFix * Matrix.CreateTranslation(0f, 0f, 1f));
            LoadPlayerPart("palm_left", "body_texture", rotationAndScaleFix * Matrix.CreateTranslation(0.25f, -0.75f, 0f));
            LoadPlayerPart("palm_right", "body_texture", rotationAndScaleFix * Matrix.CreateTranslation(-0.25f, -0.75f, 0f));
            LoadPlayerPart("crotch", "body_texture", rotationAndScaleFix * Matrix.CreateTranslation(0f, -0.5f, 0f));
            LoadPlayerPart("shield", "shield_texture", Matrix.CreateRotationX(MathHelper.Pi / 2) * rotationAndScaleFix * Matrix.CreateTranslation(1.5f, 0f, 0f));
            LoadPlayerPart("sword", null, Matrix.CreateRotationZ(MathHelper.Pi / 2) * Matrix.CreateRotationX(MathHelper.Pi / 2) * rotationAndScaleFix * Matrix.CreateTranslation(-0.5f, -0.75f, 5f));
        }

        /// <summary>
        /// Do per-frame updates.
        /// </summary>
        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            // movement vector
            Vector2 moveVector = Vector2.Zero;

            // move player controls
            if (Keyboard.GetState().IsKeyDown(Keys.Up))
            {
                moveVector.Y = 1;
            }
            else if (Keyboard.GetState().IsKeyDown(Keys.Down))
            {
                moveVector.Y = -1;
            }
            if (Keyboard.GetState().IsKeyDown(Keys.Left))
            {
                moveVector.X = 1;
            }
            else if (Keyboard.GetState().IsKeyDown(Keys.Right))
            {
                moveVector.X = -1;
            }

            // switch toggle modes
            if (Keyboard.GetState().IsKeyDown(Keys.F1))
            {
                _shouldToggleDrawingMode = true;
            }
            else if(_shouldToggleDrawingMode)
            {
                _shouldToggleDrawingMode = false;
                _drawingMode = (DrawingMode)((int)_drawingMode + 1);
                if (_drawingMode == DrawingMode._Count) _drawingMode = DrawingMode.OnlyModel;
            }

            // are we attacking
            bool isAttacking = Keyboard.GetState().IsKeyDown(Keys.Space);

            // do player movements
            if (!isAttacking && !_lockedOnAttack)
            {
                float moveSpeed = 50f * (float)gameTime.ElapsedGameTime.TotalSeconds;
                _playerPosition.X += moveVector.X * moveSpeed;
                _playerPosition.Z += moveVector.Y * moveSpeed;
            }

            // update camera
            _camera.LookAt = _playerPosition;
            _camera.Position = _playerPosition + new Vector3(0, 65, -55);
            _camera.Update();

            // attacking
            if (isAttacking || _lockedOnAttack)
            {
                _currentAnimationName = "attack";
            }
            // walking animation
            else if (moveVector != Vector2.Zero)
            {
                _currentAnimationName = "walk";
                _targetRotation = Quaternion.CreateFromAxisAngle(Vector3.Up, (float)System.Math.Atan2(moveVector.X, moveVector.Y));
            }
            // standing
            else
            {
                _currentAnimationName = "idle";
            }

            // lerp face direction 
            _rotation = Quaternion.Slerp(_rotation, _targetRotation, (float)gameTime.ElapsedGameTime.TotalSeconds * 10f);

            // update skeleton animation
            _animationsBlender.Update((float)gameTime.ElapsedGameTime.TotalSeconds);
            _animationsBlender.BlendFactor += (float)gameTime.ElapsedGameTime.TotalSeconds * 5f;

            // update animation when changed
            if (!_lockedOnAttack && (_currentAnimationName != _previousAnimationName))
            {
                // reset debug skeleton animation
                _debugSkeletonanimationProgress = 0f;

                // set new active animation for actual drawing
                _animationsBlender.Switch();
                _animationsBlender.BlendTo = _playerSkeleton.BeginAnimation(_currentAnimationName);

                // set previous animation
                _previousAnimationName = _currentAnimationName;

                // lock on attack
                if (_currentAnimationName == "attack") { _lockedOnAttack = true; }
            }
            else
            {
                _currentAnimationName = _previousAnimationName;
            }

            base.Update(gameTime);
        }

        /// <summary>
        /// Render scene.
        /// </summary>
        protected override void Draw(GameTime gameTime)
        {
            // begin frame and reset states
            GraphicsDevice.Clear(Color.CornflowerBlue);
            GraphicsDevice.SamplerStates[0] = SamplerState.PointClamp;
            GraphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;
            GraphicsDevice.DepthStencilState = DepthStencilState.Default;

            // draw floor
            for (var i = -5; i <= 5; ++i)
            {
                for (var j = -5; j <= 5; ++j)
                {
                    _floor.Draw(Matrix.CreateScale(0.2f, 0.2f, 0.2f) * Matrix.CreateTranslation(i * 40f, 0f, j * 40f), _camera.View, _camera.Projection);
                }
            }

            // calculate player world matrix
            var playerWorldMatrix = Matrix.CreateFromQuaternion(_rotation) * Matrix.CreateTranslation(_playerPosition);

            // draw debug player skeleton
            if (_drawingMode == DrawingMode.OnlySkeleton || _drawingMode == DrawingMode.Both)
            {
                _playerSkeleton.DebugDrawBones(_camera.View, _camera.Projection, playerWorldMatrix, _currentAnimationName, _debugSkeletonanimationProgress, true, null);
            }
            _debugSkeletonanimationProgress += (float)gameTime.ElapsedGameTime.TotalSeconds;

            // draw actual models
            if (_drawingMode == DrawingMode.OnlyModel || _drawingMode == DrawingMode.Both)
            {
                foreach (var part in _playerParts)
                {
                    var bone = _animationsBlender.GetBoneTransform(part.Key);
                    part.Value.Draw(bone * playerWorldMatrix, _camera.View, _camera.Projection);
                }
            }

            // draw instructions
            _spriteBatch.Begin();
            var text = "Use Arrows to move, Space to attack, and F1 to toggle drawing mode (model / skeleton / both).\nNote: skeleton render does not blend animations.";
            _spriteBatch.DrawString(_font, text, new Vector2(3, 3), Color.Black);
            _spriteBatch.DrawString(_font, text, new Vector2(5, 5), Color.White);
            _spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
