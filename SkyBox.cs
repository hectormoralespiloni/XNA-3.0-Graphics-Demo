using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace XNADemo
{
    public class SkyBox : GameComponent, IDrawable
    {
        const int NUM_BG = 6;

        Effect effect;
        int currentBg;
        bool isChangeKeyPressed;
        Model model;
        TextureCube[] envMap;
        Vector3[] lightPosition;

        public ContentManager content { get; set; }
        public Camera camera { get; set; }
        public Vector3 currentLightPos 
        {
            get { return lightPosition[currentBg]; }
        }
        public TextureCube currentEnvMap 
        {
            get { return envMap[currentBg]; }
        }

        public SkyBox(Game g) : base(g) {
        }

        public override void Initialize()
        {
            base.Initialize();

            //Load the model
            model = content.Load<Model>("Models/SkyBox");

            //Load the textures
            envMap = new TextureCube[NUM_BG];
            for (int i = 0; i < NUM_BG; i++)
            {
                envMap[i] = content.Load<TextureCube>("Textures/skybox/skyboxmap"+i);
            }

            //Set light position per background
            lightPosition = new Vector3[NUM_BG];
            lightPosition[0] = new Vector3(100, 100, 0);
            lightPosition[1] = new Vector3(-100, 100, 100);
            lightPosition[2] = new Vector3(100, 100, -100);
            lightPosition[3] = new Vector3(100, 100, -100);
            lightPosition[4] = new Vector3(-100, 100, 100);
            lightPosition[5] = new Vector3(0, 100, -100);

            //Load the shader
            effect = content.Load<Effect>("Shaders/Skybox");

            isChangeKeyPressed = false;
            currentBg = 0;
        }

        public void Draw(GameTime gameTime)
        {
            IGraphicsDeviceService graphicsService = (IGraphicsDeviceService)Game.Services.GetService(typeof(IGraphicsDeviceService));
            GraphicsDevice device = graphicsService.GraphicsDevice;
            device.RenderState.DepthBufferWriteEnable = false;

            foreach (ModelMesh mesh in model.Meshes)
            {
                foreach (ModelMeshPart part in mesh.MeshParts)
                {
                    part.Effect = effect;

                    //make the skybox to move along with the camera.
                    Matrix world = camera.WorldMatrix * Matrix.CreateTranslation(camera.CameraPosition);
                    effect.Parameters["xWorld"].SetValue(world);
                    effect.Parameters["xView"].SetValue(camera.ViewMatrix);
                    effect.Parameters["xProjection"].SetValue(camera.ProjectionMatrix);
                    effect.Parameters["xBaseTexture"].SetValue(envMap[currentBg]);
                    effect.Parameters["xCameraPosition"].SetValue(camera.CameraPosition);
                }
                mesh.Draw();
            }

            device.RenderState.DepthBufferWriteEnable = true;
        }

        public override void Update(GameTime gameTime)
        {
            KeyboardState keyState = Keyboard.GetState();

            if (keyState.IsKeyDown(Keys.B))
            {
                ChangeBackground();
                //avoid changing model again while key is pressed
                isChangeKeyPressed = true;
            }
            if (keyState.IsKeyUp(Keys.B))
                isChangeKeyPressed = false;

            base.Update(gameTime);
        }

        void ChangeBackground()
        {
            if (isChangeKeyPressed)
                return;

            currentBg++;
            if (currentBg >= NUM_BG)
                currentBg = 0;
        }

        public int DrawOrder
        {
            get { return 0; }
        }

        public event System.EventHandler DrawOrderChanged;

        public bool Visible
        {
            get { return true; }
        }

        public event System.EventHandler VisibleChanged;
    }
}
