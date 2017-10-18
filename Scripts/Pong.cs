/* Author: Marcel Croonenbroeck
 * Date: 19.09.2017
 * Description: Just another pong game... :D
 */ 


using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Pong
{
    /// <summary>
    /// This is the main type for your game.
    /// </summary>
    public class Pong : Game
    {
        private const int port = 1337;

        private static Random random = new Random();
        private const int maxScore = 3;
        private const string startText = "[1] Start Server [2] Find Server [Esc] Close Game";

        private GraphicsDeviceManager graphics;
        private SpriteBatch spriteBatch;

        private Player[] players;
        private Ball ball;
        private Rectangle field;

        private Countdown countdown;
        private string drawText;

        private SpriteFont font;
        private SpriteFont boldFont;
        private Texture2D background;
        private Texture2D sliderTexture;
        private Texture2D ballTexture;
        private GameState state = GameState.None;
        private ConnectionType connectionType;
        private bool isConnected;
        private Socket connection;
        private Thread recievingThread;
        private KeyboardState prevKeyState;
        Socket listener;

        IPHostEntry ipHostInfo;
        IPAddress ipAddress;
        IPEndPoint localEndPoint;

        public Pong()
        {
            graphics = new GraphicsDeviceManager(this);

            //Change Screen resolution here
            graphics.PreferredBackBufferWidth = 800;
            graphics.PreferredBackBufferHeight = 600;
            graphics.IsFullScreen = false;
            graphics.ApplyChanges();

            IsMouseVisible = true;

            Content.RootDirectory = "Content";
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            // TODO: Add your initialization logic here
            ipHostInfo = Dns.Resolve(Dns.GetHostName());
            ipAddress = ipHostInfo.AddressList[0];
            localEndPoint = new IPEndPoint(ipAddress, port);
            listener = new Socket(AddressFamily.InterNetwork,
                SocketType.Stream, ProtocolType.Tcp);

            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content
        /// </summary>
        protected override void LoadContent()
        {           
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);
            font = Content.Load<SpriteFont>("font");
            ballTexture = Content.Load<Texture2D>("ball");
            sliderTexture = Content.Load<Texture2D>("slider");
            background = Content.Load<Texture2D>("background");
            boldFont = Content.Load<SpriteFont>("boldFont");

            field = new Rectangle(0, 0, graphics.PreferredBackBufferWidth, graphics.PreferredBackBufferHeight);
            countdown = new Countdown(new Vector2(graphics.PreferredBackBufferWidth / 2, 100), boldFont, 3000);
            countdown.OnCountdownElapsed += Countdown_OnCountdownElapsed;
            drawText = startText;
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            KeyboardState keyState = Keyboard.GetState();
            if (state == GameState.None)
            {
                if (keyState.IsKeyDown(Keys.Escape) && !prevKeyState.IsKeyDown(Keys.Escape))
                {
                    listener.Close();
                    listener.Dispose();
                    Exit();
                }
            }
            else
            {
                if (keyState.IsKeyDown(Keys.Escape) && !prevKeyState.IsKeyDown(Keys.Escape))
                {
                    if(recievingThread.IsAlive)
                    {
                        listener.Close();
                        listener.Dispose();
                    }

                    state = GameState.None;
                    if(isConnected)
                    {
                        isConnected = false;
                        connection.Shutdown(SocketShutdown.Both);
                        connection.Close();
                    }

                    countdown = new Countdown(new Vector2(graphics.PreferredBackBufferWidth / 2, 100), boldFont, 3000);
                    countdown.OnCountdownElapsed += Countdown_OnCountdownElapsed;

                    drawText = startText;
                }
            }

            prevKeyState = keyState;

            if (state != GameState.Started)
            {
                if (state == GameState.GameOver)
                {
                    if (keyState.IsKeyDown(Keys.Space))
                    {
                        drawText = startText;
                        state = GameState.None;
                        isConnected = false;
                        if (connection != null)
                        {
                            if (connection.Connected)
                            {
                                connection.Shutdown(SocketShutdown.Both);
                                connection.Close();
                            }
                        }

                        countdown = new Countdown(new Vector2(graphics.PreferredBackBufferWidth / 2, 100), boldFont, 3000);
                        countdown.OnCountdownElapsed += Countdown_OnCountdownElapsed;

                        if (connectionType == ConnectionType.Server)
                        {
                            listener = new Socket(AddressFamily.InterNetwork,
                                SocketType.Stream, ProtocolType.Tcp);
                        }
                    }
                }
                else if (state == GameState.CountDown)
                {
                    countdown.Update(gameTime);
                }
                else if (state != GameState.Searching)
                {
                    if (keyState.IsKeyDown(Keys.D1))
                    {
                        connectionType = ConnectionType.Server;
                        state = GameState.Searching;
                        drawText = "Searching for other Players";

                        recievingThread = new Thread(new ParameterizedThreadStart(CreateServer));
                        recievingThread.IsBackground = true;
                        recievingThread.Start(port);
                    }
                    else if(keyState.IsKeyDown(Keys.D2))
                    {
                        connectionType = ConnectionType.Client;
                        state = GameState.Searching;
                        drawText = "Searching for other Players";

                        recievingThread = new Thread(new ParameterizedThreadStart(FindServer));
                        recievingThread.IsBackground = true;
                        recievingThread.Start(port);
                    }
                    else if(keyState.IsKeyDown(Keys.D3))
                    {
                        listener.Close();
                        listener.Dispose();
                    }
                    //if (Keyboard.GetState().IsKeyDown(Keys.D1))
                    //{
                    //    state = GameState.Started;
                    //    SetupGame(ballTexture, sliderTexture);
                    //}
                }
                else if(state == GameState.Searching)
                {
                    if(isConnected)
                    {
                        state = GameState.Started;
                        SetupGame(ballTexture, sliderTexture);
                    }
                }
            }
            else
            {
                    GameUpdate(gameTime);
            }

            base.Update(gameTime);
        }

        /// <summary>
        /// Creates a Server which other Players can find
        /// </summary>
        /// <param name="obj">port for the Server</param>
        private void CreateServer(object obj)
        {
            try
            {
                listener = new Socket(AddressFamily.InterNetwork,
                SocketType.Stream, ProtocolType.Tcp);

                listener.Bind(localEndPoint);
                listener.Listen(10);

                connectionType = ConnectionType.Server;

                try
                {
                    connection = listener.Accept();

                    try
                    {
                        byte[] bytes = new byte[1024];
                        int bytesRec = connection.Receive(bytes);
                        string data = Encoding.ASCII.GetString(bytes, 0, bytesRec);
                        if (data == "Darkness")
                        {
                            connection.Send(Encoding.ASCII.GetBytes("Darkness"));
                            isConnected = true;
                        }
                        else
                        {
                            state = GameState.None;
                        }
                    }

                    catch
                    {
                        ConnectionError(connection);
                        Thread.CurrentThread.Abort();
                    }
                }
                catch
                {
                    ConnectionError(connection);
                }
            }
            catch
            {
                drawText = "There already is a Server";
                Thread.Sleep(3000);
                drawText = startText;
                state = GameState.None;
                isConnected = false;

                countdown = new Countdown(new Vector2(graphics.PreferredBackBufferWidth / 2, 100), boldFont, 3000);
                countdown.OnCountdownElapsed += Countdown_OnCountdownElapsed;

                Thread.CurrentThread.Abort();
            }
        }

        /// <summary>
        /// Used to Find existing Servers
        /// </summary>
        /// <param name="obj">port of the Server</param>
        private void FindServer(object obj)
        {

            connection = new Socket(AddressFamily.InterNetwork,
                SocketType.Stream, ProtocolType.Tcp);

            try
            {
                connection.Connect(localEndPoint);

                connection.Send(Encoding.ASCII.GetBytes("Darkness"));
                byte[] bytes = new byte[1024];
                int bytesRec = connection.Receive(bytes);
                string data = Encoding.ASCII.GetString(bytes, 0, bytesRec);
                if (data == "Darkness")
                {
                    connectionType = ConnectionType.Client;
                    isConnected = true;
                }
            }
            catch
            {
                ConnectionError(connection);
                Thread.CurrentThread.Abort();
            }

        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            spriteBatch.Begin();
            spriteBatch.Draw(background, field, Color.White);
            if (state == GameState.Started || state == GameState.CountDown)
            {
                for (int i = 0; i < players.Length; i++)
                {
                    players[i].Draw(spriteBatch);
                }

                ball.Draw(spriteBatch);

                if (state == GameState.CountDown)
                    countdown.Draw(spriteBatch);

                DrawScore();
            }
            else
            {
                DrawText();
            }
            spriteBatch.End();
            base.Draw(gameTime);
        }

        /// <summary>
        /// Update for core gameplay
        /// </summary>
        /// <param name="gameTime"></param>
        private void GameUpdate(GameTime gameTime)
        {
            ball.Update(gameTime);
            switch(connectionType)
            {
                case ConnectionType.Client:
                    for (int i = 0; i < players.Length; i++)
                    {
                        players[i].CUpdate(gameTime);
                        players[i].Collision(ball);
                    }
                    break;
                case ConnectionType.Server:
                    for (int i = 0; i < players.Length; i++)
                    {
                        players[i].SUpdate(gameTime);
                        players[i].Collision(ball);
                    }
                    break;
            }

            WallCollision(ball);
        }

        /// <summary>
        /// Checks if the ball hits one of the walls
        /// </summary>
        /// <param name="ball">Ball</param>
        private void WallCollision(Ball ball)
        {
            if (ball.CollisionBox.Y <= 0 ||
                ball.CollisionBox.Y >= graphics.PreferredBackBufferHeight)
            {
                float y = ball.CollisionBox.Y > 0 ? graphics.PreferredBackBufferHeight - ball.CollisionBox.Height : 0;
                ball.ChangeMoveVector(new Vector2(ball.MoveVector.X, ball.MoveVector.Y * -1));
                ball.Position = new Vector2(ball.Position.X, y);
            }
        }

        /// <summary>
        /// Checks player scores
        /// </summary>
        private void CheckScores()
        {
            for (int i = 0; i < players.Length; i++)
            {
                if(players[i].Points >= maxScore)
                {
                    drawText = "Player " + (i + 1).ToString() + " won (Press space to restart)!";
                    state = GameState.GameOver;
                    return;
                }
            }
        }

        /// <summary>
        /// Setup players and ball 
        /// </summary>
        /// <param name="ballTexture">Ball texture</param>
        /// <param name="sliderTexture">Slider texture</param>
        private void SetupGame(Texture2D ballTexture, Texture2D sliderTexture)
        {
            drawText = startText;
            Vector2 ballSize = new Vector2(ballTexture.Width, ballTexture.Height);
            Vector2 sliderSize = new Vector2(sliderTexture.Width, sliderTexture.Height);

            players = new Player[2];
            players[0] = new Player(PlayerIndex.One,
                connectionType,
                connection,
                new Vector2(0, (graphics.PreferredBackBufferHeight / 2) - (sliderSize.Y / 2)),
                sliderTexture,
                sliderSize,
                Keys.Up,
                Keys.Down,
                new Rectangle(-500 - Convert.ToInt32(ballSize.X), -200, 500, graphics.PreferredBackBufferHeight + 400),
                0,
                graphics.PreferredBackBufferHeight);

            players[1] = new Player(PlayerIndex.Two,
                connectionType,
                connection,
                new Vector2(graphics.PreferredBackBufferWidth - sliderSize.X, (graphics.PreferredBackBufferHeight / 2) - (sliderSize.Y / 2)),
                sliderTexture,
                sliderSize,
                Keys.Up,
                Keys.Down,
                new Rectangle(graphics.PreferredBackBufferWidth + Convert.ToInt32(ballSize.X), -200, 500, graphics.PreferredBackBufferHeight + 400),
                0,
                graphics.PreferredBackBufferHeight);

            players[0].OnLeft += Player_OnLeft;
            players[1].OnLeft += Player_OnLeft;

            bool isServer;
            if (connectionType == ConnectionType.Server)
                isServer = true;
            else
                isServer = false;

            ball = new Ball(connection, isServer, new Vector2((graphics.PreferredBackBufferWidth / 2) - (ballSize.X / 2), (graphics.PreferredBackBufferHeight / 2) - (ballSize.Y / 2)), ballTexture, ballSize, new Vector2(random.Next(0, 2) == 0 ? -1 : 1, 0), 1.05f);

            ResetPositions();

            recievingThread = new Thread(new ThreadStart(HandleConnection));
            recievingThread.IsBackground = true;
            recievingThread.Start();
        }

        private void HandleConnection()
        {
            byte[] bytes = new byte[1024];
            int byteLength;
            string[] data;
            try
            {
                while (state != GameState.GameOver)
                {
                    byteLength = connection.Receive(bytes);
                    data = Encoding.ASCII.GetString(bytes, 0, byteLength).Split(' ');
                    for (int i = 0; i < players.Length; i++)
                    {
                        players[i].HandleData(data);
                    }
                    ball.RecieveMSG(data);
                }
                connection.Shutdown(SocketShutdown.Both);
                connection.Close();
            }
            catch
            {
                ConnectionError(connection);
                Thread.CurrentThread.Abort();
            }
        }

        private void ConnectionError(Socket connection)
        {
            state = GameState.None;
            isConnected = false;
            if(connection != null)
            {
                if(connection.Connected)
                {
                    connection.Shutdown(SocketShutdown.Both);
                    connection.Close();
                }
            }

            countdown = new Countdown(new Vector2(graphics.PreferredBackBufferWidth / 2, 100), boldFont, 3000);
            countdown.OnCountdownElapsed += Countdown_OnCountdownElapsed;

            if (connectionType == ConnectionType.Server)
            {
                listener = new Socket(AddressFamily.InterNetwork,
                    SocketType.Stream, ProtocolType.Tcp);
            }
        }

        /// <summary>
        /// Resets the positions when a player scores a point
        /// </summary>
        private void ResetPositions()
        {
            state = GameState.CountDown;
            for (int i = 0; i < players.Length; i++)
            {
                players[i].Slider.ResetPosition();
            }

            ball.Reset(new Vector2(random.Next(0, 2) == 0 ? -1 : 1, 0));
        }

        /// <summary>
        /// Event raised when a player scores a point
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void Player_OnLeft(object sender, EventArgs e)
        {
            for (int i = 0; i < players.Length; i++)
            {
                if (players[i] != sender)
                {
                    players[i].Points++;
                    ResetPositions();
                }
            }

            CheckScores();
        }

        /// <summary>
        /// Event raised when countdowe elapsed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Countdown_OnCountdownElapsed(object sender, EventArgs e)
        {
            state = GameState.Started;
            if(connectionType == ConnectionType.Server)
                ball.SendChange();
        }

        /// <summary>
        /// Draws info text
        /// </summary>
        private void DrawText()
        {
            Vector2 length = font.MeasureString(drawText);
            Vector2 textPosition = new Vector2(graphics.PreferredBackBufferWidth / 2f - (length.X / 2f), graphics.PreferredBackBufferHeight / 2);
            spriteBatch.DrawString(font, drawText, textPosition, Color.White);
        }

        /// <summary>
        /// Draws player score
        /// </summary>
        private void DrawScore()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < players.Length; i++)
            {
                sb.Append("P");
                sb.Append((i + 1));
                sb.Append(":");
                sb.Append(players[i].Points);
                if (i + 1 < players.Length)
                    sb.Append(" | ");
            }

            string drawText = sb.ToString();
            Vector2 length = font.MeasureString(drawText);
            Vector2 textPosition = new Vector2(graphics.PreferredBackBufferWidth / 2f - (length.X / 2f), 20);
            spriteBatch.DrawString(font, drawText, textPosition, Color.White);
        }
    }
}