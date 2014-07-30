using System;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Audio;
using Teeter.MyClasses;


namespace Teeter
{
    class GameplayScreen : GameScreen
    {

        #region Fields

        ContentManager content;
        Vector2 playerPosition = new Vector2(100, 100);
        Vector2 enemyPosition = new Vector2(100, 100);
        Random random = new Random();

        SpriteFont spriteFont;
        SpriteBatch spriteBatch;
        GraphicsDevice device;
        VertexDeclaration myVertexDeclaration;
        BasicEffect floorEffect;
        BasicEffect holesEffect;
        BasicEffect winningHoleEffect;
        SoundEffect collisionSound;
        SoundEffect holeSound;
        SoundEffect winningHoleSound;
        SoundEffect finishGameSound;
        Texture2D floorTexture;
        Texture2D holeTexture;
        Texture2D winningHoleTexture;
        Model ballModel;
        Matrix tableProjectionMatrix;
        Matrix tableViewMatrix;
        Matrix tableWorldMatrix;
        Matrix tableTranslation;
        Matrix ballWorldMatrix;
        Quaternion tableRotation = Quaternion.Identity;
        VertexBuffer floorVerticesBuffer;
        VertexBuffer holesVerticesBuffer;
        VertexBuffer winningHoleVerticesBuffer;
        VertexBuffer wallsVerticesBuffer;
        VertexBuffer blocksVerticesBuffer;
        bool drawBlocksFlag;
        int[,] floorPlan;
        int width;
        int length;
        static internal float thickness = 1;
        static internal float height = 0.5f;
        float ballRadius = 0.5f;
        Vector3 previousBallPosition;
        Vector3 ballPosition;
        float ballScale = 0.01f;
        float ballBoundingSphereScale = 8 / 10f;
        float timeX = 0;
        float speed0X = 0;
        float previousSpeedX;
        float speedX;
        float distance0X;
        float previousDistanceX;
        float distanceX;
        float timeZ = 0;
        float speed0Z = 0;
        float previousSpeedZ;
        float speedZ;
        float distance0Z;
        float previousDistanceZ;
        float distanceZ;
        static internal float gravity = 9.8f;
        float leftRightRot = 0;
        float upDownRot = 0;
        int updateFrequency = 60;
        static internal float maxRot = MathHelper.Pi / 12.0f;
        BoundingSphere previousBallBoundingSphere;
        BoundingSphere ballBoundingSphere;
        List<Vector3> holesList;
        List<BoundingBox> blocksList;
        BoundingBox tableBoundingBox;
        bool pauseGame = false;
        bool playUpDownCollisionSound = true;
        bool playLeftRightCollisionSound = true;
        static internal float frictionFactor = 0.1f;
        float frictionDirectionX = -1;
        float frictionDirectionZ = -1;
        static internal float mass = 1f;
        Vector3 WinningHoleCenter;
        Vector3 cameraPosition;
        Vector3 cameraTarget;
        Vector3 cameraUpVector;
        float cameraFieldOfView;
        float cameraMaxFieldOfView;
        float cameraMinFieldOfView;
        static internal String tableMaterial = "Wood";
        static internal string gameType = "Holes";
        bool escapePressed=false;
        List<int[,]> holesLevels;
        List<int[,]> mazeLevels;
        int CurrentLevelID =0;
        int levelsCount;
        float time=0;


        #endregion

        public GameplayScreen()
        {
            TransitionOnTime = TimeSpan.FromSeconds(1.5);
            TransitionOffTime = TimeSpan.FromSeconds(0.5);
            

        }

        public override void LoadContent()
        {
            if (content == null)
                content = new ContentManager(ScreenManager.Game.Services, "Content");

                        
            device = ScreenManager.GraphicsDevice;
            spriteFont = content.Load<SpriteFont>("MyContent/Font");
            spriteBatch = ScreenManager.SpriteBatch;
            myVertexDeclaration = new VertexDeclaration(device, VertexPositionNormalTexture.VertexElements);
            floorTexture = content.Load<Texture2D>("MyContent/FloorTexture_" + tableMaterial);
            holeTexture = content.Load<Texture2D>("MyContent/HoleTexture_" + tableMaterial);
            winningHoleTexture = content.Load<Texture2D>("MyContent/winningHoleTexture_" + tableMaterial);
            ballModel = content.Load<Model>("MyContent/BallModel");
            collisionSound = content.Load<SoundEffect>("MyContent/CollisionSound");
            holeSound = content.Load<SoundEffect>("MyContent/HoleSound");
            winningHoleSound = content.Load<SoundEffect>("MyContent/WinningHoleSound");
            finishGameSound = content.Load<SoundEffect>("MyContent/FinishGameSound");
            SetUpHolesLevels();
            SetUpMazeLevels();
            SetUpFloorPlan();

            Thread.Sleep(1000);
            ScreenManager.Game.ResetElapsedTime();
        }

        public override void UnloadContent()
        {
            content.Unload();
        }
   
