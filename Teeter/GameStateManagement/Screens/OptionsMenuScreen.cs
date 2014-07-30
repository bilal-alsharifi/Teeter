#region File Description
//-----------------------------------------------------------------------------
// OptionsMenuScreen.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

#region Using Statements
using Microsoft.Xna.Framework;
#endregion

namespace Teeter
{
    /// <summary>
    /// The options screen is brought up over the top of the main menu
    /// screen, and gives the user a chance to configure the game
    /// in various hopefully useful ways.
    /// </summary>
    class OptionsMenuScreen : MenuScreen
    {
        #region Fields
        MenuEntry gameTypeMenuEntry;
        static string[] gameTypes = { "Holes", "Maze"};
        MenuEntry materialMenuEntry;
        static int currentGameType = 0;
        static string[] materials = { "Wood", "Moquette", "Other" };
        static int currentMaterial = 0;
        float WoodFrictionFactor = 0.1f;
        float MoquetteFrictionFactor = 0.2f;
        float OtherFrictionFactor = 0.3f;
        MenuEntry frictionFactorIncreaseMenuEntry;
        MenuEntry frictionFactorDecreaseMenuEntry;
        MenuEntry massIncreaseMenuEntry;
        MenuEntry massDecreaseMenuEntry;
        MenuEntry gravityIncreaseMenuEntry;
        MenuEntry gravityDecreaseMenuEntry;
        MenuEntry maxRotIncreaseMenuEntry;
        MenuEntry maxRotDecreaseMenuEntry;
        MenuEntry heightIncreaseMenuEntry;
        MenuEntry heightDecreaseMenuEntry;
        MenuEntry thicknessIncreaseMenuEntry;
        MenuEntry thicknessDecreaseMenuEntry;



        #endregion

        #region Initialization


        /// <summary>
        /// Constructor.
        /// </summary>
        public OptionsMenuScreen()
            : base("Options")
        {
            // Create our menu entries.
            gameTypeMenuEntry = new MenuEntry(string.Empty);
            materialMenuEntry = new MenuEntry(string.Empty);
            frictionFactorIncreaseMenuEntry = new MenuEntry(string.Empty);
            frictionFactorDecreaseMenuEntry = new MenuEntry(string.Empty);
            massIncreaseMenuEntry = new MenuEntry(string.Empty);
            massDecreaseMenuEntry = new MenuEntry(string.Empty);
            gravityIncreaseMenuEntry = new MenuEntry(string.Empty);
            gravityDecreaseMenuEntry = new MenuEntry(string.Empty);
            maxRotIncreaseMenuEntry = new MenuEntry(string.Empty);
            maxRotDecreaseMenuEntry = new MenuEntry(string.Empty);
            heightIncreaseMenuEntry = new MenuEntry(string.Empty);
            heightDecreaseMenuEntry = new MenuEntry(string.Empty);
            thicknessIncreaseMenuEntry = new MenuEntry(string.Empty);
            thicknessDecreaseMenuEntry = new MenuEntry(string.Empty);

            SetMenuEntryText();

            MenuEntry backMenuEntry = new MenuEntry("Back");

            // Hook up menu event handlers.
            gameTypeMenuEntry.Selected += GameTypeMenuEntrySelected;
            materialMenuEntry.Selected += MaterialMenuEntrySelected;
            frictionFactorIncreaseMenuEntry.Selected += FrictionFactorIncreaseMenuEntrySelected;
            frictionFactorDecreaseMenuEntry.Selected += FrictionFactorDecreaseMenuEntrySelected;
            massIncreaseMenuEntry.Selected += MassIncreaseMenuEntrySelected;
            massDecreaseMenuEntry.Selected += MassDecreaseMenuEntrySelected;
            gravityIncreaseMenuEntry.Selected += GravityIncreaseMenuEntrySelected;
            gravityDecreaseMenuEntry.Selected += GravityDecreaseMenuEntrySelected;
            maxRotIncreaseMenuEntry.Selected += MaxRotIncreaseMenuEntrySelected;
            maxRotDecreaseMenuEntry.Selected += MaxRotDecreaseMenuEntrySelected;
            heightIncreaseMenuEntry.Selected += HeightIncreaseMenuEntrySelected;
            heightDecreaseMenuEntry.Selected += HeightDecreaseMenuEntrySelected;
            thicknessIncreaseMenuEntry.Selected += ThicknessIncreaseMenuEntrySelected;
            thicknessDecreaseMenuEntry.Selected += ThicknessDecreaseMenuEntrySelected;



            backMenuEntry.Selected += OnCancel;
            
            // Add entries to the menu.
            MenuEntries.Add(gameTypeMenuEntry);
            MenuEntries.Add(materialMenuEntry);
            MenuEntries.Add(frictionFactorIncreaseMenuEntry);
            MenuEntries.Add(frictionFactorDecreaseMenuEntry);
            MenuEntries.Add(massIncreaseMenuEntry);
            MenuEntries.Add(massDecreaseMenuEntry);
            MenuEntries.Add(gravityIncreaseMenuEntry);
            MenuEntries.Add(gravityDecreaseMenuEntry);
            MenuEntries.Add(maxRotIncreaseMenuEntry);
            MenuEntries.Add(maxRotDecreaseMenuEntry);
            MenuEntries.Add(heightIncreaseMenuEntry);
            MenuEntries.Add(heightDecreaseMenuEntry);
            MenuEntries.Add(thicknessIncreaseMenuEntry);
            MenuEntries.Add(thicknessDecreaseMenuEntry);


            MenuEntries.Add(backMenuEntry);
        }


