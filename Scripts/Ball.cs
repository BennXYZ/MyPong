using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace Pong
{
    public class Ball
    {
        private const float speedCap = 3;
        private Vector2 startPosition;
        private Vector2 position;
        private Texture2D texture;
        private Rectangle collisionBox;
        private Vector2 moveVector;
        private float speedMultiplier;
        private Socket connection;

        public Rectangle CollisionBox
        {
            get { return collisionBox; }
            set { collisionBox = value; }
        }

        public Vector2 Position
        {
            get { return position; }
            set { position = value; }
        }

        public Vector2 MoveVector
        {
            get { return moveVector; }
            set { moveVector = value; }
        }

        public Ball(Socket connection, bool isServer , Vector2 startPosition, Texture2D texture, Vector2 size, Vector2 startVector, float speedMultiplier)
        {
            this.connection = connection;
            this.speedMultiplier = speedMultiplier;
            this.startPosition = startPosition;
            position = startPosition;
            moveVector = startVector;
            this.texture = texture;
            collisionBox = new Rectangle(Convert.ToInt32(position.X), Convert.ToInt32(position.Y), Convert.ToInt32(size.X), Convert.ToInt32(size.Y));
        }

        public void Update(GameTime gameTime)
        {
            position += moveVector * (float)gameTime.ElapsedGameTime.TotalMilliseconds;
            collisionBox = new Rectangle(Convert.ToInt32(position.X), Convert.ToInt32(position.Y), collisionBox.Width, collisionBox.Height);
        }
        
        public void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(texture, collisionBox, Color.White);
        }

        /// <summary>
        /// Set moveVector and multiplies it by the speedMultiplier
        /// </summary>
        /// <param name="moveVector">New moveVector</param>
        public void ChangeMoveVector(Vector2 moveVector)
        {
            this.moveVector = moveVector * speedMultiplier;

            if (this.moveVector.X >= speedCap)
                this.moveVector = new Vector2(speedCap, this.moveVector.Y);
            if (moveVector.Y >= speedCap)
                this.moveVector = new Vector2(this.moveVector.X, speedCap);
        }

        /// <summary>
        /// Resets position and moveVector
        /// </summary>
        /// <param name="moveVector">New moveVector</param>
        public void Reset(Vector2 moveVector)
        {
            position = startPosition;
            collisionBox = new Rectangle(Convert.ToInt32(position.X), Convert.ToInt32(position.Y), collisionBox.Width, collisionBox.Height);
            this.moveVector = moveVector;
        }

        public void OverwritePosition(Vector2 position, Vector2 moveVector)
        {
            this.position = position;
            this.moveVector = moveVector;
            collisionBox = new Rectangle(Convert.ToInt32(position.X), Convert.ToInt32(position.Y), collisionBox.Width, collisionBox.Height);
        }

        public void RecieveMSG(string[] data)
        {
            if(data[0] == "BALL")
            {
                position.X = float.Parse(data[1]);
                position.Y = float.Parse(data[2]);
                moveVector.X = float.Parse(data[3]);
                moveVector.Y = float.Parse(data[4]);
                collisionBox = new Rectangle(Convert.ToInt32(position.X), Convert.ToInt32(position.Y), collisionBox.Width, collisionBox.Height);
            }
        }

        public void SendChange()
        {
            string msg = "BALL " + position.X.ToString() + " " + position.Y.ToString() + " " + moveVector.X.ToString() + " " + moveVector.Y.ToString();
            connection.Send(Encoding.ASCII.GetBytes(msg));
        }
    }
}
