using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace XNADemo
{
    public class Mesh : GameComponent, IDrawable
    {
        const int NUM_MODELS = 5;
        const int NUM_TEXTURES = 3;
        const int NUM_BG = 4;
        const int NUM_KEYS = 2;

        List<Vector3> diffuseList;
        List<Texture2D> textureList;
        Vector3[][] diffuseArray = new Vector3[NUM_MODELS][];
        Texture2D[][] textureArray = new Texture2D[NUM_MODELS][];

        enum TEXTURE
        {
            CONTRAST = 0,
            NOISE = 1,
            PAPER = 2
        }

        enum KEYBOARD
        {
            M = 0,
            R = 1
        }

        Model[] model;
        Texture2D[] texture;
        Effect effect;
        int techniqueNum;
        int currentModel;
        float reflectness;
        bool[] isKeyPressed;

        public ContentManager content { get; set; }
        public Camera camera { get; set; }
        public string currentTechnique { get; set; }
        public TextureCube envMap { get; set; }
        public Vector3 lightPos { get; set; }

        public Mesh(Game game): base(game)
        {
        }

        public override void Initialize()
        {
            IGraphicsDeviceService graphicsService = (IGraphicsDeviceService)Game.Services.GetService(typeof(IGraphicsDeviceService));
            GraphicsDevice device = graphicsService.GraphicsDevice;

            //load the effects
            effect = content.Load<Effect>("Shaders/Rendering");

            //load the models
            model = new Model[NUM_MODELS];
            for(int i=0; i<NUM_MODELS; i++)
            {
                diffuseList = new List<Vector3>();
                textureList = new List<Texture2D>();
                model[i] = content.Load<Model>("Models/model" + i);
                
                foreach (ModelMesh mesh in model[i].Meshes)
                {
                    foreach (ModelMeshPart part in mesh.MeshParts)
                    {
                        BasicEffect be = (BasicEffect)part.Effect;
                        diffuseList.Add(be.DiffuseColor);
                        textureList.Add(be.Texture);
                    }
                }

                diffuseArray[i] = diffuseList.ToArray();
                textureArray[i] = textureList.ToArray();
            }
            isKeyPressed = new bool[NUM_KEYS];
            currentModel = 0;
            reflectness = 0.4f;

            //load the textures
            texture = new Texture2D[NUM_TEXTURES];
            texture[0] = content.Load<Texture2D>("Textures/charcoal/contrast");
            texture[1] = content.Load<Texture2D>("Textures/charcoal/noise");
            texture[2] = content.Load<Texture2D>("Textures/charcoal/paper");

            base.Initialize();
        }

        void IDrawable.Draw(GameTime gameTime)
        {
            int i = 0;
            foreach (ModelMesh mesh in model[currentModel].Meshes)
            {
                foreach (ModelMeshPart part in mesh.MeshParts)
                {
                    part.Effect = effect;

                    //set common parameters
                    effect.Parameters["xWorld"].SetValue(camera.WorldMatrix);
                    effect.Parameters["xView"].SetValue(camera.ViewMatrix);
                    effect.Parameters["xProjection"].SetValue(camera.ProjectionMatrix);
                    effect.Parameters["xLightPosition"].SetValue(lightPos);
                    effect.Parameters["xCameraPosition"].SetValue(camera.CameraPosition);
                    effect.Parameters["xLightColor"].SetValue(Color.White.ToVector3());
                    effect.Parameters["xDiffuseColor"].SetValue(diffuseArray[currentModel][i]);
                    effect.Parameters["xAmbientColor"].SetValue(new Vector3(0.1f,0.1f,0.1f));
                    effect.Parameters["xSpecularColor"].SetValue(Color.WhiteSmoke.ToVector3());
                    effect.Parameters["xTexture"].SetValue(textureArray[currentModel][i++]);
     
                    //select current technique
                    switch (techniqueNum)
                    {
                        case 1:
                            effect.CurrentTechnique = effect.Techniques["PhongLighting"];
                            break;
                        case 2:
                            effect.CurrentTechnique = effect.Techniques["Charcoal"];
                            effect.Parameters["noiseTexture"].SetValue(texture[(int)TEXTURE.NOISE]);
                            effect.Parameters["paperTexture"].SetValue(texture[(int)TEXTURE.PAPER]);
                            effect.Parameters["CET"].SetValue(texture[(int)TEXTURE.CONTRAST]);
                            effect.Parameters["ambient"].SetValue(0.3f);
                            break;
                        case 3:
                            effect.CurrentTechnique = effect.Techniques["XRay"];
                            break;
                        case 4:
                            effect.CurrentTechnique = effect.Techniques["ProceduralStripe"];
                            effect.Parameters["backColor"].SetValue(new Vector3(0.2f,0.2f,0.1f));
                            effect.Parameters["stripeColor"].SetValue(new Vector3(1,0.5f,0));
                            effect.Parameters["Kd"].SetValue(0.8f);
                            effect.Parameters["Fuzz"].SetValue(0.1f);
                            effect.Parameters["Width"].SetValue(0.5f);
                            break;
                        case 5:
                            effect.CurrentTechnique = effect.Techniques["ReflectionMapping"];
                            effect.Parameters["textureCube"].SetValue(envMap);
                            effect.Parameters["reflectness"].SetValue(reflectness);
                            break;
                        case 6:
                            effect.CurrentTechnique = effect.Techniques["RefractionMapping"];
                            effect.Parameters["textureCube"].SetValue(envMap);
                            break;
                        case 7:
                            effect.CurrentTechnique = effect.Techniques["ToonShading"];
                            break;
                        case 8:
                            effect.CurrentTechnique = effect.Techniques["Dimples"];
                            break; 
                        default:
                            effect.CurrentTechnique = effect.Techniques["PhongLighting"];
                            break;
                    }
                    currentTechnique = effect.CurrentTechnique.Name;
                }

                mesh.Draw();
            }            
        }

        public override void Update(GameTime gameTime)
        {
            KeyboardState keyState = Keyboard.GetState();

            if (keyState.IsKeyDown(Keys.F1))
                techniqueNum = 1;
            if (keyState.IsKeyDown(Keys.F2))
                techniqueNum = 2;
            if (keyState.IsKeyDown(Keys.F3))
                techniqueNum = 3;
            if (keyState.IsKeyDown(Keys.F4))
                techniqueNum = 4;
            if (keyState.IsKeyDown(Keys.F5))
                techniqueNum = 5;
            if (keyState.IsKeyDown(Keys.F6))
                techniqueNum = 6;
            if (keyState.IsKeyDown(Keys.F7))
                techniqueNum = 7;
            if (keyState.IsKeyDown(Keys.F8))
                techniqueNum = 8;
            if (keyState.IsKeyDown(Keys.F9))
                techniqueNum = 9;

            if (keyState.IsKeyDown(Keys.M))
            {
                ChangeModel();
                isKeyPressed[(int)KEYBOARD.M] = true;              
            }
            if (keyState.IsKeyUp(Keys.M))
                isKeyPressed[(int)KEYBOARD.M] = false;

            if (keyState.IsKeyDown(Keys.R))
            {
                ChangeReflectness();
                isKeyPressed[(int)KEYBOARD.R] = true;
            }
            if (keyState.IsKeyUp(Keys.R))
                isKeyPressed[(int)KEYBOARD.R] = false;

            base.Update(gameTime);
        }

        void ChangeReflectness()
        {
            if (isKeyPressed[(int)KEYBOARD.R])
                return;

            reflectness += 0.1f;
            if (reflectness > 1.0f)
                reflectness = 0.1f;
        }

        void ChangeModel()
        {
            if (isKeyPressed[(int)KEYBOARD.M])
                return;

            currentModel++;
            if (currentModel >= NUM_MODELS)
                currentModel = 0;
        }

        int IDrawable.DrawOrder
        {
            get { return 0; }
        }

        public event System.EventHandler DrawOrderChanged;

        bool IDrawable.Visible
        {
            get { return true; }
        }

        public event System.EventHandler VisibleChanged;
    }
}