        /// <summary>
        /// Fills in the latest values for the options screen menu text.
        /// </summary>
        void SetMenuEntryText()
        {
            gameTypeMenuEntry.Text = "Game Type: " + gameTypes[currentGameType];
            materialMenuEntry.Text = "Table Material: " + materials[currentMaterial];
            frictionFactorIncreaseMenuEntry.Text = "frictionFactor + : " + GameplayScreen.frictionFactor;
            frictionFactorDecreaseMenuEntry.Text = "frictionFactor -";
            massIncreaseMenuEntry.Text = "Mass + : " + GameplayScreen.mass + " Kg";
            massDecreaseMenuEntry.Text = "Mass -";
            gravityIncreaseMenuEntry.Text = "Gravity + : " + GameplayScreen.gravity + " m/s^2";
            gravityDecreaseMenuEntry.Text = "Gravity -";
            maxRotIncreaseMenuEntry.Text = "Table Max Rotation Angle + : " + MathHelper.ToDegrees(GameplayScreen.maxRot) + " Degree";
            maxRotDecreaseMenuEntry.Text = "Table Max Rotation Angle -";
            heightIncreaseMenuEntry.Text = "Table Walls Height + : " + GameplayScreen.height + " Inch";
            heightDecreaseMenuEntry.Text = "Table Walls Height -";
            thicknessIncreaseMenuEntry.Text = "Table Walls Thikness + : " + GameplayScreen.thickness + " Inch";
            thicknessDecreaseMenuEntry.Text = "Table Walls Thikness -";

        }


        #endregion

        #region Handle Input

        void GameTypeMenuEntrySelected(object sender, PlayerIndexEventArgs e)
        {

            currentGameType = (currentGameType + 1) % gameTypes.Length;
            GameplayScreen.gameType = gameTypes[currentGameType];
            SetMenuEntryText();
        }

        void MaterialMenuEntrySelected(object sender, PlayerIndexEventArgs e)
        {

            currentMaterial = (currentMaterial + 1) % materials.Length;
            GameplayScreen.tableMaterial = materials[currentMaterial];
            if (materials[currentMaterial] == "Wood")
                GameplayScreen.frictionFactor = WoodFrictionFactor;
            else if (materials[currentMaterial] == "Moquette")
                GameplayScreen.frictionFactor = MoquetteFrictionFactor;
            else
            { 
                GameplayScreen.tableMaterial = "Wood";
                GameplayScreen.frictionFactor = OtherFrictionFactor;
            }
            SetMenuEntryText();
        }

