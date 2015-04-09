using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Net;
using Microsoft.Xna.Framework.Storage;

namespace XNADemo
{
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        SpriteFont spriteFont;

        enum COMPONENT
        {
            CAMERA = 0,
            SKYBOX = 1,
            MESH = 2
        }

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
        }

        protected override void Initialize()
        {
            base.Initialize();
        }

        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);
            //load the fonts
            spriteFont = Content.Load<SpriteFont>("Kootenay");

            //create and load the camera
            Camera camera = new Camera(this);
            camera.Initialize();
            Components.Insert((int)COMPONENT.CAMERA, camera);

            //create and load the skybox background
            SkyBox sb = new SkyBox(this);
            sb.content = this.Content;
            sb.camera = camera;
            sb.Initialize();
            Components.Insert((int)COMPONENT.SKYBOX, sb);

            //create and load the models
            Mesh mesh = new Mesh(this);
            mesh.content = this.Content;
            mesh.camera = camera;
            mesh.envMap = sb.currentEnvMap;
            mesh.Initialize();
            Components.Insert((int)COMPONENT.MESH, mesh);
        }

        protected override void UnloadContent()
        {
            Content.Unload();
        }

        protected override void Update(GameTime gameTime)
        {
            // Allows the game to exit
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
                this.Exit();

            //get the camera
            Camera camera = (Camera)this.Components[(int)COMPONENT.CAMERA];

            //update skybox
            SkyBox sb = (SkyBox)this.Components[(int)COMPONENT.SKYBOX];
            sb.camera = camera;

            //update models
            Mesh m = (Mesh)this.Components[(int)COMPONENT.MESH];
            m.camera = camera;
            m.envMap = sb.currentEnvMap;
            m.lightPos = sb.currentLightPos;

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            base.Draw(gameTime);

            Mesh m = (Mesh)this.Components[(int)COMPONENT.MESH];
            spriteBatch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Immediate, SaveStateMode.SaveState);
            string msg = "W - Walk Forward\n" +
                         "S - Walk backwards\n" +
                         "A - Strafe left\n" +
                         "D - Strafe right\n" +
                         "M - Change model\n" +
                         "B - Change background\n" +
                         "R - Change reflectness\n"+
                         "[F1 - F9] Rendering technique\n" +
                         "Current technique: " + m.currentTechnique;
            spriteBatch.DrawString(spriteFont, msg, new Vector2(1,1), Color.Silver);
            spriteBatch.End();
        }
    }
}
