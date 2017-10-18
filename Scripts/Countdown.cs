using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Pong
{
    public class Countdown
    {
        private Vector2 position;
        private SpriteFont font;

        private float elapsedTime;
        private float duration;

        public event EventHandler OnCountdownElapsed;

        public Countdown(Vector2 position, SpriteFont font, float duration)
        {
            this.duration = duration;
            this.position = position;
            this.font = font;
        }

        public void Update(GameTime gameTime)
        {
            elapsedTime += (float)gameTime.ElapsedGameTime.TotalMilliseconds;
            if(elapsedTime >= duration)
            {
                elapsedTime = 0;
                OnCountdownElapsed?.Invoke(this, EventArgs.Empty);
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            string drawText = Math.Round((duration - elapsedTime) / 1000f).ToString();
            spriteBatch.DrawString(font, drawText, new Vector2(position.X - (font.MeasureString(drawText).X / 2f), position.Y), Color.White);
        }
    }
}
