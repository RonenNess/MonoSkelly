using GeonBit.UI;
using GeonBit.UI.Entities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoSkelly.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MonoSkelly.Editor
{
    /// <summary>
    /// MonoSkelly editor software.
    /// </summary>
    public class SkellyEditor : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        // currently presented skeleton and camera
        Core.Skeleton _skeleton;
        Camera _camera;

        // camera settings
        float _cameraDistance;
        Vector2 _cameraAngle;
        float _cameraOffsetY;

        // input helpers
        int _prevMouseWheel;
        Vector2 _prevMousePos;

        // should we debug draw bones and handles?
        bool _showBones = true;
        bool _showBoneHandles = true;
        bool _showBonesOutline = true;
        bool _showLights = true;

        // bones panel UI entities
        Panel _bonesPanel;
        SelectList _bonesList;
        Header _selectedBoneDisplay;
        TextInput _boneRotateX;
        TextInput _boneRotateY;
        TextInput _boneRotateZ;
        TextInput _boneOffsetX;
        TextInput _boneOffsetY;
        TextInput _boneOffsetZ;
        TextInput _boneScaleX;
        TextInput _boneScaleY;
        TextInput _boneScaleZ;
        TextInput _boneMeshOffsetX;
        TextInput _boneMeshOffsetY;
        TextInput _boneMeshOffsetZ;
        TextInput _boneMeshScaleX;
        TextInput _boneMeshScaleY;
        TextInput _boneMeshScaleZ;
        CheckBox _boneMeshVisible;
        TextInput _boneAlias;

        // animations panel UI entities
        Panel _animationsPanel;
        DropDown _animationSelection;
        DropDown _animationStepSelection;
        CheckBox _animationRepeats;
        TimelineElement _animationTimeline;
        bool _playAnimation;
        RichParagraph _animationTimelineCaption;
        Panel _animationStepPropsPanel;
        TextInput _animationStepDuration;
        TextInput _animationStepNameInput;
        DropDown _animationStepRotateInterpolation;
        DropDown _animationStepMoveAndScaleInterpolation;
        Image _animationPlayBtn;

        // convert bone full path to index in bones list.
        Dictionary<string, int> _fullPathToBoneListIndex;

        // currently selected bone
        string _selectedBone;

        // field to change while dragging mouse
        TextInput _draggedInput;
        float _draggedTime;

        // floor model
        Model _floorModel;
        Texture2D _floorTexture;

        // saved files folder
        static string _savesFolder = "saves";

        // currently loaded file name
        string _currentFilename;

        // name to use for default pose without animation
        static string DefaultNoneAnimationName = "Default Pose";

        /// <summary>
        /// Create editor.
        /// </summary>
        public SkellyEditor()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = false;
        }

        /// <summary>
        /// Initialize editor.
        /// </summary>
        protected override void Initialize()
        {
            // set resolution and fullscreen
            var windowBarHeight = 30;
            _graphics.SynchronizeWithVerticalRetrace = true;
            _graphics.PreferredBackBufferWidth = _graphics.GraphicsDevice.DisplayMode.Width;
            _graphics.PreferredBackBufferHeight = _graphics.GraphicsDevice.DisplayMode.Height - windowBarHeight;
            _graphics.IsFullScreen = false;
            _graphics.ApplyChanges();

            // set window stuff
            Window.Title = "MonoSkelly - Editor";
            Window.IsBorderless = false;
            Window.AllowUserResizing = false;
            Window.Position = new Point(0, windowBarHeight);
    
            // init ui
            InitUI();

            // create skeleton and camera
            _skeleton = new Skeleton();
            _camera = new Camera(_graphics);
            ResetCamera();

            // make sure saves folder exist
            if (!System.IO.Directory.Exists(_savesFolder)) { System.IO.Directory.CreateDirectory(_savesFolder); }

            // call base init
            base.Initialize();
        }

        /// <summary>
        /// Initialize UI elements.
        /// </summary>
        void InitUI()
        {
            // init geonbit ui
            UserInterface.Initialize(Content, BuiltinThemes.editor);
            UserInterface.Active.GlobalScale = 0.85f;
            UserInterface.Active.ShowCursor = true;
            IsMouseVisible = false;

            // add main top menu
            var mainMenuLayout = new GeonBit.UI.Utils.MenuBar.MenuLayout();

            // file menu
            mainMenuLayout.AddMenu("File", 220);
            mainMenuLayout.AddItemToMenu("File", "New Empty", PressedTopMenu_New);
            mainMenuLayout.AddItemToMenu("File", "Save", PressedTopMenu_Save);
            mainMenuLayout.AddItemToMenu("File", "Save As", PressedTopMenu_SaveAs);
            mainMenuLayout.AddItemToMenu("File", "Load", PressedTopMenu_Load);
            mainMenuLayout.AddItemToMenu("File", "Exit", PressedTopMenu_Exit);

            // display menu
            mainMenuLayout.AddMenu("Display", 280);
            mainMenuLayout.AddItemToMenu("Display", "{{L_GREEN}}Show Handles", PressedTopMenu_ToggleShowHandlers);
            mainMenuLayout.AddItemToMenu("Display", "{{L_GREEN}}Show Bones", PressedTopMenu_ToggleShowBones);
            mainMenuLayout.AddItemToMenu("Display", "{{L_GREEN}}Bones Outline", PressedTopMenu_ToggleShowOutline);
            mainMenuLayout.AddItemToMenu("Display", "{{L_GREEN}}Enable Lighting", PressedTopMenu_ToggleLighting);
            mainMenuLayout.AddItemToMenu("Display", "Reset Camera", ResetCamera);

            // help menu
            mainMenuLayout.AddMenu("Help", 270);
            mainMenuLayout.AddItemToMenu("Help", "View Help", PressedTopMenu_ShowHelp);
            mainMenuLayout.AddItemToMenu("Help", "About MonoSkelly", PressedTopMenu_ShowAbout);

            // create menus
            var mainMenuEntity = GeonBit.UI.Utils.MenuBar.Create(mainMenuLayout);
            mainMenuEntity.PriorityBonus = 1000000;
            UserInterface.Active.AddEntity(mainMenuEntity);

            // create bones panel
            _bonesPanel = new Panel(new Vector2(440, 1050));
            _bonesPanel.Anchor = Anchor.TopLeft;
            _bonesPanel.Draggable = true;
            _bonesPanel.AddChild(new Header("Bones & Transformations"));
            _bonesPanel.AddChild(new HorizontalLine());
            UserInterface.Active.AddEntity(_bonesPanel);

            // list to show bones
            _bonesPanel.AddChild(new Paragraph("Selected Bone:"));
            _bonesList = new SelectList();
            _bonesPanel.AddChild(_bonesList);

            // add bone button
            var newBoneBtn = new Button("New Bone", ButtonSkin.Default, Anchor.AutoInline, new Vector2(0.445f, -1f));
            newBoneBtn.OnClick = CreateBoneBtn;
            newBoneBtn.ToolTipText = "Create a new bone under selected bone. This will add the new bone to all animations.";
            _bonesPanel.AddChild(newBoneBtn);

            // delete bone button
            var deleteBoneBtn = new Button("Delete Bone", ButtonSkin.Default, Anchor.AutoInline, new Vector2(0.555f, -1f));
            deleteBoneBtn.OnClick = DeleteSelectedBoneBtn;
            deleteBoneBtn.ToolTipText = "Delete selected bone and everything under it. This will remove bone from all animations.";
            _bonesPanel.AddChild(deleteBoneBtn);

            // rename bone button
            var renameBoneBtn = new Button("Rename", ButtonSkin.Default, Anchor.AutoInline, new Vector2(0.6f, -1f));
            renameBoneBtn.OnClick = RenameBoneBtn;
            renameBoneBtn.ToolTipText = "Rename selected bone. This will rename the bone and its children under all animations.";
            _bonesPanel.AddChild(renameBoneBtn);

            // clone bone button
            var cloneBoneBtn = new Button("Clone", ButtonSkin.Default, Anchor.AutoInline, new Vector2(0.4f, -1f));
            cloneBoneBtn.OnClick = CloneSelectedBoneBtn;
            cloneBoneBtn.ToolTipText = "Clone selected bone with all its children. This will add the cloned bones to all animations, but won't clone the per-animation transformations.";
            _bonesPanel.AddChild(cloneBoneBtn);

            // bone alias input
            _bonesPanel.AddChild(new Paragraph("Alias:", Anchor.AutoInline, new Vector2(0.3f, -1f)));
            _boneAlias = new TextInput(false, new Vector2(0.7f, 40), Anchor.AutoInline);
            _boneAlias.Value = "";
            _boneAlias.OnValueChange = BoneAliasChanged;
            _boneAlias.ToolTipText = "Attach an alias to this bone. You can later query this bone transformations using the alias instead of its full path.";
            _boneAlias.PlaceholderText = "";
            _bonesPanel.AddChild(_boneAlias);

            _bonesPanel.AddChild(new HorizontalLine());

            // show selected bone
            _selectedBoneDisplay = new Header("[Nothing Selected]", Anchor.BottomCenter);
            _selectedBoneDisplay.AlignToCenter = true;
            _selectedBoneDisplay.Size = new Vector2(0, -1);
            UserInterface.Active.Root.AddChild(_selectedBoneDisplay);

            // rotation input
            _bonesPanel.AddChild(new Paragraph("Bone Rotation:"));
            float transInputHeight = 42;

            _boneRotateX = new TextInput(false, new Vector2(0.33f, transInputHeight), Anchor.AutoInline);
            _boneRotateX.Value = "0";
            _boneRotateX.Validators.Add(new GeonBit.UI.Entities.TextValidators.NumbersOnly(true));
            _boneRotateX.OnValueChange = UpdateBoneTransform;
            _boneRotateX.WhileMouseDown = StartDraggingValue;
            _bonesPanel.AddChild(_boneRotateX);

            _boneRotateY = new TextInput(false, new Vector2(0.33f, transInputHeight), Anchor.AutoInline);
            _boneRotateY.Value = "0";
            _boneRotateY.Validators.Add(new GeonBit.UI.Entities.TextValidators.NumbersOnly(true));
            _boneRotateY.OnValueChange = UpdateBoneTransform;
            _boneRotateY.WhileMouseDown = StartDraggingValue;
            _bonesPanel.AddChild(_boneRotateY);

            _boneRotateZ = new TextInput(false, new Vector2(0.33f, transInputHeight), Anchor.AutoInline);
            _boneRotateZ.Value = "0";
            _boneRotateZ.Validators.Add(new GeonBit.UI.Entities.TextValidators.NumbersOnly(true));
            _boneRotateZ.OnValueChange = UpdateBoneTransform;
            _boneRotateZ.WhileMouseDown = StartDraggingValue;
            _bonesPanel.AddChild(_boneRotateZ);

            // offset input
            _bonesPanel.AddChild(new Paragraph("Bone Offset:"));

            _boneOffsetX = new TextInput(false, new Vector2(0.33f, transInputHeight), Anchor.AutoInline);
            _boneOffsetX.Value = "0";
            _boneOffsetX.Validators.Add(new GeonBit.UI.Entities.TextValidators.NumbersOnly(true));
            _boneOffsetX.OnValueChange = UpdateBoneTransform;
            _boneOffsetX.WhileMouseDown = StartDraggingValue;
            _bonesPanel.AddChild(_boneOffsetX);

            _boneOffsetY = new TextInput(false, new Vector2(0.33f, transInputHeight), Anchor.AutoInline);
            _boneOffsetY.Value = "0";
            _boneOffsetY.Validators.Add(new GeonBit.UI.Entities.TextValidators.NumbersOnly(true));
            _boneOffsetY.OnValueChange = UpdateBoneTransform;
            _boneOffsetY.WhileMouseDown = StartDraggingValue;
            _bonesPanel.AddChild(_boneOffsetY);

            _boneOffsetZ = new TextInput(false, new Vector2(0.33f, transInputHeight), Anchor.AutoInline);
            _boneOffsetZ.Value = "0";
            _boneOffsetZ.Validators.Add(new GeonBit.UI.Entities.TextValidators.NumbersOnly(true));
            _boneOffsetZ.OnValueChange = UpdateBoneTransform;
            _boneOffsetZ.WhileMouseDown = StartDraggingValue;
            _bonesPanel.AddChild(_boneOffsetZ);

            // scale input
            _bonesPanel.AddChild(new Paragraph("Bone Scale:"));

            _boneScaleX = new TextInput(false, new Vector2(0.33f, transInputHeight), Anchor.AutoInline);
            _boneScaleX.Value = "1";
            _boneScaleX.Validators.Add(new GeonBit.UI.Entities.TextValidators.NumbersOnly(true));
            _boneScaleX.OnValueChange = UpdateBoneTransform;
            _boneScaleX.WhileMouseDown = StartDraggingValue;
            _bonesPanel.AddChild(_boneScaleX);

            _boneScaleY = new TextInput(false, new Vector2(0.33f, transInputHeight), Anchor.AutoInline);
            _boneScaleY.Value = "1";
            _boneScaleY.Validators.Add(new GeonBit.UI.Entities.TextValidators.NumbersOnly(true));
            _boneScaleY.OnValueChange = UpdateBoneTransform;
            _boneScaleY.WhileMouseDown = StartDraggingValue;
            _bonesPanel.AddChild(_boneScaleY);

            _boneScaleZ = new TextInput(false, new Vector2(0.33f, transInputHeight), Anchor.AutoInline);
            _boneScaleZ.Value = "1";
            _boneScaleZ.Validators.Add(new GeonBit.UI.Entities.TextValidators.NumbersOnly(true));
            _boneScaleZ.OnValueChange = UpdateBoneTransform;
            _boneScaleZ.WhileMouseDown = StartDraggingValue;
            _bonesPanel.AddChild(_boneScaleZ);

            _bonesPanel.AddChild(new Header("Debug Bone Rendering"));
            _bonesPanel.AddChild(new HorizontalLine());

            // set bone display
            var setBoneDisplay = new CheckBox("Render Bone In Editor");
            setBoneDisplay.OnValueChange = UpdateBoneMeshTransform;
            _bonesPanel.AddChild(setBoneDisplay);
            _boneMeshVisible = setBoneDisplay;

            // offset input
            _bonesPanel.AddChild(new Paragraph("Mesh Offset:"));

            _boneMeshOffsetX = new TextInput(false, new Vector2(0.33f, transInputHeight), Anchor.AutoInline);
            _boneMeshOffsetX.Value = "0";
            _boneMeshOffsetX.Validators.Add(new GeonBit.UI.Entities.TextValidators.NumbersOnly(true));
            _boneMeshOffsetX.OnValueChange = UpdateBoneMeshTransform;
            _boneMeshOffsetX.WhileMouseDown = StartDraggingValue;
            _bonesPanel.AddChild(_boneMeshOffsetX);

            _boneMeshOffsetY = new TextInput(false, new Vector2(0.33f, transInputHeight), Anchor.AutoInline);
            _boneMeshOffsetY.Value = "0";
            _boneMeshOffsetY.Validators.Add(new GeonBit.UI.Entities.TextValidators.NumbersOnly(true));
            _boneMeshOffsetY.OnValueChange = UpdateBoneMeshTransform;
            _boneMeshOffsetY.WhileMouseDown = StartDraggingValue;
            _bonesPanel.AddChild(_boneMeshOffsetY);

            _boneMeshOffsetZ = new TextInput(false, new Vector2(0.33f, transInputHeight), Anchor.AutoInline);
            _boneMeshOffsetZ.Value = "0";
            _boneMeshOffsetZ.Validators.Add(new GeonBit.UI.Entities.TextValidators.NumbersOnly(true));
            _boneMeshOffsetZ.OnValueChange = UpdateBoneMeshTransform;
            _boneMeshOffsetZ.WhileMouseDown = StartDraggingValue;
            _bonesPanel.AddChild(_boneMeshOffsetZ);

            // scale input
            _bonesPanel.AddChild(new Paragraph("Mesh Scale:"));

            _boneMeshScaleX = new TextInput(false, new Vector2(0.33f, transInputHeight), Anchor.AutoInline);
            _boneMeshScaleX.Value = "1";
            _boneMeshScaleX.Validators.Add(new GeonBit.UI.Entities.TextValidators.NumbersOnly(true));
            _boneMeshScaleX.OnValueChange = UpdateBoneMeshTransform;
            _boneMeshScaleX.WhileMouseDown = StartDraggingValue;
            _bonesPanel.AddChild(_boneMeshScaleX);

            _boneMeshScaleY = new TextInput(false, new Vector2(0.33f, transInputHeight), Anchor.AutoInline);
            _boneMeshScaleY.Value = "1";
            _boneMeshScaleY.Validators.Add(new GeonBit.UI.Entities.TextValidators.NumbersOnly(true));
            _boneMeshScaleY.OnValueChange = UpdateBoneMeshTransform;
            _boneMeshScaleY.WhileMouseDown = StartDraggingValue;
            _bonesPanel.AddChild(_boneMeshScaleY);

            _boneMeshScaleZ = new TextInput(false, new Vector2(0.33f, transInputHeight), Anchor.AutoInline);
            _boneMeshScaleZ.Value = "1";
            _boneMeshScaleZ.Validators.Add(new GeonBit.UI.Entities.TextValidators.NumbersOnly(true));
            _boneMeshScaleZ.OnValueChange = UpdateBoneMeshTransform;
            _boneMeshScaleZ.WhileMouseDown = StartDraggingValue;
            _bonesPanel.AddChild(_boneMeshScaleZ);

            // create animations panel
            _animationsPanel = new Panel(new Vector2(440, 925));
            _animationsPanel.Anchor = Anchor.TopRight;
            _animationsPanel.Draggable = true;
            _animationsPanel.AddChild(new Header("Animations"));
            _animationsPanel.AddChild(new HorizontalLine());
            UserInterface.Active.AddEntity(_animationsPanel);

            // animation selection
            _animationsPanel.AddChild(new Paragraph("Select Animation To Edit:"));
            _animationSelection = new DropDown();
            _animationSelection.AddItem(DefaultNoneAnimationName);
            _animationsPanel.AddChild(_animationSelection);

            // add animation button
            var newAnimationBtn = new Button("New", ButtonSkin.Default, Anchor.AutoInline, new Vector2(0.27f, -1f));
            newAnimationBtn.OnClick = CreateAnimationBtn;
            newAnimationBtn.ToolTipText = "Create a new animation sequence.";
            _animationsPanel.AddChild(newAnimationBtn);
            
            // clone animation button
            var cloneAnimationBtn = new Button("Clone", ButtonSkin.Default, Anchor.AutoInline, new Vector2(0.34f, -1f));
            cloneAnimationBtn.OnClick = CloneAnimationBtn;
            cloneAnimationBtn.ToolTipText = "Clone selected animation.";
            _animationsPanel.AddChild(cloneAnimationBtn);

            // delete animation button
            var deleteAnimationBtn = new Button("Delete", ButtonSkin.Default, Anchor.AutoInline, new Vector2(0.39f, -1f));
            deleteAnimationBtn.OnClick = DeleteAnimationBtn;
            deleteAnimationBtn.ToolTipText = "Delete selected animation.";
            _animationsPanel.AddChild(deleteAnimationBtn);

            // step selection
            _animationsPanel.AddChild(new Paragraph("Select Animation Step:"));
            _animationStepSelection = new DropDown();
            _animationStepSelection.OnValueChange = SelectAnimationStep;
            _animationsPanel.AddChild(_animationStepSelection);
            
            // add step button
            var newStepBtn = new Button("New", ButtonSkin.Default, Anchor.AutoInline, new Vector2(0.27f, -1f));
            newStepBtn.OnClick = CreateAnimationStepBtn;
            newStepBtn.ToolTipText = "Create a new animation step at the end of currently selected step.";
            _animationsPanel.AddChild(newStepBtn);

            // split step button
            var splitStepBtn = new Button("Split", ButtonSkin.Default, Anchor.AutoInline, new Vector2(0.36f, -1f));
            splitStepBtn.OnClick = SplitAnimationBtn;
            splitStepBtn.ToolTipText = "Split the currently selected step into two steps, at the position the timeline is currently set on.";
            _animationsPanel.AddChild(splitStepBtn);

            // delete step button
            var deleteStepBtn = new Button("Delete", ButtonSkin.Default, Anchor.AutoInline, new Vector2(0.37f, -1f));
            deleteStepBtn.OnClick = DeleteAnimationStepBtn;
            splitStepBtn.ToolTipText = "Delete current animation step.";
            _animationsPanel.AddChild(deleteStepBtn);

            // animation properties seperator
            _animationsPanel.AddChild(new HorizontalLine());

            // animation timeline
            _animationsPanel.AddChild(new Paragraph("Animation Timeline:"));
            _animationTimeline = new TimelineElement();
            _animationTimeline.ToolTipText = "Display the animation steps over the entire animation duration. Click on this timeline to jump to a specific time position. Note: you can't edit transformations while not pointing on a step keyframe.";
            _animationsPanel.AddChild(_animationTimeline);
            _animationTimelineCaption = new RichParagraph();
            _animationsPanel.AddChild(_animationTimelineCaption);
            _animationTimeline.OnValueChange = TimelineChangeValue;

            // play button
            _animationPlayBtn = new Image(Content.Load<Texture2D>("Editor/play_btn"), new Vector2(32, 32), ImageDrawMode.Stretch, Anchor.AutoInline);
            _animationPlayBtn.SourceRectangle = new Rectangle(0, 0, 24, 24);
            _animationPlayBtn.Offset = new Vector2(-60, -32);
            _animationPlayBtn.ToolTipText = "Start / stop playing animation.";
            _animationPlayBtn.OnClick = (Entity entity) =>
            {
                _playAnimation = !_playAnimation;
                _animationPlayBtn.SourceRectangle = new Rectangle(_playAnimation ? 24 : 0, 0, 24, 24);
            };
            _animationsPanel.AddChild(_animationPlayBtn);

            // animation repeats
            _animationRepeats = new CheckBox("Repeating Animation");
            _animationRepeats.ToolTipText = "Repeating animations iterpolate back to step 0 when reaching the final step. Non-repeating will stop instead.";
            _animationRepeats.OnValueChange = ToggledAnimationRepeats;
            _animationsPanel.AddChild(_animationRepeats);

            // create per-animation-step panel
            _animationStepPropsPanel = new Panel(new Vector2(0, 500), PanelSkin.None, Anchor.Auto);
            _animationStepPropsPanel.Padding = Vector2.Zero;
            _animationStepPropsPanel.ExtraMargin = Point.Zero;
            _animationsPanel.AddChild(_animationStepPropsPanel);

            // create duration input
            _animationStepPropsPanel.AddChild(new HorizontalLine());
            _animationStepPropsPanel.AddChild(new Paragraph("Step Duration:", Anchor.AutoInline, new Vector2(0.52f, -1f)));
            _animationStepDuration = new TextInput(false, new Vector2(0.48f, transInputHeight + 2), Anchor.AutoInline);
            _animationStepDuration.Value = "1";
            _animationStepDuration.Validators.Add(new GeonBit.UI.Entities.TextValidators.NumbersOnly(true));
            _animationStepDuration.OnValueChange = StepDurationChanged;
            _animationStepDuration.WhileMouseDown = StartDraggingValue;
            _animationStepDuration.ToolTipText = "Current step duration, in seconds (duration = time to lerp transformations to next step).";
            _animationStepPropsPanel.AddChild(_animationStepDuration);

            // create animation name input
            _animationStepPropsPanel.AddChild(new Paragraph("Step Name:", Anchor.AutoInline, new Vector2(0.4f, -1f)));
            _animationStepNameInput = new TextInput(false, new Vector2(0.6f, transInputHeight + 2), Anchor.AutoInline);
            _animationStepNameInput.Value = "";
            _animationStepNameInput.OnValueChange = StepNameChanged;
            _animationStepNameInput.ToolTipText = "Step name / identifier. Used for the steps selection dropdown, and can be queried via API when animating the skeleton. You can use this field to tag important animation steps, for example the point to deliver an attack animation damage.";
            _animationStepNameInput.PlaceholderText = "[Unnamed Step]";
            _animationStepPropsPanel.AddChild(_animationStepNameInput);

            // create animation step movement and scale interpolation
            _animationStepPropsPanel.AddChild(new Paragraph("Move & Scale Interpolation:"));
            _animationStepMoveAndScaleInterpolation = new DropDown(new Vector2(0f, 140f));
            _animationStepMoveAndScaleInterpolation.AddItem(InterpolationTypes.Linear.ToString());
            _animationStepMoveAndScaleInterpolation.AddItem(InterpolationTypes.SmoothDamp.ToString());
            _animationStepMoveAndScaleInterpolation.AddItem(InterpolationTypes.SmoothStep.ToString());
            _animationStepMoveAndScaleInterpolation.ToolTipText = "Method to use when interpolating scale and offset vectors.";
            _animationStepMoveAndScaleInterpolation.OnValueChange = ChangeStepInterpolation;
            _animationStepPropsPanel.AddChild(_animationStepMoveAndScaleInterpolation);

            // create animation step rotation interpolation
            _animationStepPropsPanel.AddChild(new Paragraph("Rotation Interpolation:"));
            _animationStepRotateInterpolation = new DropDown(new Vector2(0f, 140f));
            _animationStepRotateInterpolation.AddItem(InterpolationTypes.Linear.ToString());
            _animationStepRotateInterpolation.AddItem(InterpolationTypes.SmoothDamp.ToString());
            _animationStepRotateInterpolation.AddItem(InterpolationTypes.SphericalLinear.ToString());
            _animationStepRotateInterpolation.ToolTipText = "Method to use when interpolating rotation.";
            _animationStepRotateInterpolation.OnValueChange = ChangeStepInterpolation;
            _animationStepPropsPanel.AddChild(_animationStepRotateInterpolation);

            // select default animation
            _animationStepMoveAndScaleInterpolation.SelectedIndex = 0;
            _animationStepRotateInterpolation.SelectedIndex = 0;
            _animationTimeline.OnValueChange(_animationTimeline);
            _animationSelection.OnValueChange = AnimationSelected;
            _animationSelection.SelectedIndex = 0;
        }

        /// <summary>
        /// Bone alias changed.
        /// </summary>
        void BoneAliasChanged(Entity entity)
        {
            _skeleton.SetAlias(_selectedBone, _boneAlias.Value);
        }

        /// <summary>
        /// Set animation to repeat / not repeat.
        /// </summary>
        void ToggledAnimationRepeats(Entity entity)
        {
            // no animation selected? skip
            if (_animationSelection.SelectedIndex <= 0)
            {
                return;
            }

            // update animation
            var animation = _skeleton.GetAnimation(_animationSelection.SelectedValue);
            animation.Repeats = _animationRepeats.Checked;
        }

        /// <summary>
        /// Animation step name changed.
        /// </summary>
        void StepNameChanged(Entity entity)
        {
            // no animation or step selected
            if (_animationSelection.SelectedIndex <= 0 || _animationStepSelection.SelectedIndex < 0)
            {
                return;
            }

            // get anmation and step
            var animation = _skeleton.GetAnimation(_animationSelection.SelectedValue);
            var step = animation.Steps[_animationStepSelection.SelectedIndex];

            // update name
            step.Name = _animationStepNameInput.Value;
            var displayName = string.IsNullOrWhiteSpace(step.Name) ? "[Unnamed Step]" : step.Name;
            var tempCallback = _animationStepSelection.OnValueChange;
            _animationStepSelection.OnValueChange = null;
            _animationStepSelection.ChangeItem(_animationStepSelection.SelectedIndex, displayName);
            _animationStepSelection.OnValueChange = tempCallback;
        }

        /// <summary>
        /// Timeline was dragged.
        /// </summary>
        void TimelineChangeValue(Entity entity)
        {
            // no animation selected
            if (_animationSelection.SelectedIndex <= 0)
            {
                _animationTimelineCaption.Text = "{{L_GREEN}}0{{DEFAULT}} (sec)";
                return;
            }

            // update caption
            _animationTimelineCaption.Text = (_animationTimeline.IsOnMark() ? "{{L_GREEN}}" : "") + (AnimationOffset).ToString() + "{{DEFAULT}} / " + (_animationTimeline.MaxDuration / 100).ToString() + " (sec)";

            // update selected step
            var stepIndex = _animationTimeline.GetSelectedMarkIndex();
            if ((_animationStepSelection.SelectedIndex != stepIndex) && (_animationStepSelection.Count > stepIndex))
            {
                var tempCallback = _animationStepSelection.OnValueChange;
                _animationStepSelection.OnValueChange = null;
                _animationStepSelection.SelectedIndex = stepIndex;
                _animationStepSelection.OnValueChange = tempCallback;
                UpdateAnimationStepFieldValues();
            }

            // update transformations
            _bonesList.OnValueChange(_bonesList);
        }

        /// <summary>
        /// Animation step interpolation changed.
        /// </summary>
        void ChangeStepInterpolation(Entity entity)
        {
            // no animation or step selected
            if (_animationSelection.SelectedIndex <= 0 || _animationStepSelection.SelectedIndex < 0)
            {
                return;
            }

            // get anmation and step
            var animation = _skeleton.GetAnimation(_animationSelection.SelectedValue);
            var step = animation.Steps[_animationStepSelection.SelectedIndex];

            // set interpolation
            step.RotationInterpolation = (InterpolationTypes)Enum.Parse(typeof(InterpolationTypes), _animationStepRotateInterpolation.SelectedValue);
            step.PositionInterpolation = step.ScaleInterpolation = (InterpolationTypes)Enum.Parse(typeof(InterpolationTypes), _animationStepMoveAndScaleInterpolation.SelectedValue);
        }

        /// <summary>
        /// Update the per-animation-step UI fields.
        /// </summary>
        void UpdateAnimationStepFieldValues()
        {
            // get anmation and step
            var animation = _skeleton.GetAnimation(_animationSelection.SelectedValue);
            var step = animation.Steps[_animationStepSelection.SelectedIndex];

            // update step duration
            _animationStepDuration.ChangeValue(step.Duration.ToString(), false);

            // update step name
            _animationStepNameInput.ChangeValue(step.Name, false);

            // update interpolation
            _animationStepRotateInterpolation.SelectedValue = step.RotationInterpolation.ToString();
            _animationStepMoveAndScaleInterpolation.SelectedValue = step.PositionInterpolation.ToString();
        }

        /// <summary>
        /// Select animation step.
        /// </summary>
        void SelectAnimationStep(Entity entity)
        {
            // no animation or step selected
            if (_animationSelection.SelectedIndex <= 0 || _animationStepSelection.SelectedIndex < 0)
            {
                return;
            }

            // disable timeline value change
            _animationTimeline.DisableValueChange = true;

            // update step fields
            UpdateAnimationStepFieldValues();

            // update timeline position
            _animationTimeline.TimePosition = 0;
            for (var i = 0; i < _animationStepSelection.SelectedIndex; ++i)
            {
                var animation = _skeleton.GetAnimation(_animationSelection.SelectedValue);
                _animationTimeline.TimePosition += (uint)(animation.Steps[i].Duration * 100f);
            }

            // re-enable timeline value change
            _animationTimeline.DisableValueChange = false;

            // update transformations
            _bonesList.OnValueChange(_bonesList);
        }

        /// <summary>
        /// Step duration input.
        /// </summary>
        void StepDurationChanged(Entity entity)
        {
            try
            {
                // get duration value and set to min if invalid
                var value = float.Parse(_animationStepDuration.Value);
                if (value < 0.1f)
                {
                    value = 0.1f;
                    _animationStepDuration.Value = "0.1";
                }

                // update step duration
                if (_animationSelection.SelectedIndex > 0 && _animationStepSelection.SelectedIndex >= 0)
                {
                    var animation = _skeleton.GetAnimation(_animationSelection.SelectedValue);
                    var stepIndex = _animationStepSelection.SelectedIndex;
                    var step = animation.Steps[stepIndex];
                    step.Duration = value;
                    UpdateAnimationTimeline();
                    _animationStepSelection.SelectedIndex = stepIndex;
                }
            }
            catch
            {
            }
        }

        /// <summary>
        /// Split animation step.
        /// </summary>
        void SplitAnimationBtn(Entity entity)
        {
            // no animation selected
            if (_animationSelection.SelectedIndex <= 0)
            {
                GeonBit.UI.Utils.MessageBox.ShowMsgBox("Error!", "Can't split animation steps in default pose! Please select a valid animation to split.");
                return;
            }

            // animation playing?
            if (_playAnimation)
            {
                GeonBit.UI.Utils.MessageBox.ShowMsgBox("Error!", "Can't perform this action while animation is playing.");
                return;
            }

            // split animation step
            var animation = _skeleton.GetAnimation(_animationSelection.SelectedValue);
            animation.Split(AnimationOffset);
            AnimationSelected(_animationTimeline);
        }

        /// <summary>
        /// Delete animation step.
        /// </summary>
        void DeleteAnimationStepBtn(Entity entity)
        {
            // no animation selected
            if (_animationSelection.SelectedIndex <= 0)
            {
                GeonBit.UI.Utils.MessageBox.ShowMsgBox("Error!", "Can't delete animation steps from default pose! Please select a valid animation to remove steps from.");
                return;
            }

            // animation playing?
            if (_playAnimation)
            {
                GeonBit.UI.Utils.MessageBox.ShowMsgBox("Error!", "Can't perform this action while animation is playing.");
                return;
            }

            // no step selected
            if (_animationStepSelection.SelectedIndex < 0)
            {
                GeonBit.UI.Utils.MessageBox.ShowMsgBox("Error!", "There's no valid animation step selected to delete.");
                return;
            }

            // delete animation step
            GeonBit.UI.Utils.MessageBox.ShowYesNoMsgBox("Delete Animation Step", $"Are you sure you wish to delete current animation step?", () =>
            {
                // get selected animation
                var selectedAnimation = _animationSelection.SelectedValue;
                var animation = _skeleton.GetAnimation(selectedAnimation);

                // delete step and update ui
                animation.RemoveStep(_animationStepSelection.SelectedIndex);
                UpdatePartsList(_selectedBone);
                _animationSelection.SelectedValue = selectedAnimation;
                return true;
            },
            () => { return true; });
        }

        /// <summary>
        /// Add animation step.
        /// </summary>
        void CreateAnimationStepBtn(Entity entity)
        {
            // no animation selected
            if (_animationSelection.SelectedIndex <= 0)
            {
                GeonBit.UI.Utils.MessageBox.ShowMsgBox("Error!", "Can't add animation steps to default pose! Please select a valid animation to add step for.");
                return;
            }

            // animation playing?
            if (_playAnimation)
            {
                GeonBit.UI.Utils.MessageBox.ShowMsgBox("Error!", "Can't perform this action while animation is playing.");
                return;
            }

            // new step id
            var textInput = new TextInput(false);
            textInput.PlaceholderText = "Optional Step Id";

            // copy transform source
            var copyTransformOptions = new DropDown();
            copyTransformOptions.AddItem("Selected Step");
            copyTransformOptions.AddItem("Base Pose");
            copyTransformOptions.SelectedIndex = 0;

            // get currently selected step index
            var prevIndex = _animationStepSelection.SelectedIndex;

            // open new step dialog
            GeonBit.UI.Utils.MessageBox.ShowMsgBox("Create Animation Animation", $"Add optional animation step name:",
                new GeonBit.UI.Utils.MessageBox.MsgBoxOption[] {
                        new GeonBit.UI.Utils.MessageBox.MsgBoxOption("Create", () =>
                        {
                            // create new animation
                            var stepName = textInput.Value;
                            if (string.IsNullOrWhiteSpace(stepName)) { stepName = null; }

                            // get selected animation
                            var selectedAnimation = _animationSelection.SelectedValue;
                            var animation = _skeleton.GetAnimation(selectedAnimation);

                            // get step to copy transforms from (or null if use base pose)
                            AnimationStep copyFromStep = null;
                            if (copyTransformOptions.SelectedIndex == 0 && _animationStepSelection.SelectedIndex >= 0)
                            {
                                copyFromStep = animation.Steps[_animationStepSelection.SelectedIndex];
                            }

                            // create new step and update ui
                            animation.AddStep(stepName ?? string.Empty, 1f, copyFromStep);
                            UpdatePartsList(_selectedBone);
                            _animationSelection.SelectedValue = selectedAnimation;
                            _animationStepSelection.SelectedIndex = prevIndex + 1;
                            return true;
                        }),
                        new GeonBit.UI.Utils.MessageBox.MsgBoxOption("Cancel", () => { return true; }),
                }, new Entity[] { 
                    textInput, 
                    new Paragraph("Copy transformations from:"), 
                    copyTransformOptions,
                    new RichParagraph("Note: this step will be added after step '{{L_GREEN}}" + (_animationStepSelection.SelectedValue ?? "[first step]") + "{{DEFAULT}}'.")});
        }

        /// <summary>
        /// Animation selected.
        /// </summary>
        void AnimationSelected(Entity entity)
        {
            // clear steps selection
            _animationStepSelection.ClearItems();

            // not animation? skip
            if (_animationSelection.SelectedValue == null || _animationSelection.SelectedValue == DefaultNoneAnimationName) { return; }

            // not playing animation anymore
            if (_playAnimation) { _animationPlayBtn.OnClick(_animationPlayBtn); }

            // get animation steps
            var animation = _skeleton.GetAnimation(_animationSelection.SelectedValue);
            foreach (var step in animation.Steps)
            {
                var name = string.IsNullOrWhiteSpace(step.Name) ? "[Unnamed Step]" : step.Name;
                _animationStepSelection.AddItem(name);
            }

            // set if animation is looped
            _animationRepeats.Checked = animation.Repeats;

            // set default animation step
            try
            {
                _animationStepSelection.SelectedIndex = 0;
            }
            catch { }

            // update timeline
            UpdateAnimationTimeline();
        }

        /// <summary>
        /// Update timeline element.
        /// </summary>
        void UpdateAnimationTimeline()
        {
            // clear steps selection
            _animationTimeline.TimePosition = 0;
            _animationTimeline.MaxDuration = 0;

            // not animation? skip
            if (_animationSelection.SelectedValue == null || _animationSelection.SelectedValue == DefaultNoneAnimationName) { return; }

            // get animation steps
            var animation = _skeleton.GetAnimation(_animationSelection.SelectedValue);
            List<uint> marks = new List<uint>();
            foreach (var step in animation.Steps)
            {
                marks.Add(_animationTimeline.MaxDuration);
                _animationTimeline.MaxDuration += (uint)(step.Duration * 100);
            }
            _animationTimeline.Marks = marks.ToArray();
            _animationTimeline.OnValueChange(_animationTimeline);
        }

        /// <summary>
        /// Button to clone animation pressed.
        /// </summary>
        void CloneAnimationBtn(Entity entity)
        {
            // no animation selected
            if (_animationSelection.SelectedIndex <= 0)
            {
                GeonBit.UI.Utils.MessageBox.ShowMsgBox("Error!", "Can't clone default pose! Please select a valid animation to clone.");
                return;
            }

            // open new bone input
            var textInput = new TextInput(false);
            textInput.PlaceholderText = "Cloned Animation Name";
            GeonBit.UI.Utils.MessageBox.ShowMsgBox("Clone Animation", $"Please enter a name for the cloned animation:",
                new GeonBit.UI.Utils.MessageBox.MsgBoxOption[] {
                        new GeonBit.UI.Utils.MessageBox.MsgBoxOption("Clone", () =>
                        {
                            // create new animation
                            var animationName = textInput.Value;
                            if (!string.IsNullOrWhiteSpace(animationName))
                            {
                                // check if animation already exists.
                                if (animationName == DefaultNoneAnimationName || _skeleton.AnimationExists(animationName))
                                {
                                    GeonBit.UI.Utils.MessageBox.ShowMsgBox("Animation Exists!", $"An animation named '{animationName}' already exists! Please pick a different name.");
                                    return false;
                                }
                                
                                // add animation and refresh UI
                                _skeleton.CloneAnimation(_animationSelection.SelectedValue, animationName);
                                UpdatePartsList(_selectedBone);
                                _animationSelection.SelectedValue = animationName;
                                return true;
                            }

                            // no valid name
                            return false;
                        }),
                        new GeonBit.UI.Utils.MessageBox.MsgBoxOption("Cancel", () => { return true; }),
                }, new Entity[] { textInput });
        }

        /// <summary>
        /// Button to create animation pressed.
        /// </summary>
        void CreateAnimationBtn(Entity entity)
        {
            // open new bone input
            var textInput = new TextInput(false);
            textInput.PlaceholderText = "New Animation Name";
            GeonBit.UI.Utils.MessageBox.ShowMsgBox("Create Animation", $"Please enter a name for the new animation sequence:",
                new GeonBit.UI.Utils.MessageBox.MsgBoxOption[] {
                        new GeonBit.UI.Utils.MessageBox.MsgBoxOption("Create", () =>
                        {
                            // create new animation
                            var animationName = textInput.Value;
                            if (!string.IsNullOrWhiteSpace(animationName))
                            {
                                // check if animation already exists.
                                if (animationName == DefaultNoneAnimationName || _skeleton.AnimationExists(animationName))
                                {
                                    GeonBit.UI.Utils.MessageBox.ShowMsgBox("Animation Exists!", $"An animation named '{animationName}' already exists! Please pick a different name.");
                                    return false;
                                }
                                
                                // add animation and refresh UI
                                _skeleton.CreateAnimation(animationName);
                                UpdatePartsList(_selectedBone);
                                _animationSelection.SelectedValue = animationName;
                                return true;
                            }

                            // no valid name
                            return false;
                        }),
                        new GeonBit.UI.Utils.MessageBox.MsgBoxOption("Cancel", () => { return true; }),
                }, new Entity[] { textInput });
        }

        /// <summary>
        /// Button to delete animation pressed.
        /// </summary>
        void DeleteAnimationBtn(Entity entity)
        {
            // no animation selected
            if (_animationSelection.SelectedIndex <= 0)
            {
                GeonBit.UI.Utils.MessageBox.ShowMsgBox("Error!", "Can't delete default pose! Please select a valid animation to delete.");
                return;
            }

            // delete animation
            GeonBit.UI.Utils.MessageBox.ShowYesNoMsgBox("Delete Animation", $"Are you sure you wish to completely delete animation '{_animationSelection.SelectedValue}'?", () =>
            {
                _skeleton.DeleteAnimation(_animationSelection.SelectedValue);
                UpdatePartsList();
                return true;
            },
            () => { return true; });
        }

        /// <summary>
        /// Button to clone bone pressed.
        /// </summary>
        void CloneSelectedBoneBtn(Entity entity)
        {
            // no bone selected
            if (_selectedBone == null || _selectedBone == "root")
            {
                GeonBit.UI.Utils.MessageBox.ShowMsgBox("Error!", "Can't clone root bone!");
                return;
            }

            // open new bone input
            var textInput = new TextInput(false);
            textInput.PlaceholderText = "Cloned Bone Name";
            GeonBit.UI.Utils.MessageBox.ShowMsgBox("Clone Bone", $"Please enter a new name for the bone to clone:",
                new GeonBit.UI.Utils.MessageBox.MsgBoxOption[] {
                        new GeonBit.UI.Utils.MessageBox.MsgBoxOption("Clone Bone", () =>
                        {
                            // rename bone
                            var selectedBoneParent = _selectedBone.Substring(0, _selectedBone.LastIndexOf('/'));
                            var boneName = textInput.Value;
                            if (!string.IsNullOrWhiteSpace(boneName))
                            {
                                // only if bone doesn't exist..
                                var newBonePath = selectedBoneParent + '/' + boneName;
                                if (_skeleton.BoneExists(boneName, selectedBoneParent))
                                {
                                    GeonBit.UI.Utils.MessageBox.ShowMsgBox("Bone Already Exist", $"A bone with path '{newBonePath}' already exists!");
                                    return false;
                                }

                                // add bone
                                _skeleton.CloneBone(_selectedBone, boneName);
                                UpdatePartsList(newBonePath);
                                return true;
                            }

                            // no valid bone name
                            return false;
                        }),
                        new GeonBit.UI.Utils.MessageBox.MsgBoxOption("Cancel", () => { return true; }),
                }, new Entity[] { textInput });
        }

        /// <summary>
        /// Button to rename bone pressed.
        /// </summary>
        void RenameBoneBtn(Entity entity)
        {
            // no bone selected
            if (_selectedBone == null || _selectedBone == "root")
            {
                GeonBit.UI.Utils.MessageBox.ShowMsgBox("Error!", "Can't rename root bone!");
                return;
            }

            // open new bone input
            var textInput = new TextInput(false);
            textInput.PlaceholderText = "New Bone Name";
            GeonBit.UI.Utils.MessageBox.ShowMsgBox("Rename Bone", $"Please enter a new name for the bone to rename:",
                new GeonBit.UI.Utils.MessageBox.MsgBoxOption[] {
                        new GeonBit.UI.Utils.MessageBox.MsgBoxOption("Rename Bone", () =>
                        {
                            // rename bone
                            var selectedBoneParent = _selectedBone.Substring(0, _selectedBone.LastIndexOf('/'));
                            var boneName = textInput.Value;
                            if (!string.IsNullOrWhiteSpace(boneName))
                            {
                                // only if bone doesn't exist..
                                var newBonePath = selectedBoneParent + '/' + boneName;
                                if (_skeleton.BoneExists(boneName, selectedBoneParent))
                                {
                                    GeonBit.UI.Utils.MessageBox.ShowMsgBox("Bone Already Exist", $"A bone with path '{newBonePath}' already exists!");
                                    return false;
                                }

                                // add bone
                                _skeleton.RenameBone(_selectedBone, newBonePath);
                                UpdatePartsList(newBonePath);
                                return true;
                            }

                            // no valid bone name
                            return false;
                        }),
                        new GeonBit.UI.Utils.MessageBox.MsgBoxOption("Cancel", () => { return true; }),
                }, new Entity[] { textInput });
        }

        /// <summary>
        /// Button to create bone pressed.
        /// </summary>
        void CreateBoneBtn(Entity entity)
        {
            // no bone selected
            if (_selectedBone == null)
            {
                GeonBit.UI.Utils.MessageBox.ShowMsgBox("Error!", "Can't create bone without parent!");
                return;
            }

            // open new bone input
            var textInput = new TextInput(false);
            textInput.PlaceholderText = "New Bone Name";
            GeonBit.UI.Utils.MessageBox.ShowMsgBox("Create Bone", $"Please enter a name for the new bone to add under parent bone '{_selectedBone}':",
                new GeonBit.UI.Utils.MessageBox.MsgBoxOption[] {
                        new GeonBit.UI.Utils.MessageBox.MsgBoxOption("Create Bone", () =>
                        {
                            // create new bone
                            var boneName = textInput.Value;
                            if (!string.IsNullOrWhiteSpace(boneName))
                            {
                                // only if bone doesn't exist..
                                if (_skeleton.BoneExists(boneName, _selectedBone))
                                {
                                    GeonBit.UI.Utils.MessageBox.ShowMsgBox("Bone Already Exist", $"A bone with path '{_selectedBone}/{boneName}' already exists!");
                                    return false;
                                }

                                // add bone
                                _skeleton.AddBone(boneName, _selectedBone);
                                UpdatePartsList(_selectedBone + '/' + boneName);
                                return true;
                            }

                            // no valid bone name
                            return false;
                        }),
                        new GeonBit.UI.Utils.MessageBox.MsgBoxOption("Cancel", () => { return true; }),
                }, new Entity[] { textInput });
        }

        /// <summary>
        /// Button to delete bone pressed.
        /// </summary>
        void DeleteSelectedBoneBtn(Entity entity)
        {
            // no bone selected
            if (_selectedBone == null || _selectedBone == "root")
            {
                GeonBit.UI.Utils.MessageBox.ShowMsgBox("Error!", "Can't delete 'root' bone!");
                return;
            }

            // delete bone
            GeonBit.UI.Utils.MessageBox.ShowYesNoMsgBox("Delete Bone", $"Are you sure you wish to delete everything under bone: '{_selectedBone}'?\n\nThis will remove the bone and its children from all animations and poses.", () =>
            {
                _skeleton.Delete(_selectedBone);
                UpdatePartsList();
                return true;
            },
            () => { return true; });
        }

        /// <summary>
        /// Load a project.
        /// </summary>
        void LoadProject(string file)
        {
            // load skeleton
            _skeleton = new Skeleton();
            _skeleton.LoadFrom(new Sini.IniFile(System.IO.Path.Combine(_savesFolder, file)));

            // update parts list
            UpdatePartsList();

            // set filename
            UpdateFilename(file);
        }

        /// <summary>
        /// Create empty skeleton.
        /// </summary>
        void CreateEmptySkeleton()
        {
            // create skeleton
            _skeleton = new Skeleton();
            _skeleton.AddBone("root");

            // update parts list
            UpdatePartsList();

            // reset filename
            UpdateFilename(null);
        }

        /// <summary>
        /// Create a default human skeleton.
        /// </summary>
        void CreateDefaultSkeleton()
        {
            // create skeleton
            _skeleton = new Skeleton();
            _skeleton.AddBone("root");

            // add starting bones
            var torsoHeight = 3f;
            var torsoOffset = 8f + torsoHeight;
            _skeleton.AddBone("root/torso", (new Vector3(0, torsoOffset, 0)));
            _skeleton.AddBone("root/torso/upper", (new Vector3(0, 0.5f, 0)));
            _skeleton.AddBone("root/torso/lower", (new Vector3(0, -torsoHeight / 2, 0)));
            _skeleton.SetBonePreviewModel("root/torso/upper", new Vector3(0, torsoHeight / 2 - 0.25f, 0), new Vector3(1.75f, torsoHeight / 2, 1f));
            _skeleton.SetBonePreviewModel("root/torso/lower", new Vector3(0, 0, 0), new Vector3(1.75f, torsoHeight / 2 - 0.25f, 1f));

            var thighLength = 2f;
            _skeleton.AddBone("root/torso/lower/legs", (new Vector3(0, -torsoHeight / 2, 0)));
            _skeleton.AddBone("root/torso/lower/legs/left", (new Vector3(-1f, 0, 0)));
            _skeleton.AddBone("root/torso/lower/legs/right", (new Vector3(1f, 0, 0)));
            _skeleton.SetBonePreviewModel("root/torso/lower/legs/left", new Vector3(0f, -thighLength, 0f), new Vector3(0.85f, thighLength, 1f));
            _skeleton.SetBonePreviewModel("root/torso/lower/legs/right", new Vector3(0f, -thighLength, 0f), new Vector3(0.85f, thighLength, 1f));

            var lowerLegLength = 2f;
            _skeleton.AddBone("root/torso/lower/legs/left/lower", (new Vector3(0, -thighLength * 2, 0)));
            _skeleton.AddBone("root/torso/lower/legs/right/lower", (new Vector3(0, -thighLength * 2, 0)));
            _skeleton.SetBonePreviewModel("root/torso/lower/legs/left/lower", new Vector3(0f, -lowerLegLength, 0f), new Vector3(0.85f, lowerLegLength, 1f));
            _skeleton.SetBonePreviewModel("root/torso/lower/legs/right/lower", new Vector3(0f, -lowerLegLength, 0f), new Vector3(0.85f, lowerLegLength, 1f));

            _skeleton.AddBone("root/torso/lower/legs/left/lower/foot", (new Vector3(0, -lowerLegLength * 2, 0)));
            _skeleton.AddBone("root/torso/lower/legs/right/lower/foot", (new Vector3(0, -lowerLegLength * 2, 0)));
            _skeleton.SetBonePreviewModel("root/torso/lower/legs/left/lower/foot", new Vector3(0f, 0.5f, 1f), new Vector3(0.85f, 0.5f, 1.75f));
            _skeleton.SetBonePreviewModel("root/torso/lower/legs/right/lower/foot", new Vector3(0f, 0.5f, 1f), new Vector3(0.85f, 0.5f, 1.75f));

            var armTopLength = 1.5f;
            _skeleton.AddBone("root/torso/upper/arms", (new Vector3(0, torsoHeight - 0.5f, 0)));
            _skeleton.AddBone("root/torso/upper/arms/left", (new Vector3(-2.5f, 0f, 0f)));
            _skeleton.AddBone("root/torso/upper/arms/right", (new Vector3(2.5f, 0f, 0f)));
            _skeleton.SetBonePreviewModel("root/torso/upper/arms/left", new Vector3(0f, -armTopLength, 0f), new Vector3(0.65f, armTopLength, 1f));
            _skeleton.SetBonePreviewModel("root/torso/upper/arms/right", new Vector3(0f, -armTopLength, 0f), new Vector3(0.65f, armTopLength, 1f));

            var forearmLength = 1.25f;
            _skeleton.AddBone("root/torso/upper/arms/left/forearm", (new Vector3(0, -armTopLength - forearmLength, 0)));
            _skeleton.AddBone("root/torso/upper/arms/right/forearm", (new Vector3(0, -armTopLength - forearmLength, 0)));
            _skeleton.SetBonePreviewModel("root/torso/upper/arms/right/forearm", new Vector3(0f, -forearmLength, 0f), new Vector3(0.65f, forearmLength, 1f));
            _skeleton.SetBonePreviewModel("root/torso/upper/arms/left/forearm", new Vector3(0f, -forearmLength, 0f), new Vector3(0.65f, forearmLength, 1f));

            _skeleton.AddBone("root/torso/upper/arms/left/forearm/hand", (new Vector3(0, -forearmLength * 2 - 0.5f, 0)));
            _skeleton.AddBone("root/torso/upper/arms/right/forearm/hand", (new Vector3(0, -forearmLength * 2 - 0.5f, 0)));
            _skeleton.SetBonePreviewModel("root/torso/upper/arms/right/forearm/hand", Vector3.Zero, new Vector3(1f, 1f, 1f));
            _skeleton.SetBonePreviewModel("root/torso/upper/arms/left/forearm/hand", Vector3.Zero, new Vector3(1f, 1f, 1f));

            _skeleton.AddBone("root/torso/upper/head", (new Vector3(0, torsoHeight + 1f, 0)));
            _skeleton.AddBone("root/torso/upper/head/hair", (new Vector3(0, 1f, 0)));
            _skeleton.SetBonePreviewModel("root/torso/upper/head", Vector3.Zero, new Vector3(1f, 1f, 1f));

            // set starting bones
            UpdatePartsList();

            // reset filename
            UpdateFilename(null);
        }

        /// <summary>
        /// Attached to input click event, to implement dragging to change numbers.
        /// </summary>
        private void StartDraggingValue(Entity entity)
        {
            _draggedInput = _draggedInput ?? entity as TextInput;
        }

        /// <summary>
        /// Update bone mesh transformations.
        /// </summary>
        private void UpdateBoneMeshTransform(Entity entity)
        {
            if (_freezeMeshUpdates) { return; }
            if (string.IsNullOrEmpty(_selectedBone)) { return; }
            if (!_boneMeshVisible.Checked) { _skeleton.RemoveBonePreviewModel(_selectedBone); return; }

            try
            {
                float posX = float.Parse(_boneMeshOffsetX.Value);
                float posY = float.Parse(_boneMeshOffsetY.Value);
                float posZ = float.Parse(_boneMeshOffsetZ.Value);
                float scaleX = float.Parse(_boneMeshScaleX.Value);
                float scaleY = float.Parse(_boneMeshScaleY.Value);
                float scaleZ = float.Parse(_boneMeshScaleZ.Value);
                _skeleton.SetBonePreviewModel(_selectedBone, new Vector3(posX, posY,posZ), new Vector3(scaleX, scaleY, scaleZ));
            }
            catch { }
        }

        /// <summary>
        /// Update bone transformations from UI.
        /// </summary>
        private void UpdateBoneTransform(Entity entity)
        {
            if (_freezeMeshUpdates) { return; }
            if (string.IsNullOrEmpty(_selectedBone)) { return; }

            try
            {
                float posX = float.Parse(_boneOffsetX.Value);
                float posY = float.Parse(_boneOffsetY.Value);
                float posZ = float.Parse(_boneOffsetZ.Value);
                float rotX = float.Parse(_boneRotateX.Value);
                float rotY = float.Parse(_boneRotateY.Value);
                float rotZ = float.Parse(_boneRotateZ.Value);
                float scaleX = float.Parse(_boneScaleX.Value);
                float scaleY = float.Parse(_boneScaleY.Value);
                float scaleZ = float.Parse(_boneScaleZ.Value);
                var animation = SelectedAnimation;
                var animationStep = _animationStepSelection.SelectedIndex;
                _skeleton.SetTransform(_selectedBone, animation, animationStep, new Vector3(posX, posY, posZ), new Vector3(rotX, rotY, rotZ), new Vector3(scaleX, scaleY, scaleZ));
            }
            catch { }
        }
        bool _freezeMeshUpdates = false;

        // tuple to convert bone index to path
        Tuple<string, string>[] _bonesIndexToPath;

        /// <summary>
        /// Update bones list.
        /// </summary>
        private void UpdatePartsList(string selectedBone = null)
        {
            // freeze updates
            _freezeMeshUpdates = true;

            // reset selection
            _bonesList.OnValueChange = null;
            _bonesList.SelectedIndex = -1;
            _selectedBoneDisplay.Text = "[Nothing Selected]";
            _selectedBoneDisplay.FillColor = Color.White;
            _selectedBoneDisplay.Offset = new Vector2(0, 10f);
            _selectedBone = null;

            // reset input
            _boneRotateX.Value = "0";
            _boneRotateY.Value = "0";
            _boneRotateZ.Value = "0";
            _boneOffsetX.Value = "0";
            _boneOffsetY.Value = "0";
            _boneOffsetZ.Value = "0";

            // get all bones (tuple of <display, full_string>)
            var bones = _bonesIndexToPath = _skeleton.GetFlatDisplayList();

            // update list
            int index = 0;
            _fullPathToBoneListIndex = new Dictionary<string, int>();
            _bonesList.ClearItems();
            foreach (var path in _skeleton.GetFlatDisplayList())
            {
                _bonesList.AddItem(path.Item1);
                _fullPathToBoneListIndex[path.Item2] = index++;
            }

            // set callback
            _bonesList.OnValueChange = (Entity entity) =>
            {
                UpdateTransformationsFromSkeleton();
            };

            // select default animation
            _animationSelection.SelectedIndex = 0;

            // select default bone
            if (_bonesList.Items.Length > 0)
            {
                // select specific bone
                if (selectedBone != null)
                {
                    for (var i = 0; i < bones.Length; ++i)
                    {
                        if (selectedBone == bones[i].Item2)
                        {
                            _bonesList.SelectedIndex = i;
                            break;
                        }
                    }
                }
                // select default root
                else
                {
                    _bonesList.SelectedIndex = 0;
                }
            }

            // reset animations
            _animationSelection.ClearItems();
            _animationSelection.AddItem(DefaultNoneAnimationName);
            foreach (var animation in _skeleton.Animations.OrderBy(x => x))
            {
                _animationSelection.AddItem(animation);
            }
            _animationSelection.SelectedIndex = 0;

            // unfreeze updates
            _freezeMeshUpdates = false;
        }

        /// <summary>
        /// Update all transformations from skeleton.
        /// </summary>
        void UpdateTransformationsFromSkeleton()
        {
            _freezeMeshUpdates = true;

            // select bone
            _selectedBone = _bonesIndexToPath[_bonesList.SelectedIndex].Item2;

            // get animation and step
            var animation = SelectedAnimation;
            var stepIndex = _animationStepSelection.SelectedIndex;

            // update rotation and offset
            var trans = _skeleton.GetTransform(_selectedBone, animation, stepIndex);
            _boneRotateX.Value = trans.Rotation.X.ToString();
            _boneRotateY.Value = trans.Rotation.Y.ToString();
            _boneRotateZ.Value = trans.Rotation.Z.ToString();
            _boneOffsetX.Value = trans.Offset.X.ToString();
            _boneOffsetY.Value = trans.Offset.Y.ToString();
            _boneOffsetZ.Value = trans.Offset.Z.ToString();
            _boneScaleX.Value = trans.Scale.X.ToString();
            _boneScaleY.Value = trans.Scale.Y.ToString();
            _boneScaleZ.Value = trans.Scale.Z.ToString();

            // update alias
            _boneAlias.Value = _skeleton.GetBoneAlias(_selectedBone) ?? string.Empty;

            // update mesh display
            _boneMeshVisible.Checked = _skeleton.HavePreviewModel(_selectedBone);
            var meshTrans = _skeleton.GetPreviewModelTransform(_selectedBone);
            _boneMeshOffsetX.Value = meshTrans.Offset.X.ToString();
            _boneMeshOffsetY.Value = meshTrans.Offset.Y.ToString();
            _boneMeshOffsetZ.Value = meshTrans.Offset.Z.ToString();
            _boneMeshScaleX.Value = meshTrans.Scale.X.ToString();
            _boneMeshScaleY.Value = meshTrans.Scale.Y.ToString();
            _boneMeshScaleZ.Value = meshTrans.Scale.Z.ToString();

            _freezeMeshUpdates = false;
        }

        /// <summary>
        /// Reset camera properties.
        /// </summary>
        void ResetCamera()
        {
            _cameraDistance = 35;
            _cameraAngle = new Vector2(MathF.PI / 2, MathF.PI / 4);
            _cameraOffsetY = 5;
        }

        /// <summary>
        /// Show help message.
        /// </summary>
        void PressedTopMenu_ShowHelp()
        {
            GeonBit.UI.Utils.MessageBox.ShowMsgBox("Help / Controls", @"WHAT'S THIS:
---------------------
{{L_GREEN}}MonoSkelly{{DEFAULT}} is a MonoGame extension for simple bones animation. Basically like Minecraft characters. 
This is the official editor to create and animate bones.

CONTROLS:
---------------------
{{L_GREEN}}Left Mouse Button{{DEFAULT}}: select bone handle.
{{L_GREEN}}Right Mouse Button{{DEFAULT}}: rotate camera.
{{L_GREEN}}Scroll Mouse Wheel{{DEFAULT}}: zoom in / out.
{{L_GREEN}}Press Mouse Wheel{{DEFAULT}}: change camera target height.

UI / PANELS:
---------------------
{{L_GREEN}}Bones & Transformations{{DEFAULT}}: Edit the skeleton bones for selected animation step (or default pose if none selected).
{{L_GREEN}}Debug Bone Rendering{{DEFAULT}}: Define meshes to visualize bones for editor and debug purposes.
{{L_GREEN}}Animations{{DEFAULT}}: Edit animations and animation steps. When you select an animation step, the 'Bones & Transformations' editor will apply on the given step.", 

size: new Vector2(860, 800));
        }

        /// <summary>
        /// Show about message.
        /// </summary>
        void PressedTopMenu_ShowAbout()
        {
            GeonBit.UI.Utils.MessageBox.ShowMsgBox("About", @"MonoSkelly.Editor is the official editor for MonoSkelly animations.

To learn more, please visit 
{{L_GREEN}}https://github.com/RonenNess/MonoSkelly{{DEFAULT}}

Editor version: {{L_GREEN}}1.1.0{{DEFAULT}}.", size: new Vector2(600, 400));
        }

        /// <summary>
        /// Show / hide bones handles.
        /// </summary>
        void PressedTopMenu_ToggleShowHandlers(GeonBit.UI.Utils.MenuBar.MenuCallbackContext context)
        {
            _showBoneHandles = !_showBoneHandles;
            context.Entity.ChangeItem(context.ItemIndex, (_showBoneHandles ? "{{L_GREEN}}" : "") + "Show Handles");
        }

        /// <summary>
        /// Show / hide lighting.
        /// </summary>
        void PressedTopMenu_ToggleLighting(GeonBit.UI.Utils.MenuBar.MenuCallbackContext context)
        {
            _showLights = !_showLights;
            context.Entity.ChangeItem(context.ItemIndex, (_showLights ? "{{L_GREEN}}" : "") + "Enable Lighting");
        }

        /// <summary>
        /// Show / hide bones outline.
        /// </summary>
        void PressedTopMenu_ToggleShowOutline(GeonBit.UI.Utils.MenuBar.MenuCallbackContext context)
        {
            _showBonesOutline = !_showBonesOutline;
            context.Entity.ChangeItem(context.ItemIndex, (_showBonesOutline ? "{{L_GREEN}}" : "") + "Bones Outline");
        }

        /// <summary>
        /// Show / hide bones.
        /// </summary>
        void PressedTopMenu_ToggleShowBones(GeonBit.UI.Utils.MenuBar.MenuCallbackContext context)
        {
            _showBones = !_showBones;
            context.Entity.ChangeItem(context.ItemIndex, (_showBones ? "{{L_GREEN}}" : "") + "Show Bones");
        }

        /// <summary>
        /// Pressed top menu exit.
        /// </summary>
        void PressedTopMenu_Exit(GeonBit.UI.Utils.MenuBar.MenuCallbackContext context)
        {
            ConfirmAndExit();
        }

        /// <summary>
        /// Ask for confirmation and exit app.
        /// </summary>
        void ConfirmAndExit()
        {
            if (GeonBit.UI.Utils.MessageBox.OpenedMsgBoxesCount == 0)
            {
                GeonBit.UI.Utils.MessageBox.ShowYesNoMsgBox("Exit Editor?", "Are you sure you wish to exit editor and discard any unsaved changes?", () => { Exit(); return true; }, null);
            }
        }

        /// <summary>
        /// Pressed top menu new.
        /// </summary>
        void PressedTopMenu_New(GeonBit.UI.Utils.MenuBar.MenuCallbackContext context)
        {
            GeonBit.UI.Utils.MessageBox.ShowYesNoMsgBox("Discard Changes?", "Are you sure you wish to create a new skeleton and discard any unsaved changes?", () => { CreateEmptySkeleton(); return true; }, null);
        }

        /// <summary>
        /// Pressed top menu save-as.
        /// </summary>
        void PressedTopMenu_SaveAs(GeonBit.UI.Utils.MenuBar.MenuCallbackContext context)
        {
            AskForNewSaveNameAndSave();
        }

        /// <summary>
        /// Pressed top menu save.
        /// </summary>
        void PressedTopMenu_Save(GeonBit.UI.Utils.MenuBar.MenuCallbackContext context)
        {
            // existing save
            if (_currentFilename != null)
            {
                DoSave(_currentFilename);
            }
            // new save
            else
            {
                AskForNewSaveNameAndSave();
            }
        }

        /// <summary>
        /// Open dialog to pick new save name and save.
        /// </summary>
        void AskForNewSaveNameAndSave()
        {
            var textInput = new TextInput(false);
            textInput.PlaceholderText = "New Filename";
            GeonBit.UI.Utils.MessageBox.ShowMsgBox("New Save File", "Please enter save name:",
                new GeonBit.UI.Utils.MessageBox.MsgBoxOption[] {
                        new GeonBit.UI.Utils.MessageBox.MsgBoxOption("Save", () =>
                        {
                            var filename = textInput.Value;
                            if (filename != null)
                            {
                                if (!filename.ToLower().EndsWith(".ini")) { filename += ".ini"; }
                                DoSaveWithValidations(filename, true);
                            }
                            return !string.IsNullOrEmpty(textInput.Value);
                        }),
                        new GeonBit.UI.Utils.MessageBox.MsgBoxOption("Cancel", () => { return true; }),
                }, new Entity[] { textInput });
        }

        /// <summary>
        /// Do saving with validations.
        /// </summary>
        void DoSaveWithValidations(string filename, bool confirmOverride)
        {
            // confirm override
            if (confirmOverride)
            {
                var fullPath = System.IO.Path.Combine(_savesFolder, filename);
                if (System.IO.File.Exists(fullPath))
                {
                    GeonBit.UI.Utils.MessageBox.ShowYesNoMsgBox("Override existing file?", $"File named '{filename}' already exists. Override it?",
                        () => { DoSave(filename); return true; }, () => { return true; });
                    return;
                }
            }

            // no need for validation
            DoSave(filename);
        }

        /// <summary>
        /// Actually do the saving action.
        /// </summary>
        void DoSave(string filename)
        {
            var ini = Sini.IniFile.CreateEmpty();
            _skeleton.SaveTo(ini);
            ini.SaveTo(System.IO.Path.Combine(_savesFolder, filename));
            GeonBit.UI.Utils.MessageBox.ShowMsgBox("Saved Successfully!", $"Skeleton was saved as '{filename}'.");
            UpdateFilename(filename);
        }

        /// <summary>
        /// Update current project filename.
        /// </summary>
        void UpdateFilename(string newFilename)
        {
            if (newFilename == null)
            {
                Window.Title = "MonoSkelly - * Untitled *";
            }
            else
            {
                Window.Title = "MonoSkelly - " + newFilename;
            }
            _currentFilename = newFilename;
        }

        /// <summary>
        /// Pressed top menu save level.
        /// </summary>
        void PressedTopMenu_Load(GeonBit.UI.Utils.MenuBar.MenuCallbackContext context)
        {
            // get files list
            var files = System.IO.Directory.GetFiles(_savesFolder);
            var filesList = new SelectList();
            foreach (var file in files)
            {
                if (file.ToLower().EndsWith(".ini")) { filesList.AddItem(System.IO.Path.GetFileName(file)); }
            }

            // show open dialog
            GeonBit.UI.Utils.MessageBox.ShowMsgBox("Load File", "Select file to open:",
                new GeonBit.UI.Utils.MessageBox.MsgBoxOption[] {
                        new GeonBit.UI.Utils.MessageBox.MsgBoxOption("Load", () =>
                        {
                            var loadFile = filesList.SelectedValue;
                            if (loadFile != null)
                            {
                                LoadProject(loadFile);
                                return true;
                            }
                            return false;
                        }),
                        new GeonBit.UI.Utils.MessageBox.MsgBoxOption("Cancel", () => { return true; }),
                }, new Entity[] { filesList });
        }

        /// <summary>
        /// Load editor content.
        /// </summary>
        protected override void LoadContent()
        {
            // load basic models
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            var boneModel = Content.Load<Model>("Editor\\bone");
            var handleModel = Content.Load<Model>("Editor\\handle");
            _floorModel = Content.Load<Model>("Editor\\plane");
            _floorTexture = Content.Load<Texture2D>("Editor\\grid");
            var boneTexture = Content.Load<Texture2D>("Editor\\bone_texture");
            Skeleton.SetDebugModels(handleModel, boneModel, boneTexture);

            // create default skeleton
            CreateEmptySkeleton();
        }

        /// <summary>
        /// Are we currently interacting with UI?
        /// </summary>
        public static bool IsInteractingWithUI => ((UserInterface.Active.TargetEntity != null) && (UserInterface.Active.TargetEntity != UserInterface.Active.Root));

        // time until we advance animation step
        double _timeForNextAdvanceStep = 0;

        /// <summary>
        /// Update scene and camera controls.
        /// </summary>
        protected override void Update(GameTime gameTime)
        {
            // exit app
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            {
                ConfirmAndExit();
            }

            // play animation
            if (_playAnimation)
            {
                // play animation
                _timeForNextAdvanceStep -= gameTime.ElapsedGameTime.TotalMilliseconds;
                if (_timeForNextAdvanceStep <= 0)
                {
                    _timeForNextAdvanceStep = 10;
                    _animationTimeline.TimePosition++;
                    if (_animationTimeline.TimePosition > _animationTimeline.MaxDuration) { _animationTimeline.TimePosition = 0; }
                }

                // disable if default pose or no steps
                if (_animationSelection.SelectedIndex <= 0 || _animationTimeline.MaxDuration <= 0) 
                {
                    _animationPlayBtn.OnClick(_animationPlayBtn); 
                }
            }

            // enable / disable bones panel
            _bonesPanel.Enabled = !_playAnimation && (_animationSelection.SelectedIndex <= 0 || _animationTimeline.IsOnMark());
            _selectedBoneDisplay.Text = _bonesPanel.Enabled ? _selectedBone : "[Can't edit bones because the timeline doesn't point on a key frame]";
            _selectedBoneDisplay.FillColor = _bonesPanel.Enabled ? Color.White : Color.Orange;

            // enable / disable per-step panel
            var haveValidStep = (!_playAnimation && _animationSelection.SelectedIndex > 0 && _animationStepSelection.SelectedIndex >= 0);
            _animationStepPropsPanel.Enabled = haveValidStep;

            // update ui
            UserInterface.Active.Update(gameTime);

            // get mouse movement
            var mouseX = Mouse.GetState().X;
            var mouseY = Mouse.GetState().Y;
            var mouseMove = new Vector2(mouseX - _prevMousePos.X, mouseY - _prevMousePos.Y);
            _prevMousePos = new Vector2(mouseX, mouseY);

            // do dragging to change value
            if (_draggedInput != null)
            {
                if (Mouse.GetState().LeftButton == ButtonState.Pressed)
                {
                    if (_draggedTime > 0.15)
                    {
                        try
                        {
                            float curr = float.Parse(_draggedInput.Value);
                            _draggedInput.Value = MathF.Round((curr + mouseMove.X / 10f), 2).ToString();
                        }
                        catch
                        {
                            _draggedInput.Value = "0";
                        }
                        _draggedInput.OnValueChange(_draggedInput);
                    }
                    _draggedTime += (float)gameTime.ElapsedGameTime.TotalSeconds;
                }
                else
                {
                    _draggedInput = null;
                    _draggedTime = 0;
                }
            }


            // make sure main div in bounds
            foreach (var child in UserInterface.Active.Root.Children)
            {
                if (child.Draggable && child.Visible && (child is Panel))
                {
                    var rect = child.GetActualDestRect();
                    if (rect.Y < 55)
                    {
                        child.Offset = new Vector2(rect.X, 55);
                    }
                }
            }

            // skip if interacting with ui
            if (IsInteractingWithUI)
            {
                _prevMouseWheel = Mouse.GetState().ScrollWheelValue;
                return;
            }

            // do zooming
            var mouseWheel = Mouse.GetState().ScrollWheelValue - _prevMouseWheel;
            if (mouseWheel != 0)
            {
                _cameraDistance -= (float)MathF.Sign(mouseWheel) * 10f;
                if (_cameraDistance < 10) _cameraDistance = 10;
                if (_cameraDistance > 500) _cameraDistance = 500;
                _prevMouseWheel = Mouse.GetState().ScrollWheelValue;
            }

            // raycast to select bone
            if ((Mouse.GetState().LeftButton == ButtonState.Pressed) && (_draggedInput == null))
            {
                var animationId = SelectedAnimation;
                var animationOffset = AnimationOffset;
                var ray = _camera.RayFromMouse();
                var picked = _skeleton.PickBone(ray, Matrix.Identity, animationId, animationOffset);
                if (picked != null)
                {
                    _bonesList.SelectedIndex = _fullPathToBoneListIndex[picked];
                }
            }

            // do camera rotation
            if (Mouse.GetState().RightButton == ButtonState.Pressed)
            {
                var rotation = mouseMove;
                _cameraAngle.X += rotation.X * (float)gameTime.ElapsedGameTime.TotalSeconds * 3.5f;
                _cameraAngle.Y += rotation.Y * (float)gameTime.ElapsedGameTime.TotalSeconds * 1.5f;
            }

            // do camera offsetY change
            if (Mouse.GetState().MiddleButton == ButtonState.Pressed)
            {
                _cameraOffsetY += mouseMove.Y * (float)gameTime.ElapsedGameTime.TotalSeconds * 7.5f;
                if (_cameraOffsetY < -25) _cameraOffsetY = -25;
                if (_cameraOffsetY > 50) _cameraOffsetY = 50;
            }

            // validate camera angle
            if (_cameraAngle.Y < -MathF.PI) { _cameraAngle.Y = -MathF.PI; }
            if (_cameraAngle.Y > MathF.PI) { _cameraAngle.Y = MathF.PI; }

            // set camera position and lookat
            var cameraPositionXZFromRotation = new Vector2((float)Math.Cos(_cameraAngle.X), (float)Math.Sin(_cameraAngle.X));
            _camera.LookAt = new Vector3(0, _cameraOffsetY, 0);
            _camera.Position = new Vector3(
                -(cameraPositionXZFromRotation.X * _cameraDistance),
                _cameraOffsetY + (_cameraAngle.Y * _cameraDistance / 1.175f),
                (cameraPositionXZFromRotation.Y * _cameraDistance));
            _camera.FarClipPlane = _cameraDistance * 5;

            // update camera
            _camera.Update();

            base.Update(gameTime);
        }

        /// <summary>
        /// Get currently selected animation or null.
        /// </summary>
        string SelectedAnimation => _animationSelection.SelectedIndex == 0 ? null : _animationSelection.SelectedValue;

        /// <summary>
        /// Get currently selected animation timeline offset.
        /// </summary>
        float AnimationOffset => _animationTimeline.TimePosition / 100f;

        /// <summary>
        /// Draw scene.
        /// </summary>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            // draw floor
            var depthState = new DepthStencilState();
            depthState.DepthBufferEnable = true;
            depthState.DepthBufferWriteEnable = true;
            foreach (var mesh in _floorModel.Meshes)
            {
                foreach (BasicEffect effect in mesh.Effects)
                {
                    effect.LightingEnabled = _showLights;
                    effect.EnableDefaultLighting();
                    effect.SpecularColor = Color.Black.ToVector3();
                    effect.GraphicsDevice.DepthStencilState = depthState;
                    effect.View = _camera.View;
                    effect.Projection = _camera.Projection;
                    effect.Texture = _floorTexture;
                    effect.TextureEnabled = true;
                    effect.World = Matrix.CreateRotationX(-MathF.PI / 2) * Matrix.CreateScale(50f, 1f, 50f);
                }
                mesh.Draw();
            }

            // get animation and offset
            var animationId = SelectedAnimation;
            var animationOffset = AnimationOffset;

            // draw bones
            if (_showBones)
            {
                _skeleton.DebugDrawBones(_camera.View, _camera.Projection, Matrix.Identity, animationId, animationOffset, _showBonesOutline, _selectedBone, _showLights);
            }

            // draw bone handles
            if (_showBoneHandles)
            {
                _skeleton.DebugDrawBoneHandles(_camera.View, _camera.Projection, Matrix.Identity, animationId, animationOffset, 0.2f, Color.Teal, Color.Red, _selectedBone);
            }

            // draw ui and call base draw
            UserInterface.Active.Draw(_spriteBatch);
            base.Draw(gameTime);
        }
    }
}