        public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)                                                   
        {
            base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);
            ProcessKeyboard(gameTime);
            if (!escapePressed)
            {
                SetUpCamera();
                if (!pauseGame)
                {
                    time += 1.0f / updateFrequency;
                    UpdateBall();
                }
            }
        }

        public override void HandleInput(InputState input)
        {
            if (input == null)
                throw new ArgumentNullException("input");

            // Look up inputs for the active player profile.
            int playerIndex = (int)ControllingPlayer.Value;

            KeyboardState keyboardState = input.CurrentKeyboardStates[playerIndex];
            GamePadState gamePadState = input.CurrentGamePadStates[playerIndex];

            // The game pauses either if the user presses the pause button, or if
            // they unplug the active gamepad. This requires us to keep track of
            // whether a gamepad was ever plugged in, because we don't want to pause
            // on PC if they are playing with a keyboard and have no gamepad at all!
            bool gamePadDisconnected = !gamePadState.IsConnected &&
                                       input.GamePadWasConnected[playerIndex];
            escapePressed = input.IsPauseGame(ControllingPlayer);
            if (input.IsPauseGame(ControllingPlayer) || gamePadDisconnected)
            {
                ScreenManager.AddScreen(new PauseMenuScreen(), ControllingPlayer);
            }
            else
            {
                // Otherwise move the player position.
                Vector2 movement = Vector2.Zero;

                if (keyboardState.IsKeyDown(Keys.Left))
                    movement.X--;

                if (keyboardState.IsKeyDown(Keys.Right))
                    movement.X++;

                if (keyboardState.IsKeyDown(Keys.Up))
                    movement.Y--;

                if (keyboardState.IsKeyDown(Keys.Down))
                    movement.Y++;

                Vector2 thumbstick = gamePadState.ThumbSticks.Left;

                movement.X += thumbstick.X;
                movement.Y -= thumbstick.Y;

                if (movement.Length() > 1)
                    movement.Normalize();

                playerPosition += movement * 2;
            }
        }

        public override void Draw(GameTime gameTime)
        {
            ScreenManager.GraphicsDevice.Clear(ClearOptions.Target, Color.CornflowerBlue, 0, 0);
           

            
            DrawReslutsBoard();
            SetUpDrawTable();
            DrawFloor();
            DrawWalls();
            DrawBlocks();
            DrawBall();

            // If the game is transitioning on or off, fade it out to black.
            if (TransitionPosition > 0)
                ScreenManager.FadeBackBufferToBlack(255 - TransitionAlpha);
        }


        #region My Methods

        private void InitialBallPosition()
        {

            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < length; j++)
                {
                    if (floorPlan[i, j] == -2)
                    {
                        Vector3 pos = new Vector3(j + 0.5f, thickness + ballRadius, -i - 0.5f);
                        ballPosition = Vector3.Transform(pos, tableTranslation);
                        ballBoundingSphere = new BoundingSphere(ballPosition, ballBoundingSphereScale * ballRadius);
                    }
                }
            }
            distance0X = ballPosition.X;
            distance0Z = ballPosition.Z;
            distanceX = ballPosition.X;
            distanceZ = ballPosition.Z;
        }

        private void InitialCameraPosition()
        {
            cameraPosition = new Vector3(0, 15, 15);
            cameraTarget = new Vector3(0, 8, 0);
            cameraUpVector = new Vector3(0, 1, 0);
            cameraFieldOfView = MathHelper.PiOver2;
            cameraMinFieldOfView = MathHelper.Pi / 3f;
            cameraMaxFieldOfView = MathHelper.Pi * 2 / 3f;
        }

        private void SetUpHolesLevels()
        {
            holesLevels = new List<int[,]>();
            int[,] level_temp;

            //----------------------------------------------------------------//
            level_temp = new int[,]
            {
		        {1,1,1,1,1,1,1,1,1,2,1,1,1,1,1,1,1,0},
                {1,1,1,0,1,1,0,1,1,2,1,1,1,1,1,1,1,1},
                {1,1,1,1,2,1,1,1,1,2,1,1,1,1,2,1,1,1},
                {1,1,1,1,2,1,1,1,1,2,1,1,1,1,2,1,1,1},
                {1,1,1,1,2,1,1,1,1,2,1,1,1,1,2,1,1,1},
                {1,1,1,1,2,1,1,1,1,2,1,1,1,1,2,1,1,1},
                {1,1,1,1,2,1,1,1,1,2,1,1,1,1,2,1,1,1},
                {1,1,1,1,2,1,1,1,1,2,1,1,1,1,2,1,1,1},
                {1,1,1,1,2,1,1,1,1,2,1,1,1,1,2,1,1,1},
                {1,1,1,1,2,1,1,1,1,2,1,1,1,1,2,1,1,1},
                {1,1,1,1,2,1,1,1,1,2,1,1,1,1,2,1,1,1},
                {1,1,1,1,2,1,1,1,1,2,1,1,1,1,2,1,1,1},
                {1,1,1,1,2,1,1,1,1,2,1,1,1,1,2,1,1,1},
                {1,1,1,1,2,1,1,1,1,1,1,0,1,1,2,1,1,1},
                {-2,1,1,1,2,1,0,1,1,1,1,1,1,1,2,1,1,-1}

            };
            holesLevels.Add(level_temp);
            //----------------------------------------------------------------//
            level_temp = new int[,]
            {
		        {-2,1,1,2,1,1,1,1,1,1,1,2,1,1,-1},
                {1,1,1,2,1,1,1,1,1,1,1,2,1,1,1},
                {1,1,1,2,0,1,1,1,1,1,1,2,1,1,1},
                {1,1,1,2,1,1,1,2,1,1,1,2,1,1,1},
                {1,1,1,2,1,1,1,2,1,1,1,2,1,1,1},
                {1,0,1,2,1,1,1,2,1,1,1,2,1,1,1},
                {1,1,1,2,1,1,1,2,1,1,1,2,1,1,1},
                {1,0,1,2,1,1,1,2,1,1,1,2,0,1,1},
                {1,1,1,1,1,1,1,2,1,1,1,1,1,1,1},
                {1,1,1,1,1,1,1,2,1,1,1,1,1,1,1},
                {1,1,1,1,1,1,1,2,1,1,1,1,1,1,1},
                {1,1,1,1,1,1,1,2,1,1,1,1,1,1,1}

            };
            holesLevels.Add(level_temp);
            //----------------------------------------------------------------//
            level_temp = new int[,]
            {

		        {1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,0},
                {1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,0},
                {1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,0},
                {1,1,1,2,1,1,1,1,1,1,1,1,1,1,1,0},
                {1,1,1,2,1,1,1,1,1,1,1,1,1,1,1,0},
                {1,1,1,2,1,1,1,1,1,1,1,1,1,1,1,0},
                {1,1,1,2,1,1,1,0,0,0,1,1,1,1,1,0},
                {-2,1,1,2,1,1,1,0,-1,1,1,1,1,1,1,0},
                {1,1,1,2,1,1,1,0,0,0,1,1,1,1,1,0},
                {1,1,1,2,1,1,1,1,1,1,1,1,1,1,1,0},
                {1,1,1,2,1,1,1,1,1,1,1,1,1,1,1,0},
                {1,1,1,2,1,1,1,1,1,1,1,1,1,1,1,0},
                {1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,0},
                {1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,0},
	            {1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,0}

            };
            holesLevels.Add(level_temp);
            //----------------------------------------------------------------//
            level_temp = new int[,]
            {
		        {0,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1},
                {1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1},
                {1,1,1,1,1,1,1,1,1,1,1,1,1,1,0,1,1},
                {1,1,1,1,2,2,2,2,2,2,2,2,2,2,2,1,1},
                {1,1,1,1,2,0,1,1,1,1,1,1,1,0,2,1,1},
                {1,1,1,1,2,1,1,1,1,1,1,1,1,1,2,1,1},
                {1,1,1,1,2,1,1,1,1,2,2,2,1,1,2,1,1},
                {1,1,1,1,2,1,1,1,1,2,-1,1,1,1,2,1,1},
                {1,1,1,1,2,1,1,1,1,2,1,1,1,1,2,1,1},
                {1,1,1,1,2,1,1,1,1,2,1,1,1,1,2,1,1},
                {1,1,1,1,2,1,1,1,1,2,2,2,2,2,2,1,0},
                {1,1,1,1,2,1,1,1,1,1,1,1,1,1,0,1,1},
                {1,1,1,1,2,1,1,1,1,1,1,1,1,1,1,1,1},
                {-2,1,1,1,2,1,1,0,1,1,1,1,1,1,1,1,1}

            };
            holesLevels.Add(level_temp);
            //----------------------------------------------------------------//
            level_temp = new int[,]
            {
		        {-2,1,1,0,1,1,1,1,1,1,1,1,1,1,1,0},
                {1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1},
                {1,1,1,1,1,1,1,1,0,1,1,1,1,1,1,1},
                {2,2,2,2,2,2,2,2,2,2,2,2,2,1,1,1},
                {1,1,1,1,1,1,1,0,1,1,1,1,2,1,1,1},
                {1,1,1,1,1,1,1,1,1,1,1,1,2,1,1,1},
                {1,1,1,1,0,1,1,1,1,1,1,1,2,0,1,1},
                {1,1,0,2,1,1,1,1,1,1,1,0,1,1,1,1},
                {1,1,1,2,1,1,1,1,1,1,1,1,1,1,1,1},
                {1,1,1,2,1,1,1,1,0,1,1,1,1,1,1,1},
                {1,1,1,2,2,2,2,2,2,2,2,2,2,2,2,2},
                {1,1,1,1,1,1,1,0,1,1,1,1,1,1,1,1},
                {1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1},
                {0,1,1,1,1,1,1,1,1,1,1,1,0,1,1,-1}

            };
            holesLevels.Add(level_temp);
            //----------------------------------------------------------------//
            level_temp = new int[,]
            {
		        {-1,1,1,0,1,1,1,1,1,1,1,1,1,1,1,0},
                {1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1},
                {1,1,1,1,1,1,1,1,0,1,1,1,1,1,1,1},
                {2,2,2,2,2,2,2,2,2,2,2,2,2,1,1,1},
                {1,1,1,1,1,1,1,0,1,1,1,1,2,1,1,1},
                {1,1,1,1,1,1,1,1,1,1,1,1,2,1,1,1},
                {1,1,1,1,0,1,1,1,1,1,1,1,2,0,1,1},
                {1,1,0,2,1,1,1,1,1,1,1,0,1,1,1,1},
                {1,1,1,2,1,1,1,1,1,1,1,1,1,1,1,1},
                {1,1,1,2,1,1,1,1,0,1,1,1,1,1,1,1},
                {1,1,1,2,2,2,2,2,2,2,2,2,2,2,2,2},
                {1,1,1,1,1,1,1,0,1,1,1,1,1,1,1,1},
                {1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1},
                {0,1,1,1,1,1,1,1,1,1,1,1,0,1,1,-2}

            };
            holesLevels.Add(level_temp);
            //----------------------------------------------------------------//
            level_temp = new int[,]
            {
		        {1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,0},
                {1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1},
                {1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1},
                {1,1,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,1,1},
                {1,1,1,2,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,0},
                {0,1,1,2,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,0},
                {0,1,1,2,1,1,2,2,2,2,2,2,2,2,2,2,2,2,2,2},
                {1,1,1,2,1,1,2,-1,1,1,1,1,1,1,1,1,1,1,1,0},
                {1,2,2,2,1,1,2,2,2,2,2,2,2,2,2,1,1,1,1,1},
                {1,1,0,2,1,1,1,1,-2,2,0,1,1,1,1,0,1,1,1,1},
                {1,1,1,2,0,1,1,1,1,2,1,1,1,1,1,1,1,1,1,1},
                {1,1,1,2,2,2,2,2,2,2,1,1,2,1,1,1,1,0,1,1},
                {0,1,1,1,1,1,1,1,1,1,1,1,2,1,1,1,1,1,1,1},
                {0,1,1,1,1,1,1,1,1,1,1,1,2,0,1,0,1,1,1,1} 

            };
            holesLevels.Add(level_temp);
            //----------------------------------------------------------------//
            level_temp = new int[,]
            {
		        {1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,0},
                {1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1},
                {1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1},
                {1,1,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,1,1},
                {1,1,1,2,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,0},
                {0,1,1,2,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,0},
                {0,1,1,2,1,1,2,2,2,2,2,2,2,2,2,2,2,2,2,2},
                {1,1,1,2,1,1,2,-2,1,1,1,1,1,1,1,1,1,1,1,0},
                {1,2,2,2,1,1,2,2,2,2,2,2,2,2,2,1,1,1,1,1},
                {1,1,0,2,1,1,1,1,-1,2,0,1,1,1,1,0,1,1,1,1},
                {1,1,1,2,0,1,1,1,1,2,1,1,1,1,1,1,1,1,1,1},
                {1,1,1,2,2,2,2,2,2,2,1,1,2,1,1,1,1,0,1,1},
                {0,1,1,1,1,1,1,1,1,1,1,1,2,1,1,1,1,1,1,1},
                {0,1,1,1,1,1,1,1,1,1,1,1,2,0,1,0,1,1,1,1}      

            };
            holesLevels.Add(level_temp);
            //----------------------------------------------------------------//
            level_temp = new int[,]
            {
		        {-2,1,1,1,1,1,1,1,1,1,0,1,1,1,1,1,1,1,1,1},
                {1,1,1,1,1,1,1,1,1,0,1,0,1,1,1,1,1,1,1,1},
                {1,1,1,1,1,1,1,1,0,1,1,1,0,1,1,1,1,1,1,1},
                {1,1,1,1,1,1,1,0,1,1,0,1,1,0,1,1,1,1,1,1},
                {1,1,1,1,1,1,0,1,1,0,1,0,1,1,0,1,1,1,1,1},
                {1,1,1,1,1,0,1,1,0,1,1,1,1,1,1,0,1,1,1,1},
                {1,1,1,1,0,1,1,0,1,1,1,1,1,0,1,1,0,1,1,1},
                {1,1,1,0,1,1,0,1,1,1,-1,1,1,1,0,1,1,0,1,1},
                {1,1,1,1,0,1,1,0,1,1,1,1,1,0,1,1,0,1,1,1},
                {1,1,1,1,1,0,1,1,0,1,1,1,0,1,1,0,1,1,1,1},
                {1,1,1,1,1,1,1,1,1,0,1,0,1,1,0,1,1,1,1,1},
                {1,1,1,1,1,1,1,0,1,1,0,1,1,0,1,1,1,1,1,1},
                {1,1,1,1,1,1,1,1,0,1,1,1,0,1,1,1,1,1,1,1},
                {1,1,1,1,1,1,1,1,1,0,1,0,1,1,1,1,1,1,1,1},
                {1,1,1,1,1,1,1,1,1,1,0,1,1,1,1,1,1,1,1,1} 

            };
            holesLevels.Add(level_temp);
            //----------------------------------------------------------------//
            level_temp = new int[,]
            {
		        {-2,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,0},
                {1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,0},
                {0,1,1,1,1,1,1,1,0,1,1,1,1,1,1,1,1,1,1,1},
                {2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,1,1},
                {0,1,1,1,1,1,1,1,1,1,1,1,0,2,0,1,1,1,1,1},
                {1,1,1,1,1,1,1,1,1,1,1,1,1,2,1,1,1,1,1,1},
                {1,1,2,1,1,1,1,1,1,1,1,1,1,2,1,2,2,2,2,1},
                {0,1,2,0,1,1,1,1,1,1,1,1,1,2,1,2,1,1,1,1},
                {0,1,2,0,1,1,1,1,1,1,1,1,1,2,1,2,1,1,0,0},
                {1,1,2,0,0,0,1,1,1,1,1,1,1,2,1,2,1,1,0,0},
                {1,0,2,2,2,2,2,2,0,1,1,1,1,2,1,2,1,1,0,0},
                {1,0,2,-1,2,0,0,2,0,1,1,1,1,2,1,2,1,1,1,1},
                {1,1,2,1,2,1,1,2,2,1,1,1,1,2,1,2,2,2,2,1},
                {0,1,2,1,2,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1},
                {0,1,1,1,2,1,1,0,0,1,1,1,1,1,0,0,1,1,1,1}

            };
            holesLevels.Add(level_temp);
            //----------------------------------------------------------------//
            level_temp = new int[,]
            {    
		        {-1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,0},
                {1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,0},
                {0,1,1,1,1,1,1,1,0,1,1,1,1,1,1,1,1,1,1,1},
                {2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,0,1},
                {0,1,1,1,1,1,1,1,1,1,1,1,0,2,0,1,1,1,1,1},
                {1,1,1,1,1,1,1,1,1,1,1,1,1,2,0,1,1,1,1,1},
                {1,1,2,1,1,1,1,1,1,1,1,1,1,2,2,2,2,2,2,1},
                {0,1,2,0,1,1,1,1,1,1,1,1,1,2,2,2,1,1,1,1},
                {0,1,2,0,1,1,1,1,1,1,1,1,1,2,2,2,1,1,0,0},
                {1,1,2,0,0,0,1,1,1,1,1,1,1,2,2,2,1,1,0,0},
                {1,0,2,2,2,2,2,2,0,1,1,1,1,2,2,2,1,1,0,0},
                {1,0,2,-2,2,0,0,2,0,1,1,1,1,2,2,2,1,1,1,1},
                {1,1,2,1,2,1,1,2,2,1,1,1,1,2,2,2,2,2,2,1},
                {0,1,2,1,2,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1},
                {0,1,1,1,2,1,1,0,0,1,1,1,1,1,0,0,1,1,1,1}          

            };
            holesLevels.Add(level_temp);
            //----------------------------------------------------------------//
        }

        private void SetUpMazeLevels()
        {
            mazeLevels = new List<int[,]>();
            int[,] level_temp;

            //----------------------------------------------------------------//
            level_temp = new int[,]
            {
                {-2,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1},
                {1,1,1,1,1,1,2,2,2,2,2,2,2,2,1,1,2,2,1,1,2,2,2,2,2,2,2,2,2,2,2,2,1,1},
                {1,1,1,1,1,1,2,2,1,1,1,1,2,2,1,1,2,2,1,1,2,2,1,1,1,1,1,1,1,1,1,1,1,1},
                {1,1,2,2,1,1,2,2,1,1,2,2,2,2,1,1,2,2,1,1,2,2,2,2,2,2,2,2,2,2,2,2,2,2},
                {1,1,2,2,1,1,2,2,1,1,1,1,2,2,1,1,2,2,1,1,2,2,1,1,1,1,1,1,1,1,1,1,1,1},
                {1,1,2,2,1,1,2,2,2,2,1,1,2,2,1,1,2,2,1,1,2,2,1,1,2,2,2,2,2,2,2,2,1,1},
                {1,1,2,2,1,1,1,1,1,1,1,1,2,2,1,1,2,2,1,1,2,2,1,1,2,2,1,1,1,1,2,2,1,1},
                {1,1,2,2,2,2,2,2,2,2,2,2,2,2,1,1,2,2,1,1,2,2,1,1,2,2,1,2,2,1,2,2,1,1},
                {1,1,2,2,1,1,1,1,1,1,1,1,1,1,1,1,2,2,1,1,2,2,1,1,2,2,1,2,2,2,2,2,1,1},
                {1,1,2,2,1,1,2,2,2,2,2,2,2,2,2,2,2,2,1,1,2,2,1,1,2,2,1,2,2,-1,1,1,1,1},
                {1,1,2,2,1,1,2,2,1,1,1,1,1,1,1,1,1,1,1,1,2,2,1,1,2,2,1,2,2,1,1,1,1,1},
                {1,1,2,2,1,1,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,1,1,2,2,1,2,2,1,1,1,1,1},
                {1,1,2,2,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,2,2,1,1,1,1,1},
                {1,1,2,2,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,2,2,1,1,1,1,1}

            };
            mazeLevels.Add(level_temp);
            //----------------------------------------------------------------//
            level_temp = new int[,]
            {
                {-2,1,1,1,1,1,2,2,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,2,2,1,1,1,1,1,1},
                {1,1,1,1,1,1,2,2,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,2,2,1,1,1,2,2,1},
                {1,1,1,1,1,1,2,2,1,1,2,2,1,1,2,2,2,2,2,2,2,2,1,1,1,1,2,2,1,1,1,2,2,1},
                {1,1,2,2,1,1,2,2,1,1,2,2,1,1,2,2,1,1,1,1,1,1,1,1,1,1,2,2,1,1,1,2,2,1},
                {1,1,2,2,1,1,2,2,1,1,2,2,1,1,2,2,1,1,1,1,1,1,1,1,1,1,2,2,1,1,1,2,2,1},
                {1,1,2,2,1,1,2,2,1,1,2,2,1,1,2,2,2,2,2,2,2,2,2,2,1,1,2,2,1,1,1,2,2,1},
                {1,1,2,2,1,1,2,2,1,1,2,2,1,1,1,1,1,1,1,1,1,1,2,2,1,1,2,2,1,1,1,2,2,1},
                {1,1,2,2,1,1,2,2,1,1,2,2,2,2,2,2,2,2,2,2,1,1,2,2,1,1,2,2,1,1,1,2,2,1},
                {1,1,2,2,1,1,1,1,1,1,2,2,1,1,1,1,1,1,1,1,1,1,2,2,1,1,2,2,1,1,1,2,2,1},
                {1,1,2,2,2,2,2,2,2,2,2,2,1,1,2,2,2,2,2,2,1,1,2,2,1,1,2,2,1,1,1,2,2,1},
                {1,1,1,1,1,1,1,1,1,1,2,2,1,1,2,2,1,1,1,1,1,1,2,2,1,1,2,2,1,1,1,2,2,1},
                {2,2,2,2,2,2,1,1,1,1,2,2,1,1,2,2,1,1,2,2,2,2,2,2,1,1,2,2,1,1,1,2,2,1},
                {1,1,1,1,1,1,1,1,1,1,2,2,1,1,2,2,1,1,2,2,2,2,2,2,2,2,2,2,1,1,1,2,2,1},
                {1,1,1,1,1,1,1,1,1,1,2,2,1,1,2,2,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,2,2,1},
                {2,2,2,2,2,2,1,1,1,1,2,2,1,1,2,2,2,2,2,2,2,2,2,2,2,2,1,1,1,1,1,2,2,1},
                {1,1,1,1,1,1,1,1,1,1,2,2,1,1,1,1,1,1,1,1,1,1,1,1,2,2,1,1,1,1,1,2,2,-1},

            };
            mazeLevels.Add(level_temp);
            //----------------------------------------------------------------//         
        }

        private void SetUpFloorPlan()
        {
            int[,] tempFloorPlan;
            if (gameType.Equals("Holes"))
            {
                levelsCount = holesLevels.Count;
                tempFloorPlan = holesLevels[CurrentLevelID];
            }
            else
            {
                levelsCount = mazeLevels.Count;
                tempFloorPlan = mazeLevels[CurrentLevelID];
            }
            floorPlan = ReverseMatrix(tempFloorPlan);
            width = floorPlan.GetLength(0);
            length = floorPlan.GetLength(1);
            tableTranslation = Matrix.CreateTranslation(new Vector3(-(float)length / 2, -(float)thickness / 2, (float)width / 2));
            RestartGame();
            SetUpFloor();
            SetUpWalls();
            SetUpHoles();
            SetUpBlocks();
            SetUpTableBoundingBox();
            SetUpBlocksBoundingBoxes();

        }

        private int[,] ReverseMatrix(int[,] M)
        {
            int w = M.GetLength(0);
            int l = M.GetLength(1);
            int[,] result=new int[w,l];
            for (int i = 0; i < w; i++)
            {
                for (int j = 0; j < l; j++)
                {
                    result[i, j] = M[w - i - 1, j];
                }
            }
            return result;
        }

        private void SetUpCamera()
        {

            tableViewMatrix = Matrix.CreateLookAt(cameraPosition, cameraTarget, cameraUpVector);
            tableProjectionMatrix = Matrix.CreatePerspectiveFieldOfView(cameraFieldOfView, device.Viewport.AspectRatio, 0.1f, 300f);
            tableWorldMatrix = tableTranslation * Matrix.CreateFromQuaternion(tableRotation);
            ballWorldMatrix = Matrix.CreateScale(ballScale) * Matrix.CreateTranslation(ballPosition) * Matrix.CreateFromQuaternion(tableRotation);
        }

        private void SetUpDrawTable()
        {
            //restore renderstates values to fix  SpriteBatch and 3D issue
            ScreenManager.GraphicsDevice.RenderState.DepthBufferEnable = true;
            ScreenManager.GraphicsDevice.RenderState.AlphaBlendEnable = false;
            ScreenManager.GraphicsDevice.RenderState.AlphaTestEnable = false;
            ScreenManager.GraphicsDevice.SamplerStates[0].AddressU = TextureAddressMode.Wrap;
            ScreenManager.GraphicsDevice.SamplerStates[0].AddressV = TextureAddressMode.Wrap;
            //end of restore renderstates values to fix  SpriteBatch and 3D issue
            //enable transparency in textures//
            device.RenderState.AlphaBlendEnable = true;
            device.RenderState.SourceBlend = Blend.SourceAlpha;
            device.RenderState.DestinationBlend = Blend.InverseSourceAlpha;
            //end of enable transparency in textures//
            device.RenderState.CullMode = CullMode.None;
            device.VertexDeclaration = myVertexDeclaration;
            //setup floorEffect
            floorEffect = new BasicEffect(device, null);
            floorEffect.View = tableViewMatrix;
            floorEffect.Projection = tableProjectionMatrix;
            floorEffect.World = tableWorldMatrix;
            floorEffect.TextureEnabled = true;
            floorEffect.Texture = floorTexture;
            floorEffect.EnableDefaultLighting();
            //end of setup floorEffect
            //setup holesEffect
            holesEffect = new BasicEffect(device, null);
            holesEffect.View = tableViewMatrix;
            holesEffect.Projection = tableProjectionMatrix;
            holesEffect.World = tableWorldMatrix;
            holesEffect.TextureEnabled = true;
            holesEffect.Texture = holeTexture;
            holesEffect.EnableDefaultLighting();
            //end of setup winningHoleEffect
            winningHoleEffect = new BasicEffect(device, null);
            winningHoleEffect.View = tableViewMatrix;
            winningHoleEffect.Projection = tableProjectionMatrix;
            winningHoleEffect.World = tableWorldMatrix;
            winningHoleEffect.TextureEnabled = true;
            winningHoleEffect.Texture = winningHoleTexture;
            winningHoleEffect.EnableDefaultLighting();
            //end of setup winningHoleEffect
        }

        private void SetUpFloor()
        {
            List<VertexPositionNormalTexture> floorVerticesList = new List<VertexPositionNormalTexture>();
            List<VertexPositionNormalTexture> holesVerticesList = new List<VertexPositionNormalTexture>();
            List<VertexPositionNormalTexture> winningHoleVerticesList = new List<VertexPositionNormalTexture>();
            //up plane
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < length; j++)
                {
                    if ((floorPlan[i, j] == 1) || (floorPlan[i, j] == -2))
                    {
                        Vector2 downLeftTextureCoordinate = new Vector2(j / (float)length, 1 - (i / (float)width));
                        Vector2 upLeftTextureCoordinate = new Vector2(j / (float)length, 1 - (i + 1) / (float)width);
                        Vector2 downRightTextureCoordinate = new Vector2((j + 1) / (float)length, 1 - (i / (float)width));
                        Vector2 upRightTextureCoordinate = new Vector2((j + 1) / (float)length, 1 - (i + 1) / (float)width);
                        floorVerticesList.Add(new VertexPositionNormalTexture(new Vector3(j, thickness, -i), new Vector3(0, 1, 0), downLeftTextureCoordinate));
                        floorVerticesList.Add(new VertexPositionNormalTexture(new Vector3(j, thickness, -i - 1), new Vector3(0, 1, 0), upLeftTextureCoordinate));
                        floorVerticesList.Add(new VertexPositionNormalTexture(new Vector3(j + 1, thickness, -i), new Vector3(0, 1, 0), downRightTextureCoordinate));
                        floorVerticesList.Add(new VertexPositionNormalTexture(new Vector3(j + 1, thickness, -i), new Vector3(0, 1, 0), downRightTextureCoordinate));
                        floorVerticesList.Add(new VertexPositionNormalTexture(new Vector3(j, thickness, -i - 1), new Vector3(0, 1, 0), upLeftTextureCoordinate));
                        floorVerticesList.Add(new VertexPositionNormalTexture(new Vector3(j + 1, thickness, -i - 1), new Vector3(0, 1, 0), upRightTextureCoordinate));
                    }
                    else if (floorPlan[i, j] == 0)
                    {
                        holesVerticesList.Add(new VertexPositionNormalTexture(new Vector3(j, thickness, -i), new Vector3(0, 1, 0), new Vector2(0, 1)));
                        holesVerticesList.Add(new VertexPositionNormalTexture(new Vector3(j, thickness, -i - 1), new Vector3(0, 1, 0), new Vector2(0, 0)));
                        holesVerticesList.Add(new VertexPositionNormalTexture(new Vector3(j + 1, thickness, -i), new Vector3(0, 1, 0), new Vector2(1, 1)));
                        holesVerticesList.Add(new VertexPositionNormalTexture(new Vector3(j + 1, thickness, -i), new Vector3(0, 1, 0), new Vector2(1, 1)));
                        holesVerticesList.Add(new VertexPositionNormalTexture(new Vector3(j, thickness, -i - 1), new Vector3(0, 1, 0), new Vector2(0, 0)));
                        holesVerticesList.Add(new VertexPositionNormalTexture(new Vector3(j + 1, thickness, -i - 1), new Vector3(0, 1, 0), new Vector2(1, 0)));
                    }
                    else if (floorPlan[i, j] == -1)
                    {
                        winningHoleVerticesList.Add(new VertexPositionNormalTexture(new Vector3(j, thickness, -i), new Vector3(0, 1, 0), new Vector2(0, 1)));
                        winningHoleVerticesList.Add(new VertexPositionNormalTexture(new Vector3(j, thickness, -i - 1), new Vector3(0, 1, 0), new Vector2(0, 0)));
                        winningHoleVerticesList.Add(new VertexPositionNormalTexture(new Vector3(j + 1, thickness, -i), new Vector3(0, 1, 0), new Vector2(1, 1)));
                        winningHoleVerticesList.Add(new VertexPositionNormalTexture(new Vector3(j + 1, thickness, -i), new Vector3(0, 1, 0), new Vector2(1, 1)));
                        winningHoleVerticesList.Add(new VertexPositionNormalTexture(new Vector3(j, thickness, -i - 1), new Vector3(0, 1, 0), new Vector2(0, 0)));
                        winningHoleVerticesList.Add(new VertexPositionNormalTexture(new Vector3(j + 1, thickness, -i - 1), new Vector3(0, 1, 0), new Vector2(1, 0)));
                    }
                }
            }
            //down plane
            floorVerticesList.Add(new VertexPositionNormalTexture(new Vector3(0, 0, -width), new Vector3(0, -1, 0), new Vector2(0, 1)));
            floorVerticesList.Add(new VertexPositionNormalTexture(new Vector3(0, 0, 0), new Vector3(0, -1, 0), new Vector2(0, 0)));
            floorVerticesList.Add(new VertexPositionNormalTexture(new Vector3(length, 0, -width), new Vector3(0, -1, 0), new Vector2(1, 1)));
            floorVerticesList.Add(new VertexPositionNormalTexture(new Vector3(length, 0, -width), new Vector3(0, -1, 0), new Vector2(1, 1)));
            floorVerticesList.Add(new VertexPositionNormalTexture(new Vector3(0, 0, 0), new Vector3(0, -1, 0), new Vector2(0, 0)));
            floorVerticesList.Add(new VertexPositionNormalTexture(new Vector3(length, 0, 0), new Vector3(0, -1, 0), new Vector2(1, 0)));
            //right plane
            floorVerticesList.Add(new VertexPositionNormalTexture(new Vector3(length, 0, 0), new Vector3(1, 0, 0), new Vector2(0, 1)));
            floorVerticesList.Add(new VertexPositionNormalTexture(new Vector3(length, thickness, 0), new Vector3(1, 0, 0), new Vector2(0, 0)));
            floorVerticesList.Add(new VertexPositionNormalTexture(new Vector3(length, 0, -width), new Vector3(1, 0, 0), new Vector2(1, 1)));
            floorVerticesList.Add(new VertexPositionNormalTexture(new Vector3(length, 0, -width), new Vector3(1, 0, 0), new Vector2(1, 1)));
            floorVerticesList.Add(new VertexPositionNormalTexture(new Vector3(length, thickness, 0), new Vector3(1, 0, 0), new Vector2(0, 0)));
            floorVerticesList.Add(new VertexPositionNormalTexture(new Vector3(length, thickness, -width), new Vector3(1, 0, 0), new Vector2(1, 0)));
            //left plane
            floorVerticesList.Add(new VertexPositionNormalTexture(new Vector3(0, 0, -width), new Vector3(-1, 0, 0), new Vector2(0, 1)));
            floorVerticesList.Add(new VertexPositionNormalTexture(new Vector3(0, thickness, -width), new Vector3(-1, 0, 0), new Vector2(0, 0)));
            floorVerticesList.Add(new VertexPositionNormalTexture(new Vector3(0, 0, 0), new Vector3(-1, 0, 0), new Vector2(1, 1)));
            floorVerticesList.Add(new VertexPositionNormalTexture(new Vector3(0, 0, 0), new Vector3(-1, 0, 0), new Vector2(1, 1)));
            floorVerticesList.Add(new VertexPositionNormalTexture(new Vector3(0, thickness, -width), new Vector3(-1, 0, 0), new Vector2(0, 0)));
            floorVerticesList.Add(new VertexPositionNormalTexture(new Vector3(0, thickness, 0), new Vector3(-1, 0, 0), new Vector2(1, 0)));
            //front plane
            floorVerticesList.Add(new VertexPositionNormalTexture(new Vector3(0, 0, 0), new Vector3(0, 0, 1), new Vector2(0, 1)));
            floorVerticesList.Add(new VertexPositionNormalTexture(new Vector3(0, thickness, 0), new Vector3(0, 0, 1), new Vector2(0, 0)));
            floorVerticesList.Add(new VertexPositionNormalTexture(new Vector3(length, 0, 0), new Vector3(0, 0, 1), new Vector2(1, 1)));
            floorVerticesList.Add(new VertexPositionNormalTexture(new Vector3(length, 0, 0), new Vector3(0, 0, 1), new Vector2(1, 1)));
            floorVerticesList.Add(new VertexPositionNormalTexture(new Vector3(0, thickness, 0), new Vector3(0, 0, 1), new Vector2(0, 0)));
            floorVerticesList.Add(new VertexPositionNormalTexture(new Vector3(length, thickness, 0), new Vector3(0, 0, 1), new Vector2(1, 0)));
            //back plane
            floorVerticesList.Add(new VertexPositionNormalTexture(new Vector3(length, 0, -width), new Vector3(0, 0, -1), new Vector2(0, 1)));
            floorVerticesList.Add(new VertexPositionNormalTexture(new Vector3(length, thickness, -width), new Vector3(0, 0, -1), new Vector2(0, 0)));
            floorVerticesList.Add(new VertexPositionNormalTexture(new Vector3(0, 0, -width), new Vector3(0, 0, -1), new Vector2(1, 1)));
            floorVerticesList.Add(new VertexPositionNormalTexture(new Vector3(0, 0, -width), new Vector3(0, 0, -1), new Vector2(1, 1)));
            floorVerticesList.Add(new VertexPositionNormalTexture(new Vector3(length, thickness, -width), new Vector3(0, 0, -1), new Vector2(0, 0)));
            floorVerticesList.Add(new VertexPositionNormalTexture(new Vector3(0, thickness, -width), new Vector3(0, 0, -1), new Vector2(1, 0)));

            floorVerticesBuffer = new VertexBuffer(device, VertexPositionNormalTexture.SizeInBytes * floorVerticesList.Count, BufferUsage.WriteOnly);
            floorVerticesBuffer.SetData<VertexPositionNormalTexture>(floorVerticesList.ToArray());
            if (holesVerticesList.Count != 0)
            {
                holesVerticesBuffer = new VertexBuffer(device, VertexPositionNormalTexture.SizeInBytes * holesVerticesList.Count, BufferUsage.WriteOnly);
                holesVerticesBuffer.SetData<VertexPositionNormalTexture>(holesVerticesList.ToArray());
            }
            if (winningHoleVerticesList.Count != 0)
            {
                winningHoleVerticesBuffer = new VertexBuffer(device, VertexPositionNormalTexture.SizeInBytes * winningHoleVerticesList.Count, BufferUsage.WriteOnly);
                winningHoleVerticesBuffer.SetData<VertexPositionNormalTexture>(winningHoleVerticesList.ToArray());
            }


        }

        private void DrawFloor()
        {
            //draw flat floor
            floorEffect.Begin();
            foreach (EffectPass pass in floorEffect.CurrentTechnique.Passes)
            {
                pass.Begin();
                device.Vertices[0].SetSource(floorVerticesBuffer, 0, VertexPositionNormalTexture.SizeInBytes);
                device.DrawPrimitives(PrimitiveType.TriangleList, 0, floorVerticesBuffer.SizeInBytes / VertexPositionNormalTexture.SizeInBytes / 3);
                pass.End();
            }
            floorEffect.End();
            //end off draw flat floor
            //draw holes
            if (holesVerticesBuffer != null)
            {
                holesEffect.Begin();
                foreach (EffectPass pass in holesEffect.CurrentTechnique.Passes)
                {
                    pass.Begin();
                    device.Vertices[0].SetSource(holesVerticesBuffer, 0, VertexPositionNormalTexture.SizeInBytes);
                    device.DrawPrimitives(PrimitiveType.TriangleList, 0, holesVerticesBuffer.SizeInBytes / VertexPositionNormalTexture.SizeInBytes / 3);
                    pass.End();
                }
                holesEffect.End();
            }
            //end of draw holes
            //draw winningHole
            if (winningHoleVerticesBuffer != null)
            {
                winningHoleEffect.Begin();
                foreach (EffectPass pass in winningHoleEffect.CurrentTechnique.Passes)
                {
                    pass.Begin();
                    device.Vertices[0].SetSource(winningHoleVerticesBuffer, 0, VertexPositionNormalTexture.SizeInBytes);
                    device.DrawPrimitives(PrimitiveType.TriangleList, 0, winningHoleVerticesBuffer.SizeInBytes / VertexPositionNormalTexture.SizeInBytes / 3);
                    pass.End();
                }
                winningHoleEffect.End();
            }
            //end of draw winningHole
        }

        private void SetUpHoles()
        {
            holesList = new List<Vector3>();
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < length; j++)
                {
                    if (floorPlan[i, j] == 0)
                    {
                        Vector3 holeCenter = new Vector3(j + 0.5f, thickness + ballRadius, -i - 0.5f);
                        holeCenter = Vector3.Transform(holeCenter, tableTranslation);
                        holesList.Add(holeCenter);
                    }
                    if (floorPlan[i, j] == -1)
                    {
                        WinningHoleCenter = new Vector3(j + 0.5f, thickness + ballRadius, -i - 0.5f);
                        WinningHoleCenter = Vector3.Transform(WinningHoleCenter, tableTranslation);
                    }
                }
            }
        }

        private void SetUpWalls()
        {
            List<VertexPositionNormalTexture> wallsVerticesList = new List<VertexPositionNormalTexture>();

            //--------------left wall--------------
            //up plane
            wallsVerticesList.Add(new VertexPositionNormalTexture(new Vector3(0, height + thickness, thickness), new Vector3(0, 1, 0), new Vector2(0, 1)));
            wallsVerticesList.Add(new VertexPositionNormalTexture(new Vector3(-thickness, height + thickness, thickness), new Vector3(0, 1, 0), new Vector2(0, 0)));
            wallsVerticesList.Add(new VertexPositionNormalTexture(new Vector3(0, height + thickness, -width - thickness), new Vector3(0, 1, 0), new Vector2(1, 1)));
            wallsVerticesList.Add(new VertexPositionNormalTexture(new Vector3(0, height + thickness, -width - thickness), new Vector3(0, 1, 0), new Vector2(1, 1)));
            wallsVerticesList.Add(new VertexPositionNormalTexture(new Vector3(-thickness, height + thickness, thickness), new Vector3(0, 1, 0), new Vector2(0, 0)));
            wallsVerticesList.Add(new VertexPositionNormalTexture(new Vector3(-thickness, height + thickness, -width - thickness), new Vector3(0, 1, 0), new Vector2(1, 0)));
            ////down plane
            wallsVerticesList.Add(new VertexPositionNormalTexture(new Vector3(0, 0, thickness), new Vector3(0, -1, 0), new Vector2(0, 1)));
            wallsVerticesList.Add(new VertexPositionNormalTexture(new Vector3(-thickness, 0, thickness), new Vector3(0, -1, 0), new Vector2(0, 0)));
            wallsVerticesList.Add(new VertexPositionNormalTexture(new Vector3(0, 0, -width - thickness), new Vector3(0, -1, 0), new Vector2(1, 1)));
            wallsVerticesList.Add(new VertexPositionNormalTexture(new Vector3(0, 0, -width - thickness), new Vector3(0, -1, 0), new Vector2(1, 1)));
            wallsVerticesList.Add(new VertexPositionNormalTexture(new Vector3(-thickness, 0, thickness), new Vector3(0, -1, 0), new Vector2(0, 0)));
            wallsVerticesList.Add(new VertexPositionNormalTexture(new Vector3(-thickness, 0, -width - thickness), new Vector3(0, -1, 0), new Vector2(1, 0)));
            //right plane
            wallsVerticesList.Add(new VertexPositionNormalTexture(new Vector3(0, 0, thickness), new Vector3(1, 0, 0), new Vector2(0, 1)));
            wallsVerticesList.Add(new VertexPositionNormalTexture(new Vector3(0, height + thickness, thickness), new Vector3(1, 0, 0), new Vector2(0, 0)));
            wallsVerticesList.Add(new VertexPositionNormalTexture(new Vector3(0, 0, -width - thickness), new Vector3(1, 0, 0), new Vector2(1, 1)));
            wallsVerticesList.Add(new VertexPositionNormalTexture(new Vector3(0, 0, -width - thickness), new Vector3(1, 0, 0), new Vector2(1, 1)));
            wallsVerticesList.Add(new VertexPositionNormalTexture(new Vector3(0, height + thickness, +thickness), new Vector3(1, 0, 0), new Vector2(0, 0)));
            wallsVerticesList.Add(new VertexPositionNormalTexture(new Vector3(0, height + thickness, -width - thickness), new Vector3(1, 0, 0), new Vector2(1, 0)));
            //left plane
            wallsVerticesList.Add(new VertexPositionNormalTexture(new Vector3(-thickness, 0, thickness), new Vector3(-1, 0, 0), new Vector2(0, 1)));
            wallsVerticesList.Add(new VertexPositionNormalTexture(new Vector3(-thickness, height + thickness, thickness), new Vector3(-1, 0, 0), new Vector2(0, 0)));
            wallsVerticesList.Add(new VertexPositionNormalTexture(new Vector3(-thickness, 0, -width - thickness), new Vector3(-1, 0, 0), new Vector2(1, 1)));
            wallsVerticesList.Add(new VertexPositionNormalTexture(new Vector3(-thickness, 0, -width - thickness), new Vector3(-1, 0, 0), new Vector2(1, 1)));
            wallsVerticesList.Add(new VertexPositionNormalTexture(new Vector3(-thickness, height + thickness, thickness), new Vector3(-1, 0, 0), new Vector2(0, 0)));
            wallsVerticesList.Add(new VertexPositionNormalTexture(new Vector3(-thickness, height + thickness, -width - thickness), new Vector3(-1, 0, 0), new Vector2(1, 0)));
            //front plane
            wallsVerticesList.Add(new VertexPositionNormalTexture(new Vector3(-thickness, 0, thickness), new Vector3(0, 0, 1), new Vector2(0, 1)));
            wallsVerticesList.Add(new VertexPositionNormalTexture(new Vector3(-thickness, height + thickness, thickness), new Vector3(0, 0, 1), new Vector2(0, 0)));
            wallsVerticesList.Add(new VertexPositionNormalTexture(new Vector3(0, 0, thickness), new Vector3(0, 0, 1), new Vector2(1, 1)));
            wallsVerticesList.Add(new VertexPositionNormalTexture(new Vector3(0, 0, thickness), new Vector3(0, 0, 1), new Vector2(1, 1)));
            wallsVerticesList.Add(new VertexPositionNormalTexture(new Vector3(-thickness, height + thickness, thickness), new Vector3(0, 0, 1), new Vector2(0, 0)));
            wallsVerticesList.Add(new VertexPositionNormalTexture(new Vector3(0, height + thickness, thickness), new Vector3(0, 0, 1), new Vector2(1, 0)));
            //back plane
            wallsVerticesList.Add(new VertexPositionNormalTexture(new Vector3(-thickness, 0, -width - thickness), new Vector3(0, 0, -1), new Vector2(0, 1)));
            wallsVerticesList.Add(new VertexPositionNormalTexture(new Vector3(-thickness, height + thickness, -width - thickness), new Vector3(0, 0, -1), new Vector2(0, 0)));
            wallsVerticesList.Add(new VertexPositionNormalTexture(new Vector3(0, 0, -width - thickness), new Vector3(0, 0, -1), new Vector2(1, 1)));
            wallsVerticesList.Add(new VertexPositionNormalTexture(new Vector3(0, 0, -width - thickness), new Vector3(0, 0, -1), new Vector2(1, 1)));
            wallsVerticesList.Add(new VertexPositionNormalTexture(new Vector3(-thickness, height + thickness, -width - thickness), new Vector3(0, 0, -1), new Vector2(0, 0)));
            wallsVerticesList.Add(new VertexPositionNormalTexture(new Vector3(0, height + thickness, -width - thickness), new Vector3(0, 0, -1), new Vector2(1, 0)));

            //--------------right wall--------------
            //up plane
            wallsVerticesList.Add(new VertexPositionNormalTexture(new Vector3(length, height + thickness, thickness), new Vector3(0, 1, 0), new Vector2(0, 1)));
            wallsVerticesList.Add(new VertexPositionNormalTexture(new Vector3(length + thickness, height + thickness, thickness), new Vector3(0, 1, 0), new Vector2(0, 0)));
            wallsVerticesList.Add(new VertexPositionNormalTexture(new Vector3(length, height + thickness, -width - thickness), new Vector3(0, 1, 0), new Vector2(1, 1)));
            wallsVerticesList.Add(new VertexPositionNormalTexture(new Vector3(length, height + thickness, -width - thickness), new Vector3(0, 1, 0), new Vector2(1, 1)));
            wallsVerticesList.Add(new VertexPositionNormalTexture(new Vector3(length + thickness, height + thickness, thickness), new Vector3(0, 1, 0), new Vector2(0, 0)));
            wallsVerticesList.Add(new VertexPositionNormalTexture(new Vector3(length + thickness, height + thickness, -width - thickness), new Vector3(0, 1, 0), new Vector2(1, 0)));
            ////down plane
            wallsVerticesList.Add(new VertexPositionNormalTexture(new Vector3(length, 0, thickness), new Vector3(0, -1, 0), new Vector2(0, 1)));
            wallsVerticesList.Add(new VertexPositionNormalTexture(new Vector3(length + thickness, 0, thickness), new Vector3(0, -1, 0), new Vector2(0, 0)));
            wallsVerticesList.Add(new VertexPositionNormalTexture(new Vector3(length, 0, -width - thickness), new Vector3(0, -1, 0), new Vector2(1, 1)));
            wallsVerticesList.Add(new VertexPositionNormalTexture(new Vector3(length, 0, -width - thickness), new Vector3(0, -1, 0), new Vector2(1, 1)));
            wallsVerticesList.Add(new VertexPositionNormalTexture(new Vector3(length + thickness, 0, thickness), new Vector3(0, -1, 0), new Vector2(0, 0)));
            wallsVerticesList.Add(new VertexPositionNormalTexture(new Vector3(length + thickness, 0, -width - thickness), new Vector3(0, -1, 0), new Vector2(1, 0)));
            //left plane
            wallsVerticesList.Add(new VertexPositionNormalTexture(new Vector3(length, 0, thickness), new Vector3(-1, 0, 0), new Vector2(0, 1)));
            wallsVerticesList.Add(new VertexPositionNormalTexture(new Vector3(length, height + thickness, thickness), new Vector3(-1, 0, 0), new Vector2(0, 0)));
            wallsVerticesList.Add(new VertexPositionNormalTexture(new Vector3(length, 0, -width - thickness), new Vector3(-1, 0, 0), new Vector2(1, 1)));
            wallsVerticesList.Add(new VertexPositionNormalTexture(new Vector3(length, 0, -width - thickness), new Vector3(-1, 0, 0), new Vector2(1, 1)));
            wallsVerticesList.Add(new VertexPositionNormalTexture(new Vector3(length, height + thickness, +thickness), new Vector3(-1, 0, 0), new Vector2(0, 0)));
            wallsVerticesList.Add(new VertexPositionNormalTexture(new Vector3(length, height + thickness, -width - thickness), new Vector3(-1, 0, 0), new Vector2(1, 0)));
            //right plane
            wallsVerticesList.Add(new VertexPositionNormalTexture(new Vector3(length + thickness, 0, thickness), new Vector3(1, 0, 0), new Vector2(0, 1)));
            wallsVerticesList.Add(new VertexPositionNormalTexture(new Vector3(length + thickness, height + thickness, thickness), new Vector3(1, 0, 0), new Vector2(0, 0)));
            wallsVerticesList.Add(new VertexPositionNormalTexture(new Vector3(length + thickness, 0, -width - thickness), new Vector3(1, 0, 0), new Vector2(1, 1)));
            wallsVerticesList.Add(new VertexPositionNormalTexture(new Vector3(length + thickness, 0, -width - thickness), new Vector3(1, 0, 0), new Vector2(1, 1)));
            wallsVerticesList.Add(new VertexPositionNormalTexture(new Vector3(length + thickness, height + thickness, thickness), new Vector3(1, 0, 0), new Vector2(0, 0)));
            wallsVerticesList.Add(new VertexPositionNormalTexture(new Vector3(length + thickness, height + thickness, -width - thickness), new Vector3(1, 0, 0), new Vector2(1, 0)));
            //front plane
            wallsVerticesList.Add(new VertexPositionNormalTexture(new Vector3(length + thickness, 0, thickness), new Vector3(0, 0, 1), new Vector2(0, 1)));
            wallsVerticesList.Add(new VertexPositionNormalTexture(new Vector3(length + thickness, height + thickness, thickness), new Vector3(0, 0, 1), new Vector2(0, 0)));
            wallsVerticesList.Add(new VertexPositionNormalTexture(new Vector3(length, 0, thickness), new Vector3(0, 0, 1), new Vector2(1, 1)));
            wallsVerticesList.Add(new VertexPositionNormalTexture(new Vector3(length, 0, thickness), new Vector3(0, 0, 1), new Vector2(1, 1)));
            wallsVerticesList.Add(new VertexPositionNormalTexture(new Vector3(length + thickness, height + thickness, thickness), new Vector3(0, 0, 1), new Vector2(0, 0)));
            wallsVerticesList.Add(new VertexPositionNormalTexture(new Vector3(length, height + thickness, thickness), new Vector3(0, 0, 1), new Vector2(1, 0)));
            //back plane
            wallsVerticesList.Add(new VertexPositionNormalTexture(new Vector3(length + thickness, 0, -width - thickness), new Vector3(0, 0, -1), new Vector2(0, 1)));
            wallsVerticesList.Add(new VertexPositionNormalTexture(new Vector3(length + thickness, height + thickness, -width - thickness), new Vector3(0, 0, -1), new Vector2(0, 0)));
            wallsVerticesList.Add(new VertexPositionNormalTexture(new Vector3(length, 0, -width - thickness), new Vector3(0, 0, -1), new Vector2(1, 1)));
            wallsVerticesList.Add(new VertexPositionNormalTexture(new Vector3(length, 0, -width - thickness), new Vector3(0, 0, -1), new Vector2(1, 1)));
            wallsVerticesList.Add(new VertexPositionNormalTexture(new Vector3(length + thickness, height + thickness, -width - thickness), new Vector3(0, 0, -1), new Vector2(0, 0)));
            wallsVerticesList.Add(new VertexPositionNormalTexture(new Vector3(length, height + thickness, -width - thickness), new Vector3(0, 0, -1), new Vector2(1, 0)));

            //--------------front wall--------------
            //up plane
            wallsVerticesList.Add(new VertexPositionNormalTexture(new Vector3(0, height + thickness, thickness), new Vector3(0, 1, 0), new Vector2(0, 1)));
            wallsVerticesList.Add(new VertexPositionNormalTexture(new Vector3(0, height + thickness, 0), new Vector3(0, 1, 0), new Vector2(0, 0)));
            wallsVerticesList.Add(new VertexPositionNormalTexture(new Vector3(length, height + thickness, thickness), new Vector3(0, 1, 0), new Vector2(1, 1)));
            wallsVerticesList.Add(new VertexPositionNormalTexture(new Vector3(length, height + thickness, thickness), new Vector3(0, 1, 0), new Vector2(1, 1)));
            wallsVerticesList.Add(new VertexPositionNormalTexture(new Vector3(0, height + thickness, 0), new Vector3(0, 1, 0), new Vector2(0, 0)));
            wallsVerticesList.Add(new VertexPositionNormalTexture(new Vector3(length, height + thickness, 0), new Vector3(0, 1, 0), new Vector2(1, 0)));
            ////down plane
            wallsVerticesList.Add(new VertexPositionNormalTexture(new Vector3(0, 0, thickness), new Vector3(0, -1, 0), new Vector2(0, 1)));
            wallsVerticesList.Add(new VertexPositionNormalTexture(new Vector3(0, 0, 0), new Vector3(0, -1, 0), new Vector2(0, 0)));
            wallsVerticesList.Add(new VertexPositionNormalTexture(new Vector3(length, 0, thickness), new Vector3(0, -1, 0), new Vector2(1, 1)));
            wallsVerticesList.Add(new VertexPositionNormalTexture(new Vector3(length, 0, thickness), new Vector3(0, -1, 0), new Vector2(1, 1)));
            wallsVerticesList.Add(new VertexPositionNormalTexture(new Vector3(0, 0, 0), new Vector3(0, -1, 0), new Vector2(0, 0)));
            wallsVerticesList.Add(new VertexPositionNormalTexture(new Vector3(length, 0, 0), new Vector3(0, -1, 0), new Vector2(1, 0)));
            //front plane
            wallsVerticesList.Add(new VertexPositionNormalTexture(new Vector3(0, 0, thickness), new Vector3(0, 0, 1), new Vector2(0, 1)));
            wallsVerticesList.Add(new VertexPositionNormalTexture(new Vector3(0, height + thickness, thickness), new Vector3(0, 0, 1), new Vector2(0, 0)));
            wallsVerticesList.Add(new VertexPositionNormalTexture(new Vector3(length, 0, thickness), new Vector3(0, 0, 1), new Vector2(1, 1)));
            wallsVerticesList.Add(new VertexPositionNormalTexture(new Vector3(length, 0, thickness), new Vector3(0, 0, 1), new Vector2(1, 1)));
            wallsVerticesList.Add(new VertexPositionNormalTexture(new Vector3(0, height + thickness, thickness), new Vector3(0, 0, 1), new Vector2(0, 0)));
            wallsVerticesList.Add(new VertexPositionNormalTexture(new Vector3(length, height + thickness, thickness), new Vector3(0, 0, 1), new Vector2(1, 0)));
            //back plane
            wallsVerticesList.Add(new VertexPositionNormalTexture(new Vector3(0, 0, 0), new Vector3(0, 0, -1), new Vector2(0, 1)));
            wallsVerticesList.Add(new VertexPositionNormalTexture(new Vector3(0, height + thickness, 0), new Vector3(0, 0, -1), new Vector2(0, 0)));
            wallsVerticesList.Add(new VertexPositionNormalTexture(new Vector3(length, 0, 0), new Vector3(0, 0, -1), new Vector2(1, 1)));
            wallsVerticesList.Add(new VertexPositionNormalTexture(new Vector3(length, 0, 0), new Vector3(0, 0, -1), new Vector2(1, 1)));
            wallsVerticesList.Add(new VertexPositionNormalTexture(new Vector3(0, height + thickness, 0), new Vector3(0, 0, -1), new Vector2(0, 0)));
            wallsVerticesList.Add(new VertexPositionNormalTexture(new Vector3(length, height + thickness, 0), new Vector3(0, 0, -1), new Vector2(1, 0)));

            //--------------back wall--------------
            //up plane
            wallsVerticesList.Add(new VertexPositionNormalTexture(new Vector3(0, height + thickness, -width - thickness), new Vector3(0, 1, 0), new Vector2(0, 1)));
            wallsVerticesList.Add(new VertexPositionNormalTexture(new Vector3(0, height + thickness, -width), new Vector3(0, 1, 0), new Vector2(0, 0)));
            wallsVerticesList.Add(new VertexPositionNormalTexture(new Vector3(length, height + thickness, -width - thickness), new Vector3(0, 1, 0), new Vector2(1, 1)));
            wallsVerticesList.Add(new VertexPositionNormalTexture(new Vector3(length, height + thickness, -width - thickness), new Vector3(0, 1, 0), new Vector2(1, 1)));
            wallsVerticesList.Add(new VertexPositionNormalTexture(new Vector3(0, height + thickness, -width), new Vector3(0, 1, 0), new Vector2(0, 0)));
            wallsVerticesList.Add(new VertexPositionNormalTexture(new Vector3(length, height + thickness, -width), new Vector3(0, 1, 0), new Vector2(1, 0)));
            //down plane
            wallsVerticesList.Add(new VertexPositionNormalTexture(new Vector3(0, 0, -width - thickness), new Vector3(0, -1, 0), new Vector2(0, 1)));
            wallsVerticesList.Add(new VertexPositionNormalTexture(new Vector3(0, 0, -width), new Vector3(0, -1, 0), new Vector2(0, 0)));
            wallsVerticesList.Add(new VertexPositionNormalTexture(new Vector3(length, 0, -width - thickness), new Vector3(0, -1, 0), new Vector2(1, 1)));
            wallsVerticesList.Add(new VertexPositionNormalTexture(new Vector3(length, 0, -width - thickness), new Vector3(0, -1, 0), new Vector2(1, 1)));
            wallsVerticesList.Add(new VertexPositionNormalTexture(new Vector3(0, 0, -width), new Vector3(0, -1, 0), new Vector2(0, 0)));
            wallsVerticesList.Add(new VertexPositionNormalTexture(new Vector3(length, 0, -width), new Vector3(0, -1, 0), new Vector2(1, 0)));
            //back plane
            wallsVerticesList.Add(new VertexPositionNormalTexture(new Vector3(0, 0, -width - thickness), new Vector3(0, 0, -1), new Vector2(0, 1)));
            wallsVerticesList.Add(new VertexPositionNormalTexture(new Vector3(0, height + thickness, -width - thickness), new Vector3(0, 0, -1), new Vector2(0, 0)));
            wallsVerticesList.Add(new VertexPositionNormalTexture(new Vector3(length, 0, -width - thickness), new Vector3(0, 0, -1), new Vector2(1, 1)));
            wallsVerticesList.Add(new VertexPositionNormalTexture(new Vector3(length, 0, -width - thickness), new Vector3(0, 0, -1), new Vector2(1, 1)));
            wallsVerticesList.Add(new VertexPositionNormalTexture(new Vector3(0, height + thickness, -width - thickness), new Vector3(0, 0, -1), new Vector2(0, 0)));
            wallsVerticesList.Add(new VertexPositionNormalTexture(new Vector3(length, height + thickness, -width - thickness), new Vector3(0, 0, -1), new Vector2(1, 0)));
            //front plane
            wallsVerticesList.Add(new VertexPositionNormalTexture(new Vector3(0, 0, -width), new Vector3(0, 0, 1), new Vector2(0, 1)));
            wallsVerticesList.Add(new VertexPositionNormalTexture(new Vector3(0, height + thickness, -width), new Vector3(0, 0, 1), new Vector2(0, 0)));
            wallsVerticesList.Add(new VertexPositionNormalTexture(new Vector3(length, 0, -width), new Vector3(0, 0, 1), new Vector2(1, 1)));
            wallsVerticesList.Add(new VertexPositionNormalTexture(new Vector3(length, 0, -width), new Vector3(0, 0, 1), new Vector2(1, 1)));
            wallsVerticesList.Add(new VertexPositionNormalTexture(new Vector3(0, height + thickness, -width), new Vector3(0, 0, 1), new Vector2(0, 0)));
            wallsVerticesList.Add(new VertexPositionNormalTexture(new Vector3(length, height + thickness, -width), new Vector3(0, 0, 1), new Vector2(1, 0)));


            wallsVerticesBuffer = new VertexBuffer(device, VertexPositionNormalTexture.SizeInBytes * wallsVerticesList.Count, BufferUsage.WriteOnly);
            wallsVerticesBuffer.SetData<VertexPositionNormalTexture>(wallsVerticesList.ToArray());

        }

        private void DrawWalls()
        {
            floorEffect.Begin();
            foreach (EffectPass pass in floorEffect.CurrentTechnique.Passes)
            {
                pass.Begin();
                device.Vertices[0].SetSource(wallsVerticesBuffer, 0, VertexPositionNormalTexture.SizeInBytes);
                device.DrawPrimitives(PrimitiveType.TriangleList, 0, wallsVerticesBuffer.SizeInBytes / VertexPositionNormalTexture.SizeInBytes / 3);
                pass.End();
            }
            floorEffect.End();
        }

        private void SetUpBlocks()
        {
            List<VertexPositionNormalTexture> blocksVerticesList = new List<VertexPositionNormalTexture>();
            //up plane
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < length; j++)
                {
                    if (floorPlan[i, j] == 2)
                    {
                        //up plane
                        blocksVerticesList.Add(new VertexPositionNormalTexture(new Vector3(j, thickness + height, -i), new Vector3(0, 1, 0), new Vector2(0, 1)));
                        blocksVerticesList.Add(new VertexPositionNormalTexture(new Vector3(j, thickness + height, -i - 1), new Vector3(0, 1, 0), new Vector2(0, 0)));
                        blocksVerticesList.Add(new VertexPositionNormalTexture(new Vector3(j + 1, thickness + height, -i), new Vector3(0, 1, 0), new Vector2(1, 1)));
                        blocksVerticesList.Add(new VertexPositionNormalTexture(new Vector3(j + 1, thickness + height, -i), new Vector3(0, 1, 0), new Vector2(1, 1)));
                        blocksVerticesList.Add(new VertexPositionNormalTexture(new Vector3(j, thickness + height, -i - 1), new Vector3(0, 1, 0), new Vector2(0, 0)));
                        blocksVerticesList.Add(new VertexPositionNormalTexture(new Vector3(j + 1, thickness + height, -i - 1), new Vector3(0, 1, 0), new Vector2(1, 0)));
                        //front plane
                        blocksVerticesList.Add(new VertexPositionNormalTexture(new Vector3(j, thickness, -i), new Vector3(0, 0, 1), new Vector2(0, 1)));
                        blocksVerticesList.Add(new VertexPositionNormalTexture(new Vector3(j, thickness + height, -i), new Vector3(0, 0, 1), new Vector2(0, 0)));
                        blocksVerticesList.Add(new VertexPositionNormalTexture(new Vector3(j + 1, thickness, -i), new Vector3(0, 0, 1), new Vector2(1, 1)));
                        blocksVerticesList.Add(new VertexPositionNormalTexture(new Vector3(j + 1, thickness, -i), new Vector3(0, 0, 1), new Vector2(1, 1)));
                        blocksVerticesList.Add(new VertexPositionNormalTexture(new Vector3(j, thickness + height, -i), new Vector3(0, 0, 1), new Vector2(0, 0)));
                        blocksVerticesList.Add(new VertexPositionNormalTexture(new Vector3(j + 1, thickness + height, -i), new Vector3(0, 0, 1), new Vector2(1, 0)));
                        //back plane
                        blocksVerticesList.Add(new VertexPositionNormalTexture(new Vector3(j, thickness, -i - 1), new Vector3(0, 0, -1), new Vector2(1, 1)));
                        blocksVerticesList.Add(new VertexPositionNormalTexture(new Vector3(j, thickness + height, -i - 1), new Vector3(0, 0, -1), new Vector2(1, 0)));
                        blocksVerticesList.Add(new VertexPositionNormalTexture(new Vector3(j + 1, thickness, -i - 1), new Vector3(0, 0, -1), new Vector2(0, 1)));
                        blocksVerticesList.Add(new VertexPositionNormalTexture(new Vector3(j + 1, thickness, -i - 1), new Vector3(0, 0, -1), new Vector2(0, 1)));
                        blocksVerticesList.Add(new VertexPositionNormalTexture(new Vector3(j, thickness + height, -i - 1), new Vector3(0, 0, -1), new Vector2(1, 0)));
                        blocksVerticesList.Add(new VertexPositionNormalTexture(new Vector3(j + 1, thickness + height, -i - 1), new Vector3(0, 0, -1), new Vector2(0, 0)));
                        //left plane
                        blocksVerticesList.Add(new VertexPositionNormalTexture(new Vector3(j, thickness, -i - 1), new Vector3(-1, 0, 0), new Vector2(0, 1)));
                        blocksVerticesList.Add(new VertexPositionNormalTexture(new Vector3(j, thickness + height, -i - 1), new Vector3(-1, 0, 0), new Vector2(0, 0)));
                        blocksVerticesList.Add(new VertexPositionNormalTexture(new Vector3(j, thickness, -i), new Vector3(-1, 0, 0), new Vector2(1, 1)));
                        blocksVerticesList.Add(new VertexPositionNormalTexture(new Vector3(j, thickness, -i), new Vector3(-1, 0, 0), new Vector2(1, 1)));
                        blocksVerticesList.Add(new VertexPositionNormalTexture(new Vector3(j, thickness + height, -i - 1), new Vector3(-1, 0, 0), new Vector2(0, 0)));
                        blocksVerticesList.Add(new VertexPositionNormalTexture(new Vector3(j, thickness + height, -i), new Vector3(-1, 0, 0), new Vector2(1, 0)));
                        //right plane
                        blocksVerticesList.Add(new VertexPositionNormalTexture(new Vector3(j + 1, thickness, -i), new Vector3(1, 0, 0), new Vector2(0, 1)));
                        blocksVerticesList.Add(new VertexPositionNormalTexture(new Vector3(j + 1, thickness + height, -i), new Vector3(1, 0, 0), new Vector2(0, 0)));
                        blocksVerticesList.Add(new VertexPositionNormalTexture(new Vector3(j + 1, thickness, -i - 1), new Vector3(1, 0, 0), new Vector2(1, 1)));
                        blocksVerticesList.Add(new VertexPositionNormalTexture(new Vector3(j + 1, thickness, -i - 1), new Vector3(1, 0, 0), new Vector2(1, 1)));
                        blocksVerticesList.Add(new VertexPositionNormalTexture(new Vector3(j + 1, thickness + height, -i), new Vector3(1, 0, 0), new Vector2(0, 0)));
                        blocksVerticesList.Add(new VertexPositionNormalTexture(new Vector3(j + 1, thickness + height, -i - 1), new Vector3(1, 0, 0), new Vector2(1, 0)));
                    }
                }
            }
            if (blocksVerticesList.Count != 0)
            {
                blocksVerticesBuffer = new VertexBuffer(device, VertexPositionNormalTexture.SizeInBytes * blocksVerticesList.Count, BufferUsage.WriteOnly);
                blocksVerticesBuffer.SetData<VertexPositionNormalTexture>(blocksVerticesList.ToArray());
                drawBlocksFlag = true;
            }
            else
            {
                drawBlocksFlag = false;
            }

        }

        private void SetUpTableBoundingBox()
        {
            Vector3[] tablePoints = new Vector3[2];
            tablePoints[0] = Vector3.Transform(new Vector3(0, thickness, 0), tableTranslation); ;
            tablePoints[1] = Vector3.Transform(new Vector3(length, thickness + 2 * ballRadius + height, -width), tableTranslation);
            tableBoundingBox = BoundingBox.CreateFromPoints(tablePoints);
        }

        private void SetUpBlocksBoundingBoxes()
        {
            blocksList = new List<BoundingBox>();
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < length; j++)
                {
                    if (floorPlan[i, j] == 2)
                    {
                        Vector3[] blockPoints = new Vector3[2];
                        blockPoints[0] = Vector3.Transform(new Vector3(j, thickness, -i), tableTranslation); ;
                        blockPoints[1] = Vector3.Transform(new Vector3(j + 1, thickness + height, -i - 1), tableTranslation);
                        BoundingBox blockBox = BoundingBox.CreateFromPoints(blockPoints);
                        blocksList.Add(blockBox);
                    }
                }
            }
        }

        private void DrawBlocks()
        {
            if (drawBlocksFlag)
            {
                floorEffect.Begin();
                foreach (EffectPass pass in floorEffect.CurrentTechnique.Passes)
                {
                    pass.Begin();
                    device.Vertices[0].SetSource(blocksVerticesBuffer, 0, VertexPositionNormalTexture.SizeInBytes);
                    device.DrawPrimitives(PrimitiveType.TriangleList, 0, blocksVerticesBuffer.SizeInBytes / VertexPositionNormalTexture.SizeInBytes / 3);
                    pass.End();
                }
                floorEffect.End();
            }
        }

        private void DrawReslutsBoard()
        {
            spriteBatch.Begin();
            spriteBatch.DrawString(spriteFont, "Level= " +(CurrentLevelID+1).ToString()+"/"+levelsCount, new Vector2(10, 0), Color.White);
            spriteBatch.DrawString(spriteFont, "Time= " + Math.Round(time,0).ToString(), new Vector2(10, 30), Color.White);
            if ((pauseGame)&&!CheckCollision(ballBoundingSphere).winningHole)
            {
                spriteBatch.DrawString(spriteFont, "You have loosed! press R to try again.", new Vector2(10, 160), Color.Red);
            }
            
            spriteBatch.End();
        }

        private void DrawBall()
        {
            Matrix[] transforms = new Matrix[ballModel.Bones.Count];
            ballModel.CopyAbsoluteBoneTransformsTo(transforms);
            foreach (ModelMesh mesh in ballModel.Meshes)
            {
                foreach (BasicEffect currentEffect in mesh.Effects)
                {
                    currentEffect.View = tableViewMatrix;
                    currentEffect.Projection = tableProjectionMatrix;
                    currentEffect.World = transforms[mesh.ParentBone.Index] * ballWorldMatrix;
                    currentEffect.EnableDefaultLighting();
                }
                mesh.Draw();
            }
        }

        private void UpdateBall()
        {
            //step at Z axis
            previousBallPosition = ballPosition;
            previousBallBoundingSphere = ballBoundingSphere;
            previousSpeedZ = speedZ;
            previousDistanceZ = distanceZ;
            timeZ += (float)1 / updateFrequency;
            speedZ = Physics.Calc_Speed(gravity, upDownRot, timeZ, speed0Z, frictionFactor, frictionDirectionZ);
            distanceZ = Physics.Calc_Distance(gravity, upDownRot, timeZ, speed0Z, distance0Z, frictionFactor, frictionDirectionZ);
            ballPosition = new Vector3(distanceX, (float)thickness / 2 + ballRadius, distanceZ);
            ballBoundingSphere = new BoundingSphere(ballPosition, ballBoundingSphereScale * ballRadius);
            if ((CheckCollision(ballBoundingSphere).wall) || (CheckCollision(ballBoundingSphere).block) || !Physics.Check_Movement(gravity, upDownRot, mass, frictionFactor, previousSpeedZ))
            {
                if ((playUpDownCollisionSound) && Physics.Check_Movement(gravity, upDownRot, mass, frictionFactor, previousSpeedZ))
                {
                    collisionSound.Play();
                    playUpDownCollisionSound = false;
                }
                ballPosition = previousBallPosition;
                ballBoundingSphere = previousBallBoundingSphere;
                timeZ = 0;
                speed0Z = 0;
                speedZ = 0;
                distanceZ = previousDistanceZ;
                distance0Z = distanceZ;
            }
            if (Math.Abs(distanceZ - previousDistanceZ) > 0.01f)
                playUpDownCollisionSound = true;

            if (speedZ * frictionDirectionZ > 0)
            {
                frictionDirectionZ = -frictionDirectionZ;
                speed0Z = 0;
                distance0Z = distanceZ;
                timeZ = 0;
            }

            //step at X axis
            previousBallPosition = ballPosition;
            previousBallBoundingSphere = ballBoundingSphere;
            previousSpeedX = speedX;
            previousDistanceX = distanceX;
            timeX += (float)1 / updateFrequency;
            speedX = Physics.Calc_Speed(gravity, leftRightRot, timeX, speed0X, frictionFactor, frictionDirectionX);
            distanceX = Physics.Calc_Distance(gravity, leftRightRot, timeX, speed0X, distance0X, frictionFactor, frictionDirectionX);
            ballPosition = new Vector3(distanceX, (float)thickness / 2 + ballRadius, distanceZ);
            ballBoundingSphere = new BoundingSphere(ballPosition, ballBoundingSphereScale * ballRadius);
            if ((CheckCollision(ballBoundingSphere).wall) || (CheckCollision(ballBoundingSphere).block) || !Physics.Check_Movement(gravity, leftRightRot, mass, frictionFactor, previousSpeedX))
            {
                if ((playLeftRightCollisionSound) && Physics.Check_Movement(gravity, leftRightRot, mass, frictionFactor, previousSpeedX))
                {
                    collisionSound.Play();
                    playLeftRightCollisionSound = false;
                }
                ballPosition = previousBallPosition;
                ballBoundingSphere = previousBallBoundingSphere;
                timeX = 0;
                speed0X = 0;
                speedX = 0;
                distanceX = previousDistanceX;
                distance0X = distanceX;

            }
            if (Math.Abs(distanceX - previousDistanceX) > 0.01f)
                playLeftRightCollisionSound = true;

            if (speedX * frictionDirectionX > 0)
            {
                frictionDirectionX = -frictionDirectionX;
                speed0X = 0;
                distance0X = distanceX;
                timeX = 0;
            }

            if (CheckCollision(ballBoundingSphere).hole)
            {
                holeSound.Play();
                ballPosition = CheckCollision(ballBoundingSphere).holeCenter - (new Vector3(0, ballRadius, 0));
                pauseGame = true;
            }
            if (CheckCollision(ballBoundingSphere).winningHole)
            {
                ballPosition = WinningHoleCenter - (new Vector3(0, ballRadius, 0));
                if (CurrentLevelID<(levelsCount-1))
                {
                    winningHoleSound.Play();
                    CurrentLevelID++;
                    time = 0;
                    SetUpFloorPlan();
                }
                else
                {
                    finishGameSound.Play();
                    pauseGame = true;
                }
            }

        }

        private CollisionType CheckCollision(BoundingSphere ball)
        {
            CollisionType result = new CollisionType();
            if (tableBoundingBox.Contains(ball) != ContainmentType.Contains)
            {
                result.wall = true;
            }
            foreach (Vector3 hole in holesList)
            {
                if (ball.Contains(hole) == ContainmentType.Contains)
                {
                    result.hole = true;
                    result.holeCenter = hole;
                }
            }
            if (ball.Contains(WinningHoleCenter) == ContainmentType.Contains)
            {
                result.winningHole = true;
            }
            foreach (BoundingBox blockBox in blocksList)
            {
                if (blockBox.Contains(ball) != ContainmentType.Disjoint)
                {
                    result.block = true;
                }
            }
            return result;
        }

        private void RestartGame()
        {
            InitialBallPosition();
            InitialCameraPosition();
            time = 0;
            timeX = 0;
            speed0X = 0;
            speedX = 0;
            timeZ = 0;
            speed0Z = 0;
            speedZ = 0;
            leftRightRot = 0;
            upDownRot = 0;
            pauseGame = false;
        }

        private void ProcessKeyboard(GameTime gameTime)
        {

            float turningSpeed = (float)gameTime.ElapsedGameTime.TotalMilliseconds / 1000.0f;
            if (!escapePressed)
            {
                if (Keyboard.GetState().IsKeyDown(Keys.Up))
                {
                    if (upDownRot > -maxRot)
                        upDownRot -= turningSpeed;
                    speed0Z = speedZ;
                    distance0Z = distanceZ;
                    timeZ = 0;
                }
                if (Keyboard.GetState().IsKeyDown(Keys.Down))
                {
                    if (upDownRot < maxRot)
                        upDownRot += turningSpeed;
                    speed0Z = speedZ;
                    distance0Z = distanceZ;
                    timeZ = 0;
                }
                if (Keyboard.GetState().IsKeyDown(Keys.Left))
                {
                    if (leftRightRot > -maxRot)
                        leftRightRot -= turningSpeed;
                    speed0X = speedX;
                    distance0X = distanceX;
                    timeX = 0;
                }
                if (Keyboard.GetState().IsKeyDown(Keys.Right))
                {
                    if (leftRightRot < maxRot)
                        leftRightRot += turningSpeed;
                    speed0X = speedX;
                    distance0X = distanceX;
                    timeX = 0;
                }
                if (Keyboard.GetState().IsKeyDown(Keys.R))
                {
                    RestartGame();
                }
                if (Keyboard.GetState().IsKeyDown(Keys.D))
                {
                    Quaternion rot = Quaternion.CreateFromAxisAngle(new Vector3(0, 1, 0), turningSpeed);
                    cameraPosition = Vector3.Transform(cameraPosition, Matrix.CreateFromQuaternion(rot));
                    cameraTarget = Vector3.Transform(cameraTarget, Matrix.CreateFromQuaternion(rot));
                    cameraUpVector = Vector3.Transform(cameraUpVector, Matrix.CreateFromQuaternion(rot));
                }
                if (Keyboard.GetState().IsKeyDown(Keys.A))
                {
                    Quaternion rot = Quaternion.CreateFromAxisAngle(new Vector3(0, 1, 0), -turningSpeed);
                    cameraPosition = Vector3.Transform(cameraPosition, Matrix.CreateFromQuaternion(rot));
                    cameraTarget = Vector3.Transform(cameraTarget, Matrix.CreateFromQuaternion(rot));
                    cameraUpVector = Vector3.Transform(cameraUpVector, Matrix.CreateFromQuaternion(rot));
                }
                if (Keyboard.GetState().IsKeyDown(Keys.W))
                {
                    Quaternion rot = Quaternion.CreateFromAxisAngle(new Vector3(1, 0, 0), -turningSpeed);
                    cameraPosition = Vector3.Transform(cameraPosition, Matrix.CreateFromQuaternion(rot));
                    cameraTarget = Vector3.Transform(cameraTarget, Matrix.CreateFromQuaternion(rot));
                    cameraUpVector = Vector3.Transform(cameraUpVector, Matrix.CreateFromQuaternion(rot));
                }
                if (Keyboard.GetState().IsKeyDown(Keys.S))
                {
                    Quaternion rot = Quaternion.CreateFromAxisAngle(new Vector3(1, 0, 0), turningSpeed);
                    cameraPosition = Vector3.Transform(cameraPosition, Matrix.CreateFromQuaternion(rot));
                    cameraTarget = Vector3.Transform(cameraTarget, Matrix.CreateFromQuaternion(rot));
                    cameraUpVector = Vector3.Transform(cameraUpVector, Matrix.CreateFromQuaternion(rot));
                }
                if (Keyboard.GetState().IsKeyDown(Keys.Add))
                {
                    if (cameraFieldOfView >= cameraMinFieldOfView)
                    {
                        cameraFieldOfView -= turningSpeed;
                    }
                }
                if (Keyboard.GetState().IsKeyDown(Keys.Subtract))
                {
                    if (cameraFieldOfView <= cameraMaxFieldOfView)
                    {
                        cameraFieldOfView += turningSpeed;
                    }
                }
                if (Keyboard.GetState().IsKeyDown(Keys.Delete))
                {
                    InitialCameraPosition();
                }
            }
          
            Quaternion additionalRot = Quaternion.CreateFromAxisAngle(new Vector3(0, 0, -1), leftRightRot) * Quaternion.CreateFromAxisAngle(new Vector3(1, 0, 0), upDownRot);
            tableRotation = additionalRot;

        }

        #endregion
    }
}
