using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Pong
{
    public class Slider
    {
        private const float speed = 2;

        private float fieldMinY;
        private float fieldMaxY;

        private Vector2 size;
        private Vector2 startPosition;
        private Vector2 position;
        private Texture2D texture;
        private Rectangle collisionBox;
        private Rectangle leftSide;
        private Rectangle rightSide;

        private Vector2 moveVector;

        public Rectangle CollisionBox
        {
            get { return collisionBox; }
            set { collisionBox = value; }
        }

        public Vector2 MoveVector
        {
            get { return moveVector; }
            set { moveVector = value; }
        }

        public Vector2 Position
        {
            get { return position; }
            set { position = value; }           
        }

        /// <summary>
        /// Reprasentation of the player slider
        /// </summary>
        /// <param name="position">Slider start position</param>
        /// <param name="texture">Slider texture</param>
        /// <param name="size">Slider size</param>
        public Slider(Vector2 position, Texture2D texture, Vector2 size, float fieldMinY, float fieldMaxY)
        {
            startPosition = position;
            this.fieldMinY = fieldMinY;
            this.fieldMaxY = fieldMaxY;
            this.position = position;
            this.texture = texture;
            this.size = size;
            ResetMoveVector();
            collisionBox = new Rectangle(Convert.ToInt32(position.X), Convert.ToInt32(position.Y), Convert.ToInt32(size.X), Convert.ToInt32(size.Y));
        }

        public void Update(GameTime gameTime)
        {
            position += moveVector * (float)gameTime.ElapsedGameTime.TotalMilliseconds;
            CheckSliderPosition();

            collisionBox = new Rectangle(Convert.ToInt32(position.X), Convert.ToInt32(position.Y), collisionBox.Width, collisionBox.Height);
        }

        /// <summary>
        /// Keeps the slider on screen
        /// </summary>
        private void CheckSliderPosition()
        {
            if (position.Y <= fieldMinY)
                position.Y = fieldMinY;
            else if (position.Y + size.Y > fieldMaxY)
                position.Y = fieldMaxY - size.Y;
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(texture, collisionBox, Color.White);
        } 

        /// <summary>
        /// Moves slider
        /// </summary>
        /// <param name="direction">Indicates the direction</param>
        /// <param name="multiplier">Manipulates the speed</param>
        public void Move(float direction, float multiplier)
        {
            moveVector += new Vector2(0, direction * Math.Abs(multiplier));
        }

        /// <summary>
        /// Resets moveVector
        /// </summary>
        public void ResetMoveVector()
        {
            moveVector = Vector2.Zero;
        }

        /// <summary>
        /// Resets position to the startposition
        /// </summary>
        public void ResetPosition()
        {
            position = startPosition;
            collisionBox = new Rectangle(Convert.ToInt32(position.X), Convert.ToInt32(position.Y), collisionBox.Width, collisionBox.Height);
        }

        public void OverwritePosition(Vector2 position)
        {
            this.position = position;
            collisionBox = new Rectangle(Convert.ToInt32(position.X), Convert.ToInt32(position.Y), collisionBox.Width, collisionBox.Height);
        }

        public Vector2 GetSideVector(Rectangle collisionBox)
        {
            leftSide = new Rectangle(Convert.ToInt32(position.X), Convert.ToInt32(position.Y), Convert.ToInt32(size.X), Convert.ToInt32(size.Y / 3f));
            rightSide = new Rectangle(Convert.ToInt32(position.X), Convert.ToInt32(position.Y + (size.Y / 1.5f)), Convert.ToInt32(size.X), Convert.ToInt32(size.Y / 3f));

            bool left = leftSide.Intersects(collisionBox);
            bool right = rightSide.Intersects(collisionBox);
  
            if(!(left && right))
            {
                if(left)
                {
                    return new Vector2(0, 0.3f);
                }
                
                if(right)
                {
                    return new Vector2(0, -0.3f);
                }
            }

            return new Vector2(0, 0);
        }
    }
}