        void FrictionFactorIncreaseMenuEntrySelected(object sender, PlayerIndexEventArgs e)
        {
            GameplayScreen.frictionFactor = (float)System.Math.Round(GameplayScreen.frictionFactor + 0.1f, 1);
            GameplayScreen.tableMaterial = materials[currentMaterial];
            if (GameplayScreen.frictionFactor == WoodFrictionFactor)
            {
                currentMaterial = 0;
                GameplayScreen.tableMaterial = materials[currentMaterial];
            }
            else if (GameplayScreen.frictionFactor == MoquetteFrictionFactor)
            {
                currentMaterial = 1;
                GameplayScreen.tableMaterial = materials[currentMaterial];
            }
            else
            {
                currentMaterial = 2;
                GameplayScreen.tableMaterial = "Wood";
            }
            SetMenuEntryText();
        }

        void FrictionFactorDecreaseMenuEntrySelected(object sender, PlayerIndexEventArgs e)
        {
            if (GameplayScreen.frictionFactor>=0.1f)
                GameplayScreen.frictionFactor = (float)System.Math.Round(GameplayScreen.frictionFactor - 0.1f,1);

            if (GameplayScreen.frictionFactor == WoodFrictionFactor)
            {
                currentMaterial = 0;
                GameplayScreen.tableMaterial = materials[currentMaterial];
            }
            else if (GameplayScreen.frictionFactor == MoquetteFrictionFactor)
            {
                currentMaterial = 1;
                GameplayScreen.tableMaterial = materials[currentMaterial];
            }
            else
            {
                currentMaterial = 2;
                GameplayScreen.tableMaterial = "Wood";
            }
            SetMenuEntryText();
        }

        void MassIncreaseMenuEntrySelected(object sender, PlayerIndexEventArgs e)
        {
            GameplayScreen.mass = (float)System.Math.Round(GameplayScreen.mass + 0.1f, 1);
            SetMenuEntryText();
        }

        void MassDecreaseMenuEntrySelected(object sender, PlayerIndexEventArgs e)
        {
            if (GameplayScreen.mass>=0.2f)
                GameplayScreen.mass = (float)System.Math.Round(GameplayScreen.mass - 0.1f, 1);
            SetMenuEntryText();
        }

        void GravityIncreaseMenuEntrySelected(object sender, PlayerIndexEventArgs e)
        {
            GameplayScreen.gravity = (float)System.Math.Round(GameplayScreen.gravity + 0.1f, 1);
            SetMenuEntryText();
        }

        void GravityDecreaseMenuEntrySelected(object sender, PlayerIndexEventArgs e)
        {
            if (GameplayScreen.gravity >= 0.1f)
                GameplayScreen.gravity = (float)System.Math.Round(GameplayScreen.gravity - 0.1f, 1);
            SetMenuEntryText();
        }

        void MaxRotIncreaseMenuEntrySelected(object sender, PlayerIndexEventArgs e)
        {
            if (MathHelper.ToDegrees(GameplayScreen.maxRot) <= 45)
                GameplayScreen.maxRot = GameplayScreen.maxRot + MathHelper.ToRadians(1);
            SetMenuEntryText();
        }

        void MaxRotDecreaseMenuEntrySelected(object sender, PlayerIndexEventArgs e)
        {
            if (MathHelper.ToDegrees(GameplayScreen.maxRot) >= 5)
                GameplayScreen.maxRot = GameplayScreen.maxRot - MathHelper.ToRadians(1);
            SetMenuEntryText();
        }

        void HeightIncreaseMenuEntrySelected(object sender, PlayerIndexEventArgs e)
        {
            GameplayScreen.height = (float)System.Math.Round(GameplayScreen.height + 0.1f, 1);
            SetMenuEntryText();
        }

        void HeightDecreaseMenuEntrySelected(object sender, PlayerIndexEventArgs e)
        {
            if (GameplayScreen.height >= 0.2f)
                GameplayScreen.height = (float)System.Math.Round(GameplayScreen.height - 0.1f, 1);
            SetMenuEntryText();
        }

        void ThicknessIncreaseMenuEntrySelected(object sender, PlayerIndexEventArgs e)
        {
            GameplayScreen.thickness = (float)System.Math.Round(GameplayScreen.thickness + 0.1f, 1);
            SetMenuEntryText();
        }

        void ThicknessDecreaseMenuEntrySelected(object sender, PlayerIndexEventArgs e)
        {
            if (GameplayScreen.thickness >= 0.2f)
                GameplayScreen.thickness = (float)System.Math.Round(GameplayScreen.thickness - 0.1f, 1);
            SetMenuEntryText();
        }







        #endregion
    }
}
