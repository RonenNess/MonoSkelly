/**
 * MonoSkelly Camera.
 * Ronen Ness 2021
 */
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace MonoSkelly.Core
{
    /// <summary>
    /// A 3d camera object.
    /// </summary>
    public class Camera
    {
        /// <summary>
        /// Default field of view.
        /// </summary>
        public static readonly float DefaultFieldOfView = MathHelper.PiOver4;

        // projection params
        float _fieldOfView = MathHelper.PiOver4;
        float _nearClipPlane = 1.0f;
        float _farClipPlane = 950.0f;
        float _aspectRatio = 1.0f;

        // current camera type
        CameraType _cameraType = CameraType.Perspective;

        // camera screen size
        Point? _altScreenSize = null;

        /// <summary>
        /// If defined, this will be used as screen size (affect aspect ratio in perspective camera,
        /// and view size in Orthographic camera). If not set, the actual screen resolution will be used.
        /// </summary>
        public Point? ForceScreenSize
        {
            get { return _altScreenSize; }
            set { _altScreenSize = value; }
        }

        /// <summary>
        /// Set / get camera type.
        /// </summary>
        public CameraType CameraType
        {
            set { _cameraType = value; _needUpdateProjection = true; }
            get { return _cameraType; }
        }

        /// <summary>
        /// Set / Get camera field of view.
        /// </summary>
        public float FieldOfView
        {
            get { return _fieldOfView; }
            set { _fieldOfView = value; _needUpdateProjection = true; }
        }

        /// <summary>
        /// Set / Get camera near clip plane.
        /// </summary>
        public float NearClipPlane
        {
            get { return _nearClipPlane; }
            set { _nearClipPlane = value; _needUpdateProjection = true; }
        }

        /// <summary>
        /// Set / Get camera far clip plane.
        /// </summary>
        public float FarClipPlane
        {
            get { return _farClipPlane; }
            set { _farClipPlane = value; _needUpdateProjection = true; }
        }

        /// <summary>
        /// If defined, camera will always look at this target.
        /// </summary>
        public Vector3? LookAt;

        // true if we need to update projection matrix next time we try to get it
        private bool _needUpdateProjection = true;

        /// <summary>
        /// Get camera position.
        /// </summary>
        public Vector3 Position;

        /// <summary>
        /// Up vector.
        /// </summary>
        public Vector3 UpVector = Vector3.Up;

        /// <summary>
        /// Camera rotation (X = horizontal, y = vertical).
        /// Only applied when LookAt is null.
        /// </summary>
        public Vector2 Rotation;

        // graphic device manager
        GraphicsDeviceManager _deviceManager;

        /// <summary>
        /// Create a new camera instance
        /// </summary>
        public Camera(GraphicsDeviceManager deviceManager)
        {
            _deviceManager = deviceManager;
        }

        /// <summary>
        /// Get the current camera projection matrix.
        /// </summary>
        public Matrix Projection { get; private set; }

        /// <summary>
        /// Get the current camera view matrix.
        /// </summary>
        public Matrix View { get; private set; }

        /// <summary>
        /// Get the current camera view-projection matrix.
        /// </summary>
        public Matrix ViewProjection { get; private set; }

        /// <summary>
        /// Get camera forward vector.
        /// </summary>
        public Vector3 ForwardVector { get; private set; }

        /// <summary>
        /// Get camera left side vector.
        /// </summary>
        public Vector3 LeftVector { get; private set; }

        /// <summary>
        /// Get camera right side vector.
        /// </summary>
        public Vector3 RightVector { get; private set; }

        /// <summary>
        /// Get camera backward vector.
        /// </summary>
        public Vector3 BackwardVector => -ForwardVector;

        /// <summary>
        /// Camera rotation around Y axis in degrees.
        /// </summary>
        public int FaceDirectionDegree { get; private set; }

        /// <summary>
        /// Get camera bounding frustum.
        /// </summary>
        public BoundingFrustum ViewFrustum { get; private set; }

        /// <summary>
        /// Update camera's matrices.
        /// Call this every frame before using camera.
        /// </summary>
        public void Update()
        {
            // use lookat
            if (LookAt.HasValue)
            {
                View = Matrix.CreateLookAt(Position, LookAt.Value, UpVector);
                var forward = LookAt.Value - Position;
                forward.Normalize();
                ForwardVector = forward;
            }
            // use rotation
            else
            {
                var pos = Position;
                var rotationMatrix = Matrix.CreateRotationY(Rotation.X) * Matrix.CreateRotationX(Rotation.Y);
                View = Matrix.CreateTranslation(-pos) * rotationMatrix;
                var forward = Matrix.Invert(rotationMatrix).Forward;
                forward.Normalize();
                ForwardVector = forward;
            }

            // calc left and right vectors
            LeftVector = Vector3.Transform(ForwardVector, Matrix.CreateRotationY((float)Math.PI / 2f));
            RightVector = Vector3.Transform(ForwardVector, Matrix.CreateRotationY((float)-Math.PI / 2f));

            // calculate face direction
            var temp = new Vector2(ForwardVector.X, ForwardVector.Z);
            temp.Normalize();
            FaceDirectionDegree = (int)-(MathF.Round(MathF.Atan2(temp.Y, -temp.X) * (180 / MathF.PI)));
            while (FaceDirectionDegree < 0) FaceDirectionDegree += 360;

            // calc view-projection matrix
            ViewProjection = View * Projection;

            // camera view frustum
            ViewFrustum = new BoundingFrustum(ViewProjection);

            // update projection
            UpdateProjectionIfNeeded();
        }

        /// <summary>
        /// Update projection matrix after changes.
        /// </summary>
        private void UpdateProjectionIfNeeded()
        {
            // if don't need update, skip
            if (!_needUpdateProjection)
            {
                return;
            }

            // screen width and height
            float width; float height;

            // if we have alternative screen size defined, use it
            if (ForceScreenSize != null)
            {
                width = ForceScreenSize.Value.X;
                height = ForceScreenSize.Value.Y;
            }
            // if we don't have alternative screen size defined, get current backbuffer size
            else
            {
                var deviceManager = _deviceManager;
                width = deviceManager.PreferredBackBufferWidth;
                height = deviceManager.PreferredBackBufferHeight;
            }

            // calc aspect ratio
            _aspectRatio = width / height;

            // create view and projection matrix
            switch (_cameraType)
            {
                case CameraType.Perspective:
                    Projection = Matrix.CreatePerspectiveFieldOfView(_fieldOfView, _aspectRatio, _nearClipPlane, _farClipPlane);
                    break;

                case CameraType.Orthographic:
                    Projection = Matrix.CreateOrthographic(width, height, _nearClipPlane, _farClipPlane);
                    break;
            }

            // no longer need projection update
            _needUpdateProjection = false;
        }

        /// <summary>
        /// Return a ray starting from the camera and pointing directly at mouse position (translated to 3d space).
        /// This is a helper function that help to get ray collision based on camera and mouse.
        /// </summary>
        /// <returns>Ray from camera to mouse.</returns>
        public Ray RayFromMouse()
        {
            MouseState mouseState = Mouse.GetState();
            return RayFrom2dPoint(new Vector2(mouseState.X, mouseState.Y));
        }

        /// <summary>
        /// Return a ray starting from the camera and pointing directly at a 3d position.
        /// </summary>
        /// <param name="point">Point to send ray to.</param>
        /// <returns>Ray from camera to given position.</returns>
        public Ray RayFrom3dPoint(Vector3 point)
        {
            return new Ray(Position, point - Position);
        }

        /// <summary>
        /// Return a ray starting from the camera and pointing directly at a 2d position translated to 3d space.
        /// This is a helper function that help to get ray collision based on camera and position on screen.
        /// </summary>
        /// <param name="point">Point to send ray to.</param>
        /// <returns>Ray from camera to given position.</returns>
        public Ray RayFrom2dPoint(Vector2 point)
        {
            // get graphic device
            GraphicsDevice device = _deviceManager.GraphicsDevice;

            // convert point to near and far points as 3d vectors
            Vector3 nearsource = new Vector3(point.X, point.Y, 0f);
            Vector3 farsource = new Vector3(point.X, point.Y, 1f);

            // create empty world matrix
            Matrix world = Matrix.CreateTranslation(0, 0, 0);

            // convert near point to world space
            Vector3 nearPoint = device.Viewport.Unproject(nearsource,
                Projection, View, world);

            // convert far point to world space
            Vector3 farPoint = device.Viewport.Unproject(farsource,
                Projection, View, world);
            
            // get direction
            Vector3 dir = farPoint - nearPoint;
            dir.Normalize();

            // return ray
            return new Ray(nearPoint, dir);
        }
    }

    /// <summary>
    /// Camera types.
    /// </summary>
    public enum CameraType
    {
        /// <summary>
        /// Perspective camera.
        /// </summary>
        Perspective,

        /// <summary>
        /// Orthographic camera.
        /// </summary>
        Orthographic,
    };
}
