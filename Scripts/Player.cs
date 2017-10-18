using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace Pong
{
    public class Player
    {
        private PlayerIndex playerInex;
        private Keys upKey;
        private Keys downKey;
        private Slider slider;
        private int points;
        private Rectangle endZone;
        private bool moveUp;
        private bool moveDown;
        private bool prevMoveUp;
        private bool prevMoveDown;
        private KeyboardState prevState;

        private Socket connection;
        private ConnectionType connectionType;

        public event EventHandler OnLeft; 

        public int Points
        {
            get { return points; }
            set { points = value; }
        }

        public Slider Slider
        {
            get
            { return slider; }
        }

        public PlayerIndex Index
        {
            get
            { return playerInex; }
        }

        public Player(PlayerIndex playerIndex, ConnectionType connectionType, Socket connection, Vector2 position, Texture2D texture, Vector2 size, Keys upKey, Keys downKey, Rectangle endZone, float fieldMinY, float fieldMaxY)
        {
            this.playerInex = playerIndex;
            this.connection = connection;
            this.connectionType = connectionType;
            this.slider = new Slider(position, texture, size, fieldMinY, fieldMaxY);
            this.points = 0;
            this.upKey = upKey;
            this.downKey = downKey;
            this.endZone = endZone;
            prevState = Keyboard.GetState();

        }

        public void CUpdate(GameTime gameTime)
        {
            KeyboardState state = Keyboard.GetState();
            GamePadState padState = GamePad.GetState(playerInex);
            slider.ResetMoveVector();

            if (moveUp)
            {
                slider.Move(-1, 1);
            }
            if (moveDown)
            {
                slider.Move(1, 1);
            }

            if(playerInex == PlayerIndex.Two)
            {
                if (state.IsKeyDown(upKey) && !prevState.IsKeyDown(upKey))
                {
                    connection.Send(Encoding.ASCII.GetBytes("COMMAND UP"));
                }
                else if (!state.IsKeyDown(upKey) && prevState.IsKeyDown(upKey))
                {
                    connection.Send(Encoding.ASCII.GetBytes("COMMAND STOPUP"));
                }
                if (state.IsKeyDown(downKey) && !prevState.IsKeyDown(downKey))
                {
                    connection.Send(Encoding.ASCII.GetBytes("COMMAND DOWN"));
                }
                else if (!state.IsKeyDown(downKey) && prevState.IsKeyDown(downKey))
                {
                    connection.Send(Encoding.ASCII.GetBytes("COMMAND STOPDOWN"));
                }
                prevState = state;
            }

            slider.Update(gameTime);
        }

        public void SUpdate(GameTime gameTime)
        {
            KeyboardState state = Keyboard.GetState();
            GamePadState padState = GamePad.GetState(playerInex);
            slider.ResetMoveVector();

            if(playerInex == PlayerIndex.One)
            {
                if (state.IsKeyDown(upKey))
                {
                    moveUp = true;
                }
                else
                {
                    moveUp = false;
                }
                if (state.IsKeyDown(downKey))
                {
                    moveDown = true;
                }
                else
                {
                    moveDown = false;
                }
            }

            switch (playerInex)
            {
                case PlayerIndex.One:
                    if (moveUp)
                    {
                        slider.Move(-1, 1);
                    }
                    if (moveDown)
                    {
                        slider.Move(1, 1);
                    }
                    if ((moveDown && !prevMoveDown) || (moveUp && !prevMoveUp) || (!moveDown && prevMoveDown) || (!moveUp && prevMoveUp))
                    {
                        connection.Send(Encoding.ASCII.GetBytes("P1 " + slider.Position.Y.ToString() + " " + slider.MoveVector.Y.ToString()));
                    }
                    prevMoveUp = moveUp;
                    prevMoveDown = moveDown;
                    break;
                case PlayerIndex.Two:
                    if (moveUp)
                    {
                        slider.Move(-1, 1);
                    }
                    if (moveDown)
                    {
                        slider.Move(1, 1);
                    }
                    if((moveDown && !prevMoveDown) || (moveUp && !prevMoveUp) || (!moveDown && prevMoveDown) || (!moveUp && prevMoveUp))
                    {
                        connection.Send(Encoding.ASCII.GetBytes("P2 " + slider.Position.Y.ToString() + " " + slider.MoveVector.Y.ToString()));
                    }
                    prevMoveUp = moveUp;
                    prevMoveDown = moveDown;
                    break;
            }

            slider.Update(gameTime);
        }

        public void HandleData(string[] data)
        {
            if (data[0] == "P1")
            {
                if (playerInex == PlayerIndex.One)
                {
                    slider.Position = new Vector2(slider.Position.X, float.Parse(data[1]));
                    if (float.Parse(data[2]) > 0)
                    {
                        moveDown = true;
                        moveUp = false;
                    }
                    else if (float.Parse(data[2]) < 0)
                    {
                        moveUp = true;
                        moveDown = false;
                    }
                    else
                    {
                        moveUp = false;
                        moveDown = false;
                    }
                }
            }
            else if (data[0] == "P2")
            {
                if (playerInex == PlayerIndex.Two)
                {
                    slider.Position = new Vector2(slider.Position.X, float.Parse(data[1]));
                    if (float.Parse(data[2]) > 0)
                    {
                        moveDown = true;
                        moveUp = false;
                    }
                    else if (float.Parse(data[2]) < 0)
                    {
                        moveUp = true;
                        moveDown = false;
                    }
                    else
                    {
                        moveUp = false;
                        moveDown = false;
                    }
                }
            }
            else if (data[0] == "COMMAND")
            {
                if (playerInex == PlayerIndex.Two)
                {
                    switch (data[1])
                    {
                        case "UP":
                            moveUp = true;
                            break;
                        case "STOPUP":
                            moveUp = false;
                            break;
                        case "DOWN":
                            moveDown = true;
                            break;
                        case "STOPDOWN":
                            moveDown = false;
                            break;
                    }
                }
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            slider.Draw(spriteBatch);
        }

        /// <summary>
        /// Checks Collision between ball and player slider
        /// </summary>
        /// <param name="ball">Ball ball</param>
        public void Collision(Ball ball)
        {
            if (ball.CollisionBox.Intersects(slider.CollisionBox))
            {
                float width = ball.MoveVector.X > 0 ? -ball.CollisionBox.Width : ball.CollisionBox.Width; //prevents the ball from stucking in a slider

                ball.Position = new Vector2(slider.CollisionBox.X + width, ball.Position.Y);
                Vector2 sideVec = slider.GetSideVector(ball.CollisionBox);
                ball.ChangeMoveVector((ball.MoveVector * -1) + (slider.MoveVector / 2) + sideVec);
                ball.SendChange();
            }

            if (ball.CollisionBox.Intersects(endZone)) //ball out of field
            {
                OnLeft?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}
