using GeonBit.UI;
using GeonBit.UI.Entities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace MonoSkelly.Editor
{
    /// <summary>
    /// UI element to show animation timeline.
    /// </summary>
    internal class TimelineElement : Entity
    {
        // white rectangle texture.
        static Texture2D _whiteRectangle;

        /// <summary>
        /// Current position in timeline.
        /// </summary>
        public uint TimePosition
        {
            get { return _timePosition; }
            set { if (_timePosition != value) { _timePosition = value; if (!DisableValueChange) { DoOnValueChange(); } } }
        }
        uint _timePosition;

        /// <summary>
        /// If true, will not emit value change events.
        /// </summary>
        public bool DisableValueChange;

        /// <summary>
        /// Max duration.
        /// </summary>
        public uint MaxDuration
        {
            get { return _maxDuration; }
            set { if (_maxDuration != value) { _maxDuration = value; if (_timePosition > _maxDuration) { _timePosition = _maxDuration; } ; } }
        }
        uint _maxDuration;

        /// <summary>
        /// Marks to show on timeline.
        /// </summary>
        public uint[] Marks;

        /// <summary>
        /// Return if currently pointing on a mark.
        /// </summary>
        public bool IsOnMark()
        {
            if (Marks != null)
            {
                foreach (var mark in Marks)
                {
                    if (TimePosition == mark) { return true; }
                }
            }
            return false;
        }

        /// <summary>
        /// Get currently selected mark index based on timeline position.
        /// </summary>
        /// <returns></returns>
        public int GetSelectedMarkIndex()
        {
            var stepIndex = 0;
            foreach (var markOffset in Marks)
            {
                if (TimePosition < markOffset)
                {
                    return stepIndex <= 0 ? stepIndex : (stepIndex - 1);
                }
                stepIndex++;
            }
            return stepIndex - 1;
        }

        /// <summary>
        /// Draw the element.
        /// </summary>
        public override void Draw(SpriteBatch spriteBatch)
        {
            base.Draw(spriteBatch);
            try
            {
                // create white texture
                if (_whiteRectangle == null)
                {
                    _whiteRectangle = new Texture2D(spriteBatch.GraphicsDevice, 1, 1);
                    _whiteRectangle.SetData(new[] { Color.White });
                }

                spriteBatch.Begin();

                // background
                var destRect = GetActualDestRect();
                spriteBatch.Draw(_whiteRectangle, destRect, Color.Black);
                destRect.X += 1;
                destRect.Width -= 2;
                destRect.Y += 1;
                destRect.Height -= 2;
                spriteBatch.Draw(_whiteRectangle, destRect, new Color(0, 127, 248));

                // draw marks
                if (Marks != null)
                {
                    foreach (var mark in Marks)
                    {
                        var offset = (int)(((float)mark / (float)MaxDuration) * destRect.Width);
                        spriteBatch.Draw(_whiteRectangle, new Rectangle(destRect.X + offset, destRect.Y, 2, destRect.Height), new Color(0, 255, 255));
                    }
                }

                // draw position
                if (MaxDuration > 0)
                {
                    var offset = (int)(((float)TimePosition / (float)MaxDuration) * destRect.Width);
                    spriteBatch.Draw(_whiteRectangle, new Rectangle(destRect.X + offset, destRect.Y, 2, destRect.Height), Color.Red);
                }
            }
            catch
            {
            }

            spriteBatch.End();
        }

        /// <summary>
        /// Get actual dest rect.
        /// </summary>
        public override Rectangle GetActualDestRect()
        {
            var ret = base.GetActualDestRect();
            ret.Height = 20;
            return ret;
        }

        /// <summary>
        /// Do updates.
        /// </summary>
        public override void Update(ref Entity targetEntity, ref Entity dragTargetEntity, ref bool wasEventHandled, Point scrollVal)
        {
            base.Update(ref targetEntity, ref dragTargetEntity, ref wasEventHandled, scrollVal);
            if (MouseInput.MouseButtonDown() && (targetEntity == null || targetEntity == this))
            {
                var mousePos = MouseInput.MousePosition;
                var rect = GetActualDestRect();
                if ((mousePos.X >= rect.Left) && (mousePos.X <= rect.Right + 5) && (mousePos.Y >= rect.Top) && (mousePos.Y <= rect.Bottom))
                {
                    float offset = (float)(MouseInput.MousePosition.X - rect.X) / (float)(rect.Width - 5);
                    TimePosition = (uint)(offset * (float)MaxDuration);
                    if (TimePosition > MaxDuration) { TimePosition = MaxDuration; }
                    targetEntity = this;
                    dragTargetEntity = this;
                    wasEventHandled = true;
                }
            }
        }
    }
}
